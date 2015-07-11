//  Copyright: Portland State University 2009-2014
//  Authors:  Robert M. Scheller, Amin Almassian

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Landis.Core;

namespace  Landis.Library.Climate
{
    public class AnnualClimate_Monthly: AnnualClimate
    {
        public double[] MonthlyTemp = new double[12];
        public double[] MonthlyMinTemp = new double[12];
        public double[] MonthlyMaxTemp = new double[12];
        public double[] MonthlyPrecip = new double[12];
        public double[] MonthlyPAR = new double[12];
        public double[] MonthlyVarTemp = new double[12];
        public double[] MonthlyPptVarTemp = new double[12];
        public int tempEcoIndex = -1;

        public double[] MonthlyPET = new double[12];  // Potential Evapotranspiration
        public double[] MonthlyVPD = new double[12];  // Vapor Pressure Deficit
        public double[] MonthlyDayLength = new double[12];
        public double[] MonthlyNightLength = new double[12];
        public int[] MonthlyGDD = new int[12];

        public AnnualClimate_Monthly(IEcoregion ecoregion, int actualYear, double latitude, Climate.Phase spinupOrfuture = Climate.Phase.Future_Climate, int timeStep = Int32.MinValue) //For Hist and Random timeStep arg should be passed
        {
            this.climatePhase = spinupOrfuture;
            this.Latitude = latitude;

            // ------------------------------------------------------------------------------------------------------
            // Case:  Daily data used for future climate.  Note: No need to ever use daily data with spinup climate
            //if (Climate.AllData_granularity == TemporalGranularity.Daily && spinupOrfuture == Climate.Phase.Future_Climate)
            //{
            //    //Climate.ModelCore.UI.WriteLine("  Processing Daily data into Monthly data.  Ecoregion = {0}, Year = {1}, timestep = {2}.", ecoregion.Name, actualYear, timeStep);
            //    this.AnnualClimate_From_AnnualClimate_Daily(ecoregion, actualYear, latitude, spinupOrfuture, timeStep);
            //    return;
            //}

            IClimateRecord[,] timestepData = new IClimateRecord[Climate.ModelCore.Ecoregions.Count, 12]; 

            // ------------------------------------------------------------------------------------------------------
            // PossibleValues = "MonthlyRandom, MonthlyAverage, DailyHistRandom, DailyHistAverage, MonthlyStandard, DailyGCM";

            string climateOption = Climate.ConfigParameters.ClimateTimeSeries;
            if (this.climatePhase == Climate.Phase.SpinUp_Climate)
                climateOption = Climate.ConfigParameters.SpinUpClimateTimeSeries;

            switch (climateOption)
            {
                case "Monthly_AverageAllYears":
                    {
                        //if (this.climatePhase == Climate.Phase.Future_Climate) 
                        //else if (this.climatePhase == Climate.Phase.SpinUp_Climate) 
                        //    timestepData = AnnualClimate_Avg(ecoregion, actualYear, latitude); 
                        timestepData = AnnualClimate_AvgMonth(ecoregion, actualYear, latitude);
                        break;
                    }
                case "Monthly_AverageWithVariation":
                    {
                        timestepData = AnnualClimate_AvgMonth(ecoregion, actualYear, latitude);
                        CalculateMonthlyData_AddVariance(ecoregion, timestepData, actualYear, latitude);
                        break;
                    }
                case "Monthly_RandomYear":
                    {
                        TimeStep = timeStep;
                        try {
                            if (this.climatePhase == Climate.Phase.Future_Climate)
                                timestepData = Climate.Future_AllData[Climate.RandSelectedTimeSteps_future[TimeStep]];
                            else if (this.climatePhase == Climate.Phase.SpinUp_Climate)
                                timestepData = Climate.Spinup_AllData[Climate.RandSelectedTimeSteps_spinup[TimeStep]];

                            CalculateMonthlyData_NoVariance(ecoregion, timestepData, actualYear, latitude);
                        }
                        catch (System.Collections.Generic.KeyNotFoundException ex)
                        {
                            throw new ClimateDataOutOfRangeException("Exception: The requested Time-step is out of range for " + this.climatePhase.ToString() + " input file.", ex);
                        }
                        break;
                    }
                case "Monthly_SequencedYears":
                    {
                        TimeStep = timeStep;
                        try
                        {
                            if (this.climatePhase == Climate.Phase.Future_Climate)
                                timestepData = Climate.Future_AllData[TimeStep];
                            else if (this.climatePhase == Climate.Phase.SpinUp_Climate)
                                timestepData = Climate.Spinup_AllData[TimeStep];

                            CalculateMonthlyData_NoVariance(ecoregion, timestepData, actualYear, latitude);
                        }
                        catch (System.Collections.Generic.KeyNotFoundException ex)
                        {
                            throw new ClimateDataOutOfRangeException("Exception: The requested Time-step is out of range for " + this.climatePhase.ToString() + " input file.", ex);
                        }
                        break;
                    }
                case "Daily_RandomYear":
                    {
                        this.AnnualClimate_From_AnnualClimate_Daily(ecoregion, actualYear, latitude, spinupOrfuture, timeStep);
                        break;
                    }
                case "Daily_AverageAllYears":
                    {
                        this.AnnualClimate_From_AnnualClimate_Daily(ecoregion, actualYear, latitude, spinupOrfuture, timeStep);
                        return;
                    }
                //case "MonthlyStandard":  
                //    {
                //        TimeStep = timeStep;
                //        try
                //        {
                //            if (this.climatePhase == Climate.Phase.Future_Climate)
                //                timestepData = Climate.Future_AllData[TimeStep];
                //            else if (this.climatePhase == Climate.Phase.SpinUp_Climate)
                //                timestepData = Climate.Spinup_AllData[TimeStep];

                //            CalculateMonthlyData_AddVariance(ecoregion, timestepData, actualYear, latitude);
                //        }
                //        catch (System.Collections.Generic.KeyNotFoundException ex)
                //        {
                //            throw new ClimateDataOutOfRangeException("Exception: The requested Time-step is out of range for " + this.climatePhase.ToString() + " input file.", ex); 
                //        }
                //        break;
                //    }
                case "Daily_SequencedYears":
                    {
                        this.AnnualClimate_From_AnnualClimate_Daily(ecoregion, actualYear, latitude, spinupOrfuture, timeStep);
                        return;
                    }
                default:
                    throw new ApplicationException(String.Format("Unknown Climate Time Series: {}", climateOption));

            }

            this.MonthlyPET = CalculatePotentialEvapotranspiration(); 
            this.MonthlyVPD = CalculateVaporPressureDeficit();
            this.MonthlyGDD = CalculatePnETGDD(); 

            this.beginGrowing = CalculateBeginGrowingSeason(); 
            this.endGrowing = CalculateEndGrowingSeason(); 
            this.growingDegreeDays = GrowSeasonDegreeDays();

            for (int mo = 5; mo < 8; mo++)
                this.JJAtemperature += this.MonthlyTemp[mo];
            this.JJAtemperature /= 3.0;
            

        }

        // ------------------------------------------------------------------------------------------------------
        private void CalculateMonthlyData_AddVariance(IEcoregion ecoregion, IClimateRecord[,] timestepData, int actualYear, double latitude)
        {
            IClimateRecord[] ecoClimate = new IClimateRecord[12];

            this.Year = actualYear;
            this.AnnualPrecip = 0.0;

            //if(timestepData[ecoregion.Index].  ADD MONTH CHECK HERE.

            for (int mo = 0; mo < 12; mo++)
            {
                ecoClimate[mo] = timestepData[ecoregion.Index, mo]; //Climate.TimestepData[ecoregion.Index, mo];

                double MonthlyAvgTemp = (ecoClimate[mo].AvgMinTemp + ecoClimate[mo].AvgMaxTemp) / 2.0;

                double standardDeviation = ecoClimate[mo].StdDevTemp * (Climate.ModelCore.GenerateUniform() * 2.0 - 1.0);

                this.MonthlyTemp[mo] = MonthlyAvgTemp + standardDeviation;
                this.MonthlyMinTemp[mo] = ecoClimate[mo].AvgMinTemp + standardDeviation;
                this.MonthlyMaxTemp[mo] = ecoClimate[mo].AvgMaxTemp + standardDeviation;
                this.MonthlyPrecip[mo] = Math.Max(0.0, ecoClimate[mo].AvgPpt + (ecoClimate[mo].StdDevPpt * (Climate.ModelCore.GenerateUniform() * 2.0 - 1.0)));
                this.MonthlyPAR[mo] = ecoClimate[mo].PAR;

                this.AnnualPrecip += this.MonthlyPrecip[mo];

                if (this.MonthlyPrecip[mo] < 0)
                    this.MonthlyPrecip[mo] = 0;

                double hr = CalculateDayNightLength(mo, latitude);
                this.MonthlyDayLength[mo] = (60.0 * 60.0 * hr);                  // seconds of daylight/day
                this.MonthlyNightLength[mo] = (60.0 * 60.0 * (24 - hr));         // seconds of nighttime/day

                //this.DOY[mo] = DayOfYear(mo);
            }
        }

        private void CalculateMonthlyData_NoVariance(IEcoregion ecoregion, IClimateRecord[,] timestepData, int actualYear, double latitude)
        {
            IClimateRecord[] ecoClimate = new IClimateRecord[12];

            this.Year = actualYear;
            this.AnnualPrecip = 0.0;

            //if(timestepData[ecoregion.Index].  ADD MONTH CHECK HERE.


            for (int mo = 0; mo < 12; mo++)
            {
                //Climate.ModelCore.UI.WriteLine("  Calculating Monthly Climate (No Variance):  Yr={0}, month={1}, Eco={2}, Phase={3}.", actualYear, mo, ecoregion.Name, this.climatePhase);
                ecoClimate[mo] = timestepData[ecoregion.Index, mo]; 

                double MonthlyAvgTemp = (ecoClimate[mo].AvgMinTemp + ecoClimate[mo].AvgMaxTemp) / 2.0;

                //double standardDeviation = ecoClimate[mo].StdDevTemp * (Climate.ModelCore.GenerateUniform() * 2.0 - 1.0);

                this.MonthlyTemp[mo] = MonthlyAvgTemp;
                this.MonthlyMinTemp[mo] = ecoClimate[mo].AvgMinTemp;
                this.MonthlyMaxTemp[mo] = ecoClimate[mo].AvgMaxTemp;
                this.MonthlyPrecip[mo] = Math.Max(0.0, ecoClimate[mo].AvgPpt); 
                this.MonthlyPAR[mo] = ecoClimate[mo].PAR;

                this.AnnualPrecip += this.MonthlyPrecip[mo];

                if (this.MonthlyPrecip[mo] < 0)
                    this.MonthlyPrecip[mo] = 0;

                double hr = CalculateDayNightLength(mo, latitude);
                this.MonthlyDayLength[mo] = (60.0 * 60.0 * hr);                  // seconds of daylight/day
                this.MonthlyNightLength[mo] = (60.0 * 60.0 * (24 - hr));         // seconds of nighttime/day

            }
        }
        
        //Daily will not come to here. the average in daily is calculated in the AnnualClimate_Daily
        private IClimateRecord[,] AnnualClimate_AvgMonth(IEcoregion ecoregion, int year, double latitude)
        {

            IClimateRecord[,] timestepData = new IClimateRecord[Climate.ModelCore.Ecoregions.Count, 12];
            IClimateRecord[,] avgEcoClimate = new IClimateRecord[Climate.ModelCore.Ecoregions.Count, 12]; 
            //IClimateRecord[,] ecoClimateT = new IClimateRecord[Climate.ModelCore.Ecoregions.Count, 12];

            for (int i = 0; i < 12; i++)
            {
                this.MonthlyMinTemp[i] = 0.0;
                this.MonthlyMaxTemp[i] = 0.0;
                this.MonthlyVarTemp[i] = 0.0;
                this.MonthlyPptVarTemp[i] = 0.0;
                this.MonthlyPrecip[i] = 0.0;
                this.MonthlyPAR[i] = 0.0;

            }

            int allDataCount = 0;
            if (this.climatePhase == Climate.Phase.Future_Climate)
                allDataCount = Climate.Future_AllData.Count;
            else if (this.climatePhase == Climate.Phase.SpinUp_Climate)
                allDataCount = Climate.Spinup_AllData.Count;

            for (int mo = 0; mo < 12; mo++)
            {

                for (int stp = 0; stp < allDataCount; stp++)
                {

                    if (this.climatePhase == Climate.Phase.Future_Climate)
                        timestepData = Climate.Future_AllData.ElementAt(stp).Value;
                    else if (this.climatePhase == Climate.Phase.SpinUp_Climate)
                        timestepData = Climate.Spinup_AllData.ElementAt(stp).Value;

                    //Climate.ModelCore.UI.WriteLine("  Calculating annual average climate:  Yr={0}, month={1}, Eco={2}, Phase={3}.", Climate.Future_AllData.ElementAt(stp).Key, mo, ecoregion.Name, this.climatePhase);

                    this.MonthlyMinTemp[mo] += timestepData[ecoregion.Index, mo].AvgMinTemp;
                    this.MonthlyMaxTemp[mo] += timestepData[ecoregion.Index, mo].AvgMaxTemp;
                    this.MonthlyVarTemp[mo] += timestepData[ecoregion.Index, mo].AvgVarTemp;
                    this.MonthlyPptVarTemp[mo] += timestepData[ecoregion.Index, mo].AvgPptVarTemp;
                    this.MonthlyPrecip[mo] += timestepData[ecoregion.Index, mo].AvgPpt;
                    this.MonthlyPAR[mo] += timestepData[ecoregion.Index, mo].PAR;


                }
                this.MonthlyMinTemp[mo] = this.MonthlyMinTemp[mo] / allDataCount;
                this.MonthlyMaxTemp[mo] = this.MonthlyMaxTemp[mo] / allDataCount;
                this.MonthlyVarTemp[mo] = this.MonthlyVarTemp[mo] / allDataCount;
                this.MonthlyPptVarTemp[mo] = this.MonthlyPptVarTemp[mo] / allDataCount;
                this.MonthlyPrecip[mo] = this.MonthlyPrecip[mo] / allDataCount;
                this.MonthlyPAR[mo] = this.MonthlyPAR[mo] / allDataCount;
                avgEcoClimate[ecoregion.Index, mo] = new ClimateRecord();
                avgEcoClimate[ecoregion.Index, mo].AvgMinTemp = this.MonthlyMinTemp[mo];
                avgEcoClimate[ecoregion.Index, mo].AvgMaxTemp = this.MonthlyMaxTemp[mo];
                avgEcoClimate[ecoregion.Index, mo].AvgVarTemp = this.MonthlyVarTemp[mo];

                avgEcoClimate[ecoregion.Index, mo].StdDevTemp = Math.Sqrt(MonthlyVarTemp[mo]);

                avgEcoClimate[ecoregion.Index, mo].AvgPptVarTemp = this.MonthlyPptVarTemp[mo];
                avgEcoClimate[ecoregion.Index, mo].AvgPpt = this.MonthlyPrecip[mo];
                avgEcoClimate[ecoregion.Index, mo].StdDevPpt = Math.Sqrt(this.MonthlyPrecip[mo]);
                avgEcoClimate[ecoregion.Index, mo].PAR = this.MonthlyPAR[mo];

            }

            IClimateRecord[] ecoClimate = new IClimateRecord[12];
            this.Year = year;
            this.AnnualPrecip = 0.0;

            for (int mo = 0; mo < 12; mo++)
            {
                ecoClimate[mo] = timestepData[ecoregion.Index, mo]; 

                double MonthlyAvgTemp = (ecoClimate[mo].AvgMinTemp + ecoClimate[mo].AvgMaxTemp) / 2.0;

                //double standardDeviation = ecoClimate[mo].StdDevTemp * (Climate.ModelCore.GenerateUniform() * 2.0 - 1.0);

                this.MonthlyTemp[mo] = MonthlyAvgTemp;// + standardDeviation;
                this.MonthlyMinTemp[mo] = ecoClimate[mo].AvgMinTemp; // + standardDeviation;
                this.MonthlyMaxTemp[mo] = ecoClimate[mo].AvgMaxTemp; // + standardDeviation;


                this.MonthlyPrecip[mo] = Math.Max(0.0, ecoClimate[mo].AvgPpt); // + (ecoClimate[mo].StdDevPpt * (Climate.ModelCore.GenerateUniform() * 2.0 - 1.0)));
                this.MonthlyPAR[mo] = ecoClimate[mo].PAR;

                this.AnnualPrecip += this.MonthlyPrecip[mo];

                if (this.MonthlyPrecip[mo] < 0)
                    this.MonthlyPrecip[mo] = 0;

                double hr = CalculateDayNightLength(mo, latitude);
                this.MonthlyDayLength[mo] = (60.0 * 60.0 * hr);                  // seconds of daylight/day
                this.MonthlyNightLength[mo] = (60.0 * 60.0 * (24 - hr));         // seconds of nighttime/day

                //this.DOY[mo] = DayOfYear(mo);
            }


            
            //Climate.ModelCore.UI.WriteLine("  Completed calculations for {0} from AVERAGE MONTHLY data... Ecoregion = {1}, Year = {2}, BeginGrow = {3}.", this.climatePhase, ecoregion.Name, year, this.beginGrowing);

            return avgEcoClimate;

        }

        //private void AnnualClimate_Base(IEcoregion ecoregion, int year, double latitude)
        //{
        //    //Climate.ModelCore.Log.WriteLine("  Generate new annual climate:  Yr={0}, Eco={1}.", year, ecoregion.Name);
        //    Ecoregion = ecoregion;
        //    IClimateRecord[] ecoClimate = new IClimateRecord[12];

        //    this.Year = year;
        //    this.AnnualPrecip = 0.0;
        //    //this.AnnualN = 0.0;
        //    this.Latitude = latitude;

        //    for (int mo = 0; mo < 12; mo++)
        //    {
        //        ecoClimate[mo] = Climate.TimestepData[ecoregion.Index, mo];

        //        double MonthlyAvgTemp = (ecoClimate[mo].AvgMinTemp + ecoClimate[mo].AvgMaxTemp) / 2.0;

        //        double standardDeviation = ecoClimate[mo].StdDevTemp * (Climate.ModelCore.GenerateUniform() * 2.0 - 1.0);

        //        this.MonthlyTemp[mo] = MonthlyAvgTemp + standardDeviation;
        //        this.MonthlyMinTemp[mo] = ecoClimate[mo].AvgMinTemp + standardDeviation;
        //        this.MonthlyMaxTemp[mo] = ecoClimate[mo].AvgMaxTemp + standardDeviation;
        //        this.MonthlyPrecip[mo] = Math.Max(0.0, ecoClimate[mo].AvgPpt + (ecoClimate[mo].StdDevPpt * (Climate.ModelCore.GenerateUniform() * 2.0 - 1.0)));
        //        this.MonthlyPAR[mo] = ecoClimate[mo].PAR;

        //        this.AnnualPrecip += this.MonthlyPrecip[mo];

        //        if (this.MonthlyPrecip[mo] < 0)
        //            this.MonthlyPrecip[mo] = 0;

        //        double hr = CalculateDayNightLength(mo, latitude);
        //        this.MonthlyDayLength[mo] = (60.0 * 60.0 * hr);                  // seconds of daylight/day
        //        this.MonthlyNightLength[mo] = (60.0 * 60.0 * (24 - hr));         // seconds of nighttime/day

        //        //this.DOY[mo] = DayOfYear(mo);
        //    }


        //    this.MonthlyPET = CalculatePotentialEvapotranspiration(); //ecoClimate);
        //    this.MonthlyVPD = CalculateVaporPressureDeficit(); //ecoClimate);
        //    this.MonthlyGDD = CalculatePnETGDD(); //this.MonthlyTemp, year);

        //    this.beginGrowing = CalculateBeginGrowingSeason(); //ecoClimate);
        //    this.endGrowing = CalculateEndGrowingSeason(); //ecoClimate);
        //    this.growingDegreeDays = GrowSeasonDegreeDays(); //year);

        //    for (int mo = 5; mo < 8; mo++)
        //        this.JJAtemperature += this.MonthlyTemp[mo];
        //    this.JJAtemperature /= 3.0;


        //}
        private void AnnualClimate_From_AnnualClimate_Daily(IEcoregion ecoregion,  int actualYear, double latitude, Climate.Phase spinupOrfuture,  int timeStep)
        {

            //Climate.ModelCore.UI.WriteLine("  Retrieve Daily data... Ecoregion = {0}, Year = {1}.", ecoregion.Name, actualYear);
            //timestepData = Climate.Future_AllData[Climate.RandSelectedTimeSteps_future[TimeStep]];
            
            int nDays;
            int dayOfYear = 0;
            AnnualClimate_Daily annDaily = new AnnualClimate_Daily(ecoregion, actualYear, latitude, spinupOrfuture, timeStep); //for the same timeStep
            
            if (spinupOrfuture == Climate.Phase.Future_Climate)
                Climate.Future_DailyData[actualYear][ecoregion.Index] = annDaily;
            else
                Climate.Spinup_DailyData[actualYear][ecoregion.Index] = annDaily;  

            //IClimateRecord[] ecoClimate = new IClimateRecord[12];

            //----------------------------------------
            // Calculate precipitation and temperature 
            for (int mo = 0; mo < 12; mo++)
            {
                //ecoClimate[mo] = Climate.TimestepData[ecoregion.Index, mo];
                
                nDays = DaysInMonth(mo, actualYear);
                for (int d=1; d <= nDays; d++)
                {
                    

                    this.MonthlyTemp[mo]+= annDaily.DailyTemp[dayOfYear];
                    this.MonthlyMinTemp[mo] += annDaily.DailyMinTemp[dayOfYear];
                    this.MonthlyMaxTemp[mo] += annDaily.DailyMaxTemp[dayOfYear];
                    this.MonthlyPrecip[mo] += annDaily.DailyPrecip[dayOfYear];
                    this.MonthlyPAR[mo] += annDaily.DailyPAR[dayOfYear];
                    this.MonthlyVarTemp[mo] += annDaily.DailyVarTemp[dayOfYear];
                    this.MonthlyPptVarTemp[mo] += annDaily.DailyPptVarTemp[dayOfYear];

                    dayOfYear++;
                    //dayOfYear += nDays;
                }


                this.MonthlyTemp[mo] /= nDays;
                this.MonthlyMinTemp[mo] /= nDays;
                this.MonthlyMaxTemp[mo] /= nDays;
                //MonthlyPrecip[mo] /= nDays;
                this.MonthlyPAR[mo] /= nDays;
                this.MonthlyVarTemp[mo] /= nDays;
                this.MonthlyPptVarTemp[mo] /= nDays;
            }

            //------------------------------------------------------------
            // Calculate monthly data derived from precipitation and temperature

            this.Year = actualYear;
            this.AnnualPrecip = 0.0;
            for (int mo = 0; mo < 12; mo++)
            {
                //ecoClimate[mo] = Climate.TimestepData[ecoregion.Index, mo];

                this.AnnualPrecip += this.MonthlyPrecip[mo];

                if (this.MonthlyPrecip[mo] < 0)
                    throw new System.ApplicationException(String.Format("Error: Precipitation < 0.  Year={0}, Month={1}, Ppt={2}", this.Year, mo, this.MonthlyPrecip[mo]));

                double hr = CalculateDayNightLength(mo, latitude);
                this.MonthlyDayLength[mo] = (60.0 * 60.0 * hr);                  // seconds of daylight/day
                this.MonthlyNightLength[mo] = (60.0 * 60.0 * (24 - hr));         // seconds of nighttime/day

            }


            this.MonthlyPET = CalculatePotentialEvapotranspiration();
            this.MonthlyVPD = CalculateVaporPressureDeficit(); 
            this.MonthlyGDD = CalculatePnETGDD(); 

            
            this.beginGrowing = annDaily.BeginGrowing; 
            this.endGrowing = annDaily.EndGrowing; 
            this.growingDegreeDays = annDaily.GrowingDegreeDays; 


            for (int mo = 5; mo < 8; mo++)
                this.JJAtemperature += this.MonthlyTemp[mo];
            this.JJAtemperature /= 3.0;

            //Climate.ModelCore.UI.WriteLine("  Completed calculations from daily data... Ecoregion = {0}, Year = {1}, BeginGrow = {2}.", ecoregion.Name, actualYear, this.beginGrowing);
            
        }


        //---------------------------------------------------------------------------
        // Calc growing season degree days (Degree_Day) based on monthly temperatures
        // normally distributed around a specified mean with a specified standard
        // deviation.
        public int GrowSeasonDegreeDays()//int currentYear)
        {
            //degDayBase is temperature (C) above which degree days (Degree_Day)
            //are counted
            double degDayBase = 5.56;      // 42F.
            double Deg_Days = 0.0;

            //Calc monthly temperatures (mean +/- normally distributed
            //random number times standard deviation) and
            //sum degree days for consecutve months.
            for (int month = 0; month < 12; month++) //12 months in year
            {
                if (this.MonthlyTemp[month] > degDayBase)
                    Deg_Days += (MonthlyTemp[month] - degDayBase) * DaysInMonth(month, this.Year);
            }

            this.growingDegreeDays = (int)Deg_Days;
            return (int) Deg_Days;
        }


        //---------------------------------------------------------------------------
        private int[] CalculatePnETGDD() //double[] monthlyTemp, int currentYear)
        {
            //************************************************
            //  Heat Sum Routine
            //**********************

            int[] MonthlyGDD = new int[12];

            for (int month = 0; month < 12; month++) //12 months in year
            {
                //double GDD = monthlyTemp[i] * DaysInMonth(i, currentYear);
                double GDD = this.MonthlyTemp[month] * DaysInMonth(month, this.Year); //currentYear);
                if (GDD < 0)
                    GDD = 0;
                MonthlyGDD[month] = (int)GDD;
                //GDDTot = GDDTot + GDD;
            }

            return MonthlyGDD;
        }


        //---------------------------------------------------------------------------
        // Calculate Begin Growing Degree Day (Last Frost; Minimum = 0 degrees C):
        // This method assumes you do not have daily data, which would be far more accurate.
        private int CalculateBeginGrowingSeason() 
        {
             //Climate.ModelCore.UI.WriteLine("  Calculating monthly growing season....");


            double lastMonthMinTemp = this.MonthlyMinTemp[11]; // yearClimate[0].AvgMinTemp;
            int dayCnt = 15;  //the middle of February
            int beginGrowingSeason = 0;

            for (int month = 0; month < 5; month++)  //Begin looking in February (1).  Should be safe for at least 100 years.
            {

                int totalDays = (DaysInMonth(month, this.Year) + DaysInMonth(month - 1, 3)) / 2;
                double MonthlyMinTemp = this.MonthlyMinTemp[month]; // yearClimate[i].AvgMinTemp;// + (monthlyTempSD[i] * randVar.GenerateNumber());

                //Now interpolate between days:
                double degreeIncrement = System.Math.Abs(MonthlyMinTemp - lastMonthMinTemp) / (double)totalDays;
                double Tnight = MonthlyMinTemp;  //start from warmer month
                double TnightRandom = Tnight + (this.MonthlyVarTemp[month] * (Climate.ModelCore.GenerateUniform() * 2 - 1));

                for (int day = 1; day <= totalDays; day++)
                {
                    if (TnightRandom <= 0)
                        beginGrowingSeason = (dayCnt + day);
                    Tnight += degreeIncrement;  //work backwards to find last frost day.
                    TnightRandom = Tnight + (this.MonthlyVarTemp[month] * (Climate.ModelCore.GenerateUniform() * 2 - 1));
                }

                lastMonthMinTemp = MonthlyMinTemp;
                dayCnt += totalDays;  //new monthly mid-point
            }
            
            //this.beginGrowing = beginGrowingSeason;
            return beginGrowingSeason;
        }

        //---------------------------------------------------------------------------
        // Calculate End Growing Degree Day (First frost; Minimum = 0 degrees C):
        // This method assumes you do not have daily data, which would be far more accurate.
        private int CalculateEndGrowingSeason() //IClimateRecord[] annualClimate)//, Random autoRand)
        {

            //Defaults for the middle of July:
            double lastMonthTemp = this.MonthlyMinTemp[6]; // annualClimate[6].AvgMinTemp;
            int dayCnt = 198;
            //int endGrowingSeason = 198;

            for (int month = 7; month < 12; month++)  //Begin looking in August.  Should be safe for at least 100 years.
            {
                int totalDays = (DaysInMonth(month, this.Year) + DaysInMonth(month - 1, this.Year)) / 2;
                double MonthlyMinTemp = this.MonthlyMinTemp[month]; // annualClimate[i].AvgMinTemp;

                //Now interpolate between days:
                double degreeIncrement = System.Math.Abs(lastMonthTemp - MonthlyMinTemp) / (double)totalDays;
                double Tnight = lastMonthTemp;  //start from warmer month

                double TnightRandom = Tnight + (this.MonthlyVarTemp[month] * (Climate.ModelCore.GenerateUniform() * 2 - 1));

                for (int day = 1; day <= totalDays; day++)
                {
                    if (TnightRandom <= 0)
                        return (dayCnt + day);
                    Tnight -= degreeIncrement;  //work forwards to find first frost day.
                    TnightRandom = Tnight + (this.MonthlyVarTemp[month] * (Climate.ModelCore.GenerateUniform() * 2 - 1));
                    //Climate.ModelCore.UI.WriteLine("Tnight = {0}.", TnightRandom);
                }

                lastMonthTemp = MonthlyMinTemp;
                dayCnt += totalDays;  //new monthly mid-point
            }
            return 365;
        }


        //---------------------------------------------------------------------------
        private double[] CalculateVaporPressureDeficit()//IClimateRecord[] annualClimate)
        {
            // From PnET:
            // Estimation of saturated vapor pressure from daily average temperature.
            // Calculates saturated vp and delta from temperature, from Murray J Applied Meteorol 6:203
            //   Tday    average air temperature, degC
            //   ES  saturated vapor pressure at Tday, kPa
            //   DELTA dES/dTA at TA, kPa/K which is the slope of the sat. vapor pressure curve
            //   Saturation equations are from:
            //       Murry, (1967). Journal of Applied Meteorology. 6:203.
            double[] monthlyVPD = new double[12];

            for (int month = 0; month < 12; month++)
            {
                double Tmin = this.MonthlyMinTemp[month]; // annualClimate[month].AvgMinTemp;
                double Tday = this.MonthlyTemp[month]; // (annualClimate[month].AvgMinTemp + annualClimate[month].AvgMaxTemp) / 2.0;

                double es = 0.61078 * Math.Exp(17.26939 * Tday / (Tday + 237.3)); //kPa
                if (Tday < 0)
                {
                    es = 0.61078 * Math.Exp(21.87456 * Tday / (Tday + 265.5)); //kPa
                }

                //Calculation of mean daily vapor pressure from minimum daily temperature.

                //   Tmin = minimum daily air temperature                  //degrees C
                //   emean = mean daily vapor pressure                     //kPa
                //   Vapor pressure equations are from:
                //       Murray (1967). Journal of Applied Meteorology. 6:203.

                double emean = 0.61078 * Math.Exp(17.26939 * Tmin / (Tmin + 237.3)); //kPa

                if (Tmin < 0)
                    emean = 0.61078 * Math.Exp(21.87456 * Tmin / (Tmin + 265.5));

                double VPD = es - emean;
                monthlyVPD[month] = VPD;
            }

            return monthlyVPD;
        }
        //---------------------------------------------------------------------------
        private double[] CalculatePotentialEvapotranspiration()//IClimateRecord[] annualClimate)
        {
            //Calculate potential evapotranspiration (pevap)
            //...Originally from pevap.f
            // FWLOSS(4) - Scaling factor for potential evapotranspiration (pevap).
            double waterLossFactor4 = 0.9;  //from Century v4.5


            double elev = 1.0;       
            double sitlat = this.Latitude; 

            double highest = -40.0;
            double lowest = 100.0;

            for (int i = 0; i < 12; i++)
            {
                double avgTemp = this.MonthlyTemp[i]; // (annualClimate[i].AvgMinTemp + annualClimate[i].AvgMaxTemp) / 2.0;
                highest = System.Math.Max(highest, avgTemp);
                lowest = System.Math.Min(lowest, avgTemp);
            }

            lowest = System.Math.Max(lowest, -10.0);

            //...Determine average temperature range
            double avgTempRange = System.Math.Abs(highest - lowest);

            double[] monthlyPET = new double[12];


            for (int month = 0; month < 12; month++)
            {

                //...Temperature range calculation
                //double tr = annualClimate[month].AvgMaxTemp - System.Math.Max(-10.0, annualClimate[month].AvgMinTemp);
                double tr = this.MonthlyMaxTemp[month] - System.Math.Max(-10.0, this.MonthlyMinTemp[month]);

                //double t = tr / 2.0 + annualClimate[month].AvgMinTemp;
                double t = tr / 2.0 + this.MonthlyMinTemp[month];
                double tm = t + 0.006 * elev;
                double td = (0.0023 * elev) + (0.37 * t) + (0.53 * tr) + (0.35 * avgTempRange) - 10.9;
                double e = ((700.0 * tm / (100.0 - System.Math.Abs(sitlat))) + 15.0 * td) / (80.0 - t);
                double monthPET = (e * 30.0) / 10.0;

                //if (monthPET < 0.05)
                //    monthPET = 0.05;

                //...fwloss(4) is a modifier for PET loss.   vek may90
                monthlyPET[month] = monthPET * waterLossFactor4;
                //Climate.ModelCore.UI.WriteLine("Year={0}, Month={1}, PET={2:0.00}.", this.Year, month, monthlyPET[month]);

            }

            return monthlyPET;
        }


        //---------------------------------------------------------------------------
        public static double CalculateAnnualActualEvapotranspiration(AnnualClimate_Monthly annualClimate, double fieldCapacity)
        {
            // field capacity input as cm
            // variable with xVariableName indicate conversion to mm

            double xFieldCap = fieldCapacity * 10.0;

            double waterAvail = 0.0;
            double actualET = 0.0;
            double oldWaterAvail = 0.0;
            double accPotWaterLoss = 0.0;

            for (int month = 0; month < 12; month++)
            {

                double monthlyRain = annualClimate.MonthlyPrecip[month];
                double potentialET = annualClimate.MonthlyPET[month];


                //Calc potential water loss this month
                double potWaterLoss = monthlyRain - potentialET;

                //If monthlyRain doesn't satisfy potentialET, add this month's potential
                //water loss to accumulated water loss from soil
                if (potWaterLoss < 0.0)
                {
                    accPotWaterLoss += potWaterLoss;
                    double xAccPotWaterLoss = accPotWaterLoss * 10.0;

                    //Calc water retained in soil given so much accumulated potential
                    //water loss Pastor and Post. 1984.  Can. J. For. Res. 14:466:467.

                    waterAvail = fieldCapacity *
                                 System.Math.Exp((.000461 - 1.10559 / xFieldCap) * (-1.0 * xAccPotWaterLoss));

                    if (waterAvail < 0.0)
                        waterAvail = 0.0;

                    //changeSoilMoisture - during this month
                    double changeSoilMoisture = waterAvail - oldWaterAvail;

                    //Calc actual evapotranspiration (AET) if soil water is drawn down
                    actualET += (monthlyRain - changeSoilMoisture);
                }

                //If monthlyRain satisfies potentialET, don't draw down soil water
                else
                {
                    waterAvail = oldWaterAvail + potWaterLoss;
                    if (waterAvail >= fieldCapacity)
                        waterAvail = fieldCapacity;

                    double changeSoilMoisture = waterAvail - oldWaterAvail;

                    //If soil partially recharged, reduce accumulated potential
                    //water loss accordingly
                    accPotWaterLoss += changeSoilMoisture;

                    //If soil completely recharged, reset accumulated potential
                    //water loss to zero
                    if (waterAvail >= fieldCapacity)
                        accPotWaterLoss = 0.0;

                    //If soil water is not drawn upon, add potentialET to AET
                    actualET += potentialET;
                }

                oldWaterAvail = waterAvail;
            }

            return actualET;
        }

        //---------------------------------------------------------------------------
        public double MeanAnnualTemp(int currentYear)
        {
            double MAT = 0.0;
            //Calc monthly temperatures (mean +/- normally distributed
            //random number times  standard deviation) and
            //sum degree days for consecutve months.
            for (int i = 0; i < 12; i++) //12 months in year
            {
                int daysInMonth = DaysInMonth(i, currentYear);
                MAT += daysInMonth * MonthlyTemp[i];
            }

            if (currentYear % 4 == 0)
                MAT /= 366.0;
            else
                MAT /= 365.0;

            return MAT;
        }

        //---------------------------------------------------------------------------
        public double TotalAnnualPrecip()
        {
            //Main loop for yearly water balance calculation by month   */
            double TAP = 0.0;
            for (int i = 0; i < 12; i++)
            {
                TAP += MonthlyPrecip[i];
            }
            return TAP;
        }

        ////---------------------------------------------------------------------------
        public void WriteToLandisLogFile()
        {
            Climate.ModelCore.UI.WriteLine("  ClimatePhase = {0}, Year = {1}, MAP = {2:0.00}.", this.climatePhase, this.Year, this.AnnualPrecip);

            //(IEcoregion ecoregion, int actualYear, double latitude, ClimatePhase spinupOrfuture = Climate.Phase.Future_Climate, int timeStep = Int32.MinValue)
        }

    }
}
