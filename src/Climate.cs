using Landis.Core;
using Landis.Library.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Landis.Library.Climate
{
    public static partial class Climate
    {
        #region fields

        private static ICore _modelCore;

        private static MetadataTable<InputLog> _futureInputLog;
        private static MetadataTable<AnnualLog> _futureAnnualLog;

        private static List<int> _spinupCalendarYears;
        private static List<int> _futureCalendarYears;

        private static List<ClimateRecord>[][] _spinupClimateRecords;      // [ecoregionIndex][yearIndex] -> List<ClimateRecord>      yearIndex is 0-based
        private static List<ClimateRecord>[][] _futureClimateRecords;      // [ecoregionIndex][yearIndex] -> List<ClimateRecord>      yearIndex is 0-based

        private static int _spinupRequiredYearCount;
        private static int _futureRequiredYearCount;

        private static List<int> _spinupClimateRecordYearIndexOrder;
        private static List<int> _futureClimateRecordYearIndexOrder;

        #endregion

        #region properties

        public static List<AnnualClimate>[] SpinupEcoregionYearClimate { get; private set; }    // indexing: [ecoregionIndex][year].  'year' is 1-BASED simulation year, e.g. 1, 2, ...
        public static List<AnnualClimate>[] FutureEcoregionYearClimate { get; private set; }    // indexing: [ecoregionIndex][year].  'year' is 1-BASED simulation year, e.g. 1, 2, ...

        public static TimeSeriesTimeStep SpinupTimeStep { get; private set; }
        public static TimeSeriesYearOrder SpinupYearOrder { get; private set; }

        public static TimeSeriesTimeStep FutureTimeStep { get; private set; }
        public static TimeSeriesYearOrder FutureYearOrder { get; private set; }

        internal static IInputParameters ConfigParameters { get; private set; }
        internal static StreamWriter TextLog { get; private set; }

        public readonly static List<int> FirstDayOfMonth = new List<int> { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };
        public static List<int> MiddleDayOfMonth = new List<int> { 15, 44, 74, 104, 135, 166, 196, 227, 258, 288, 318, 349 };
        public static List<int> DaysInMonth = new List<int> { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        // source: https://power.larc.nasa.gov/docs/methodology/energy-fluxes/geometry/
        public static readonly double[] MonthlyAvgDeclination = { -0.3651, -0.2261, -0.04220, 0.1643, 0.3280, 0.4029, 0.3697, 0.2348, 0.03869, -0.1675, -0.3301, -0.4023 };       // [radians]

        #endregion

        #region methods

        public static int SpinupCalendarYear(int year) => SpinupEcoregionYearClimate.First(x => x != null)[year].CalendarYear;      // 'year' is 1-BASED simulation year, e.g. 1, 2, ...
        public static int FutureCalendarYear(int year) => FutureEcoregionYearClimate.First(x => x != null)[year].CalendarYear;      // 'year' is 1-BASED simulation year, e.g. 1, 2, ...

        // Tuples of (first day, number of days) for each month:
        public static List<(int, int)> MonthCalendar = new List<(int, int)> { (0, 31), (31, 28), (59, 31), (90, 30), (120, 31), (151, 30), (181, 31), (212, 31), (243, 30), (273, 31), (304, 30), (334, 31) };

        public static int MonthOfYear(int day) => FirstDayOfMonth.FindLastIndex(x => x <= day);

        public static void Initialize(string climateConfigFilename, bool writeOutput, ICore modelCore)
        {
            _modelCore = modelCore;

            ConfigParameters = Data.Load(climateConfigFilename, new InputParametersParser());

            TextLog = Data.CreateTextFile("Landis-climate-log.txt");
            TextLog.AutoFlush = true;

            InitializeMetadata();

            // validate climate time series options
            var s = ConfigParameters.SpinUpClimateTimeSeries.Split('_');
            if (s.Length != 2 || !Enum.TryParse<TimeSeriesTimeStep>(s[0], out var spinupTimeStep) || !Enum.TryParse<TimeSeriesYearOrder>(s[1], out var spinupYearOrder))
                throw new ApplicationException($"Unknown SpinUp Climate Time Series: {ConfigParameters.SpinUpClimateTimeSeries}");

            s = ConfigParameters.ClimateTimeSeries.Split('_');
            if (s.Length != 2 || !Enum.TryParse<TimeSeriesTimeStep>(s[0], out var futureTimeStep) || !Enum.TryParse<TimeSeriesYearOrder>(s[1], out var futureYearOrder))
                throw new ApplicationException($"Unknown (Future) Climate Time Series: {ConfigParameters.ClimateTimeSeries}");

            SpinupTimeStep = spinupTimeStep;
            SpinupYearOrder = spinupYearOrder;
            FutureTimeStep = futureTimeStep;
            FutureYearOrder = futureYearOrder;

            // **
            // read climate data
            // returned data format: [ecoregionIndex][yearIndex] -> List<ClimateRecord>. yearIndex is 0-based. ClimateRecord.Count is 12 (Monthly) or 365 (Daily, regardless of leap year).

            TextLog.WriteLine($"   Loading spinup climate data from file {ConfigParameters.SpinUpClimateFile} ...");
            ReadClimateData(spinupTimeStep, ConfigParameters.SpinUpClimateFile, out _spinupCalendarYears, out _spinupClimateRecords);
            //ConvertUsgsToClimateData(spinupTimeStep, ConfigParameters.SpinUpClimateFile, ConfigParameters.SpinUpClimateFileFormat, out _spinupCalendarYears, out _spinupClimateRecords);

            TextLog.WriteLine($"   Loading future climate data from file {ConfigParameters.ClimateFile} ...");
            ReadClimateData(futureTimeStep, ConfigParameters.ClimateFile, out _futureCalendarYears, out _futureClimateRecords);
            //ConvertUsgsToClimateData(futureTimeStep, ConfigParameters.ClimateFile, ConfigParameters.ClimateFileFormat, out _futureCalendarYears, out _futureClimateRecords);

            // **
            // setup year ordering

            _spinupRequiredYearCount = modelCore.Species.Max(x => x.Longevity);  // use maximum species longevity as the maximum possible year count for spin up
            if (spinupYearOrder == TimeSeriesYearOrder.SequencedYears)
            {
                // use the last year of data for years beyond the input data
                _spinupClimateRecordYearIndexOrder = Enumerable.Range(0, _spinupRequiredYearCount).Select(x => x < _spinupCalendarYears.Count ? x : _spinupCalendarYears.Count - 1).ToList();
            }
            else if (spinupYearOrder == TimeSeriesYearOrder.RandomYears)
            {
                // randomly sample input year indices
                _spinupClimateRecordYearIndexOrder = Enumerable.Range(0, _spinupRequiredYearCount).Select(x => (int)(_spinupCalendarYears.Count * modelCore.GenerateUniform())).ToList();
            }

            _futureRequiredYearCount = modelCore.EndTime - modelCore.StartTime;
            if (futureYearOrder == TimeSeriesYearOrder.SequencedYears)
            {
                // use the last year of data for years beyond the input data
                _futureClimateRecordYearIndexOrder = Enumerable.Range(0, _futureRequiredYearCount).Select(x => x < _futureCalendarYears.Count ? x : _futureCalendarYears.Count - 1).ToList();
            }
            else if (futureYearOrder == TimeSeriesYearOrder.RandomYears)
            {
                // randomly sample input year indices
                _futureClimateRecordYearIndexOrder = Enumerable.Range(0, _futureRequiredYearCount).Select(x => (int)(_futureCalendarYears.Count * modelCore.GenerateUniform())).ToList();
            }
        }

        public static void GenerateEcoregionClimateData(double latitudeForAllEcoregions) => GenerateEcoregionClimateData(Enumerable.Repeat(latitudeForAllEcoregions, _modelCore.Ecoregions.Count));

        public static void GenerateEcoregionClimateData(IEnumerable<double> ecoregionLatitudes)
        {
            var latitudes = ecoregionLatitudes as List<double> ?? ecoregionLatitudes.ToList();

            SpinupEcoregionYearClimate = new List<AnnualClimate>[_modelCore.Ecoregions.Count];
            FutureEcoregionYearClimate = new List<AnnualClimate>[_modelCore.Ecoregions.Count];

            for (var e = 0; e < _modelCore.Ecoregions.Count; ++e)
            {
                var ecoregion = _modelCore.Ecoregions[e];
                if (!ecoregion.Active) continue;

                // calculate daylight and nighttime hours that depend on latitude, but not climate
                CalculateMonthlyDayLengths(latitudes[e], out var monthlyDayLightHours, out var monthlyNightTimeHours);

                // generate annual climate instances for each year of input data
                var spinupInputAnnualClimate = new List<AnnualClimate>();
                for (var i = 0; i < _spinupCalendarYears.Count; ++i)
                {
                    var climate = new AnnualClimate(_spinupCalendarYears[i], SpinupTimeStep, _spinupClimateRecords[ecoregion.Index][i], latitudes[e], monthlyDayLightHours, monthlyNightTimeHours);
                    spinupInputAnnualClimate.Add(climate);
                }

                var futureInputAnnualClimate = new List<AnnualClimate>();
                for (var i = 0; i < _futureCalendarYears.Count; ++i)
                {
                    var climate = new AnnualClimate(_futureCalendarYears[i], FutureTimeStep, _futureClimateRecords[ecoregion.Index][i], latitudes[e], monthlyDayLightHours, monthlyNightTimeHours);
                    futureInputAnnualClimate.Add(climate);
                }

                // calculate SPEI
                CalculateMonthlySpei(spinupInputAnnualClimate, 1);
                CalculateMonthlySpei(futureInputAnnualClimate, 1);

                // final spinup data
                if (SpinupYearOrder == TimeSeriesYearOrder.SequencedYears || SpinupYearOrder == TimeSeriesYearOrder.RandomYears)
                {
                    SpinupEcoregionYearClimate[e] = _spinupClimateRecordYearIndexOrder.Select(x => spinupInputAnnualClimate[x]).ToList();
                }
                else
                {
                    // for TimeSeriesYearOrder.AverageAllYears, set all years to the average climate
                    var avgClimate = new AnnualClimate(spinupInputAnnualClimate, SpinupTimeStep);
                    SpinupEcoregionYearClimate[e] = Enumerable.Repeat(avgClimate, _spinupRequiredYearCount).ToList();
                }

                // insert null at index zero because simulation year is 1-based
                SpinupEcoregionYearClimate[e].Insert(0, null);

                // final future data
                if (FutureYearOrder == TimeSeriesYearOrder.SequencedYears || FutureYearOrder == TimeSeriesYearOrder.RandomYears)
                {
                    FutureEcoregionYearClimate[e] = _futureClimateRecordYearIndexOrder.Select(x => futureInputAnnualClimate[x]).ToList();
                }
                else
                {
                    // for TimeSeriesYearOrder.AverageAllYears, set all years to the average climate
                    var avgClimate = new AnnualClimate(futureInputAnnualClimate, FutureTimeStep);
                    FutureEcoregionYearClimate[e] = Enumerable.Repeat(avgClimate, _spinupRequiredYearCount).ToList();
                }

                // insert null at index zero because simulation year is 1-based
                FutureEcoregionYearClimate[e].Insert(0, null);
            }

            // todo: make this optional
            // write future input log
            WriteFutureInputLog();

            // write future annual log
            WriteFutureAnnualLog();
        }

        #endregion

        #region private methods

        private static void CalculateMonthlyDayLengths(double latitude, out List<double> monthlyDayLightHours, out List<double> monthlyNightTimeHours)
        {
            // source: https://power.larc.nasa.gov/docs/methodology/energy-fluxes/geometry/

            monthlyDayLightHours = new List<double>();
            monthlyNightTimeHours = new List<double>();

            var tanlat = Math.Tan(Math.Min(Math.Max(latitude, -90.0), 90.0) * Math.PI / 180.0);

            foreach (var decl in MonthlyAvgDeclination)
            {
                var z = tanlat * Math.Tan(decl);

                double ws;
                if (z < -1.0) // sun stays below horizon
                {
                    ws = 0.0;
                }
                else if (z > 1.0) // sun stays above the horizon
                {
                    ws = Math.PI;
                }
                else
                {
                    ws = Math.Acos(-z);
                }

                var hr = 24.0 * ws / Math.PI; // length of day in hours
                monthlyDayLightHours.Add(hr);
                monthlyNightTimeHours.Add(24.0 - hr);
            }
        }

        private static void WriteFutureInputLog()
        {
            _futureInputLog.Clear();

            for (var year = 1; year <= _futureRequiredYearCount; ++year) // 1-based year
            {
                for (var e = 0; e < _modelCore.Ecoregions.Count; ++e)
                {
                    if (FutureEcoregionYearClimate[e] == null) continue;

                    foreach (var log in FutureEcoregionYearClimate[e][year].ToInputLogs(year, _modelCore.Ecoregions[e]))
                    {
                        _futureInputLog.AddObject(log);
                    }
                }
            }

            _futureInputLog.WriteToFile();
        }

        private static void WriteFutureAnnualLog()
        {
            _futureAnnualLog.Clear();

            for (var year = 1; year <= _futureRequiredYearCount; ++year) // 1-based year
            {
                for (var e = 0; e < _modelCore.Ecoregions.Count; ++e)
                {
                    if (FutureEcoregionYearClimate[e] == null) continue;

                    _futureAnnualLog.AddObject(new AnnualLog
                                               {
                                                   Year = year,
                                                   CalendarYear = FutureEcoregionYearClimate[e][year].CalendarYear,
                                                   EcoregionName = _modelCore.Ecoregions[e].Name,
                                                   EcoregionIndex = _modelCore.Ecoregions[e].Index,

                                                   TAP = FutureEcoregionYearClimate[e][year].TotalAnnualPrecip,
                                                   MAT = FutureEcoregionYearClimate[e][year].MeanAnnualTemperature,
                                                   BeginGrow = FutureEcoregionYearClimate[e][year].BeginGrowingDay,
                                                   EndGrow = FutureEcoregionYearClimate[e][year].EndGrowingDay,
                    });
                }
            }

            _futureAnnualLog.WriteToFile();
        }

        #endregion

        #region private classes

        #endregion
    }
}
