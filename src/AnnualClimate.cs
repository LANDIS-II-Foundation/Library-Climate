using Landis.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Landis.Library.Climate
{
    public class AnnualClimate
    {
        #region fields
        #endregion

        #region constructor

        internal AnnualClimate(int calendarYear, TimeSeriesTimeStep climateTimeStep, List<ClimateRecord> records, double latitude, List<double> monthlyDayLightHours, List<double> monthlyNightTimeHours)
        {
            CalendarYear = calendarYear;
            Latitude = latitude;
            MonthlyDayLightHours = monthlyDayLightHours;
            MonthlyNightTimeHours = monthlyNightTimeHours;

            if (climateTimeStep == TimeSeriesTimeStep.Monthly)
            {
                // TimeSeriesTimeStep.Monthly

                // gather monthly data across (monthly) records
                MonthlyMinTemp = records.Select(x => x.MinTemp).ToList();
                MonthlyMaxTemp = records.Select(x => x.MaxTemp).ToList();
                MonthlyTemp = records.Select(x => x.Temp).ToList();

                MonthlyPrecip = records.Select(x => x.Precip).ToList();

                MonthlyWindDirection = records.Select(x => x.WindDirection).ToList();
                MonthlyWindSpeed = records.Select(x => x.WindSpeed).ToList();

                MonthlyNDeposition = records.Select(x => x.NDeposition).ToList();
                MonthlyCO2 = records.Select(x => x.CO2).ToList();

                MonthlyMinRH = records.Select(x => x.MinRH).ToList();
                MonthlyMaxRH = records.Select(x => x.MaxRH).ToList();
                MonthlyRH = records.Select(x => x.RH).ToList();
                MonthlySpecificHumidity = records.Select(x => x.SpecificHumidity).ToList();

                MonthlyPET = records.Select(x => x.PET).ToList();
                MonthlyPAR = records.Select(x => x.PAR).ToList();
                MonthlyOzone = records.Select(x => x.Ozone).ToList();
                MonthlyShortWaveRadiation = records.Select(x => x.ShortWaveRadiation).ToList();

                // results from monthly data:
                BeginGrowingDay = CalculateBeginGrowingSeasonFromMonthlyData(MonthlyMinTemp);
                EndGrowingDay = CalculateEndGrowingSeasonFromMonthlyData(MonthlyMinTemp);
                GrowingDegreeDays = CalculateGrowingDegreeDaysFromMonthlyData(MonthlyTemp);
            }
            else
            {
                // TimeSeriesTimeStep.Daily

                // gather daily data across (daily) records
                DailyMinTemp = records.Select(x => x.MinTemp).ToList();
                DailyMaxTemp = records.Select(x => x.MaxTemp).ToList();
                DailyTemp = records.Select(x => x.Temp).ToList();

                DailyPrecip = records.Select(x => x.Precip).ToList();

                DailyWindDirection = records.Select(x => x.WindDirection).ToList();
                DailyWindSpeed = records.Select(x => x.WindSpeed).ToList();
                
                DailyNDeposition = records.Select(x => x.NDeposition).ToList();
                DailyCO2 = records.Select(x => x.CO2).ToList();
                
                DailyMinRH = records.Select(x => x.MinRH).ToList();
                DailyMaxRH = records.Select(x => x.MaxRH).ToList();
                DailyRH = records.Select(x => x.RH).ToList();
                DailySpecificHumidity = records.Select(x => x.SpecificHumidity).ToList();
                
                DailyPET = records.Select(x => x.PET).ToList();
                DailyPAR = records.Select(x => x.PAR).ToList();
                DailyOzone = records.Select(x => x.Ozone).ToList();
                DailyShortWaveRadiation = records.Select(x => x.ShortWaveRadiation).ToList();

                DailyDuffMoistureCode = records.Select(x => x.DuffMoistureCode).ToList();
                DailyDroughtCode = records.Select(x => x.DroughtCode).ToList();
                DailyBuildUpIndex = records.Select(x => x.BuildUpIndex).ToList();
                DailyFineFuelMoistureCode = records.Select(x => x.FineFuelMoistureCode).ToList();
                DailyFireWeatherIndex = records.Select(x => x.FireWeatherIndex).ToList();

                // results from daily data
                DailyTdew = records.Select(x => CalculateTdew(x.SpecificHumidity)).ToList();
                BeginGrowingDay = CalculateBeginGrowingSeasonFromDailyData(DailyMinTemp);
                EndGrowingDay = CalculateEndGrowingSeasonFromDailyData(DailyMinTemp);
                GrowingDegreeDays = CalculateGrowingDegreeDaysFromDailyData(DailyTemp);

                // gather monthly data across (daily) records
                MonthlyMinTemp = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Average(y => y.MinTemp)).ToList();
                MonthlyMaxTemp = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Average(y => y.MaxTemp)).ToList();
                MonthlyTemp = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Average(y => y.Temp)).ToList();

                // Precip is summed across days in the month
                MonthlyPrecip = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Sum(y => y.Precip)).ToList();

                MonthlyWindDirection= Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Average(y => y.WindDirection)).ToList();
                MonthlyWindSpeed = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Average(y => y.WindSpeed)).ToList();

                // NDeposition is summed across days in the month
                MonthlyNDeposition = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Sum(y => y.NDeposition)).ToList();
                MonthlyCO2 = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Average(y => y.CO2)).ToList();

                MonthlyMinRH = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Average(y => y.MinRH)).ToList();
                MonthlyMaxRH = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Average(y => y.MaxRH)).ToList();
                MonthlyRH = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Average(y => y.RH)).ToList();
                MonthlySpecificHumidity = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Average(y => y.SpecificHumidity)).ToList();

                MonthlyPET = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Average(y => y.PET)).ToList();
                MonthlyPAR = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Average(y => y.PAR)).ToList();
                MonthlyOzone = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Average(y => y.Ozone)).ToList();
                MonthlyShortWaveRadiation = Climate.MonthCalendar.Select(x => records.GetRange(x.Item1, x.Item2).Average(y => y.ShortWaveRadiation)).ToList();

                MonthlyDuffMoistureCode = Climate.MonthCalendar.Select(x => DailyDuffMoistureCode.GetRange(x.Item1, x.Item2).Average()).ToList();
                MonthlyDroughtCode = Climate.MonthCalendar.Select(x => DailyDroughtCode.GetRange(x.Item1, x.Item2).Average()).ToList();
                MonthlyBuildUpIndex = Climate.MonthCalendar.Select(x => DailyBuildUpIndex.GetRange(x.Item1, x.Item2).Average()).ToList();
                MonthlyFineFuelMoistureCode = Climate.MonthCalendar.Select(x => DailyFineFuelMoistureCode.GetRange(x.Item1, x.Item2).Average()).ToList();
                MonthlyFireWeatherIndex = Climate.MonthCalendar.Select(x => DailyFireWeatherIndex.GetRange(x.Item1, x.Item2).Average()).ToList();
            }

            // if monthly PET is missing, calculate using Thornwaite equation
            if (double.IsNaN(MonthlyPET[0]))
            {
                MonthlyPET = CalculatePotentialEvapotranspirationThornwaite(MonthlyTemp, monthlyDayLightHours);
            }

            MonthlyVPD = CalculateVaporPressureDeficit(MonthlyTemp, MonthlyMinTemp);
            MonthlyGDD = Climate.DaysInMonth.Select((x, i) => (int)Math.Max(0.0, x * this.MonthlyTemp[i])).ToList();
            MeanAnnualTemperature = Climate.DaysInMonth.Select((x, i) => x * this.MonthlyTemp[i]).Sum();        // this is correct whether the input data are monthly or daily
            JJAtemperature = (MonthlyTemp[5] + MonthlyTemp[6] + MonthlyTemp[7]) / 3.0;
        }

        internal AnnualClimate(List<AnnualClimate> yearlyAnnualClimate, TimeSeriesTimeStep climateTimeStep)
        {
            // generates an instance that averages climate data over yearlyAnnualClimate
            // sets CalendarYear to -1

            CalendarYear = -1;

            Latitude = yearlyAnnualClimate[0].Latitude;

            BeginGrowingDay = (int)yearlyAnnualClimate.Average(x => x.BeginGrowingDay);
            EndGrowingDay = (int)yearlyAnnualClimate.Average(x => x.EndGrowingDay);
            GrowingDegreeDays = (int)yearlyAnnualClimate.Average(x => x.GrowingDegreeDays);

            MeanAnnualTemperature = yearlyAnnualClimate.Average(x => x.MeanAnnualTemperature);
            JJAtemperature = yearlyAnnualClimate.Average(x => x.JJAtemperature);

            MonthlyDayLightHours = yearlyAnnualClimate[0].MonthlyDayLightHours;
            MonthlyNightTimeHours = yearlyAnnualClimate[0].MonthlyNightTimeHours;

            if (climateTimeStep == TimeSeriesTimeStep.Daily)
            {
                DailyMinTemp = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyMinTemp[x])).ToList();
                DailyMaxTemp = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyMaxTemp[x])).ToList();
                DailyTemp = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyTemp[x])).ToList();
                
                DailyPrecip = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyPrecip[x])).ToList();

                DailyWindDirection = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyWindDirection[x])).ToList();
                DailyWindSpeed = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyWindSpeed[x])).ToList();

                DailyNDeposition = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyNDeposition[x])).ToList();
                DailyCO2 = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyCO2[x])).ToList();

                DailyMinRH = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyMinRH[x])).ToList();
                DailyMaxRH = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyMaxRH[x])).ToList();
                DailyRH = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyRH[x])).ToList();
                DailySpecificHumidity = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailySpecificHumidity[x])).ToList();

                DailyPET = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyPET[x])).ToList();
                DailyPAR = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyPAR[x])).ToList();
                DailyOzone = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyOzone[x])).ToList();
                DailyShortWaveRadiation = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyShortWaveRadiation[x])).ToList();

                DailyTdew = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyTdew[x])).ToList();

                DailyDuffMoistureCode = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyDuffMoistureCode[x])).ToList();
                DailyDroughtCode = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyDroughtCode[x])).ToList();
                DailyBuildUpIndex = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyBuildUpIndex[x])).ToList();
                DailyFineFuelMoistureCode = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyFineFuelMoistureCode[x])).ToList();
                DailyFireWeatherIndex = Enumerable.Range(0, 365).Select(x => yearlyAnnualClimate.Average(y => y.DailyFireWeatherIndex[x])).ToList();
            }

            MonthlyMinTemp = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyMinTemp[x])).ToList();
            MonthlyMaxTemp = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyMaxTemp[x])).ToList();
            MonthlyTemp = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyTemp[x])).ToList();

            MonthlyPrecip = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyPrecip[x])).ToList();

            MonthlyWindDirection = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyWindDirection[x])).ToList();
            MonthlyWindSpeed = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyWindSpeed[x])).ToList();

            MonthlyNDeposition = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyNDeposition[x])).ToList();
            MonthlyCO2 = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyCO2[x])).ToList();

            MonthlyMinRH = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyMinRH[x])).ToList();
            MonthlyMaxRH = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyMaxRH[x])).ToList();
            MonthlyRH = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyRH[x])).ToList();
            MonthlySpecificHumidity = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlySpecificHumidity[x])).ToList();

            MonthlyPET = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyPET[x])).ToList();
            MonthlyPAR = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyPAR[x])).ToList();
            MonthlyOzone = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyOzone[x])).ToList();
            MonthlyShortWaveRadiation = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyShortWaveRadiation[x])).ToList();

            MonthlyDuffMoistureCode = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyDuffMoistureCode[x])).ToList();
            MonthlyDroughtCode = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyDroughtCode[x])).ToList();
            MonthlyBuildUpIndex = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyBuildUpIndex[x])).ToList();
            MonthlyFineFuelMoistureCode = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyFineFuelMoistureCode[x])).ToList();
            MonthlyFireWeatherIndex = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyFireWeatherIndex[x])).ToList();

            MonthlyVPD = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlyVPD[x])).ToList();
            MonthlyGDD = Enumerable.Range(0, 12).Select(x => (int)yearlyAnnualClimate.Average(y => y.MonthlyGDD[x])).ToList();
            
            MonthlySpei = Enumerable.Range(0, 12).Select(x => yearlyAnnualClimate.Average(y => y.MonthlySpei[x])).ToArray();
        }

        #endregion

        #region properties

        public int CalendarYear { get; }     // actual year of input climate data, e.g. 2015, 2016, ...
        public double Latitude { get; }

        public int BeginGrowingDay { get; }
        public int EndGrowingDay { get; }
        public int GrowingDegreeDays { get; }

        public double MeanAnnualTemperature { get; }
        public double TotalAnnualPrecip => MonthlyPrecip.Sum();
        public double JJAtemperature { get; }

        // non-climate data
        public List<double> MonthlyDayLightHours { get; }
        public List<double> MonthlyNightTimeHours { get; }

        // **
        // Daily climate data

        public List<double> DailyMinTemp { get; }
        public List<double> DailyMaxTemp { get; }
        public List<double> DailyTemp { get; }

        public List<double> DailyPrecip { get; }

        public List<double> DailyWindDirection { get; }
        public List<double> DailyWindSpeed { get; }

        public List<double> DailyNDeposition { get; }
        public List<double> DailyCO2 { get; }
        
        public List<double> DailyMinRH { get; }
        public List<double> DailyMaxRH { get; }
        public List<double> DailyRH { get; }
        public List<double> DailySpecificHumidity { get; }

        public List<double> DailyPET { get; }
        public List<double> DailyPAR { get; }
        public List<double> DailyOzone { get; }
        public List<double> DailyShortWaveRadiation { get; }

        // other daily data
        public List<double> DailyTdew { get; }

        public List<double> DailyDuffMoistureCode { get; private set; } = new double[365].ToList();
        public List<double> DailyDroughtCode { get; private set; } = new double[365].ToList();
        public List<double> DailyBuildUpIndex { get; private set; } = new double[365].ToList();
        public List<double> DailyFineFuelMoistureCode { get; private set; } = new double[365].ToList();
        public List<double> DailyFireWeatherIndex { get; private set; } = new double[365].ToList();

        // **
        // Monthly climate data

        public List<double> MonthlyMinTemp { get; }
        public List<double> MonthlyMaxTemp { get; }
        public List<double> MonthlyTemp { get; }

        public List<double> MonthlyPrecip { get; }

        public List<double> MonthlyWindDirection { get; }
        public List<double> MonthlyWindSpeed { get; }

        public List<double> MonthlyNDeposition { get; }
        public List<double> MonthlyCO2 { get; }

        public List<double> MonthlyMinRH { get; }
        public List<double> MonthlyMaxRH { get; }
        public List<double> MonthlyRH { get; }
        public List<double> MonthlySpecificHumidity { get; }

        public List<double> MonthlyPET { get; }
        public List<double> MonthlyPAR { get; }
        public List<double> MonthlyOzone { get; }
        public List<double> MonthlyShortWaveRadiation { get; }

        // other monthly data
        public List<double> MonthlyDuffMoistureCode { get; private set; } = new double[12].ToList();
        public List<double> MonthlyDroughtCode { get; private set; } = new double[12].ToList();
        public List<double> MonthlyBuildUpIndex { get; private set; } = new double[12].ToList();
        public List<double> MonthlyFineFuelMoistureCode { get; private set; } = new double[12].ToList();
        public List<double> MonthlyFireWeatherIndex { get; private set; } = new double[12].ToList();

        public List<double> MonthlyVPD { get; }
        public List<int> MonthlyGDD { get; }

        public double[] MonthlySpei { get; internal set; }

        #endregion

        #region methods

        public List<double> DailyMinTempForMonth(int month) => DailyMinTemp.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();
        public List<double> DailyMaxTempForMonth(int month) => DailyMaxTemp.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();
        public List<double> DailyTempForMonth(int month) => DailyTemp.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();

        public List<double> DailyPrecipForMonth(int month) => DailyPrecip.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();

        public List<double> DailyWindDirectionForMonth(int month) => DailyWindDirection.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();
        public List<double> DailyWindSpeedForMonth(int month) => DailyWindSpeed.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();

        public List<double> DailyNDepositionForMonth(int month) => DailyNDeposition.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();
        public List<double> DailyCO2ForMonth(int month) => DailyCO2.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();

        public List<double> DailyMinRHForMonth(int month) => DailyMinRH.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();
        public List<double> DailyMaxRHForMonth(int month) => DailyMaxRH.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();
        public List<double> DailyRHForMonth(int month) => DailyRH.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();
        public List<double> DailySpecificHumidityForMonth(int month) => DailySpecificHumidity.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();

        public List<double> DailyPETForMonth(int month) => DailyPET.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();
        public List<double> DailyPARForMonth(int month) => DailyPAR.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();
        public List<double> DailyOzoneForMonth(int month) => DailyOzone.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();
        public List<double> DailyShortWaveRadiationForMonth(int month) => DailyShortWaveRadiation.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();

        public List<double> DailyTdewForMonth(int month) => DailyTdew.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();

        public List<double> DailyDuffMoistureCodeForMonth(int month) => DailyDuffMoistureCode.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();
        public List<double> DailyDroughtCodeForMonth(int month) => DailyDroughtCode.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();
        public List<double> DailyBuildUpIndexForMonth(int month) => DailyBuildUpIndex.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();
        public List<double> DailyFineFuelMoistureCodeForMonth(int month) => DailyFineFuelMoistureCode.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();
        public List<double> DailyFireWeatherIndexForMonth(int month) => DailyFireWeatherIndex.GetRange(Climate.MonthCalendar[month].Item1, Climate.MonthCalendar[month].Item2).ToList();

        internal List<InputLog> ToInputLogs(int year, IEcoregion ecoregion)
        {
            // return a list of input log records populated with monthly data
            var monthLogs = new List<InputLog>();

            for (var month = 0; month < 12; ++month)
            {
                monthLogs.Add(new InputLog
                         {
                             Year = year,
                             CalendarYear = CalendarYear,
                             Month = month,
                             EcoregionName = ecoregion.Name,

                             MinTemp = MonthlyMinTemp[month],
                             MaxTemp = MonthlyMaxTemp[month],
                             Temp = MonthlyTemp[month],
                             Precip = MonthlyPrecip[month],
                             WindDirection = MonthlyWindDirection[month],
                             WindSpeed = MonthlyWindSpeed[month],
                             NDeposition = MonthlyNDeposition[month],
                             CO2 = MonthlyCO2[month],
                             MinRH = MonthlyMinRH[month],
                             MaxRH = MonthlyMaxRH[month],
                             RH = MonthlyRH[month],
                             SpecificHumidity = MonthlySpecificHumidity[month],
                             PET = MonthlyPET[month],
                             PAR = MonthlyPAR[month],
                             Ozone = MonthlyOzone[month],
                             ShortWaveRadiation = MonthlyShortWaveRadiation[month],
                             DuffMoistureCode = MonthlyDuffMoistureCode[month],
                             DroughtCode = MonthlyDroughtCode[month],
                             BuildUpIndex = MonthlyBuildUpIndex[month],
                             FineFuelMoistureCode = MonthlyFineFuelMoistureCode[month],
                             FireWeatherindex = MonthlyFireWeatherIndex[month],
                             VPD = MonthlyVPD[month],
                             GDD = MonthlyGDD[month],
                             SPEI = MonthlySpei[month],
                         });
            }

            return monthLogs;
        }

        #endregion

        #region private methods

        private double CalculateTdew(double specificHumidity)
        {
            if (double.IsNaN(specificHumidity)) return double.NaN;

            // (https://archive.eol.ucar.edu/projects/ceop/dm/documents/refdata_report/eqns.html)
            //# From Bolton, 1980

            var atmPressure = Climate.ConfigParameters.AtmPressure * 10.0;  // [kPa] -> [mb]
            var e = specificHumidity * atmPressure / (0.378 * specificHumidity + 0.622);   // [mb]
            var dewPoint = Math.Log(e / 6.112) * 243.5 / (17.67 - Math.Log(e / 6.112));  // [C]
            return dewPoint;
        }

        private static List<double> CalculatePotentialEvapotranspirationThornwaite(List<double> monthlyTemp, List<double> monthlyDayLightHours)
        {
            // Calculate potential evapotranspiration using the Thornwaite method.
            
            // Calculate Heat index first because it depends on monthly mean temps throughout the entire year at a given location
            var heatIndex = monthlyTemp.Sum(x => Math.Pow(Math.Max(0.0, x) / 5.0, 1.514));
            var alpha = 0.000000675 * Math.Pow(heatIndex, 3.0) - 0.0000771 * Math.Pow(heatIndex, 2.0) + 0.01792 * heatIndex + 0.49239;

            // calculate PET for each month using the heat index from above
            var monthlyPET = new List<double>();
            for (var month = 0; month < 12; ++month)
            {
                // alpha term in Thornwaite equation depends solely on the heat index
                var pet = 16.0 * (monthlyDayLightHours[month] / 12.0) * (Climate.DaysInMonth[month] / 30.0) * Math.Pow(10.0 * Math.Max(0.0, monthlyTemp[month]) / heatIndex, alpha) / 10.0;  // equation by Thornwaite divided by 10 to get cm
                monthlyPET.Add(pet);
            }

            return monthlyPET;
        }

        private static List<double> CalculateVaporPressureDeficit(List<double> monthlyTemp, List<double> monthlyMinTemp)
        {
            // From PnET:
            // Estimation of saturated vapor pressure from daily average temperature.
            // Calculates saturated vp and delta from temperature, from Murray J Applied Meteorol 6:203
            //   Tday    average air temperature, degC
            //   ES  saturated vapor pressure at Tday, kPa
            //   DELTA dES/dTA at TA, kPa/K which is the slope of the sat. vapor pressure curve
            //   Saturation equations are from:
            //       Murry, (1967). Journal of Applied Meteorology. 6:203.
            
            var monthlyVPD = new List<double>();
            for (var month = 0; month < 12; ++month)
            {
                var temp = monthlyTemp[month];
                var es = temp < 0.0 ? 0.61078 * Math.Exp(21.87456 * temp / (temp + 265.5)) : 0.61078 * Math.Exp(17.26939 * temp / (temp + 237.3)); //kPa

                //Calculation of mean daily vapor pressure from minimum daily temperature.
                //   Tmin = minimum daily air temperature                  //degrees C
                //   emean = mean daily vapor pressure                     //kPa
                //   Vapor pressure equations are from:
                //       Murray (1967). Journal of Applied Meteorology. 6:203.

                var minTemp = monthlyMinTemp[month];
                var emean = minTemp < 0.0 ? 0.61078 * Math.Exp(21.87456 * minTemp / (minTemp + 265.5)) : 0.61078 * Math.Exp(17.26939 * minTemp / (minTemp + 237.3)); //kPa

                var vpd = es - emean;
                monthlyVPD.Add(vpd);
            }

            return monthlyVPD;
        }

        private static int CalculateBeginGrowingSeasonFromMonthlyData(List<double> monthlyMinTemp)
        {
            // estimate the first day for which the minimum daily temperature is positive.

            // find the months that bracket the transition from negative to positive min temperatures
            var firstMonthAboveZero = monthlyMinTemp.FindIndex(x => x > 0.0);

            if (firstMonthAboveZero == 0) return 0;         // Jan. is already above 0.0
            if (firstMonthAboveZero < 0) return 364;        // all months below 0.0

            // assume the monthly min temperatures are from the middle day of each month
            // interpolate zero degrees onto the monthly min temperatures to estimate the day the min temperature is zero
            var beginGrowingSeason = Climate.MiddleDayOfMonth[firstMonthAboveZero - 1] + (Climate.MiddleDayOfMonth[firstMonthAboveZero] - Climate.MiddleDayOfMonth[firstMonthAboveZero - 1]) * (0.0 - monthlyMinTemp[firstMonthAboveZero - 1]) / (monthlyMinTemp[firstMonthAboveZero] - monthlyMinTemp[firstMonthAboveZero - 1]);
            return (int)beginGrowingSeason;
        }

        private static int CalculateEndGrowingSeasonFromMonthlyData(List<double> monthlyMinTemp)
        {
            // estimate the last day for which the minimum daily temperature is positive.

            // find the months that bracket the transition from positive to negative min temperatures
            var lastMonthAboveZero = monthlyMinTemp.FindLastIndex(x => x > 0.0);

            if (lastMonthAboveZero == 11) return 364;       // Dec. is still above 0.0
            if (lastMonthAboveZero < 0) return 0;           // all months below 0.0

            // assume the monthly min temperatures are from the middle day of each month
            // interpolate zero degrees onto the monthly min temperatures to estimate the day the min temperature is zero
            var endGrowingSeason = Climate.MiddleDayOfMonth[lastMonthAboveZero] + (Climate.MiddleDayOfMonth[lastMonthAboveZero + 1] - Climate.MiddleDayOfMonth[lastMonthAboveZero]) * (0.0 - monthlyMinTemp[lastMonthAboveZero]) / (monthlyMinTemp[lastMonthAboveZero + 1] - monthlyMinTemp[lastMonthAboveZero]);
            return (int)endGrowingSeason;
        }

        private static int CalculateGrowingDegreeDaysFromMonthlyData(List<double> monthlyTemp)
        {
            // calculate growing season degree days based on monthly temperatures

            // degDayBase is temperature (C) above which degree days are counted
            const double degDayBase = 4.44; // 40F used as base in Botkin reference.
            var degreeDays = 0.0;
            for (var month = 0; month < 12; ++month)
            {
                if (monthlyTemp[month] > degDayBase)
                    degreeDays += (monthlyTemp[month] - degDayBase) * Climate.DaysInMonth[month];
            }
            
            return (int)degreeDays;
        }

        private static int CalculateBeginGrowingSeasonFromDailyData(List<double> dailyMinTemp)
        {
            // get the first day for which the minimum daily temperature is positive.

            var day = dailyMinTemp.FindIndex(x => x > 0.0);
            if (day < 0) day = 364;     // all days below 0.0

            return day;
        }

        private static int CalculateEndGrowingSeasonFromDailyData(List<double> dailyMinTemp)
        {
            // get the last day for which the minimum daily temperature is positive.

            var day = dailyMinTemp.FindLastIndex(x => x > 0.0);
            if (day < 0) day = 0;     // all days below 0.0

            return day;
        }

        private static int CalculateGrowingDegreeDaysFromDailyData(List<double> dailyTemp)
        {
            // calculate growing season degree days based on daily temperatures

            // degDayBase is temperature (C) above which degree days are counted
            const double degDayBase = 4.44; // 40F used as base in Botkin reference.
            var degreeDays = 0.0;
            for (var day = 0; day < 365; ++day)
            {
                if (dailyTemp[day] > degDayBase)
                    degreeDays += dailyTemp[day] - degDayBase;
            }

            return (int)degreeDays;
        }

        #endregion

        #region private classes

        #endregion

    }
}
