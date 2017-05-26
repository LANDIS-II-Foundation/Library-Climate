//  Copyright: Portland State University 2009-2014
//  Authors:  Robert M. Scheller, John McNabb, Amin Almassian

using Landis.Core;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;
using Landis.Library.Metadata;
using System.Linq;


namespace Landis.Library.Climate
{

    public class Climate
    {
        //fire stuff
        public static double FireWeatherIndex;
        public static double FineFuelMoistureCode;
        public static double DuffMoistureCode;
        public static double DroughtCode;
        public static double BuildUpIndex;
        public static double WindSpeedVelocity;
        public static double WindAzimuth;

        private static TemporalGranularity future_allData_granularity;
        private static TemporalGranularity spinup_allData_granularity;
        private static Dictionary<int, ClimateRecord[][]> future_allData;
        private static Dictionary<int, ClimateRecord[][]> spinup_allData;
        private static List<int> randSelectedTimeKeys_future;
        private static List<int> randSelectedTimeKeys_spinup;
        private static ICore modelCore;
        private static IInputParameters configParameters;

        //private static System.Data.DataTable annualPDSI;
        private static double[,] annualPDSI;
        private static double[] landscapeAnnualPDSI;

        //public static MetadataTable<PDSI_Log> PdsiLog;
        public static MetadataTable<InputLog> SpinupInputLog;
        public static MetadataTable<InputLog> FutureInputLog;
        public static MetadataTable<AnnualLog> AnnualLog;
        public static StreamWriter TextLog;

        public enum Phase {SpinUp_Climate = 0, Future_Climate = 1 }

        // Rob testing storing all monthly and daily data during spinup, to avoid new data creation.
        public static Dictionary<int, AnnualClimate_Daily[] > Future_DailyData;  //dict key = year; climate record = ecoreregion, day
        public static Dictionary<int, AnnualClimate_Monthly[]> Future_MonthlyData;  //dict key = year; climate record = ecoreregion, month
        public static Dictionary<int, AnnualClimate_Daily[]> Spinup_DailyData;  //dict key = year; climate record = ecoreregion, day
        public static Dictionary<int, AnnualClimate_Monthly[]> Spinup_MonthlyData;  //dict key = year; climate record = ecoreregion, month

        public Climate()
        {
        }

        //---------------------------------------------------------------------

        public static ICore ModelCore
        {
            get
            {
                return modelCore;
            }
        }

        public static double[,] AnnualPDSI  //ecoregion.Index, Year
        {
            get
            {
                return annualPDSI;
            }
            set
            {
                annualPDSI = value;
            }
        }

        public static double[] LandscapeAnnualPDSI //year
        {
            get
            {
                return landscapeAnnualPDSI;
            }
            set
            {
                landscapeAnnualPDSI = value;
            }

        }

        public static TemporalGranularity AllData_granularity
        {
            get
            {
                return future_allData_granularity;
            }
        }
        public static TemporalGranularity Spinup_allData_granularity
        {
            get
            {
                return spinup_allData_granularity;
            }
        }
        public static Dictionary<int, ClimateRecord[][]> Future_AllData 
        {
            get
            {
                return future_allData;
            }
        }
        public static Dictionary<int, ClimateRecord[][]> Spinup_AllData
        {
            get
            {
                return spinup_allData;
            }
        }

        public static List<int> RandSelectedTimeKeys_future { get { return randSelectedTimeKeys_future; } }
        public static List<int> RandSelectedTimeKeys_spinup { get { return randSelectedTimeKeys_spinup; } }

       
        public static IInputParameters ConfigParameters
        {
            get
            {
                return configParameters;
            }
            //set
            //{
            //    configParameters = value;
            //}
        }



        //---------------------------------------------------------------------
        public static void Initialize(string climateConfigFilename, bool writeOutput, ICore mCore)
        {
            InputParametersParser inParamsParser = new InputParametersParser();
            configParameters = Landis.Data.Load<IInputParameters>(climateConfigFilename, inParamsParser);

            TextLog = Landis.Data.CreateTextFile("Landis-climate-log.txt");
            TextLog.AutoFlush = true;

            modelCore = mCore;
            MetadataHandler.InitializeMetadata(1, modelCore);

            ModelCore.UI.WriteLine("   Loading weather data ...");
            Climate.future_allData = new Dictionary<int, ClimateRecord[][]>();
            Climate.spinup_allData = new Dictionary<int, ClimateRecord[][]>();

            Future_MonthlyData = new Dictionary<int, AnnualClimate_Monthly[]>();
            Spinup_MonthlyData = new Dictionary<int, AnnualClimate_Monthly[]>();
            Future_DailyData = new Dictionary<int, AnnualClimate_Daily[]>();
            Spinup_DailyData = new Dictionary<int, AnnualClimate_Daily[]>();
            LandscapeAnnualPDSI = new double[Climate.ModelCore.EndTime - Climate.ModelCore.StartTime + 1];
            
            TextLog.WriteLine("   Loading spin-up weather data from file {0} ...", configParameters.SpinUpClimateFile);
            Climate.ConvertFileFormat_FillOutAllData(configParameters.SpinUpClimateTimeSeries, configParameters.SpinUpClimateFile, configParameters.SpinUpClimateFileFormat, Climate.Phase.SpinUp_Climate);

            TextLog.WriteLine("   Loading future weather data from file {0} ...", configParameters.ClimateFile);
            Climate.ConvertFileFormat_FillOutAllData(configParameters.ClimateTimeSeries, configParameters.ClimateFile, configParameters.ClimateFileFormat, Climate.Phase.Future_Climate);


            // **
            // spinup

            // write input data to the log
            foreach (KeyValuePair<int, ClimateRecord[][]> timeStep in spinup_allData)
            {
                Climate.WriteSpinupInputLog(timeStep.Value, timeStep.Key); //, Climate.Phase.SpinUp_Climate.ToString());
            }

            // find maxSpeciesAge as the maximum possible time step count for spin up
            int maxSpeciesAge = 0;
            foreach (ISpecies sp in ModelCore.Species)
            {
                if (sp.Longevity > maxSpeciesAge)
                    maxSpeciesAge = sp.Longevity;
            }

            var spinupTimeStepKeys = new List<int>();
            var spinupKeyList = new List<int>(Climate.spinup_allData.Keys);
            var spinupStartYear = spinupKeyList.Min();
            var spinupTimeStepCount = maxSpeciesAge;

            for (var i = 0; i < spinupTimeStepCount; ++i)
                spinupTimeStepKeys.Add(spinupStartYear + i);

            if (Climate.ConfigParameters.SpinUpClimateTimeSeries.ToLower().Contains("random"))
            {
                // generate random keys for the length of maxSpeciesAge
                Climate.randSelectedTimeKeys_spinup = new List<int>();

                // pick a random year key from allData
                for (var i = 0; i < spinupTimeStepCount; ++i)
                    Climate.randSelectedTimeKeys_spinup.Add(spinupKeyList[(int)(spinupKeyList.Count * Climate.ModelCore.GenerateUniform())]);
            }

            // initialize Spinup data arrays
            foreach (var timeStepKey in spinupTimeStepKeys)
            {
                Spinup_MonthlyData.Add(timeStepKey, new AnnualClimate_Monthly[modelCore.Ecoregions.Count]);
                Spinup_DailyData.Add(timeStepKey, new AnnualClimate_Daily[modelCore.Ecoregions.Count]);
            }


            // **
            // future

            // VS: <---this may be a hack...Calculate FWI
            foreach (KeyValuePair<int, ClimateRecord[][]> timeStep in future_allData)
            {
                Climate.CalculateFWI(timeStep.Value, timeStep.Key); //, future_allData_granularity);
                
            }

            // write input data to the log
            foreach (KeyValuePair<int, ClimateRecord[][]> timeStep in future_allData)
            {
                Climate.WriteFutureInputLog(timeStep.Value, timeStep.Key);
                //, future_allData_granularity);
            }

            var futureTimeStepKeys = new List<int>();
            var futureKeyList = new List<int>(Climate.future_allData.Keys);
            var futureStartYear = futureKeyList.Min();
            var futureTimeStepCount = ModelCore.EndTime - ModelCore.StartTime;

            for (var i = 0; i < futureTimeStepCount; ++i)
                futureTimeStepKeys.Add(futureStartYear + i);

            if (Climate.ConfigParameters.ClimateTimeSeries.ToLower().Contains("random")) 
            {
                // generate random keys for the length of the simulation
                Climate.randSelectedTimeKeys_future = new List<int>();

                // pick a random year key from allData
                for (var i = 0; i < futureTimeStepCount; ++i)
                    Climate.randSelectedTimeKeys_future.Add(futureKeyList[(int)(futureKeyList.Count * Climate.ModelCore.GenerateUniform())]);                                
            }

            // initialize Future data arrays
            foreach (var timeStepKey in futureTimeStepKeys)
            {
                Future_MonthlyData.Add(timeStepKey, new AnnualClimate_Monthly[modelCore.Ecoregions.Count]);
                Future_DailyData.Add(timeStepKey, new AnnualClimate_Daily[modelCore.Ecoregions.Count]);
            }
        }

        // Overload method without field capacity and wilting point.  RMS added 9/7/2016
        // If using this method, CANNOT calculate AET or PDSI.  Note: PDSI not working regardless.
        public static void GenerateEcoregionClimateData(IEcoregion ecoregion, int startYear, double latitude)
        {
            GenerateEcoregionClimateData(ecoregion, startYear, latitude, 20.0, 10.0);
        }

        public static void GenerateEcoregionClimateData(IEcoregion ecoregion, int startYear, double latitude, double fieldCapacity, double wiltingPoint)
        {
                                    
            // JM:  these next three lines are not currently used, but may need to be modified if used:
            //int numberOftimeSteps = Climate.ModelCore.EndTime - Climate.ModelCore.StartTime;
            //annualPDSI = new double[Climate.ModelCore.Ecoregions.Count, future_allData.Count]; 
            //landscapeAnnualPDSI = new double[future_allData.Count]; 
            double[] temperature_normals = new double[12];
            double[] precip_normals = new double[12];
            
            double availableWaterCapacity = fieldCapacity - wiltingPoint;

            Climate.TextLog.WriteLine("Core.StartTime = {0}, Core.EndTime = {1}.", ModelCore.StartTime, ModelCore.EndTime);
            //Climate.TextLog.WriteLine("   Climate.LandscapeAnnualPDSI.Length = {0}.", Climate.LandscapeAnnualPDSI.Length);

            //First Calculate Climate Normals from Spin-up data
            int timeStepIndex = 0;
            foreach (KeyValuePair<int, AnnualClimate_Monthly[]> timeStep in Spinup_MonthlyData)
            {

                //Climate.TextLog.WriteLine("  Calculating Weather for SPINUP: timeStep = {0}, actualYear = {1}", timeStep.Key, startYear + timeStep.Key);
                AnnualClimate_Monthly annualClimateMonthly = new AnnualClimate_Monthly(ecoregion, latitude, Climate.Phase.SpinUp_Climate, timeStep.Key, timeStepIndex); 
                Spinup_MonthlyData[startYear + timeStep.Key][ecoregion.Index] = annualClimateMonthly;

                for (int mo = 0; mo < 12; mo++)
                {
                    temperature_normals[mo] += annualClimateMonthly.MonthlyTemp[mo];
                    precip_normals[mo] += annualClimateMonthly.MonthlyPrecip[mo];
                }

                timeStepIndex++;
            }

            // Calculate AVERAGE T normal.
            for (int mo = 0; mo < 12; mo++)
            {
                temperature_normals[mo] /= (double)Spinup_MonthlyData.Count;
                precip_normals[mo] /= (double)Spinup_MonthlyData.Count;
                //Climate.TextLog.WriteLine("Month = {0}, Original Monthly T normal = {1}", mo, month_Temp_normal[mo]);

            }
            
            timeStepIndex = 0;

            //PDSI_Calculator.InitializeEcoregion_PDSI(temperature_normals, precip_normals, availableWaterCapacity, latitude, UnitSystem.metrics, ecoregion);

            foreach (KeyValuePair<int, AnnualClimate_Monthly[]> timeStep in Future_MonthlyData)
            {
                //Climate.TextLog.WriteLine("  Completed calculations for Future_Climate: TimeStepYear = {0}, actualYear = {1}", timeStep.Key, startYear + timeStep.Key);
                AnnualClimate_Monthly annualClimateMonthly = new AnnualClimate_Monthly(ecoregion, latitude, Climate.Phase.Future_Climate, timeStep.Key, timeStepIndex);
                Future_MonthlyData[startYear + timeStep.Key][ecoregion.Index] = annualClimateMonthly;

                // Next calculate PSDI for the future data
                //Future_MonthlyData[startYear + timeStep.Key][ecoregion.Index].PDSI = PDSI_Calculator.CalculateEcoregion_PDSI(annualClimateMonthly, temperature_normals, precip_normals, availableWaterCapacity, latitude, UnitSystem.metrics, ecoregion);
                //Future_MonthlyData[startYear + timeStep.Key][ecoregion.Index].PDSI = PDSI_Calculator.CalculateEcoregion_PDSI(annualClimateMonthly, temperature_normals, precip_normals, latitude, UnitSystem.metrics, ecoregion);
                // Climate.LandscapeAnnualPDSI[timeStepIndex] += (Future_MonthlyData[startYear + timeStep.Key][ecoregion.Index].PDSI / Climate.ModelCore.Ecoregions.Count);

                //Climate.TextLog.WriteLine("Calculated PDSI for Ecoregion {0}, timestep {1}, PDSI Year {2}; PDSI={3:0.00}.", ecoregion.Name, timeStepIndex, timeStep.Key, Future_MonthlyData[startYear + timeStep.Key][ecoregion.Index].PDSI);
                timeStepIndex++;

                WriteAnnualLog(ecoregion, startYear + timeStep.Key, annualClimateMonthly);
            }


        }

        private static void CalculateFWI(ClimateRecord[][] TimestepData, int year)
        {
            int maxtimestep = 12;
            if (future_allData_granularity == TemporalGranularity.Daily)
                maxtimestep = 365;
            int springStart = 60;
            int winterStart = 336;
            WindSpeedVelocity = -9999.0;
            WindAzimuth = -9999.0;
            double temperature = -9999.0;
            double precipitation = -9999.0;
            double rhSlopeAdjust = 77.68;
            double relativeHumidity = -9999.0;

            foreach (IEcoregion ecoregion in Climate.ModelCore.Ecoregions)
            {
                if (ecoregion.Active)
                {
                    // These are seed values for the beginning of the fire season
                    

                    // These are seed values for the beginning of the fire season
                    double FineFuelMoistureCode_yesterday = 85;
                    double DuffMoistureCode_yesterday = 6;
                    double DroughtCode_yesterday = 15;
                    //for (int month = 0; month < 12; month++)
                    for (int timestep = 0; timestep < maxtimestep; timestep++)
                    {
                        if(timestep >= springStart && timestep < winterStart)
                        {
                            temperature = (TimestepData[ecoregion.Index][timestep].AvgMaxTemp + TimestepData[ecoregion.Index][timestep].AvgMinTemp) / 2;
                            precipitation = TimestepData[ecoregion.Index][timestep].AvgPpt;
                            WindSpeedVelocity = TimestepData[ecoregion.Index][timestep].AvgWindSpeed;
                            WindAzimuth = TimestepData[ecoregion.Index][timestep].AvgWindDirection;
                            relativeHumidity = 100 * Math.Exp((rhSlopeAdjust * TimestepData[ecoregion.Index][timestep].AvgMinTemp) / (273.15 + TimestepData[ecoregion.Index][timestep].AvgMinTemp) - (rhSlopeAdjust * temperature) / (273.15 + temperature));

                            if (timestep != springStart) //for each day, this loop assigns yesterday's fire weather variables
                            {
                                FineFuelMoistureCode_yesterday = FineFuelMoistureCode;
                                DuffMoistureCode_yesterday = DuffMoistureCode;
                                DroughtCode_yesterday = DroughtCode;
                            }

                            double mo = Calculate_mo(FineFuelMoistureCode_yesterday);
                            double rf = Calculate_rf(precipitation);
                            double mr = Calculate_mr(mo, rf);
                            double Ed = Calculate_Ed(relativeHumidity, temperature);
                            double Ew = Calculate_Ew(relativeHumidity, temperature);
                            double ko = Calculate_ko(relativeHumidity, WindSpeedVelocity);
                            double kd = Calculate_kd(ko, temperature);
                            double kl = Calculate_kl(relativeHumidity, WindSpeedVelocity);
                            double kw = Calculate_kw(kl, temperature);
                            double m = Calculate_m(mo, Ed, kd, Ew, kw);
                            double re = Calculate_re(precipitation);
                            double Mo = Calculate_Mo(DuffMoistureCode_yesterday);
                            double b = Calculate_b(DuffMoistureCode_yesterday);
                            double Mr = Calculate_Mr(re, b, Mo);
                            double Pr = Calculate_Pr(Mr);
                            int month = Calculate_month(timestep);
                            double Le1 = Calculate_Le1(month);
                            double Le2 = Calculate_Le2(month);
                            double Le = Calculate_Le(Le1, Le2);
                            double K = Calculate_K(temperature, relativeHumidity, Le);
                            Calculate_DuffMoistureCode(precipitation, Pr, K, DuffMoistureCode_yesterday);
                            double rd = Calculate_rd(precipitation);
                            double Qo = Calculate_Qo(DroughtCode_yesterday);
                            double Qr = Calculate_Qr(Qo, rd);
                            double Dr = Calculate_Dr(Qr);
                            double Lf = Calculate_Lf(month);
                            double V = Calculate_V(temperature, Lf);
                            Calculate_DroughtCode(precipitation, Dr, V, DroughtCode_yesterday);
                            double WindFunction_ISI = Calculate_WindFunction_ISI(WindSpeedVelocity);
                            double FineFuelMoistureFunction_ISI = Calculate_FineFuelMoistureFunction_ISI(m);
                            double InitialSpreadIndex = Calculate_InitialSpreadIndex(WindFunction_ISI, FineFuelMoistureFunction_ISI);
                            Calculate_BuildUpIndex(DuffMoistureCode, DroughtCode);
                            double fD = Calculate_fD(BuildUpIndex);
                            double B = Calculate_B(InitialSpreadIndex, fD);
                            Calculate_FireWeatherIndex(B);
                            double I_scale = Calculate_I_scale(FireWeatherIndex);
                            double DSR = Calculate_DSR(FireWeatherIndex);
                            Calculate_FineFuelMoistureCode(m);

                            TimestepData[ecoregion.Index][timestep].AvgFWI = FireWeatherIndex;
                            //ModelCore.UI.WriteLine(string.Format("{0}", FireWeatherIndex));
                        }
                        else
                        {
                            TimestepData[ecoregion.Index][timestep].AvgFWI = 0;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Converts USGS Data to Standard Input climate Data and fill out the Future_AllData and/or Spinup_AllData
        /// </summary>
        /// 
        public static void ConvertFileFormat_FillOutAllData(String timeSeries, string filePath, string fileFormat, Climate.Phase climatePhase)
        {
            if (climatePhase == Climate.Phase.Future_Climate && timeSeries.Contains("Daily"))
                future_allData_granularity = TemporalGranularity.Daily;
                
            else if (climatePhase == Climate.Phase.Future_Climate && timeSeries.Contains("Monthly"))
                future_allData_granularity = TemporalGranularity.Monthly;

            else if (climatePhase == Climate.Phase.SpinUp_Climate && timeSeries.Contains("Daily"))
                spinup_allData_granularity = TemporalGranularity.Daily;

            else if (climatePhase == Climate.Phase.SpinUp_Climate && timeSeries.Contains("Monthly"))
                spinup_allData_granularity = TemporalGranularity.Monthly;

            if (timeSeries.Contains("Daily"))
                ClimateDataConvertor.Convert_USGS_to_ClimateData_FillAlldata(TemporalGranularity.Daily, filePath, fileFormat, climatePhase);
            
            else if (timeSeries.Contains("Monthly"))
                ClimateDataConvertor.Convert_USGS_to_ClimateData_FillAlldata(TemporalGranularity.Monthly, filePath, fileFormat, climatePhase);

            return;

        }
        //---------------------------------------------------------------------
        private static void WriteSpinupInputLog(ClimateRecord[][] TimestepData, int year)
        {
            int maxtimestep = 12;
            if (spinup_allData_granularity == TemporalGranularity.Daily)
                maxtimestep = 365;
            
            //spinup_allData.
            foreach (IEcoregion ecoregion in Climate.ModelCore.Ecoregions)
            {
                if (ecoregion.Active)
                {
                    //for (int month = 0; month < 12; month++)
                        for (int timestep = 0; timestep < maxtimestep; timestep++)
                        {
                        SpinupInputLog.Clear();
                        InputLog sil = new InputLog();

                        //sil.SimulationPeriod = period;
                        sil.Year = year;
                        sil.Timestep = timestep + 1;
                        sil.EcoregionName = ecoregion.Name;
                        sil.EcoregionIndex = ecoregion.Index;
                        sil.min_airtemp = TimestepData[ecoregion.Index][timestep].AvgMinTemp;
                        sil.max_airtemp = TimestepData[ecoregion.Index][timestep].AvgMaxTemp;
                        sil.std_temp = TimestepData[ecoregion.Index][timestep].StdDevTemp;
                        sil.ppt = TimestepData[ecoregion.Index][timestep].AvgPpt;
                        sil.std_ppt = TimestepData[ecoregion.Index][timestep].StdDevPpt;
                        sil.ndeposition = TimestepData[ecoregion.Index][timestep].AvgNDeposition;
                        //sil.co2 = TimestepData[ecoregion.Index][timestep].AvgCO2;
                        if (FireClimate.UsingFireClimate)
                        {
                            sil.FWI = TimestepData[ecoregion.Index][timestep].AvgFWI;
                        }


                        SpinupInputLog.AddObject(sil);
                        SpinupInputLog.WriteToFile();

                    }
                }
            }

        }

        //---------------------------------------------------------------------
        private static void WriteFutureInputLog(ClimateRecord[][] TimestepData, int year)
        {
            //spinup_allData.
            //CalculateFWI(ref TimestepData, year);
            int maxtimestep = 12;
            if (future_allData_granularity == TemporalGranularity.Daily)
                maxtimestep = 365;

            foreach (IEcoregion ecoregion in Climate.ModelCore.Ecoregions)
            {
                if (ecoregion.Active)
                {

                    //for (int month = 0; month < 12; month++)
                    for (int timestep = 0; timestep < maxtimestep; timestep++)
                    {
                        FutureInputLog.Clear();
                        InputLog fil = new InputLog();

                        //fil.SimulationPeriod = period;
                        fil.Year = year;
                        fil.Timestep = timestep + 1;
                        fil.EcoregionName = ecoregion.Name;
                        fil.EcoregionIndex = ecoregion.Index;
                        fil.min_airtemp = TimestepData[ecoregion.Index][timestep].AvgMinTemp;
                        fil.max_airtemp = TimestepData[ecoregion.Index][timestep].AvgMaxTemp;
                        fil.std_temp = TimestepData[ecoregion.Index][timestep].StdDevTemp;
                        fil.ppt = TimestepData[ecoregion.Index][timestep].AvgPpt;
                        fil.std_ppt = TimestepData[ecoregion.Index][timestep].StdDevPpt;
                        fil.winddirection = TimestepData[ecoregion.Index][timestep].AvgWindDirection;
                        fil.windspeed = TimestepData[ecoregion.Index][timestep].AvgWindSpeed;
                        fil.ndeposition = TimestepData[ecoregion.Index][timestep].AvgNDeposition;
                        //fil.co2 = TimestepData[ecoregion.Index][timestep].AvgCO2;
                        //if (FireClimate.UsingFireClimate)
                        //{
                        fil.FWI = TimestepData[ecoregion.Index][timestep].AvgFWI;
                        //}


                        FutureInputLog.AddObject(fil);
                        FutureInputLog.WriteToFile();

                    }
                }
            }

        }

        //---------------------------------------------------------------------
        private static void WriteAnnualLog(IEcoregion ecoregion, int year, AnnualClimate_Monthly annualClimateMonthly)
        {
            AnnualLog.Clear();
            AnnualLog al = new AnnualLog();

            //al.SimulationPeriod = TBD
            al.Time = year;
            al.EcoregionName = ecoregion.Name;
            al.EcoregionIndex = ecoregion.Index;
            al.BeginGrow = annualClimateMonthly.BeginGrowing;
            al.EndGrow = annualClimateMonthly.EndGrowing;
            al.TAP = annualClimateMonthly.TotalAnnualPrecip;
            al.MAT = annualClimateMonthly.MeanAnnualTemperature;
            al.PDSI = Future_MonthlyData[year][ecoregion.Index].PDSI;
            // VS: might need FWI in annual climate
            //al.FWI = Future_MonthlyData[year][ecoregion.Index].FWI;

            AnnualLog.AddObject(al);
            AnnualLog.WriteToFile();


        }


        private static double Calculate_mo(double FineFuelMoistureCode_yesterday)
        {
            double mo = 0;
            try
            {
                mo = 147.2 * (101.0 - FineFuelMoistureCode_yesterday) / (59.5 + FineFuelMoistureCode_yesterday);  //This used to be an explicit seed value for FFMC
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }


            return mo;
        }

        private static double Calculate_rf(double precipitation)
        {
            double rf = 0.0;

            try
            {
                rf = precipitation - 0.5;

                if (rf < 0)
                {
                    rf = 0.0;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return rf;
        }

        private static double Calculate_mr(double mo, double rf)
        {
            double mr = 0.0;
            try
            {
                if (mo <= 150.0)
                {

                    if (rf > 0)
                    {
                        mr = mo + 42.5 * rf * Math.Exp(-100.0 / (251.0 - mo)) * (1 - Math.Exp(-6.93 / rf));
                    }
                    else
                    {
                        mr = mo;
                    }
                }
                else
                {
                    if (rf > 0)
                    {
                        mr = mo + 42.5 * rf * Math.Exp(-100.0 / (251.0 - mo)) * (1 - Math.Exp(-6.93 / rf)) + 0.0015 * Math.Pow((mo - 150.0), 2) * Math.Pow(rf, 0.5);
                    }
                    else
                    {
                        mr = mo;
                    }
                }

                if (mr > 250)
                {
                    mr = 250;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return mr;
        }

        private static double Calculate_Ed(double relative_humidity, double temperature)
        {
            double Ed = 0.0;
            try
            {
                Ed = 0.942 * Math.Pow(relative_humidity, 0.679) + 11.0 * Math.Exp((relative_humidity - 100.0) / 10.0) + 0.18 * (21.1 - temperature) * (1.0 - Math.Exp(-0.115 * relative_humidity));
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Ed;
        }

        private static double Calculate_Ew(double relative_humidity, double temperature)
        {
            double Ew = 0.0;
            try
            {
                Ew = 0.618 * Math.Pow(relative_humidity, 0.753) + 10.0 * Math.Exp((relative_humidity - 100.0) / 10.0) + 0.18 * (21.1 - temperature) * (1.0 - Math.Exp(-0.115 * relative_humidity));                          //selfs
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Ew;
        }

        private static double Calculate_ko(double relative_humidity, double WindSpeedVelocity)
        {
            double ko = 0.0;
            try
            {
                ko = 0.424 * (1.0 - Math.Pow((relative_humidity / 100.0), 1.7)) + 0.0694 * Math.Pow(WindSpeedVelocity, 0.5) * (1.0 - Math.Pow((relative_humidity / 100.0), 8));
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return ko;
        }

        private static double Calculate_kd(double ko, double temperature)
        {
            double kd = 0.0;
            try
            {

                kd = ko * 0.581 * Math.Exp(0.0365 * temperature);
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return kd;
        }

        private static double Calculate_kl(double relative_humidity, double WindSpeedVelocity)
        {
            double kl = 0.0;
            try
            {
                kl = 0.424 * (1.0 - Math.Pow(((100.0 - relative_humidity) / 100.0), 1.7)) + 0.0694 * Math.Pow(WindSpeedVelocity, 0.5) * (1.0 - Math.Pow(((100.0 - relative_humidity) / 100.0), 8));
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return kl;
        }

        private static double Calculate_kw(double kl, double temperature)
        {
            double kw = 0.0;
            try
            {
                kw = kl * 0.581 * Math.Exp(0.0365 * temperature);
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return kw;
        }

        private static double Calculate_m(double mo, double Ed, double kd, double Ew, double kw)
        {
            double m = 0.0;
            try
            {
                if (mo > Ed)
                {
                    m = Ed + (mo - Ed) * Math.Pow(10.0, (-kd));
                }
                else
                {
                    if (mo < Ed)
                    {
                        if (mo < Ew)
                        {
                            m = Ew - (Ew - mo) * Math.Pow(10.0, (-kw));
                        }
                        else
                        {
                            m = mo;
                        }
                    }
                    else
                    {
                        m = mo;
                    }
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return m;
        }

        private static void Calculate_FineFuelMoistureCode(double m)
        {
            FineFuelMoistureCode = 0.0;

            try
            {
                FineFuelMoistureCode = 59.5 * (250.0 - m) / (147.2 + m);

                if (FineFuelMoistureCode > 100.0)
                {
                    FineFuelMoistureCode = 100.0;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return;
        }

        private static double Calculate_re(double precipitation)
        {
            double re = 0.0;
            try
            {
                if (precipitation > 1.5)
                {
                    re = 0.92 * precipitation - 1.27;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return re;
        }

        private static double Calculate_Mo(double DuffMoistureCode_yesterday)
        {
            double Mo = 0.0;
            try
            {
                Mo = 20.0 + Math.Exp(5.6348 - DuffMoistureCode_yesterday / 43.43);
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Mo;
        }

        private static double Calculate_b(double DuffMoistureCode_yesterday) //, int DMC_start
        {
            double b = 0.0;

            try
            {
                if (DuffMoistureCode_yesterday <= 33)
                {
                    b = 100 / (0.5 + 0.3 * DuffMoistureCode_yesterday);
                }

                else if (DuffMoistureCode_yesterday > 65)
                {
                    b = 6.2 * Math.Log(DuffMoistureCode_yesterday) - 17.2;
                }

                else
                {
                    b = 14.0 - 1.3 * Math.Log(DuffMoistureCode_yesterday);
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return b;
        }

        private static double Calculate_Mr(double re, double b, double Mo)
        {
            double Mr = 0.0;
            try
            {
                Mr = Mo + 1000.0 * re / (48.77 + b * re);
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Mr;
        }

        private static double Calculate_Pr(double Mr)
        {
            double Pr = 0.0;
            /* VS: Why the if statement
            if (day == 91)
            {
                Pr = 244.72 - 43.43 * Math.Log(Mr - 20.0);
                if (Pr < 0.0)
                {
                    Pr = 0.0;
                }
            }
            else
            {
               Pr = 244.72 - 43.43 * Math.Log(Mr - 20.0);

                if (Pr < 0.0)
                {
                    Pr = 0.0;
                }
            }
            */

            try
            {
                Pr = 244.72 - 43.43 * Math.Log(Mr - 20.0);
                if (Pr < 0.0)
                {
                    Pr = 0.0;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Pr;
        }

        private static int Calculate_month(int d)
        {
            int month = 0;

            try
            {
                if (d <= 31)
                {
                    month = 1;
                }
                else if (d > 31 && d <= 60)
                {
                    month = 2;
                }
                else if (d > 60 && d <= 91)
                {
                    month = 3;
                }
                else if (d > 91 && d <= 121)
                {
                    month = 4;
                }
                else if (d > 121 && d <= 152)
                {
                    month = 5;
                }
                else if (d > 152 && d <= 182)
                {
                    month = 6;
                }
                else if (d > 182 && d <= 213)
                {
                    month = 7;
                }
                else if (d > 213 && d <= 244)
                {
                    month = 8;
                }
                else if (d > 244 && d <= 274)
                {
                    month = 9;
                }
                else if (d > 274 && d <= 305)
                {
                    month = 10;
                }
                else if (d > 305 && d <= 335)
                {
                    month = 11;
                }

                else
                {
                    month = 12;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return month;
        }

        private static double Calculate_Le1(int month)
        {
            double Le1 = 0.0;

            try
            {
                if (month == 1)
                {
                    Le1 = 6.5;
                }
                else if (month == 2)
                {
                    Le1 = 7.5;
                }
                else if (month == 3)
                {
                    Le1 = 9.0;
                }
                else if (month == 4)
                {
                    Le1 = 12.8;
                }
                else if (month == 5)
                {
                    Le1 = 13.9;
                }
                else if (month == 6)
                {
                    Le1 = 13.9;
                }
                else if (month == 7)
                {
                    Le1 = 12.4;
                }
                else if (month == 8)
                {
                    Le1 = 10.9;
                }
                else
                {
                    Le1 = 0.0;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Le1;
        }

        private static double Calculate_Le2(int month)
        {
            double Le2 = 0.0;

            try
            {
                if (month == 9)
                {
                    Le2 = 9.2;
                }
                else if (month == 10)
                {
                    Le2 = 8.0;
                }
                else if (month == 11)
                {
                    Le2 = 7.0;
                }
                else if (month == 12)
                {
                    Le2 = 6.0;
                }

                else
                {
                    Le2 = 0.0;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Le2;
        }

        private static double Calculate_Le(double Le1, double Le2)
        {
            double Le = 0.0;

            try
            {
                if (Le1 == 0.0)
                {
                    Le = Le2;
                }
                else
                {
                    Le = Le1;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Le;
        }

        private static double Calculate_K(double temperature, double relative_humidity, double Le)
        {
            double K = 0.0;

            try
            {
                if (temperature < -1.1)
                {
                    K = 0.0;
                }

                else
                {
                    K = 1.894 * (temperature + 1.1) * (100.0 - relative_humidity) * Le * Math.Pow(10.0, -6.0);
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return K;
        }

        private static double Calculate_DuffMoistureCode(double precipitation, double Pr, double K, double DuffMoistureCode_yesterday) //int spring_start, int winter_start, double DMC_start
        {
            try
            {
                if (precipitation > 1.5)
                {
                    DuffMoistureCode = Pr + 100.0 * K;
                }

                else
                {
                    DuffMoistureCode = DuffMoistureCode_yesterday + 100.0 * K;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return DuffMoistureCode;
        }

        private static double Calculate_rd(double precipitation)
        {
            double rd = 0.0;

            try
            {
                if (precipitation > 2.8)
                {
                    rd = 0.83 * precipitation - 1.27;
                }

                else
                {
                    rd = 0;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return rd;
        }

        private static double Calculate_Qo(double DroughtCode_yesterday) //, int DC_start
        {
            double Qo = 0.0;

            try
            {
                Qo = 800.0 * Math.Exp(-DroughtCode_yesterday / 400.0);
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Qo;
        }

        private static double Calculate_Qr(double Qo, double rd)
        {
            double Qr = 0.0;

            try
            {
                Qr = Qo + 3.937 * rd;
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Qr;
        }

        private static double Calculate_Dr(double Qr)
        {
            double Dr = 0.0;

            try
            {
                Dr = 400.0 * Math.Log(800.0 / Qr);

                if (Dr < 0)
                {
                    Dr = 0.0;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Dr;
        }

        private static double Calculate_Lf(int month)
        {
            double Lf = 0.0;
            try
            {

                if (month <= 3)
                {
                    Lf = 1.6;
                }
                else if (month == 4.0)
                {
                    Lf = 0.9;
                }
                else if (month == 5.0)
                {
                    Lf = 3.8;
                }
                else if (month == 6.0)
                {
                    Lf = 5.8;
                }
                else if (month == 7.0)
                {
                    Lf = 6.4;
                }
                else if (month == 8.0)
                {
                    Lf = 5.0;
                }
                else if (month == 9.0)
                {
                    Lf = 2.4;
                }
                else if (month == 10.0)
                {
                    Lf = 0.4;
                }
                else
                {
                    Lf = -1.6;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Lf;
        }

        private static double Calculate_V(double temperature, double Lf)
        {
            double V = 0.0;

            try
            {
                if (temperature < -2.8)
                {
                    V = 0.36 * (-2.8 + 2.8) + Lf;
                }
                else
                {
                    V = 0.36 * (temperature + 2.8) + Lf;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return V;
        }

        private static double Calculate_DroughtCode(double precipitation, /*int spring_start,*/ double Dr, double V, double DroughtCode_yesterday)
        {
            //if (d == spring_start)
            //{

            try
            {
                if (precipitation > 2.8)
                {
                    DroughtCode = Dr + 0.5 * V;
                }
                else
                {
                    DroughtCode = DroughtCode_yesterday + 0.5 * V;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }
            //}
            /* VS: Why? does the same thing
            else
            {
                if (precipitation > 2.8)
                {
                    DroughtCode = Dr + 0.5 * V;
                }
                else
                {
                    DroughtCode = DroughtCode_yesterday + 0.5 * V;
                }
            }
            */

            return DroughtCode;
        }

        private static double Calculate_WindFunction_ISI(double WindSpeedVelocity)
        {
            double WindFunction_ISI = 0.0;
            try
            {

                WindFunction_ISI = Math.Exp(0.05039 * WindSpeedVelocity);
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return WindFunction_ISI;
        }

        private static double Calculate_FineFuelMoistureFunction_ISI(double m)
        {
            double FineFuelMoistureFunction_ISI = 0.0;

            try
            {
                FineFuelMoistureFunction_ISI = 91.9 * Math.Exp(-0.1386 * m) * (1.0 + Math.Pow(m, 5.31) / (4.93 * Math.Pow(10.0, 7.0)));

            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }
            return FineFuelMoistureFunction_ISI;
        }

        private static double Calculate_InitialSpreadIndex(double WindFunction_ISI, double FineFuelMoistureFunction_ISI)
        {
            double InitialSpreadIndex = 0.0;

            try
            {
                InitialSpreadIndex = 0.208 * WindFunction_ISI * FineFuelMoistureFunction_ISI;
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return InitialSpreadIndex;
        }

        private static double Calculate_BuildUpIndex(double DuffMoistureCode, double DroughtCode)  //int spring_start, int winter_start,
        {
            try
            {
                if (DuffMoistureCode <= (0.4 * DroughtCode))
                {
                    BuildUpIndex = 0.8 * DuffMoistureCode * DroughtCode / (DuffMoistureCode + 0.4 * DroughtCode);
                }
                else
                {
                    BuildUpIndex = DuffMoistureCode - (1.0 - 0.8 * DroughtCode / (DuffMoistureCode + 0.4 * DroughtCode)) * (0.92 + (0.0114 * Math.Pow(DuffMoistureCode, 1.7)));
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return BuildUpIndex;
        }

        private static double Calculate_fD(double BuildUpIndex)
        {
            double fD = 0.0;

            try
            {
                if (BuildUpIndex <= 80.0)
                {
                    fD = 0.626 * Math.Pow(BuildUpIndex, 0.809) + 2.0;
                }
                else
                {
                    fD = 1000.0 / (25.0 + 108.64 * Math.Exp(-0.023 * BuildUpIndex));
                }

            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }
            return fD;
        }

        private static double Calculate_B(double InitialSpreadIndex, double fD)
        {
            double B = 0.0;

            try
            {
                B = 0.1 * InitialSpreadIndex * fD;
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return B;
        }

        private static double Calculate_FireWeatherIndex(double B) //int spring_start, int winter_start, 
        {
            FireWeatherIndex = 0.0;

            try
            {
                if (B > 1.0)
                {
                    FireWeatherIndex = Math.Exp(2.72 * Math.Pow((0.434 * Math.Log(B)), 0.647));
                }
                else
                {
                    FireWeatherIndex = B;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return FireWeatherIndex;
        }

        private static double Calculate_I_scale(double FireWeatherIndex)
        {
            double I_scale = 0.0;

            try
            {
                I_scale = (1.0 / 0.289) * (Math.Exp(0.98 * (Math.Pow(Math.Log(FireWeatherIndex), 1.546))));
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return I_scale;
        }

        private static double Calculate_DSR(double FireWeatherIndex)
        {
            double DSR = 0.0;

            try
            {
                DSR = 0.0272 * Math.Pow(FireWeatherIndex, 1.77);
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return DSR;
        }
    }

}






























////////////////////////////////////FIRE STUFF









