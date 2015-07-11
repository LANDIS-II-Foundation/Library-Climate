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
            
            ModelCore.UI.WriteLine("   Loading spin-up weather data from file {0} ...", configParameters.SpinUpClimateFile);
            Climate.ConvertFileFormat_FillOutAllData(configParameters.SpinUpClimateTimeSeries, configParameters.SpinUpClimateFile, configParameters.SpinUpClimateFileFormat, Climate.Phase.SpinUp_Climate);

            ModelCore.UI.WriteLine("   Loading future weather data from file {0} ...", configParameters.ClimateFile);
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

            // write input data to the log
            foreach (KeyValuePair<int, ClimateRecord[][]> timeStep in future_allData)
            {
                Climate.WriteFutureInputLog(timeStep.Value, timeStep.Key); //, future_allData_granularity);
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


        public static void GenerateEcoregionClimateData(IEcoregion ecoregion, int startYear, double latitude, double fieldCapacity, double wiltingPoint)
        {
                                    
            // JM:  these next three lines are not currently used, but may need to be modified if used:
            //int numberOftimeSteps = Climate.ModelCore.EndTime - Climate.ModelCore.StartTime;
            //annualPDSI = new double[Climate.ModelCore.Ecoregions.Count, future_allData.Count]; 
            //landscapeAnnualPDSI = new double[future_allData.Count]; 
            double[] temperature_normals = new double[12];
            double[] precip_normals = new double[12];
            
            double availableWaterCapacity = fieldCapacity - wiltingPoint;

            Climate.ModelCore.UI.WriteLine("   Core.StartTime = {0}, Core.EndTime = {1}.", ModelCore.StartTime, ModelCore.EndTime);
            //Climate.ModelCore.UI.WriteLine("   Climate.LandscapeAnnualPDSI.Length = {0}.", Climate.LandscapeAnnualPDSI.Length);

            //First Calculate Climate Normals from Spin-up data
            int timeStepIndex = 0;
            foreach (KeyValuePair<int, AnnualClimate_Monthly[]> timeStep in Spinup_MonthlyData)
            {

                //Climate.ModelCore.UI.WriteLine("  Calculating Weather for SPINUP: timeStep = {0}, actualYear = {1}", timeStep.Key, startYear + timeStep.Key);
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
                //Climate.ModelCore.UI.WriteLine("Month = {0}, Original Monthly T normal = {1}", mo, month_Temp_normal[mo]);

            }
            
            timeStepIndex = 0;

            PDSI_Calculator.InitializeEcoregion_PDSI(temperature_normals, precip_normals, availableWaterCapacity, latitude, UnitSystem.metrics, ecoregion);

            foreach (KeyValuePair<int, AnnualClimate_Monthly[]> timeStep in Future_MonthlyData)
            {
                //Climate.ModelCore.UI.WriteLine("  Completed calculations for Future_Climate: TimeStepYear = {0}, actualYear = {1}", timeStep.Key, startYear + timeStep.Key);
                AnnualClimate_Monthly annualClimateMonthly = new AnnualClimate_Monthly(ecoregion, latitude, Climate.Phase.Future_Climate, timeStep.Key, timeStepIndex);
                Future_MonthlyData[startYear + timeStep.Key][ecoregion.Index] = annualClimateMonthly;

                // Next calculate PSDI for the future data
<<<<<<< .mine
                Future_MonthlyData[startYear + timeStep.Key][ecoregion.Index].PDSI = PDSI_Calculator.CalculateEcoregion_PDSI(annualClimateMonthly, temperature_normals, precip_normals, availableWaterCapacity, latitude, UnitSystem.metrics, ecoregion);
                //Future_MonthlyData[startYear + timeStep.Key][ecoregion.Index].PDSI = PDSI_Calculator.CalculateEcoregion_PDSI(annualClimateMonthly, temperature_normals, precip_normals, latitude, UnitSystem.metrics, ecoregion);
=======
               // Future_MonthlyData[startYear + timeStep.Key][ecoregion.Index].PDSI = PDSI_Calculator.CalculateEcoregion_PDSI(annualClimateMonthly, temperature_normals, availableWaterCapacity, latitude, UnitSystem.metrics, ecoregion);
>>>>>>> .r3035
                // Climate.LandscapeAnnualPDSI[timeStepIndex] += (Future_MonthlyData[startYear + timeStep.Key][ecoregion.Index].PDSI / Climate.ModelCore.Ecoregions.Count);

                //Climate.ModelCore.UI.WriteLine("Calculated PDSI for Ecoregion {0}, timestep {1}, PDSI Year {2}; PDSI={3:0.00}.", ecoregion.Name, timeStepIndex, timeStep.Key, Future_MonthlyData[startYear + timeStep.Key][ecoregion.Index].PDSI);
                timeStepIndex++;

                WriteAnnualLog(ecoregion, startYear + timeStep.Key, annualClimateMonthly);
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

            AnnualLog.AddObject(al);
            AnnualLog.WriteToFile();


        }


    }

}









