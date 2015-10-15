//  Copyright: Portland State University 2009-2014
//  Authors:  Robert M. Scheller, John McNabb and Amin Almassian

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
        public double[] MonthlyVarPpt = new double[12];
        public double[] MonthlyWindDirection = new double[12];
        public double[] MonthlyWindSpeed = new double[12];
        public double[] MonthlyNDeposition = new double[12];
        public int tempEcoIndex = -1;
        public double[] MonthlyPET = new double[12];  // Potential Evapotranspiration
        public double[] MonthlyVPD = new double[12];  // Vapor Pressure Deficit
        public double[] MonthlyDayLength = new double[12];
        public double[] MonthlyNightLength = new double[12];
        public int[] MonthlyGDD = new int[12];

        public AnnualClimate_Monthly(IEcoregion ecoregion, double latitude, Climate.Phase spinupOrfuture, int timeStep, int timeStepIndex)
        {
            this.climatePhase = spinupOrfuture;
            this.Latitude = latitude;

            // ------------------------------------------------------------------------------------------------------
            // Case:  Daily data used for future climate.  Note: No need to ever use daily data with spinup climate
            //if (Climate.AllData_granularity == TemporalGranularity.Daily && spinupOrfuture == Climate.Phase.Future_Climate)
            //{
            //    //Climate.ModelCore.UI.WriteLine("  Processing Daily data into Monthly data.  Ecoregion = {0}, Year = {1}, timestep = {2}.", ecoregion.Name, monthlyDataKey, timeStep);
            //    this.AnnualClimate_From_AnnualClimate_Daily(ecoregion, monthlyDataKey, latitude, spinupOrfuture, timeStep);
            //    return;
            //}

            ClimateRecord[][] timestepData = new ClimateRecord[Climate.ModelCore.Ecoregions.Count][];
            for (var i = 0; i < Climate.ModelCore.Ecoregions.Count; ++i)
                timestepData[i]= new ClimateRecord[12];

            // ------------------------------------------------------------------------------------------------------
            
            string climateOption = Climate.ConfigParameters.ClimateTimeSeries;
            if (this.climatePhase == Climate.Phase.SpinUp_Climate)
                climateOption = Climate.ConfigParameters.SpinUpClimateTimeSeries;

            ClimateRecord[] monthlyData;

            int actualTimeStep;

            switch (climateOption)
            {
                case "Monthly_AverageAllYears":
                    {
                        TimeStep = timeStep;
                        actualTimeStep = 0;
                        monthlyData = AnnualClimate_AvgMonth(ecoregion, latitude);
                        CalculateMonthlyData(ecoregion, monthlyData, actualTimeStep, latitude);
                        //Climate.ModelCore.UI.WriteLine("  Completed calculations for {0} using AVERAGE MONTHLY data. Ecoregion = {1}, SimulatedYear = AVERAGED.", this.climatePhase, ecoregion.Name, actualTimeStep);
                        break;
                    }

                case "Monthly_AverageWithVariation": //this case is not working as of 5/15/14
                    {
                        TimeStep = timeStep;
                        actualTimeStep = 0;
                        monthlyData = AnnualClimate_AvgMonth(ecoregion, latitude);
                        //Climate.ModelCore.UI.WriteLine("  Completed calculations for {0} from AVERAGE MONTHLY data. Ecoregion = {1}, SimulatedYear = AVERAGED.", this.climatePhase, ecoregion.Name, actualTimeStep);
                        //timestepData = AnnualClimate_AvgMonth(ecoregion, monthlyDataKey, latitude);
                        //CalculateMonthlyData_AddVariance(ecoregion, monthlyData, actualTimeStep, latitude);
                        break;
                    }
                case "Monthly_RandomYears":
                    {
                        TimeStep = timeStep;
                        Dictionary<int, ClimateRecord[][]> allData;
                        List<int> randomKeyList;

                        if (this.climatePhase == Climate.Phase.Future_Climate)
                        {
                            allData = Climate.Future_AllData;
                            randomKeyList = Climate.RandSelectedTimeKeys_future;
                        }
                        else
                        {
                            allData = Climate.Spinup_AllData;
                            randomKeyList = Climate.RandSelectedTimeKeys_spinup;
                        }

                        if (timeStepIndex >= randomKeyList.Count())
                        {
                            throw new ApplicationException(string.Format("Exception: the requested Time-step {0} is out-of-range for the {1} input file.", timeStep, this.climatePhase));
                        }
                        else
                            actualTimeStep = randomKeyList[timeStepIndex];

                        //Climate.ModelCore.UI.WriteLine("  AnnualClimate_Monthly: Monthly_RandomYear: timeStep = {0}, actualYear = {1}, phase = {2}.", timeStep, actualTimeStep, this.climatePhase);
                        //Climate.ModelCore.UI.WriteLine("  Completed calculations for FutureData using AnnualClimate_Monthly: SimulatedYear = {0}, actualYearSelected = {1}.", timeStep, actualTimeStep);
                        //Climate.ModelCore.UI.WriteLine("  Completed calculations for {0} using RandomYear_Monthly. Ecoregion = {1}, SimulatedYear = {2}, actualYearUsed={3}.", this.climatePhase, ecoregion.Name, timeStep, actualTimeStep);

                        monthlyData = allData[actualTimeStep][ecoregion.Index];
                        CalculateMonthlyData(ecoregion, monthlyData, actualTimeStep, latitude);
                        break;

                        
                    }
                case "Monthly_SequencedYears":
                    {
                        TimeStep = timeStep;
                        actualTimeStep = timeStep;
                        Dictionary<int, ClimateRecord[][]> allData;

                        if (this.climatePhase == Climate.Phase.Future_Climate)
                            allData = Climate.Future_AllData;
                        else
                            allData = Climate.Spinup_AllData;

                        ClimateRecord[][] yearRecords;

                        // get the climate records for the requested year, or if the year is not found, get the records for the last year
                        if (!allData.TryGetValue(timeStep, out yearRecords))
                        {
                            actualTimeStep = allData.Keys.Max();
                            yearRecords = allData[actualTimeStep];
                        }

                        monthlyData = yearRecords[ecoregion.Index];
                        CalculateMonthlyData(ecoregion, monthlyData, actualTimeStep, latitude);

                        //Climate.ModelCore.UI.WriteLine("  Completed calculations for {0} using Monthly_SequencedYears. Ecoregion = {1}, SimulatedYear = {2}, actualYearUsed={3}.", this.climatePhase, ecoregion.Name, timeStep, actualTimeStep);
                        break;
                    }
                case "Daily_RandomYears":
                    {
                        TimeStep = timeStep;
                        Dictionary<int, ClimateRecord[][]> allData;
                        List<int> randomKeyList;

                        if (this.climatePhase == Climate.Phase.Future_Climate)
                        {
                            allData = Climate.Future_AllData;
                            randomKeyList = Climate.RandSelectedTimeKeys_future;
                        }
                        else
                        {
                            allData = Climate.Spinup_AllData;
                            randomKeyList = Climate.RandSelectedTimeKeys_spinup;
                        }

                        if (timeStepIndex >= randomKeyList.Count())
                        {
                            throw new ApplicationException(string.Format("Exception: the requested Time-step {0} is out-of-range for the {1} input file.", timeStep, this.climatePhase));
                        }
                        else
                            actualTimeStep = randomKeyList[timeStepIndex];

                        //Climate.ModelCore.UI.WriteLine("  Completed calculations for {0} using Daily_RandomYear. Ecoregion = {1}, SimulatedYear = {2}, actualYearUsed={3}.", this.climatePhase, ecoregion.Name, timeStep, actualTimeStep);


                        monthlyData = AnnualClimate_From_AnnualClimate_Daily(ecoregion, latitude, spinupOrfuture, timeStep, timeStepIndex);
                        CalculateMonthlyData(ecoregion, monthlyData, actualTimeStep, latitude);
                        break;
                                                
                    }
                case "Daily_AverageAllYears":
                    {
                        TimeStep = timeStep;
                        actualTimeStep = 0;
                        monthlyData = AnnualClimate_From_AnnualClimate_Daily(ecoregion, latitude, spinupOrfuture, timeStep, timeStepIndex);
                        CalculateMonthlyData(ecoregion, monthlyData, actualTimeStep, latitude);
                        //Climate.ModelCore.UI.WriteLine("  Completed calculations for {0} using Daily_AverageAllYears. Ecoregion = {1}, SimulatedYear = {2}.", this.climatePhase, ecoregion.Name, timeStep);
                        break;
                    }
                case "Daily_SequencedYears":
                    {
                        TimeStep = timeStep;
                        actualTimeStep = timeStep;
                        Dictionary<int, ClimateRecord[][]> allData;

                        if (this.climatePhase == Climate.Phase.Future_Climate)
                            allData = Climate.Future_AllData;
                        else
                            allData = Climate.Spinup_AllData;

                        if (!allData.ContainsKey(timeStep))
                            actualTimeStep = allData.Keys.Max();

                        monthlyData = AnnualClimate_From_AnnualClimate_Daily(ecoregion, latitude, spinupOrfuture, timeStep, timeStepIndex);
                        CalculateMonthlyData(ecoregion, monthlyData, actualTimeStep, latitude);
                        //Climate.ModelCore.UI.WriteLine("  Completed calculations for {0} using Daily_SequencedYears. Ecoregion = {1}, SimulatedYear = {2}, actualYearUsed={3}.", this.climatePhase, ecoregion.Name, timeStep, actualTimeStep);
                        break;
                    }
               
                default:
                    throw new ApplicationException(String.Format("Unknown Climate Time Series: {0}", climateOption));

            }

            //this.MonthlyPET = CalculatePotentialEvapotranspiration();
            this.MonthlyPET = CalculatePotentialEvapotranspirationThornwaite(); 
            this.MonthlyVPD = CalculateVaporPressureDeficit();
            this.MonthlyGDD = CalculatePnETGDD(); 

            this.beginGrowing = CalculateBeginGrowingSeason(); 
            this.endGrowing = CalculateEndGrowingSeason(); 
            this.growingDegreeDays = GrowSeasonDegreeDays();

            this.JJAtemperature = 0.0;
            for (int mo = 5; mo < 8; mo++)
                this.JJAtemperature += this.MonthlyTemp[mo];
            this.JJAtemperature /= 3.0;
            

        }

        // ------------------------------------------------------------------------------------------------------
        private void CalculateMonthlyData(IEcoregion ecoregion, ClimateRecord[] monthlyClimateRecords, int actualYear, double latitude)
        {
            this.Year = actualYear;

            this.TotalAnnualPrecip = 0.0;
            for (int mo = 0; mo < 12; mo++)
            {
                this.MonthlyMinTemp[mo] = monthlyClimateRecords[mo].AvgMinTemp;
                this.MonthlyMaxTemp[mo] = monthlyClimateRecords[mo].AvgMaxTemp;
                this.MonthlyVarTemp[mo] = monthlyClimateRecords[mo].AvgVarTemp;
                this.MonthlyVarPpt[mo] = monthlyClimateRecords[mo].AvgVarPpt;
                this.MonthlyPrecip[mo] = monthlyClimateRecords[mo].AvgPpt;
                this.MonthlyPAR[mo] = monthlyClimateRecords[mo].AvgPAR;

                this.MonthlyTemp[mo] = (this.MonthlyMinTemp[mo] + this.MonthlyMaxTemp[mo]) / 2.0;

                this.TotalAnnualPrecip += this.MonthlyPrecip[mo];

                this.MonthlyWindDirection[mo] = monthlyClimateRecords[mo].AvgWindDirection;
                this.MonthlyWindSpeed[mo] = monthlyClimateRecords[mo].AvgWindSpeed;
                this.MonthlyNDeposition[mo] = monthlyClimateRecords[mo].AvgNDeposition;
                var hr = CalculateDayLength(mo, latitude);
                this.MonthlyDayLength[mo] = (3600.0 * hr);                  // seconds of daylight/day
                this.MonthlyNightLength[mo] = (3600.0 * (24.0 - hr));         // seconds of nighttime/day
            }

            this.MeanAnnualTemperature = CalculateMeanAnnualTemp(actualYear);
        }

        //This method is not currently being called.  If it is later used, it will need to be checked to make sure it's working properly.
        private void CalculateMonthlyData_AddVariance(IEcoregion ecoregion, ClimateRecord[][] timestepData, int actualYear, double latitude)
        {
            ClimateRecord[] ecoClimate = new ClimateRecord[12];

            this.Year = actualYear;
            this.TotalAnnualPrecip = 0.0;

            //if(timestepData[ecoregion.Index].  ADD MONTH CHECK HERE.

            for (int mo = 0; mo < 12; mo++)
            {
                ecoClimate[mo] = timestepData[ecoregion.Index][mo]; 

                double MonthlyAvgTemp = (ecoClimate[mo].AvgMinTemp + ecoClimate[mo].AvgMaxTemp) / 2.0;

                double standardDeviation = ecoClimate[mo].StdDevTemp * (Climate.ModelCore.GenerateUniform() * 2.0 - 1.0);

                this.MonthlyTemp[mo] = MonthlyAvgTemp + standardDeviation;
                this.MonthlyMinTemp[mo] = ecoClimate[mo].AvgMinTemp + standardDeviation;
                this.MonthlyMaxTemp[mo] = ecoClimate[mo].AvgMaxTemp + standardDeviation;
                this.MonthlyPrecip[mo] = Math.Max(0.0, ecoClimate[mo].AvgPpt + (ecoClimate[mo].StdDevPpt * (Climate.ModelCore.GenerateUniform() * 2.0 - 1.0)));
                this.MonthlyPAR[mo] = ecoClimate[mo].AvgPAR;
                this.MonthlyWindDirection[mo] = ecoClimate[mo].AvgWindDirection;
                this.MonthlyWindSpeed[mo] = ecoClimate[mo].AvgWindSpeed;
                this.MonthlyNDeposition[mo] = ecoClimate[mo].AvgNDeposition;

                this.TotalAnnualPrecip += this.MonthlyPrecip[mo];

                if (this.MonthlyPrecip[mo] < 0)
                    this.MonthlyPrecip[mo] = 0;

                double hr = CalculateDayLength(mo, latitude);
                this.MonthlyDayLength[mo] = (60.0 * 60.0 * hr);                  // seconds of daylight/day
                this.MonthlyNightLength[mo] = (60.0 * 60.0 * (24 - hr));         // seconds of nighttime/day

            }

            this.MeanAnnualTemperature = CalculateMeanAnnualTemp(actualYear);

        }

        private void CalculateMonthlyData_NoVariance(IEcoregion ecoregion, ClimateRecord[][] timestepData, int actualYear, double latitude)
        {
            ClimateRecord[] ecoClimate = new ClimateRecord[12];

            this.Year = actualYear;
            this.TotalAnnualPrecip = 0.0;

            for (int mo = 0; mo < 12; mo++)
            {
                //Climate.ModelCore.UI.WriteLine("  Calculating Monthly Climate (No Variance):  Yr={0}, month={1}, Eco={2}, Phase={3}.", actualYear, mo, ecoregion.Name, this.climatePhase);
                ecoClimate[mo] = timestepData[ecoregion.Index][mo]; 

                double MonthlyAvgTemp = (ecoClimate[mo].AvgMinTemp + ecoClimate[mo].AvgMaxTemp) / 2.0;
                               
                this.MonthlyTemp[mo] = MonthlyAvgTemp;
                this.MonthlyMinTemp[mo] = ecoClimate[mo].AvgMinTemp;
                this.MonthlyMaxTemp[mo] = ecoClimate[mo].AvgMaxTemp;
                this.MonthlyPrecip[mo] = Math.Max(0.0, ecoClimate[mo].AvgPpt); 
                this.MonthlyPAR[mo] = ecoClimate[mo].AvgPAR;
                this.MonthlyWindDirection[mo] = ecoClimate[mo].AvgWindDirection;
                this.MonthlyWindSpeed[mo] = ecoClimate[mo].AvgWindSpeed;
                this.MonthlyNDeposition[mo] = ecoClimate[mo].AvgNDeposition;

                this.TotalAnnualPrecip += this.MonthlyPrecip[mo];

                if (this.MonthlyPrecip[mo] < 0)
                    this.MonthlyPrecip[mo] = 0;

                double hr = CalculateDayLength(mo, latitude);
                this.MonthlyDayLength[mo] = (60.0 * 60.0 * hr);                  // seconds of daylight/day
                this.MonthlyNightLength[mo] = (60.0 * 60.0 * (24 - hr));         // seconds of nighttime/day

            }

            this.MeanAnnualTemperature = CalculateMeanAnnualTemp(actualYear);

        }
        
        //Daily will not come to here. the average in daily is calculated in the AnnualClimate_Daily
        private ClimateRecord[] AnnualClimate_AvgMonth(IEcoregion ecoregion, double latitude)
        {

            Dictionary<int, ClimateRecord[][]> timestepData;

            if (this.climatePhase == Climate.Phase.Future_Climate)
                timestepData = Climate.Future_AllData;
            else
                timestepData = Climate.Spinup_AllData;

            var monthlyData = new ClimateRecord[12];      // for returning summary data in ClimateRecord form.

            var yearCount = timestepData.Count;

            for (int mo = 0; mo < 12; mo++)
            {
                var monthlyMinTemp = 0.0;
                var monthlyMaxTemp = 0.0;
                var monthlyVarTemp = 0.0;
                var monthlyVarPpt = 0.0;
                var monthlyPrecip = 0.0;
                var monthlyPAR = 0.0;
                var monthlyWindDirection = 0.0;
                var monthlyWindSpeed = 0.0;
                var monthlyNDeposition = 0.0;

                foreach (var yearMonthlyRecords in timestepData.Values)
                {
                    monthlyMinTemp += yearMonthlyRecords[ecoregion.Index][mo].AvgMinTemp;
                    monthlyMaxTemp += yearMonthlyRecords[ecoregion.Index][mo].AvgMaxTemp;
                    monthlyVarTemp += yearMonthlyRecords[ecoregion.Index][mo].AvgVarTemp;
                    monthlyVarPpt += yearMonthlyRecords[ecoregion.Index][mo].AvgVarPpt;
                    monthlyPrecip += yearMonthlyRecords[ecoregion.Index][mo].AvgPpt;
                    monthlyPAR += yearMonthlyRecords[ecoregion.Index][mo].AvgPAR;
                    monthlyWindDirection += yearMonthlyRecords[ecoregion.Index][mo].AvgWindDirection;
                    monthlyWindSpeed += yearMonthlyRecords[ecoregion.Index][mo].AvgWindSpeed;
                    monthlyNDeposition += yearMonthlyRecords[ecoregion.Index][mo].AvgNDeposition;

                }

                monthlyData[mo] = new ClimateRecord();
                if (yearCount > 0)
                {
                    monthlyData[mo].AvgMinTemp = monthlyMinTemp / yearCount;
                    monthlyData[mo].AvgMaxTemp = monthlyMaxTemp / yearCount;
                    monthlyData[mo].AvgVarTemp = monthlyVarTemp / yearCount;
                    monthlyData[mo].StdDevTemp = Math.Sqrt(monthlyVarTemp / yearCount);
                    monthlyData[mo].AvgVarPpt = monthlyVarPpt / yearCount;
                    monthlyData[mo].AvgPpt = monthlyPrecip / yearCount;
                    monthlyData[mo].StdDevPpt = Math.Sqrt(monthlyPrecip / yearCount);
                    monthlyData[mo].AvgPAR = monthlyPAR / yearCount;
                    monthlyData[mo].AvgWindDirection = monthlyWindDirection / yearCount;
                    monthlyData[mo].AvgWindSpeed = monthlyWindSpeed / yearCount;
                    monthlyData[mo].AvgNDeposition = monthlyNDeposition / yearCount;
                }
            }
            return monthlyData;
        }

          
        private ClimateRecord[] AnnualClimate_From_AnnualClimate_Daily(IEcoregion ecoregion, double latitude, Climate.Phase spinupOrfuture, int timeStep, int timeStepIndex)
        {
            var monthlyData = new ClimateRecord[12];
            
            int nDays;
            int dayOfYear = 0;
            AnnualClimate_Daily annDaily = new AnnualClimate_Daily(ecoregion, latitude, spinupOrfuture, timeStep, timeStepIndex); //for the same timeStep
            
            // if annDaily data come from averaging over years, it will always have 365 days, so I can't use the DaysInMonth() method based on the actualYear
            var daysInMonth = annDaily.DailyDataIsLeapYear ? new int[] { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 } : new int[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

            if (spinupOrfuture == Climate.Phase.Future_Climate)
                Climate.Future_DailyData[timeStep][ecoregion.Index] = annDaily;
            else
                Climate.Spinup_DailyData[timeStep][ecoregion.Index] = annDaily;  

            for (int mo = 0; mo < 12; mo++)
            {
                var monthlyMinTemp = 0.0;
                var monthlyMaxTemp = 0.0;
                var monthlyVarTemp = 0.0;
                var monthlyPptVarTemp = 0.0;
                var monthlyPrecip = 0.0;
                var monthlyPAR = 0.0;
                var monthlyWindDirection = 0.0;
                var monthlyWindSpeed = 0.0; 
                var monthlyNDeposition = 0.0;
                var monthlyCO2 = 0.0;
                var monthlyRH = 0.0;

                nDays = daysInMonth[mo];
                for (int d = 0; d < nDays; d++)
                {
                    monthlyMinTemp += annDaily.DailyMinTemp[dayOfYear];
                    monthlyMaxTemp += annDaily.DailyMaxTemp[dayOfYear];
                    monthlyVarTemp += annDaily.DailyVarTemp[dayOfYear];
                    monthlyPptVarTemp += annDaily.DailyVarPpt[dayOfYear];
                    monthlyPrecip += annDaily.DailyPrecip[dayOfYear];
                    monthlyPAR += annDaily.DailyPAR[dayOfYear];
                    monthlyWindDirection += annDaily.DailyWindDirection[dayOfYear];
                    monthlyWindSpeed += annDaily.DailyWindSpeed[dayOfYear];
                    monthlyNDeposition += annDaily.DailyNDeposition[dayOfYear];
                    monthlyCO2 += annDaily.DailyCO2[dayOfYear];
                    monthlyRH += annDaily.DailyRH[dayOfYear];

                    dayOfYear++;
                }

                monthlyData[mo] = new ClimateRecord();
                
                monthlyData[mo].AvgMinTemp = monthlyMinTemp / nDays;
                monthlyData[mo].AvgMaxTemp = monthlyMaxTemp / nDays;
                monthlyData[mo].AvgVarTemp = monthlyVarTemp / nDays;
                monthlyData[mo].StdDevTemp = Math.Sqrt(monthlyVarTemp / nDays);
                monthlyData[mo].AvgVarPpt = monthlyPptVarTemp / nDays;
                monthlyData[mo].AvgPpt = monthlyPrecip;
                monthlyData[mo].StdDevPpt = Math.Sqrt(monthlyPrecip / nDays);
                monthlyData[mo].AvgPAR = monthlyPAR / nDays;
                monthlyData[mo].AvgWindDirection = monthlyWindDirection / nDays;
                monthlyData[mo].AvgWindSpeed = monthlyWindSpeed / nDays;
                monthlyData[mo].AvgNDeposition = monthlyNDeposition / nDays;
                monthlyData[mo].AvgCO2 = monthlyCO2 / nDays;
                monthlyData[mo].AvgRH = monthlyRH / nDays;
            }

            return monthlyData;

            
        }


        //---------------------------------------------------------------------------
        // Calc growing season degree days (Degree_Day) based on monthly temperatures
        // normally distributed around a specified mean with a specified standard
        // deviation.
        public int GrowSeasonDegreeDays()//int currentYear)
        {
            //degDayBase is temperature (C) above which degree days (Degree_Day)
            //are counted
            //double degDayBase = 5.56;      // 42F.
            double degDayBase = 4.44;      // 40F used as base in Botkin reference.
            double Deg_Days = 0.0;

            //Calc monthly temperatures (mean +/- normally distributed
            //random number times standard deviation) and
            //sum degree days for consecutve months.
            for (int month = 0; month < 12; month++) //12 months in year
            {
                if (this.MonthlyTemp[month] > degDayBase)
                    Deg_Days += (MonthlyTemp[month] - degDayBase) * DaysInMonth(month, this.Year);
                //Climate.ModelCore.UI.WriteLine("MonthlyTemp = {0:0.00}, Deg_DayBase = {1:0.0}, Deg_Days = {2}.", MonthlyTemp[month], degDayBase, Deg_Days);
            }
            //Climate.ModelCore.UI.WriteLine("AnnualGDD = {0}.", Deg_Days);
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
                double GDD = this.MonthlyTemp[month] * DaysInMonth(month, this.Year); //currentYear);
                if (GDD < 0)
                    GDD = 0;
                MonthlyGDD[month] = (int)GDD;
            }

            return MonthlyGDD;
        }


        //---------------------------------------------------------------------------
        // Calculate Begin Growing Degree Day (Last Frost; Minimum = 0 degrees C):
        // This method assumes you do not have daily data, which would be far more accurate.
        // Revised in Dec 2014 by M. Lucash
        private int CalculateBeginGrowingSeason() 
        {
            double lastMonthMinTemp = this.MonthlyMinTemp[11];
            int beginGrowingSeasonMonth = 0;  //"Month" when growing season starts, units are actually in days though.  For example, it returns 29 in Feb, 60(29+31) in March, etc.
            int beginGrowingSeason = 0;  //Returns that actual day when the growing season starts.
            
            for (int month = 1; month < 5; month++)  //Begin looking in February (month=1).  Should be safe for at least 100 years.
            {
                int totalDays = (DaysInMonth(month, this.Year) + DaysInMonth(month - 1, this.Year)) / 2; 
                double MonthlyMinTemp = this.MonthlyMinTemp[month];
                
                beginGrowingSeasonMonth = ((month * totalDays) + 15);  //you need to add 15 because your temps are "recorded" at day 15 of the month.

                //Now interpolate between days because you don't know which day you switched to positive temps.
                double degreeIncrement = System.Math.Abs(MonthlyMinTemp - lastMonthMinTemp) / (double)totalDays;                
                double InterpolatedTemp = MonthlyMinTemp;  //start from current (warmer) month

                if (MonthlyMinTemp > 0.0 && lastMonthMinTemp < 0.0)  // Only loop through days if you passed the threshold from negative to postive temps between months.
                {
                    for (int day = 1; day <= totalDays; day++)
                    {
                        if (InterpolatedTemp > 0.0)  // Stop loop if you temps > 0.  That's when you want the growing season to start.
                        {
                        InterpolatedTemp -= degreeIncrement;  // This works back to find first frost day (temp < 0) from the current month.                                               
                        beginGrowingSeason = day;
                        }
                    }
                    beginGrowingSeason = (beginGrowingSeasonMonth - beginGrowingSeason);        //Takes the days associated with the month loop and subtracts them.
                }                   
                lastMonthMinTemp = MonthlyMinTemp;
            }
            return beginGrowingSeason;
        }
        //private int CalculateBeginGrowingSeason()  Original method for calculating when the growing season starts.  
            //ML discovered this allowed the growing season to start WAY too early for MN.  Revised method above.
        //{
        //    //Climate.ModelCore.UI.WriteLine("  Calculating monthly growing season....");


        //    double lastMonthMinTemp = this.MonthlyMinTemp[11];
        //    int dayCnt = 15;  //the middle of February
        //    int beginGrowingSeason = 0;

        //    for (int month = 0; month < 5; month++)  //Begin looking in February (1).  Should be safe for at least 100 years.
        //    {

        //        int totalDays = (DaysInMonth(month, this.Year) + DaysInMonth(month - 1, this.Year)) / 2;
        //        double MonthlyMinTemp = this.MonthlyMinTemp[month];

        //        //Now interpolate between days:
        //        double degreeIncrement = System.Math.Abs(MonthlyMinTemp - lastMonthMinTemp) / (double)totalDays;
        //        double Tnight = MonthlyMinTemp;  //start from warmer month
        //        double TnightRandom = Tnight + (this.MonthlyVarTemp[month] * (Climate.ModelCore.GenerateUniform() * 2 - 1));

        //        for (int day = 1; day <= totalDays; day++)
        //        {
        //            if (TnightRandom <= 0)
        //                beginGrowingSeason = (dayCnt + day);
        //            Tnight += degreeIncrement;  //work backwards to find last frost day.
        //            TnightRandom = Tnight + (this.MonthlyVarTemp[month] * (Climate.ModelCore.GenerateUniform() * 2 - 1));
        //        }

        //        lastMonthMinTemp = MonthlyMinTemp;
        //        dayCnt += totalDays;  //new monthly mid-point
        //    }

        //    return beginGrowingSeason;
        //}

        //---------------------------------------------------------------------------
        // Calculate End Growing Degree Day (First frost; Minimum = 0 degrees C):
        // This method assumes you do not have daily data, which would be far more accurate.
        private int CalculateEndGrowingSeason() //ClimateRecord[] annualClimate)//, Random autoRand)
        {

            //Defaults for the middle of July:
            double lastMonthMinTemp = this.MonthlyMinTemp[6]; // annualClimate[6].AvgMinTemp;
            int dayCnt = 198;
            int endGrowingSeason = 365;
            //int endGrowingSeason = 198;

            for (int month = 7; month < 12; month++)  //Begin looking in August.  Should be safe for at least 100 years.
            {
                //int totalDays = (DaysInMonth(month, this.Year) + DaysInMonth(month - 1, this.Year)) / 2;  //See line 527 for explanation
                int totalDays = (DaysInMonth(month, this.Year)); 
                double MonthlyMinTemp = this.MonthlyMinTemp[month]; // annualClimate[i].AvgMinTemp;

                //Now interpolate between days:
                double degreeIncrement = System.Math.Abs(lastMonthMinTemp - MonthlyMinTemp) / (double)totalDays;
                double Tnight = MonthlyMinTemp;  //start from warmer month.  ML used to be last month but changed it to be consistent with beginning growing season

                double TnightRandom = Tnight + (this.MonthlyVarTemp[month] * (Climate.ModelCore.GenerateUniform() * 2 - 1));

                for (int day = 1; day <= totalDays; day++)
                {
                    if (TnightRandom <= 0)
                        return (dayCnt + day);
                    Tnight -= degreeIncrement;  //work forwards to find first frost day.
                    TnightRandom = Tnight + (this.MonthlyVarTemp[month] * (Climate.ModelCore.GenerateUniform() * 2 - 1));
                    //Climate.ModelCore.UI.WriteLine("Tnight = {0}.", TnightRandom);
                }

                lastMonthMinTemp = MonthlyMinTemp;
                dayCnt += totalDays;  //new monthly mid-point
                
            }
            //Climate.ModelCore.UI.WriteLine("  Calculating monthly ending growing season day...endGrowingSeason={0}", endGrowingSeason);
            return endGrowingSeason;
            //return 365; ML revised it so it didn't always return 365
        }


        //---------------------------------------------------------------------------
        private double[] CalculateVaporPressureDeficit()//ClimateRecord[] annualClimate)
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
                double Tmin = this.MonthlyMinTemp[month]; 
                double Tday = this.MonthlyTemp[month]; 

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
        private double[] CalculatePotentialEvapotranspiration()//ClimateRecord[] annualClimate)
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
                Climate.ModelCore.UI.WriteLine("PET Calcs. AvgTemp={0},HighestTemp={1}, LowestTemp={2:0.0},", avgTemp, highest, lowest);
            }

            lowest = System.Math.Max(lowest, -10.0);

            //...Determine average temperature range
            double avgTempRange = System.Math.Abs(highest - lowest);

            Climate.ModelCore.UI.WriteLine("AfterLoop in PET CalcuLater. lowestTemp={0:0.000},AvgTempRange={1},",  lowest, avgTempRange);
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
                //Climate.ModelCore.UI.WriteLine("FinalLoop in PET CalcuLater. tr={0}, t={1}, tm={2}, td={3}, e={4}, monthPET={5}, maxtemp={6}, mintemp={7},finalPET={7}", tr, t, tm, td, e, monthPET, MonthlyMaxTemp[month], this.MonthlyMinTemp[month], monthlyPET[month]);

            }

            return monthlyPET;
        }

        //  Calculate potential evapotranspiration using the Thornwaite method.
        private double[] CalculatePotentialEvapotranspirationThornwaite()
        {

           
            //Calculate Heat index first because it depends on monthly mean temps throughout the entire year at a given location
            double heatIndex = 0.0;

            for (int i = 0; i < 12; i++)
            {
                
                double avgTemp = this.MonthlyTemp[i]; // (annualClimate[i].AvgMinTemp + annualClimate[i].AvgMaxTemp) / 2.0;
                avgTemp = Math.Max(0.0, avgTemp);
                double heatIndexpermonth = System.Math.Pow((avgTemp / 5.0), 1.514);
                heatIndexpermonth = Math.Max(0.0, heatIndexpermonth);
                heatIndex += (heatIndexpermonth);
                //Climate.ModelCore.UI.WriteLine("PET Calcs1. AvgTemp={0},heatIndexpermonth={1}", avgTemp, heatIndexpermonth);
            }
            //Climate.ModelCore.UI.WriteLine("PET Calcs2. heatindex={0}", heatIndex);
            double alpha = (0.000000675 * System.Math.Pow(heatIndex, 3.0) - (0.0000771 * System.Math.Pow(heatIndex, 2.0)) + (0.01792 * heatIndex) + 0.49239);
            
            //Then need to calculate PET for each month using the heat index from above.

            double[] monthlyPET = new double[12];
            for (int month = 0; month < 12; month++)
            {

                double MeanDayLength = this.MonthlyDayLength[month] / 3600.0;  //take day length from above in sec and convert to hours
                int totalDays = (DaysInMonth(month, this.Year));
                double AvgTemp = this.MonthlyTemp[month];

                AvgTemp = Math.Max(0.0, AvgTemp);
                
                //alpha term in Thornwaite equation that depends solely on the heat index
                                                                           
                monthlyPET[month] = (16.0 * (MeanDayLength / 12.0) * (totalDays / 30.0) * (System.Math.Pow(((10.0 * AvgTemp )/ heatIndex), alpha))/10.0);// equation by Thornthwaite divided by 10 to get cm/month
                //Climate.ModelCore.UI.WriteLine("Year={0}, Month={1}, PET={2:0.00}.", this.Year, month, monthlyPET[month]);
                //Climate.ModelCore.UI.WriteLine("FinalLoop in PET CalcuLator. DayLength={0}, TotalDaysMonth={1}, AvgTemp={2}, alpha={3}, heatIndex={4}, PET={5:0.0000}, Month={6}", MeanDayLength, totalDays, AvgTemp, alpha, heatIndex, monthlyPET[month], month);

            }

            return monthlyPET;
        }


        //---------------------------------------------------------------------------
        public static double CalculateAnnualActualEvapotranspiration(AnnualClimate_Monthly annualClimate, double fieldCapacity)
        {
            
            //I don't think this method is every called.
            
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

                //Climate.ModelCore.UI.WriteLine("Actual ET Calcs. Rain={0},PET={1}, potWaterLoss={2:0.0},,fieldcapacity={3:0.000}, oldwaterAvail={4:0.000}, actualET={5:0.000}", monthlyRain, potentialET, potWaterLoss, fieldCapacity, oldWaterAvail, actualET);
            }

            return actualET;
        }

        //---------------------------------------------------------------------------
        public double CalculateMeanAnnualTemp(int currentYear)
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
        //public double TotalAnnualPrecip()
        //{
        //    //Main loop for yearly water balance calculation by month   */
        //    double TAP = 0.0;
        //    for (int i = 0; i < 12; i++)
        //    {
        //        TAP += MonthlyPrecip[i];
        //    }
        //    return TAP;
        //}

        ////---------------------------------------------------------------------------
        //public void WriteToLandisLogFile()
        //{
        //    Climate.ModelCore.UI.WriteLine("  ClimatePhase = {0}, Year = {1}, MAP = {2:0.00}.", this.climatePhase, this.Year, this.AnnualPrecip);

        //    //(IEcoregion ecoregion, int actualYear, double latitude, ClimatePhase spinupOrfuture = Climate.Phase.Future_Climate, int timeStep = Int32.MinValue)
        //}

    }
}
