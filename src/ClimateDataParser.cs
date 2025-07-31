using Landis.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Landis.Library.Climate
{
    public static partial class Climate
    {
        private const double _windSpeedTransformation = 3.6;        // converts input data from [m/s] to [km/hr]
        private const double _windDirectionTransformation = 180;    // converts the input direction the wind comes FROM to the direction where wind is blowing TO.

        private static void ReadClimateData(TimeSeriesTimeStep climateTimeStep, string climateFile, out List<int> calendarYears, out List<ClimateRecord>[][] climateRecords)
        {
            // returned data format:  [ecoregionIndex][yearIndex] -> List<ClimateRecord>.  ClimateRecord.Count is 12 (Monthly) or 365 (Daily, regardless of leap year).

            calendarYears = null;
            climateRecords = new List<ClimateRecord>[_modelCore.Ecoregions.Count][];

            if (!File.Exists(climateFile))
            {
                throw new ApplicationException($"Error in ReadClimateData: Cannot open climate file '{climateFile}'");
            }

            var reader = File.OpenText(climateFile);

            TextLog.WriteLine($"   Converting raw data from text file: {climateFile}, TimeStep={climateTimeStep}.");

            string row;

            var ecoDataColIndex = climateTimeStep == TimeSeriesTimeStep.Monthly ? 3 : 4;
            var ecoRegionHeaderIndices = new List<int>();
            var headersParsed = false;
            var r = 0;
            var readTimeStamps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var climateInputRows = new List<ClimateInputRow>();


            // **
            // read climate input rows

            while ((row = reader.ReadLine()) != null)
            {
                ++r;
                var fields = row.Replace(" ", "").Split(',').ToList();

                if (fields.All(x => string.IsNullOrEmpty(x))) continue;     // skip blank rows

                if (!headersParsed)
                {
                    // check for proper headers
                    var existingHeaders = climateTimeStep == TimeSeriesTimeStep.Monthly ? $"{fields[0]},{fields[1]},{fields[2]}" : $"{fields[0]},{fields[1]},{fields[2]},{fields[3]}";
                    var expectedHeaders = climateTimeStep == TimeSeriesTimeStep.Monthly ? "Year,Month,Variable" : "Year,Month,Day,Variable";
                    if (!existingHeaders.Equals(expectedHeaders, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ApplicationException($"Error in ReadClimateData: Unexpected headers '{existingHeaders},...' for {climateTimeStep} data in climate file '{climateFile}'");
                    }

                    for (var i = ecoDataColIndex; i < fields.Count; ++i)
                    {
                        var eco = _modelCore.Ecoregions[fields[i]];
                        if (eco == null)
                        {
                            throw new ApplicationException($"Error in ReadClimateData: Ecoregion name '{fields[i]}' in climate file '{climateFile}' is not recognized.");
                        }
                        ecoRegionHeaderIndices.Add(eco.Index);
                    }

                    headersParsed = true;
                    continue;
                }

                if (fields.Count != ecoDataColIndex + ecoRegionHeaderIndices.Count)
                {
                    throw new ApplicationException($"Error in ReadClimateData: Missing data at row {r} in climate file '{climateFile}'");
                }

                var timeStamp = string.Join(",", fields.GetRange(0, ecoDataColIndex));
                if (readTimeStamps.Contains(timeStamp))
                {
                    throw new ApplicationException($"Error in ReadClimateData: Duplicate climate timestamp-variable '{timeStamp},...' at row {r} in climate file '{climateFile}'");
                }
                readTimeStamps.Add(timeStamp);

                var day = -1;
                if (!int.TryParse(fields[0], out var year) || !int.TryParse(fields[1], out var month) || climateTimeStep == TimeSeriesTimeStep.Daily && !int.TryParse(fields[2], out day))
                {
                    throw new ApplicationException($"Error in ReadClimateData: Cannot parse timestamp '{timeStamp}' at row {r} in climate file '{climateFile}'");
                }

                var data = fields.GetRange(ecoDataColIndex, fields.Count - ecoDataColIndex).Select(x => double.TryParse(x, out var d) ? d : double.NaN).ToList();
                if (data.Any(x => double.IsNaN(x)))
                {
                    throw new ApplicationException($"Error in ReadClimateData: Climate data contains non-numeric values at row {r} in climate file '{climateFile}'");
                }

                climateInputRows.Add(new ClimateInputRow(year, month, day, fields[ecoDataColIndex - 1], data, r));
            }

            reader.Close();

            // sort data
            climateInputRows = climateInputRows.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day).ThenBy(x => x.Variable).ToList();

            calendarYears = climateInputRows.Select(x => x.Year).Distinct().ToList();


            // **
            // build climate records

            for (var e = 0; e < _modelCore.Ecoregions.Count; ++e)
            {
                if (!_modelCore.Ecoregions[e].Active) continue;

                climateRecords[e] = Enumerable.Range(0, calendarYears.Count).Select(x => new List<ClimateRecord>()).ToArray();
            }

            var y = -1;
            var previousYear = -1;
            var previousMonth = -1;
            var previousDay = -1;

            foreach (var inputRow in climateInputRows)
            {
                if (inputRow.Year != previousYear)
                {
                    ++y;
                }

                for (var i = 0; i < ecoRegionHeaderIndices.Count; ++i)
                {
                    var e = ecoRegionHeaderIndices[i];
                    if (!_modelCore.Ecoregions[e].Active) continue;

                    if (inputRow.Month != previousMonth || inputRow.Day != previousDay)
                        climateRecords[e][y].Add(new ClimateRecord());

                    var rec = climateRecords[e][y].Last();
                    var value = inputRow.Data[i];

                    switch (inputRow.Variable)
                    {
                        case "mintemp":
                        case "tmin":
                            rec.MinTemp = value;
                            break;

                        case "maxtemp":
                        case "tmax":
                            rec.MaxTemp = value;
                            break;

                        case "temp":
                            rec.Temp = value;
                            break;

                        case "ppt":
                        case "precip":
                            rec.Precip = value;
                            break;

                        case "winddirection":
                            rec.WindDirection = (value + _windDirectionTransformation) % 360;
                            break;

                        case "windspeed":
                            rec.WindSpeed = value * _windSpeedTransformation;
                            break;

                        case "windeasting":
                            rec.WindEasting = value;
                            break;

                        case "windnorthing":
                            rec.WindNorthing = value;
                            break;

                        case "ndep":
                        case "ndeposition":
                            rec.NDeposition = value;
                            break;

                        case "co2":
                            rec.CO2 = value;
                            break;

                        case "minrh":
                            rec.MinRH = value;
                            break;

                        case "maxrh":
                            rec.MaxRH = value;
                            break;

                        case "rh":
                            rec.RH = value;
                            break;

                        case "sh":
                        case "specifichumidity":
                            rec.SpecificHumidity = value;
                            break;

                        case "dewpt":
                        case "dewpoint":
                            rec.DewPoint = value;
                            break;

                        case "pet":
                            rec.PET = value;
                            break;

                        case "par":
                            rec.PAR = value;
                            break;

                        case "o3":
                        case "ozone":
                            rec.Ozone = value;
                            break;

                        case "swr":
                        case "shortwaveradiation":
                            rec.ShortWaveRadiation = value;
                            break;


                        default:
                            throw new ApplicationException($"Error in ReadClimateData: Unrecognized climate variable '{inputRow.Variable}' at row {inputRow.Row} in climate file '{climateFile}'");

                    }

                }

                previousYear = inputRow.Year;
                previousMonth = inputRow.Month;
                previousDay = inputRow.Day;
            }


            // ** 
            // basic data checks

            var firstEcoRecords = climateRecords.First(x => x != null);         // check the first active eco region
            for (var i = 0; i < calendarYears.Count; ++i)
            {
                if (climateTimeStep == TimeSeriesTimeStep.Monthly && firstEcoRecords[i].Count != 12)
                    throw new ApplicationException($"Error in ReadClimateData: Monthly data for year {calendarYears[i]} in climate file '{climateFile}' do not have 12 records. The year has {firstEcoRecords[i].Count} records.");

                if (climateTimeStep == TimeSeriesTimeStep.Daily && firstEcoRecords[i].Count != 365 && firstEcoRecords[i].Count != 366)
                    throw new ApplicationException($"Error in ReadClimateData: Daily data for year {calendarYears[i]} in climate file '{climateFile}' do not have 365 or 366 records. The year has {firstEcoRecords[i].Count} records.");
            }


            // **
            // normalize daily data for leap years into 365 days

            if (climateTimeStep == TimeSeriesTimeStep.Daily)
            {
                foreach (var ecoRecords in climateRecords.Where(x => x != null))
                {
                    foreach (var yearRecords in ecoRecords)
                    {
                        if (yearRecords.Count == 366)
                        {
                            var feb28Record = yearRecords[58]; // get data for Feb. 28 (day 59).
                            var feb29Record = yearRecords[59]; // get data for Feb. 29 (day 60).
                            yearRecords.RemoveAt(59); // remove Feb. 29 from the ecoRecords

                            // average Feb. 29 values with their corresponding Feb. 28 values, except for Precip and NDeposition
                            feb28Record.MinTemp = 0.5 * (feb28Record.MinTemp + feb29Record.MinTemp);
                            feb28Record.MaxTemp = 0.5 * (feb28Record.MaxTemp + feb29Record.MaxTemp);
                            feb28Record.Temp = 0.5 * (feb28Record.Temp + feb29Record.Temp);
                            feb28Record.WindDirection = 0.5 * (feb28Record.WindDirection + feb29Record.WindDirection);
                            feb28Record.WindSpeed = 0.5 * (feb28Record.WindSpeed + feb29Record.WindSpeed);
                            feb28Record.WindEasting = 0.5 * (feb28Record.WindEasting + feb29Record.WindEasting);
                            feb28Record.WindNorthing = 0.5 * (feb28Record.WindNorthing + feb29Record.WindNorthing);
                            feb28Record.CO2 = 0.5 * (feb28Record.CO2 + feb29Record.CO2);
                            feb28Record.MinRH = 0.5 * (feb28Record.MinRH + feb29Record.MinRH);
                            feb28Record.MaxRH = 0.5 * (feb28Record.MaxRH + feb29Record.MaxRH);
                            feb28Record.RH = 0.5 * (feb28Record.RH + feb29Record.RH);
                            feb28Record.SpecificHumidity = 0.5 * (feb28Record.SpecificHumidity + feb29Record.SpecificHumidity);
                            feb28Record.DewPoint = 0.5 * (feb28Record.DewPoint + feb29Record.DewPoint);
                            feb28Record.PET = 0.5 * (feb28Record.PET + feb29Record.PET);
                            feb28Record.PAR = 0.5 * (feb28Record.PAR + feb29Record.PAR);
                            feb28Record.Ozone = 0.5 * (feb28Record.Ozone + feb29Record.Ozone);
                            feb28Record.ShortWaveRadiation = 0.5 * (feb28Record.ShortWaveRadiation + feb29Record.ShortWaveRadiation);

                            // amortize (spread out) Feb. 29 Precip and NDeposition over the entire month so that a monthly total still contains the Feb. 29 value.
                            //  do this rather than simply adding the Feb. 28 and Feb. 29 values, which would leave a spike in the final Feb. 28 data.
                            var avgPptIncrement = feb28Record.Precip / 28.0;
                            var avgNDepositionIncrement = feb28Record.NDeposition / 28.0;

                            var feb1 = 31; // Feb. 1 index (day 32)                      
                            for (var j = feb1; j < feb1 + 28; ++j)
                            {
                                yearRecords[j].Precip += avgPptIncrement;
                                yearRecords[j].NDeposition += avgNDepositionIncrement;
                            }
                        }
                    }
                }
            }


            // **
            // calculate missing data from other data when possible

            foreach (var rec in climateRecords.Where(x => x != null).SelectMany(x => x.SelectMany(z => z)))
            {
                // if Temp is missing, then average MinTemp and MaxTemp
                if (double.IsNaN(rec.Temp)) rec.Temp = 0.5 * (rec.MinTemp + rec.MaxTemp);

                // if WindEasting and WindNorthing exist, calculate WindDirection and WindSpeed
                if ((double.IsNaN(rec.WindDirection) || double.IsNaN(rec.WindSpeed)) && !double.IsNaN(rec.WindEasting) && !double.IsNaN(rec.WindNorthing))
                {
                    rec.WindSpeed = Math.Sqrt(rec.WindEasting * rec.WindEasting + rec.WindNorthing * rec.WindNorthing);
                    var t = Math.Atan2(-rec.WindNorthing, rec.WindEasting) * 180.0 / Math.PI + 90.0;
                    if (t < 0.0) t += 360.0;
                    rec.WindDirection = t;
                }

                // if RH is missing then calculate from MinRH and MaxRH or from SpecificHumidity or from DewPoint
                if (double.IsNaN(rec.RH))
                {
                    if (!double.IsNaN(rec.MinRH) && !double.IsNaN(rec.MaxRH))
                        rec.RH = 0.5 * (rec.MinRH + rec.MaxRH);
                    else if (!double.IsNaN(rec.SpecificHumidity))
                        rec.RH = CalculateRelativeHumidityFromSH(rec.SpecificHumidity, rec.Temp);
                    else if (!double.IsNaN(rec.DewPoint))
                        rec.RH = CalculateRelativeHumidityFromTD(rec.DewPoint, rec.Temp);
                }

                // if DewPoint is missing then calculate from SpecificHumidity
                if (double.IsNaN(rec.DewPoint) && !double.IsNaN(rec.SpecificHumidity))
                {
                    rec.DewPoint = CalculateTdewFromSH(rec.SpecificHumidity);
                }
            }


            // **
            // check for missing data

            var allFirstEcoRecords = firstEcoRecords.SelectMany(x => x).ToList();

            int nanCount;

            // MinTemp is required for all records
            nanCount = allFirstEcoRecords.Select(x => x.MinTemp).Count(x => double.IsNaN(x));
            if (nanCount != 0)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'MinTemp'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            // MaxTemp is required for all records
            nanCount = allFirstEcoRecords.Select(x => x.MaxTemp).Count(x => double.IsNaN(x));
            if (nanCount != 0)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'MaxTemp'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            nanCount = allFirstEcoRecords.Select(x => x.Temp).Count(x => double.IsNaN(x));
            if (nanCount != 0 && nanCount != allFirstEcoRecords.Count)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'Temp'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            // precip is required for all records
            nanCount = allFirstEcoRecords.Select(x => x.Precip).Count(x => double.IsNaN(x));
            if (nanCount != 0)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'Precip'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            nanCount = allFirstEcoRecords.Select(x => x.WindDirection).Count(x => double.IsNaN(x));
            if (nanCount != 0 && nanCount != allFirstEcoRecords.Count)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'WindDirection'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            nanCount = allFirstEcoRecords.Select(x => x.WindSpeed).Count(x => double.IsNaN(x));
            if (nanCount != 0 && nanCount != allFirstEcoRecords.Count)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'WindSpeed'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            nanCount = allFirstEcoRecords.Select(x => x.NDeposition).Count(x => double.IsNaN(x));
            if (nanCount != 0 && nanCount != allFirstEcoRecords.Count)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'NDeposition'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            nanCount = allFirstEcoRecords.Select(x => x.CO2).Count(x => double.IsNaN(x));
            if (nanCount != 0 && nanCount != allFirstEcoRecords.Count)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'CO2'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            nanCount = allFirstEcoRecords.Select(x => x.MinRH).Count(x => double.IsNaN(x));
            if (nanCount != 0 && nanCount != allFirstEcoRecords.Count)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'MinRH'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            nanCount = allFirstEcoRecords.Select(x => x.MaxRH).Count(x => double.IsNaN(x));
            if (nanCount != 0 && nanCount != allFirstEcoRecords.Count)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'MaxRH'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            nanCount = allFirstEcoRecords.Select(x => x.RH).Count(x => double.IsNaN(x));
            if (nanCount != 0 && nanCount != allFirstEcoRecords.Count)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'RH'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            nanCount = allFirstEcoRecords.Select(x => x.SpecificHumidity).Count(x => double.IsNaN(x));
            if (nanCount != 0 && nanCount != allFirstEcoRecords.Count)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'SpecificHumidity'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            nanCount = allFirstEcoRecords.Select(x => x.DewPoint).Count(x => double.IsNaN(x));
            if (nanCount != 0 && nanCount != allFirstEcoRecords.Count)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'DewPoint'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            nanCount = allFirstEcoRecords.Select(x => x.PET).Count(x => double.IsNaN(x));
            if (nanCount != 0 && nanCount != allFirstEcoRecords.Count)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'PET'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            nanCount = allFirstEcoRecords.Select(x => x.PAR).Count(x => double.IsNaN(x));
            if (nanCount != 0 && nanCount != allFirstEcoRecords.Count)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'PAR'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            nanCount = allFirstEcoRecords.Select(x => x.Ozone).Count(x => double.IsNaN(x));
            if (nanCount != 0 && nanCount != allFirstEcoRecords.Count)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'Ozone'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");

            nanCount = allFirstEcoRecords.Select(x => x.ShortWaveRadiation).Count(x => double.IsNaN(x));
            if (nanCount != 0 && nanCount != allFirstEcoRecords.Count)
                throw new ApplicationException($"Error in ReadClimateData: Missing data for 'ShortWaveRadiation'. Data are not defined for all years and {(climateTimeStep == TimeSeriesTimeStep.Monthly ? "months" : "days")}");


            // **
            // calculate fire weather data for daily input data

            if (climateTimeStep == TimeSeriesTimeStep.Daily && ConfigParameters.UsingFireClimate)
            {
                foreach (var yearRecords in climateRecords.Where(x => x != null).SelectMany(x => x))
                {
                    CalculateDailyFireWeather(yearRecords);
                }
            }
        }

        private static double CalculateRelativeHumidityFromSH(double specificHumidity, double temp)
        {
            //Calculate relative humidity based on average temp and specific humidity:   
            //(https://archive.eol.ucar.edu/projects/ceop/dm/documents/refdata_report/eqns.html) From Bolton, 1980

            // specificHumidity: [unitless], e.g. [kg/kg]
            // temp: [C]

            // calculate saturated vapor pressure based on temperature
            var es = 6.112 * Math.Exp(17.67 * temp / (temp + 243.5));   // [mb]

            var atmPressure = ConfigParameters.AtmPressure * 10.0;  // [kPa] -> [mb]
            var e = specificHumidity * atmPressure / (0.378 * specificHumidity + 0.622);   // [mb]

            var relativeHumidity = 100.0 * Math.Min(1.0, e / es);    // [%]
            return relativeHumidity;
        }

        private static double CalculateRelativeHumidityFromTD(double dewPoint, double temp)
        {
            //Calculate relative humidity based on average temp and dewPoint:   
            //(https://bmcnoldy.earth.miami.edu/Humidity.html)

            // dewPoint: [C]
            // temp: [C]

            return 100.0 * Math.Min(1.0, Math.Exp(17.625 * (dewPoint / (243.04 + dewPoint) - (temp / (243.04 + temp)))));   // [%]
        }

        private static double CalculateTdewFromSH(double specificHumidity)
        {
            // (https://archive.eol.ucar.edu/projects/ceop/dm/documents/refdata_report/eqns.html)
            //# From Bolton, 1980

            var atmPressure = ConfigParameters.AtmPressure * 10.0;  // [kPa] -> [mb]
            var e = specificHumidity * atmPressure / (0.378 * specificHumidity + 0.622);   // [mb]
            var dewPoint = Math.Log(e / 6.112) * 243.5 / (17.67 - Math.Log(e / 6.112));  // [C]
            return dewPoint;
        }

        private class ClimateInputRow
        {
            public ClimateInputRow(int year, int month, int day, string variable, List<double> data, int row)
            {
                Year = year;
                Month = month;
                Day = day;
                Variable = variable.ToLower().Replace("_", "");
                Data = data;
                Row = row;
            }

            public int Year { get; }
            public int Month { get; }
            public int Day { get; }
            public string Variable { get; }
            public List<double> Data { get; }
            public int Row { get; }

            public override string ToString() => $"Year: {Year}, Month: {Month}, Day: {Day}, Variable: {Variable}, Data: {(string.Join(",", Data))}";
        }
    }
}
