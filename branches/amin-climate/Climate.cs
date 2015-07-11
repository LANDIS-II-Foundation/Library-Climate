//  Copyright: Portland State University 2009-2014
//  Authors:  Robert M. Scheller, Amin Almassian

using Landis.Core;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;
using Landis.Library.Metadata;


namespace Landis.Library.Climate
{

    public class Climate
    {


        private static TemporalGranularity future_allData_granularity;
        private static TemporalGranularity spinup_allData_granularity;
        private static Dictionary<int, IClimateRecord[,]> future_allData;
        private static Dictionary<int, IClimateRecord[,]> spinup_allData;
        private static int[] randSelectedTimeSteps_future;
        private static int[] randSelectedTimeSteps_spinup;
        //private static IClimateRecord[,] timestepData;
        private static ICore modelCore;
        //private static bool flag;
        private static IInputParameters configParameters;
        //internal static Dictionary<int, IClimateRecord[,]> avgEcoClimate_future_cache;
        //internal static Dictionary<int, IClimateRecord[,]> avgEcoClimate_spinup_cache;

        //private static System.Data.DataTable annualPDSI;
        private static double[,] annualPDSI;
        private static double[] landscapeAnnualPDSI;

        public static MetadataTable<PDSI_Log> PdsiLog;
        public static MetadataTable<MonthlyLog> MonthlyLog;

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
        public static Dictionary<int, IClimateRecord[,]> Future_AllData 
        {
            get
            {
                return future_allData;
            }
        }
        public static Dictionary<int, IClimateRecord[,]> Spinup_AllData
        {
            get
            {
                return spinup_allData;
            }
        }

        public static int[] RandSelectedTimeSteps_future { get { return randSelectedTimeSteps_future; } }
        public static int[] RandSelectedTimeSteps_spinup { get { return randSelectedTimeSteps_spinup; } }

        //---------------------------------------------------------------------
        //public static IClimateRecord[,] TimestepData
        //{
        //    get
        //    {
        //        return timestepData;
        //    }
        //    set
        //    {
        //        timestepData = value;
        //    }
        //}
        //---------------------------------------------------------------------
        //public static bool Flag
        //{
        //    get
        //    {
        //        return flag;
        //    }
        //    set
        //    {
        //        flag = value;
        //    }
        //}

        //------------------------------------------------------------------------
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
        private static void Write(IClimateRecord[,] TimestepData, int year, string period)
        {

            //spinup_allData.
            foreach (IEcoregion ecoregion in Climate.ModelCore.Ecoregions)
            {
                if (ecoregion.Active)
                {
                    for (int month = 0; month < 12; month++)
                    {
                        MonthlyLog.Clear();
                        MonthlyLog ml = new MonthlyLog();

                        ml.SimulationPeriod = period;
                        ml.Time = year;
                        ml.Month = month + 1;
                        ml.EcoregionName = ecoregion.Name;
                        ml.EcoregionIndex = ecoregion.Index;
                        ml.min_airtemp = TimestepData[ecoregion.Index, month].AvgMinTemp;
                        ml.max_airtemp = TimestepData[ecoregion.Index, month].AvgMaxTemp;
                        ml.std_temp = TimestepData[ecoregion.Index, month].StdDevTemp;
                        ml.ppt = TimestepData[ecoregion.Index, month].AvgPpt;
                        ml.std_ppt = TimestepData[ecoregion.Index, month].StdDevPpt;

                        MonthlyLog.AddObject(ml);
                        MonthlyLog.WriteToFile();

                    }
                }
            }

        }
        //---------------------------------------------------------------------
        public static void Initialize(string climateConfigFilename, bool writeOutput, ICore mCore)
        {
            InputParametersParser inParamsParser = new InputParametersParser();
            configParameters = Landis.Data.Load<IInputParameters>(climateConfigFilename, inParamsParser);

            modelCore = mCore;
            MetadataHandler.InitializeMetadata(1, modelCore);

            ModelCore.UI.WriteLine("   Loading weather data ...");
            Climate.future_allData = new Dictionary<int, IClimateRecord[,]>();
            Climate.spinup_allData = new Dictionary<int, IClimateRecord[,]>();

            Future_MonthlyData = new Dictionary<int, AnnualClimate_Monthly[]>();
            Spinup_MonthlyData = new Dictionary<int, AnnualClimate_Monthly[]>();
            Future_DailyData = new Dictionary<int, AnnualClimate_Daily[]>();
            Spinup_DailyData = new Dictionary<int, AnnualClimate_Daily[]>();
            LandscapeAnnualPDSI = new double[Climate.ModelCore.EndTime - Climate.ModelCore.StartTime + 1];

            ModelCore.UI.WriteLine("   Loading spin-up weather data from file {0} ...", configParameters.SpinUpClimateFile);
            Climate.ConvertFileFormat_FillOutAllData(configParameters.SpinUpClimateTimeSeries, configParameters.SpinUpClimateFile, configParameters.SpinUpClimateFileFormat, Climate.Phase.SpinUp_Climate);

            ModelCore.UI.WriteLine("   Loading future weather data from file {0} ...", configParameters.ClimateFile);
            Climate.ConvertFileFormat_FillOutAllData(configParameters.ClimateTimeSeries, configParameters.ClimateFile, configParameters.ClimateFileFormat, Climate.Phase.Future_Climate);
            

                //string climateOption = Climate.ConfigParameters.ClimateTimeSeries;
                //if (this.climatePhase == Climate.Phase.SpinUp_Climate)
                //    climateOption = Climate.ConfigParameters.SpinUpClimateTimeSeries;

                //switch (climateOption)
                //{
                //    case "MonthlyAverage":
                //        {
                //            break;
                //        }
                //    case "MonthlyRandom":
                //        {
                //            break;
                //        }
                //    case "DailyHistRandom":
                //        {
                //            break;
                //        }
                //    case "DailyHistAverage":
                //        {
                //            return;
                //        }
                //    case "MonthlyStandard":
                //        {
                //            break;
                //        }
                //    case "DailyGCM":
                //        {
                //        }
                //    case "MonthlyGCM":
                //        {
                //            break;
                //        }
                //    default:
                //        throw new ApplicationException(String.Format("Unknown Climate Time Series: {}", climateOption));

                //}


            if (Climate.ConfigParameters.ClimateTimeSeries.ToLower().Contains("random")) 
            {
                Climate.randSelectedTimeSteps_future = new int[Climate.future_allData.Count];//should be future_allData.Count or it needs to be different?
                for (int i = 0; i < Climate.future_allData.Count; i++)
                {
                    Climate.randSelectedTimeSteps_future[i] = (int)Math.Round(Climate.ModelCore.GenerateUniform() * (Climate.future_allData.Count - 1));
                }

            }

            if (Climate.ConfigParameters.SpinUpClimateTimeSeries.ToLower().Contains("random"))
            {

                //int maxSpeciesAge = modelCore.Species.Max(sp => sp.Longevity);
                int maxSpeciesAge = 0;
                foreach (ISpecies sp in ModelCore.Species)
                {
                    if (sp.Longevity > maxSpeciesAge)
                        maxSpeciesAge = sp.Longevity;
                }

                Climate.randSelectedTimeSteps_spinup = new int[maxSpeciesAge]; 
                for (int i = 0; i < maxSpeciesAge; i++)
                    Climate.randSelectedTimeSteps_spinup[i] = (int)Math.Round(Climate.ModelCore.GenerateUniform() * (Climate.spinup_allData.Count - 1));

            }
            foreach (KeyValuePair<int, IClimateRecord[,]> timeStep in spinup_allData)
            {
                //Climate.TimestepData = timeStep.Value;
                IClimateRecord[,] timestepData = timeStep.Value;
                int year = timeStep.Key;
                //Write(timestepData, year, "SpinUp");
                
                Spinup_MonthlyData.Add(timeStep.Key, new AnnualClimate_Monthly[modelCore.Ecoregions.Count]);  
                Spinup_DailyData.Add(timeStep.Key, new AnnualClimate_Daily[modelCore.Ecoregions.Count]);
                Climate.Write(timestepData, timeStep.Key, Climate.Phase.SpinUp_Climate.ToString()); 
            }
            foreach (KeyValuePair<int, IClimateRecord[,]> timeStep in future_allData)
            {
                //Climate.TimestepData = timeStep.Value;
                IClimateRecord[,] timestepData = timeStep.Value;
                int year = timeStep.Key;
                //Write(timestepData, year, "Future");
                
                Future_MonthlyData.Add(timeStep.Key, new AnnualClimate_Monthly[modelCore.Ecoregions.Count]);  
                Future_DailyData.Add(timeStep.Key, new AnnualClimate_Daily[modelCore.Ecoregions.Count]);
                Climate.Write(timestepData, timeStep.Key, Climate.Phase.Future_Climate.ToString());
            }

        }

        //public static void GenerateClimate_GetPDSI(int startYear, int endYear, int latitude, double fieldCapacity, double wiltingPoint)
        //{
        //    string outputFilePath = @"PDSI_BaseBDA_Genrated_Climate.csv";
        //    File.WriteAllText(outputFilePath, String.Empty);

        //    foreach (IEcoregion ecoregion in Climate.ModelCore.Ecoregions)
        //    {
        //        AnnualClimate_Monthly[] acs;
        //        int numOfYears = endYear - startYear + 1;
        //        acs = new AnnualClimate_Monthly[numOfYears];

        //        //foreach time step it should be called

        //        for (int i = startYear; i <= endYear; i++)
        //        {
        //            acs[i - startYear] = new AnnualClimate_Monthly(ecoregion, 0, latitude); // Latitude should be given
        //            //Climate.ModelCore.UI.WriteLine(ac.MonthlyTemp[0].ToString() + "\n");
        //            //Climate.ModelCore.UI.WriteLine(ac.MonthlyPrecip[0].ToString() + "\n");
        //        }



        //        double[] mon_T_normal = new double[12];//new double[12] { 19.693, 23.849, 34.988, 49.082, 60.467, 70.074, 75.505, 73.478, 64.484, 52.634, 36.201, 24.267 };
        //        IClimateRecord[] climateRecs = new ClimateRecord[12];

        //        //If timestep is 0 then calculate otherwise get the mon_T_normal for timestep 0

        //        Climate.TimestepData = future_allData[0];
        //        for (int mo = 0; mo < 12; mo++)
        //        {
        //            climateRecs[mo] = Climate.TimestepData[ecoregion.Index, mo];

        //            mon_T_normal[mo] = (climateRecs[mo].AvgMinTemp + climateRecs[mo].AvgMinTemp) / 2;
        //        }
        //        double AWC = fieldCapacity - wiltingPoint;
        //        //double latitude = Landis.Extension.Succession.Century.EcoregionData.Latitude[ecoregion];
        //        new PDSI_Calculator().CalculatePDSI(acs, mon_T_normal, AWC, latitude, /*outputFilePath,*/ UnitSystem.metrics);
        //    }
        //}
        //public static void GenerateEcoregionClimateData()
        //{
        //}

        public static void GenerateEcoregionClimateData(IEcoregion ecoregion, int startYear, double latitude, double fieldCapacity, double wiltingPoint)
        {

            Climate.ModelCore.UI.WriteLine("  Generating Ecoregion Climate Data for ecoregion = {0}.", ecoregion.Name);
            
            int numberOftimeSteps = Climate.ModelCore.EndTime - Climate.ModelCore.StartTime;
            annualPDSI = new double[Climate.ModelCore.Ecoregions.Count, future_allData.Count]; //numberOftimeSteps + 1];
            landscapeAnnualPDSI = new double[future_allData.Count]; //numberOftimeSteps+1];
            double[] temperature_normals = new double[12];
            
            double availableWaterCapacity = fieldCapacity - wiltingPoint;


            //Climate.ModelCore.UI.WriteLine("   Latitude = {0}, Available Water = {1}.", latitude, availableWaterCapacity);

            //IClimateRecord[] climateRecs = new ClimateRecord[12];
            //int minimumTime = 5000;
            
            
            //Firt Calculate Climate Normals from Spin-up data
            foreach (KeyValuePair<int, IClimateRecord[,]> timeStep in spinup_allData)
            {

                //Climate.ModelCore.UI.WriteLine("  Calculating Weather for SPINUP Year = {0}.", timeStep.Key);
                AnnualClimate_Monthly annualClimateMonthly = new AnnualClimate_Monthly(ecoregion, startYear + timeStep.Key, latitude, Climate.Phase.SpinUp_Climate, timeStep.Key); 
                Spinup_MonthlyData[startYear + timeStep.Key][ecoregion.Index] = annualClimateMonthly;

                for (int mo = 0; mo < 12; mo++)
                {
                    temperature_normals[mo] += annualClimateMonthly.MonthlyTemp[mo]; 
                }
            }

            // Calculate AVERAGE T normal.
            for (int mo = 0; mo < 12; mo++)
            {
                temperature_normals[mo] /= (double)spinup_allData.Count;
                //Climate.ModelCore.UI.WriteLine("Month = {0}, Original Monthly T normal = {1}", mo, month_Temp_normal[mo]);

            }
            
            int timestepIndex = 0;

            // Next calculate PSDI for the future data
            foreach (KeyValuePair<int, AnnualClimate_Monthly[]> timeStep in Future_MonthlyData)
            //foreach (KeyValuePair<int, IClimateRecord[,]> timeStep in future_allData)
            {
                //if (timeStep.Key < minimumTime)
                //    minimumTime = timeStep.Key;

                //if (timestepIndex > numberOftimeSteps)
                //    break;

                //Climate.ModelCore.UI.WriteLine("  Calculating Weather for FUTURE Year = {0}.", timeStep.Key);
                AnnualClimate_Monthly annualClimateMonthly = new AnnualClimate_Monthly(ecoregion, startYear + timeStep.Key, latitude, Climate.Phase.Future_Climate, timeStep.Key);
                Future_MonthlyData[startYear + timeStep.Key][ecoregion.Index] = annualClimateMonthly;

                Future_MonthlyData[startYear + timeStep.Key][ecoregion.Index].PDSI = PDSI_Calculator.CalculatePDSI(annualClimateMonthly, temperature_normals, availableWaterCapacity, latitude, UnitSystem.metrics, ecoregion);
                Climate.LandscapeAnnualPDSI[timestepIndex] += (Future_MonthlyData[startYear + timeStep.Key][ecoregion.Index].PDSI / Climate.ModelCore.Ecoregions.Count);

                //Climate.ModelCore.UI.WriteLine("Calculated PDSI for Ecoregion {0}, timestep {1}, PDSI Year {2}; PDSI={3:0.00}.", ecoregion.Name, timestepIndex, timeStep.Key, PDSI);
                timestepIndex++;
            }

        }

        //public static void GetPDSI_Test()
        //{
        //    IEcoregion ecoregion = Climate.ModelCore.Ecoregions[0];
        //    //here:
        //    string outputFilePath = @"C:\Program Files\LANDIS-II\v6\examples\base-BDA_1\PDSI_BaseBDA_Test2.csv";
        //    File.WriteAllText(outputFilePath, String.Empty);
        //    int startYear = 1893, endYear = 1897;
        //    AnnualClimate_Monthly[] acs;
        //    if (endYear > startYear)
        //    {
        //        int numOfYears = endYear - startYear + 1;
        //        acs = new AnnualClimate_Monthly[numOfYears];

        //        double[] mon_T_normal = new double[12] { 19.693, 23.849, 34.988, 49.082, 60.467, 70.074, 75.505, 73.478, 64.484, 52.634, 36.201, 24.267 };
        //        IClimateRecord[] climateRecs = new ClimateRecord[12];

        //        //Climate.TimestepData = allData[0];
        //        //for (int mo = 0; mo < 12; mo++)
        //        //{
        //        //    climateRecs[mo] = Climate.TimestepData[ecoregion.Index, mo];
        //        //    //mon_T_normal[mo] = (climateRecs[mo].AvgMinTemp + climateRecs[mo].AvgMinTemp) / 2;
        //        //}

        //        acs[0] = new AnnualClimate_Monthly(ecoregion, 1893, 0);
        //        ((AnnualClimate_Monthly)acs[0]).MonthlyTemp = new double[] { 14.371, 14.000, 26.435, 44.250, 54.645, 70.683, 73.355, 69.323, 63.600, 48.806, 32.867, 19.161 };
        //        //acs[0].MonthlyPrecip = new double[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
        //        ((AnnualClimate_Monthly)acs[0]).MonthlyPrecip = new double[] { 0.610, 1.500, 1.730, 4.050, 1.950, 0.790, 3.020, 2.570, 1.430, 0.850, 1.260, 2.350 };

        //        acs[1] = new AnnualClimate_Monthly(ecoregion, 1894, 0);
        //        ((AnnualClimate_Monthly)acs[1]).MonthlyTemp = new double[] { 12.705, 14.979, 37.984, 49.700, 61.209, 71.463, 77.935, 74.312, 65.283, 51.516, 34.767, 29.548 };
        //        ((AnnualClimate_Monthly)acs[1]).MonthlyPrecip = new double[] { 0.700, 0.550, 0.580, 4.240, 2.430, 1.150, 0.580, 1.480, 0.550, 1.760, 0.050, 1.000 };

        //        acs[2] = new AnnualClimate_Monthly(ecoregion, 1895, 0);
        //        ((AnnualClimate_Monthly)acs[2]).MonthlyTemp = new double[] { 12.519, 17.964, 33.994, 54.506, 60.411, 66.172, 70.548, 69.622, 65.288, 44.795, 32.433, 23.333 };
        //        ((AnnualClimate_Monthly)acs[2]).MonthlyPrecip = new double[] { 0.650, 0.540, 0.520, 3.980, 2.380, 6.240, 2.320, 3.920, 4.770, 0.060, 1.040, 0.000 };

        //        acs[3] = new AnnualClimate_Monthly(ecoregion, 1896, 0);
        //        ((AnnualClimate_Monthly)acs[3]).MonthlyTemp = new double[] { 23.258, 27.397, 26.425, 48.833, 62.790, 68.054, 71.365, 70.677, 57.991, 46.355, 21.154, 28.597 };
        //        ((AnnualClimate_Monthly)acs[3]).MonthlyPrecip = new double[] { 0.250, 0.270, 1.670, 5.680, 6.240, 7.740, 5.550, 1.660, 1.810, 3.230, 3.850, 0.230 };

        //        acs[4] = new AnnualClimate_Monthly(ecoregion, 1897, 0);
        //        ((AnnualClimate_Monthly)acs[4]).MonthlyTemp = new double[] { 13.758, 20.179, 26.613, 46.700, 59.016, 66.533, 74.032, 67.928, 71.617, 54.613, 32.450, 18.686 };
        //        ((AnnualClimate_Monthly)acs[4]).MonthlyPrecip = new double[] { 2.500, 0.540, 3.010, 4.480, 0.980, 5.820, 3.780, 1.600, 1.010, 1.940, 0.910, 2.950 };



        //        //for (int i = startYear; i <= endYear; i++)
        //        //{
        //        //    acs[i - startYear] = new AnnualClimate(ecoregion, i, 0); // Latitude should be given
        //        //    //Climate.ModelCore.UI.WriteLine(ac.MonthlyTemp[0].ToString() + "\n");
        //        //    //Climate.ModelCore.UI.WriteLine(ac.MonthlyPrecip[0].ToString() + "\n");
        //        //}



        //        //for (int mo = 0; mo < 12; mo++)
        //        //{
        //        //    climateRecs[mo] = Climate.TimestepData[ecoregion.Index, mo];
        //        //    mon_T_normal[mo] = (climateRecs[mo].AvgMinTemp + climateRecs[mo].AvgMinTemp) / 2;
        //        //}

        //        double AWC = 0.3;//Landis.Extension.Succession.Century.EcoregionData.FieldCapacity[ecoregion] - Landis.Extension.Succession.Century.EcoregionData.WiltingPoint[ecoregion];
        //        double latitude = 42.60;//Landis.Extension.Succession.Century.EcoregionData.Latitude[ecoregion];
        //        new PDSI_Calculator().CalculatePDSI(acs, mon_T_normal, AWC, latitude, /*outputFilePath,*/ UnitSystem.USCustomaryUnits);

        //    }


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

                //string readableFile = "";
            //if (timeSeries.Contains("MonthlyStandard"))
            //{
            //    ModelCore.UI.WriteLine("Loading from file with Monthly Standard format...\n"); 
            //    if (future_allData_granularity == TemporalGranularity.Daily)
            //    {
            //        ClimateParser parser = new ClimateParser();
            //        future_allData = Landis.Data.Load<Dictionary<int, IClimateRecord[,]>>(filePath, parser);
            //    }
            //    else if (future_allData_granularity == TemporalGranularity.Monthly)
            //    {
            //        ClimateParser spinup_parser = new ClimateParser();
            //        spinup_allData = Landis.Data.Load<Dictionary<int, IClimateRecord[,]>>(filePath, spinup_parser);
            //    }
            //    return; // filePath;
            //}

            //else if (timeSeries.Contains("Average") || timeSeries.Contains("Random"))
            //{
            //    if (timeSeries.Contains("Daily"))
            //        //return readableFile = 
            //        ClimateDataConvertor.Convert_USGS_to_ClimateData_FillAlldata(TemporalGranularity.Daily, filePath, fileFormat, climatePhase);
            //    else if (timeSeries.Contains("Monthly"))
            //        //return readableFile = 
            //        ClimateDataConvertor.Convert_USGS_to_ClimateData_FillAlldata(TemporalGranularity.Monthly, filePath, fileFormat, climatePhase);

            //}

            //else if (timeSeries.Contains("MonthlyAverage"))//AverageMonthly
            //{
            //    //return readableFile = 
            //        ClimateDataConvertor.Convert_USGS_to_ClimateData_FillAlldata(TemporalGranularity.Monthly, filePath, fileFormat, climatePhase);
            //}

            ////else if (timeSeries.Contains("Random"))
            ////{
            ////    if (timeSeries.Contains("Daily"))
            ////        return readableFile = Landis.Library.Climate.ClimateDataConvertor.Convert_USGS_to_ClimateData_FillAlldata(TemporalGranularity.Daily, File, fileFormat, climatePhase);
            ////    else if (timeSeries.Contains("Monthly"))
            ////        return readableFile = Landis.Library.Climate.ClimateDataConvertor.Convert_USGS_to_ClimateData_FillAlldata(TemporalGranularity.Monthly, File, fileFormat, climatePhase);
            ////}

            //else if (timeSeries.Contains("DailyGCM"))
            //{
            //    //return readableFile = 
            //        ClimateDataConvertor.Convert_USGS_to_ClimateData_FillAlldata(TemporalGranularity.Daily, filePath, fileFormat, climatePhase);
            //}

            //else if (timeSeries.Contains("MonthlyGCM"))
            //{
            //    //return readableFile = 
            //        ClimateDataConvertor.Convert_USGS_to_ClimateData_FillAlldata(TemporalGranularity.Monthly, filePath, fileFormat, climatePhase);
            //}

            //else
            //{
            //    ModelCore.UI.WriteLine("Error in converting input-climate-file format: invalid ClimateTimeSeries value provided in cliamte-generator input file.");
            //    throw new Exception("Error in converting input-climate-file format: invalid ClimateTimeSeries value provided in cliamte-generator input file.");
            //}
            return;// readableFile;

        }



    }

}









