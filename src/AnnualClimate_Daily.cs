//  Authors:  Amin Almassian, Robert M. Scheller, John McNabb, Melissa Lucash

using Landis.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Landis.Library.Climate
{
    public class AnnualClimate_Daily: AnnualClimate
    {
        public bool DailyDataIsLeapYear;

        public int MaxDayInYear { get { return DailyDataIsLeapYear? 366 : 365; } } // = 366;

        public double[] DailyTemp = new double[366];
        public double[] DailyMinTemp = new double[366];
        public double[] DailyMaxTemp = new double[366];
        public double[] DailyPrecip = new double[366];
        public double[] DailyPAR = new double[366];
        public double[] DailyVarTemp = new double[366];
        public double[] DailyVarPpt = new double[366];
        public double[] DailyWindDirection = new double[366];
        public double[] DailyWindSpeed = new double[366];
        public double[] DailyNDeposition = new double[366];
        public double[] DailyCO2 = new double[366];
        public double[] DailyRH = new double[366];
        public double[] DailyMinRH = new double[366];
        public double[] DailyMaxRH = new double[366];
        public double[] DailyFireWeatherIndex = new double[366];


        //public int tempEcoIndex = -1;

        //public double[] DailyPET = new double[366];  // Potential Evapotranspiration
        //public double[] DailyVPD = new double[366];  // Vapor Pressure Deficit
        //public double[] DailyDayLength = new double[366];
        //public double[] DailyNightLength = new double[366];
        //public int[] DailyGDD = new int[366];
        public AnnualClimate_Daily() { }

        //For Sequenced and Random timeStep arg should be passed
        public AnnualClimate_Daily(IEcoregion ecoregion, double latitude, Climate.Phase spinupOrfuture, int timeStep, int timeStepIndex)
        {

            this.climatePhase = spinupOrfuture;
            ClimateRecord[][] timestepData = new ClimateRecord[Climate.ModelCore.Ecoregions.Count][];
            for (var i = 0; i < Climate.ModelCore.Ecoregions.Count; ++i)
                timestepData[i] = new ClimateRecord[366];

            string climateOption = Climate.ConfigParameters.ClimateTimeSeries;
            if (this.climatePhase == Climate.Phase.SpinUp_Climate)
                climateOption = Climate.ConfigParameters.SpinUpClimateTimeSeries;

            ClimateRecord[] dailyData = null;

            int actualTimeStep;

            //Climate.TextLog.WriteLine("  Calculating daily data ...  Ecoregion = {0}, Year = {1}, timestep = {2}.", ecoregion.Name, actualYear, timeStep);
            switch (climateOption)
            {
                case "Daily_RandomYears":
                    {
                        // JM: this code assumes that the constructor for AnnualClimate_Daily is ONLY called from within
                        //  AnnualClimate_Monthly.AnnualClimate_From_AnnualClimate_Daily(), and, for Daily_RandomYear, the
                        //  actualYear contains the randomly-selected year.

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

                        Climate.TextLog.WriteLine("  AnnualClimate_Daily: Daily_RandomYear: timeStep = {0}, actualYear = {1}, phase = {2}.", timeStep, actualTimeStep, this.climatePhase);

                        dailyData = allData[actualTimeStep][ecoregion.Index];
                        CalculateDailyData(ecoregion, dailyData, actualTimeStep, latitude);
                        break;

                       
                    }
                case "Daily_AverageAllYears":
                    {
                        TimeStep = timeStep;
                        actualTimeStep = 0;

                        dailyData = AnnualClimate_AvgDaily(ecoregion, latitude);
                        CalculateDailyData(ecoregion, dailyData, actualTimeStep, latitude);
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

                        ClimateRecord[][] yearRecords;

                        // get the climate records for the requested year, or if the year is not found, get the records for the last year
                        if (!allData.TryGetValue(timeStep, out yearRecords))
                        {
                            actualTimeStep = allData.Keys.Max();
                            yearRecords = allData[actualTimeStep];
                        }

                        dailyData = yearRecords[ecoregion.Index];
                        CalculateDailyData(ecoregion, dailyData, actualTimeStep, latitude);
                        break;
                    }
                default:
                    throw new ApplicationException(String.Format("Unknown Climate Time Series: {0}", climateOption));

            }

                       
            this.beginGrowing = CalculateBeginGrowingDay_Daily(); //ecoClimate);
            this.endGrowing = CalculateEndGrowingDay_Daily(dailyData);
            this.growingDegreeDays = GrowSeasonDegreeDays();

            this.DailyDataIsLeapYear = dailyData.Length == 366;

        }

        private void CalculateDailyData(IEcoregion ecoregion, ClimateRecord[] dailyClimateRecords, int actualYear, double latitude)
        {
            this.Year = actualYear;

            this.TotalAnnualPrecip = 0.0;
            for (int d = 0; d < dailyClimateRecords.Length; d++)
            {
                this.DailyMinTemp[d] = dailyClimateRecords[d].AvgMinTemp;
                this.DailyMaxTemp[d] = dailyClimateRecords[d].AvgMaxTemp;
                this.DailyVarTemp[d] = dailyClimateRecords[d].AvgVarTemp;
                this.DailyVarPpt[d] = dailyClimateRecords[d].AvgVarPpt;
                this.DailyPrecip[d] = dailyClimateRecords[d].AvgPpt;
                this.DailyPAR[d] = dailyClimateRecords[d].AvgPAR;
                this.DailyWindDirection[d] = dailyClimateRecords[d].AvgWindDirection;
                this.DailyWindSpeed[d] = dailyClimateRecords[d].AvgWindSpeed;
                this.DailyNDeposition[d] = dailyClimateRecords[d].AvgNDeposition;
                this.DailyCO2[d] = dailyClimateRecords[d].AvgCO2;
                this.DailyMinRH[d] = dailyClimateRecords[d].AvgMinRH;
                this.DailyMaxRH[d] = dailyClimateRecords[d].AvgMaxRH;
                this.DailyFireWeatherIndex[d] = dailyClimateRecords[d].AvgFWI;

                this.DailyTemp[d] = (this.DailyMinTemp[d] + this.DailyMaxTemp[d]) / 2.0;
                this.DailyRH[d] = (this.DailyMinRH[d] + this.DailyMaxRH[d]) / 2.0;

                this.TotalAnnualPrecip += this.DailyPrecip[d];

                //var hr = CalculateDayLength(d, latitude);
                //this.DailyDayLength[d] = (3600.0 * hr);                  // seconds of daylight/day
                //this.DailyNightLength[d] = (3600.0 * (24.0 - hr));         // seconds of nighttime/day
                var avgTemp = (this.DailyMinTemp[d] + this.DailyMaxTemp[d])/2;
                //this.DailyRH[d] = 100 * Math.Exp((17.269 * this.DailyMinTemp[d]) / (273.15 + this.DailyMinTemp[d]) - (17.269 * avgTemp) / (273.15 + avgTemp));
            }
        }


        //public double[] DailyRH = new double[366];
        //public double[] DailyWindSpeed = new double[366];
        //public double[] DailyNdeposition = new double[366];

        private ClimateRecord[] AnnualClimate_AvgDaily(IEcoregion ecoregion, double latitude)
        {
            var dailyData = new ClimateRecord[365];     // year-averaged data are always of length 365, even if averaging includes leapyears.

            Dictionary<int, ClimateRecord[][]> timestepData;

            if (this.climatePhase == Climate.Phase.Future_Climate)
                timestepData = Climate.Future_AllData;
            else
                timestepData = Climate.Spinup_AllData;

            var yearCount = timestepData.Count;

            var feb28DayIndex = 31 + 28 - 1;  // zero-based so subtract 1

            for (var d = 0; d < 365; ++d)
            {
                var dailyMinTemp = 0.0;
                var dailyMaxTemp = 0.0;
                var dailyVarTemp = 0.0;
                var dailyVarPpt = 0.0;
                var dailyPrecip = 0.0;
                var dailyPAR = 0.0;
                var dailyWindDirection = 0.0;
                var dailyWindSpeed = 0.0;
                var dailyNDeposition = 0.0;
                var dailyCO2 = 0.0;
                var dailyMinRH = 0.0;
                var dailyMaxRH = 0.0;
                var dailyFWI = 0.0;

                // loop over years
                int dIndex;

                foreach (var yearDailyRecords in timestepData.Values)
                {
                    var yearRecords = yearDailyRecords[ecoregion.Index];

                    if (yearRecords.Length == 366 && d == feb28DayIndex)
                    {
                        // average data for both Feb28 and Feb29
                        dailyMinTemp += (yearRecords[d].AvgMinTemp + yearRecords[d + 1].AvgMinTemp) / 2.0;
                        dailyMaxTemp += (yearRecords[d].AvgMaxTemp + yearRecords[d + 1].AvgMaxTemp) / 2.0;
                        dailyVarTemp += (yearRecords[d].AvgVarTemp + yearRecords[d + 1].AvgVarTemp) / 2.0;
                        dailyVarPpt += (yearRecords[d].AvgVarPpt + yearRecords[d + 1].AvgVarPpt) / 2.0;
                        dailyPrecip += (yearRecords[d].AvgPpt + yearRecords[d + 1].AvgPpt) / 2.0;
                        dailyPAR += (yearRecords[d].AvgPAR + yearRecords[d + 1].AvgPAR) / 2.0;
                        dailyWindDirection += (yearRecords[d].AvgWindDirection + yearRecords[d + 1].AvgWindDirection) / 2.0;
                        dailyWindSpeed += (yearRecords[d].AvgWindSpeed + yearRecords[d + 1].AvgWindSpeed) / 2.0;
                        dailyNDeposition += (yearRecords[d].AvgNDeposition + yearRecords[d + 1].AvgNDeposition) / 2.0;
                        dailyCO2 += (yearRecords[d].AvgCO2 + yearRecords[d + 1].AvgCO2) / 2.0;
                        dailyMinRH += (yearRecords[d].AvgMinRH + yearRecords[d + 1].AvgMinRH) / 2.0;
                        dailyMaxRH += (yearRecords[d].AvgMaxRH + yearRecords[d + 1].AvgMaxRH) / 2.0;
                        dailyFWI += (yearRecords[d].AvgFWI + yearRecords[d + 1].AvgFWI) / 2.0;
                    }
                    else
                    {
                        // if it is a leapyear and the day is after Feb28, add one to the day index
                        dIndex = (yearRecords.Length == 366 && d > feb28DayIndex) ? d + 1 : d;

                        dailyMinTemp += yearRecords[dIndex].AvgMinTemp;
                        dailyMaxTemp += yearRecords[dIndex].AvgMaxTemp;
                        dailyVarTemp += yearRecords[dIndex].AvgVarTemp;
                        dailyVarPpt += yearRecords[dIndex].AvgVarPpt;
                        dailyPrecip += yearRecords[dIndex].AvgPpt;
                        dailyPAR += yearRecords[dIndex].AvgPAR;
                        dailyWindDirection += yearRecords[dIndex].AvgWindDirection;
                        dailyWindSpeed += yearRecords[dIndex].AvgWindSpeed;
                        dailyNDeposition += yearRecords[dIndex].AvgNDeposition;
                        dailyMinRH += yearRecords[dIndex].AvgMinRH;
                        dailyMaxRH += yearRecords[dIndex].AvgMaxRH;
                        dailyFWI += yearRecords[dIndex].AvgFWI;
                        
                    }
                }

                dailyData[d] = new ClimateRecord();
                if (yearCount > 0)
                {
                    dailyData[d].AvgMinTemp = dailyMinTemp / yearCount;
                    dailyData[d].AvgMaxTemp = dailyMaxTemp / yearCount;
                    dailyData[d].AvgVarTemp = dailyVarTemp / yearCount;
                    dailyData[d].StdDevTemp = Math.Sqrt(dailyVarTemp / yearCount);
                    dailyData[d].AvgVarPpt = dailyVarPpt / yearCount;
                    dailyData[d].AvgPpt = dailyPrecip / yearCount;
                    dailyData[d].StdDevPpt = Math.Sqrt(dailyPrecip / yearCount);
                    dailyData[d].AvgPAR = dailyPAR / yearCount;
                    dailyData[d].AvgWindDirection = dailyWindDirection / yearCount;
                    dailyData[d].AvgWindSpeed = dailyWindSpeed / yearCount;
                    dailyData[d].AvgNDeposition = dailyNDeposition / yearCount;
                    dailyData[d].AvgCO2 = dailyCO2 / yearCount;
                    dailyData[d].AvgMinRH = dailyMinRH / yearCount;
                    dailyData[d].AvgMaxRH = dailyMaxRH / yearCount;
                    dailyData[d].AvgFWI = dailyFWI / yearCount;
                }
            }

            return dailyData;
        }

        
        
        private int GetJulianMonthFromJulianDay(int yr, int mo, int d)
        {
            //System.Globalization.GregorianCalendar gc = new System.Globalization.GregorianCalendar();
            System.Globalization.JulianCalendar jc = new System.Globalization.JulianCalendar();
            return jc.GetMonth(new DateTime(yr,mo,d,jc));
        }
                     

        //---------------------------------------------------------------------------
        private int CalculateBeginGrowingDay_Daily()  //Actually only using monthly data to calculate parameter for establishment.
        
        //Calculate Begin Growing Degree Day (Last Frost; Minimum = 0 degrees C): 
        {
            double nightTemp = 0.0;
            int beginGrow = 162;
            for (int i = 1 ; i < 162; i++)  //Loop through all the days of the year from day 1 to day 162
            {
                nightTemp = this.DailyMinTemp[i];
                if (nightTemp > 0.0)
                {
                   // this.beginGrowing = i;
                    beginGrow = i;
                    break;
                }
            }
            
            return beginGrow;           
        }

        //---------------------------------------------------------------------------
        private int CalculateEndGrowingDay_Daily(ClimateRecord[] annualClimate)//, Random autoRand)  //Actually only using monthly data for establishment.
        //Calculate End Growing Degree Day (First frost; Minimum = 0 degrees C):
        {
            double nightTemp = 0.0;
            //int beginGrowingDay = CalculateBeginGrowingDay_Daily(annualClimate);
            int endGrowingDay = MaxDayInYear;
            //int i = beginGrowingDay;
            for (int day = MaxDayInYear; day > this.BeginGrowing; day--)  //Loop through all the days of the year from day 1 to day 162
            {
                nightTemp = this.DailyMinTemp[day];
                if (nightTemp > 0)
                {
                    //this.endGrowing = i;
                    //endGrowingDay = i;
                    return day;
                }
                //Climate.TextLog.WriteLine("  Calculating end begin growing season day...{0}", endGrowingDay);
            }
            
            return 0;
        }

       

         //---------------------------------------------------------------------------
        public int GrowSeasonDegreeDays()            //Actually only using only monthly data for establishment.
        //Method for calculating the growing season degree days (Degree_Day) based on daily temperatures
        {
             //degDayBase is temperature (C) above which degree days (Degree_Day)
            //are counted
            //In v3.1, we used to use a base of 42F but Botkin et al actually recommends 40oF in his original publication- RS/ML
            //double degDayBase = 5.56;      // 42F.
            double degDayBase = 4.44;      // 40F.
            
            double Deg_Days = 0.0;

            for (int day = 0; day < 365; day++) //for every day of the year
            {
                if (DailyTemp[day] > degDayBase)
                //Deg_Days += (DailyTemp[i] - degDayBase);
                {
                    Deg_Days += (DailyTemp[day] - degDayBase);
                    //Climate.TextLog.WriteLine("DailyTemp={0:0.0}, Deg_DayBase={1:0.00}, Deg_Days={2:0.00},", DailyTemp[day], degDayBase, Deg_Days);
                }
                                    
            }
            this.growingDegreeDays = (int)Deg_Days;            
            return (int) Deg_Days;

            
        }

            
        //---------------------------------------------------------------------------
                    
        }              
    }

