using Landis.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Landis.Library.Climate
{
    public static partial class Climate
    {
        // JM: private enum used in parsing data
        private enum FileSection
        {
            MinTemperature = 1,
            MaxTemperature = 2,
            Precipitation = 3,
            Winddirection = 4,
            Windspeed = 5,
            WindEasting = 6,
            WindNorthing = 7,
            NDeposition = 8,
            CO2 = 9,
            RelativeHumidity = 10,
            MinRelativeHumidity = 11,
            MaxRelativeHumidity = 12,
            SpecificHumidity = 13,
            PET = 14,
            PAR = 15,
            Ozone = 16,
            ShortWaveRadiation = 17,
            Temperature = 18
        }

        private static void ConvertUsgsToClimateData(TimeSeriesTimeStep climateTimeStep, string climateFile, string climateFileFormat, out List<int> calendarYears, out List<ClimateRecord>[][] climateRecords)
        {
            // returned data format:  [ecoregionIndex][yearIndex] -> List<ClimateRecord>.  ClimateRecord.Count is 12 (Monthly) or 365 (Daily, regardless of leap year).

            var precipYearKeys = new List<int>();
            var windYearKeys = new List<int>();

            var precipClimateRecords = new List<List<ClimateRecord>[]>();
            var windClimateRecords = new List<List<ClimateRecord>[]>();


            // get trigger words for parsing based on file format
            var format = new ClimateFileFormatProvider(climateFileFormat);

            if (!File.Exists(climateFile))
            {
                throw new ApplicationException($"Error in ClimateDataConvertor: Cannot open climate file '{climateFile}'");
            }

            var reader = File.OpenText(climateFile);

            TextLog.WriteLine($"   Converting raw data from text file: {climateFile}, Format={climateFileFormat.ToLower()}, TimeStep={climateTimeStep}.");

            // maps from ecoregion column index in the input file to the ecoregion.index for the region
            int[] ecoRegionIndexMap = null;
            var ecoRegionCount = 0;

            var numberOfGroups = 2;     // the number of allowed groups. Presently:  (1) precip, tmin, tmax (2) winddirection, windspeed,  windeasting, windnorthing, Ndep, CO2, RH, minRH, maxRH, SH, PAR, O3, SWR & Temp
            var groupSectionCounts = new int[numberOfGroups];       // used to know if I'm beyond the first section in a group 
            var groupTimeSteps = new List<string>[numberOfGroups];      // keeps track of timesteps within each group to ensure they match
            for (var i = 0; i < numberOfGroups; ++i)
                groupTimeSteps[i] = new List<string>();

            var sectionYearRowIndex = -1;
            var sectionYear = -1;
            var sectionYearIndex = -1;
            var sectionRowIndex = -1;

            FileSection section = 0;
            var groupIndex = -1;

            List<ClimateRecord>[] yearEcoRecords = null;        // could be either precip or wind data, depending on the section group.

            string row;

            while (!string.IsNullOrEmpty(row = reader.ReadLine()))
            {
                var fields = row.Replace(" ", "").Split(',').ToList();    // JM: don't know if stripping blanks is needed, but just in case

                // skip blank rows
                if (fields.All(x => string.IsNullOrEmpty(x)))
                    continue;

                // check for trigger word
                if (fields[0].StartsWith("#"))
                {
                    // determine which section we're in
                    var triggerWord = fields[0].TrimStart('#');   // remove the leading "#"

                    if (format.MinTempTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.MinTemperature;
                        groupIndex = 0;
                    }

                    else if (format.MaxTempTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.MaxTemperature;
                        groupIndex = 0;
                    }

                    else if (format.PrecipTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.Precipitation;
                        groupIndex = 0;
                    }

                    else if (format.WindDirectionTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.Winddirection;
                        groupIndex = 1;
                    }
                    else if (format.WindSpeedTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.Windspeed;
                        groupIndex = 1;
                    }
                    else if (format.WindEastingTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.WindEasting;
                        groupIndex = 1;
                    }
                    else if (format.WindNorthingTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.WindNorthing;
                        groupIndex = 1;
                    }
                    else if (format.NDepositionTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.NDeposition;
                        groupIndex = 1;
                    }
                    else if (format.CO2TriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.CO2;
                        groupIndex = 1;
                    }
                    else if (format.MinRHTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.MinRelativeHumidity;
                        groupIndex = 1;
                    }
                    else if (format.MaxRHTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.MaxRelativeHumidity;
                        groupIndex = 1;
                    }
                    else if (format.SpecificHumidityTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.SpecificHumidity;
                        groupIndex = 1;
                    }
                    else if (format.PETTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.PET;
                        groupIndex = 1;
                    }
                    else if (format.PARTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.PAR;
                        groupIndex = 1;
                    }
                    else if (format.OzoneTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.Ozone;
                        groupIndex = 1;
                    }
                    else if (format.ShortWaveRadiationTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.ShortWaveRadiation;
                        groupIndex = 1;
                    }
                    else if (format.TemperatureTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.Temperature;
                        groupIndex = 1;
                    }



                    else
                        throw new ApplicationException($"Error in ClimateDataConvertor: Unrecognized trigger word '{triggerWord}' in climate file '{climateFile}'.");

                    // increment group section count
                    ++groupSectionCounts[groupIndex];

                    // if this is the first section in the file then parse the ecoregions, etc.
                    if (ecoRegionIndexMap == null)
                    {
                        // read next line to get ecoregion headers
                        var ecoRegionHeaders = reader.ReadLine().Replace(" ", "").Split(',').ToList();
                        ecoRegionHeaders.RemoveAt(0);   // remove blank cell at the beginning of ecoregion header row

                        // JM: the next line assumes all input files have exactly three groups of columns: Mean, Variance, Std_dev
                        ecoRegionCount = ecoRegionHeaders.Count / 3;

                        if (ecoRegionCount == 0)
                            throw new ApplicationException($"Error in ClimateDataConvertor: climate file '{climateFile}' contains no ecoregion data.");

                        var modelCoreActiveEcoRegionCount = _modelCore.Ecoregions.Count(x => x.Active);

                        if (ecoRegionCount != modelCoreActiveEcoRegionCount)
                            throw new ApplicationException($"Error in ClimateDataConvertor: climate file '{climateFile}' contains data for {ecoRegionCount} ecoregions, but the simulation has {modelCoreActiveEcoRegionCount} active ecoregions.");

                        // determine the map from ecoregions in this file to ecoregion indices in _modelCore
                        ecoRegionIndexMap = new int[ecoRegionCount];
                        for (var i = 0; i < ecoRegionCount; ++i)
                        {
                            IEcoregion eco = _modelCore.Ecoregions[ecoRegionHeaders[i]];     // JM:  Ecoregions appear to be indexed by string name, but I don't know if it is case-sensitive.
                            if (eco != null && eco.Active)
                                ecoRegionIndexMap[i] = eco.Index;
                            else
                                throw new ApplicationException($"Error in ClimateDataConvertor: Ecoregion name '{ecoRegionHeaders[i]}' in climate file '{climateFile}' is not recognized or is inactive");
                        }
                    }
                    else
                        // skip ecoregion header line
                        reader.ReadLine();

                    // skip data headers
                    reader.ReadLine();

                    // get next line as first line of data
                    fields = reader.ReadLine().Replace(" ", "").Split(',').ToList();

                    sectionYear = -999;
                    sectionYearIndex = -1;
                    sectionRowIndex = -1;
                }


                // **
                // process line of data


                // grab the timeStep as the first field and remove it from the data
                var timeStep = fields[0];
                fields.RemoveAt(0);

                ++sectionRowIndex;
                // if this is the first section for this group, add the timeStep to the group timeStep List
                // otherwise check that the timeStep matches that of the same row in the first section for the group
                //  this also ensures that the sectionYearIndex exists in the yearEcoRecords below
                if (groupSectionCounts[groupIndex] == 1)
                {
                    groupTimeSteps[groupIndex].Add(timeStep);
                }
                else if (sectionRowIndex > groupTimeSteps[groupIndex].Count - 1 || timeStep != groupTimeSteps[groupIndex][sectionRowIndex])
                {
                    throw new ApplicationException($"Error in ClimateDataConvertor: Timestamp order mismatch in section '{section}', timestamp '{timeStep}', sectionRowIndex {sectionRowIndex}, in climate file '{climateFile}'.");
                }

                // parse out the year
                var year = int.Parse(timeStep.Substring(0, 4));

                if (year != sectionYear)
                {
                    // start of a new year
                    sectionYear = year;
                    ++sectionYearIndex;

                    // if this is the first section for the group, then make a new yearEcoRecord and add it to the output data, either precip or wind
                    if (groupSectionCounts[groupIndex] == 1)
                    {
                        yearEcoRecords = new List<ClimateRecord>[_modelCore.Ecoregions.Count];
                        for (var i = 0; i < _modelCore.Ecoregions.Count; ++i)
                            yearEcoRecords[i] = new List<ClimateRecord>();

                        if (groupIndex == 0)
                        {
                            precipClimateRecords.Add(yearEcoRecords);
                            precipYearKeys.Add(year);
                        }
                        else if (groupIndex == 1)
                        {
                            windClimateRecords.Add(yearEcoRecords);
                            windYearKeys.Add(year);
                        }
                    }
                    else
                    {
                        // if not the first section, grab the ecorecords for this year, either precip or wind
                        yearEcoRecords = groupIndex == 0 ? precipClimateRecords[sectionYearIndex] : windClimateRecords[sectionYearIndex];
                    }

                    sectionYearRowIndex = -1;
                }


                // **
                // incorporate (or add) this row's data into yearEcoRecords

                ++sectionYearRowIndex;

                // if this is the first section for the group, add new ClimateRecords for each ecoregion.
                if (groupSectionCounts[groupIndex] == 1)
                    for (var i = 0; i < _modelCore.Ecoregions.Count; ++i)
                        yearEcoRecords[i].Add(new ClimateRecord());

                for (var i = 0; i < ecoRegionCount; ++i)
                {
                    var ecoRecord = yearEcoRecords[ecoRegionIndexMap[i]][sectionYearRowIndex];      // if this is the first section for the group, sectionYearRowIndex will give the record just instantiated above

                    // JM: the next line assumes all input files have exactly three groups of columns: Mean, Variance, Std_dev
                    var mean = double.Parse(fields[i]);
                    var variance = double.Parse(fields[ecoRegionCount + i]);
                    var stdev = double.Parse(fields[2 * ecoRegionCount + i]);

                    if (groupIndex == 0)
                    {
                        // Required parameters
                        switch (section)
                        {
                            case FileSection.MinTemperature:
                                ecoRecord.MinTemp = mean + format.TemperatureTransformation;
                                break;

                            case FileSection.MaxTemperature:
                                ecoRecord.MaxTemp = mean + format.TemperatureTransformation;
                                break;

                            case FileSection.Precipitation:
                                ecoRecord.Precip = mean * format.PrecipTransformation;
                                break;

                        }
                    }
                    else if (groupIndex == 1)
                    {
                        // Optional parameters
                        switch (section)
                        {
                            case FileSection.Temperature:
                                ecoRecord.Temp = mean + format.TemperatureTransformation;
                                break;

                            case FileSection.Winddirection:
                                mean += format.WindDirectionTransformation;
                                if (mean > 360.0) mean -= 360;
                                ecoRecord.WindDirection = mean;
                                break;

                            case FileSection.Windspeed:
                                ecoRecord.WindSpeed = mean * format.WindSpeedTransformation;
                                break;

                            case FileSection.WindEasting:
                                ecoRecord.WindEasting = mean;
                                break;

                            case FileSection.WindNorthing:
                                ecoRecord.WindNorthing = mean;
                                break;

                            case FileSection.NDeposition:
                                ecoRecord.NDeposition = mean;
                                break;

                            case FileSection.CO2:
                                ecoRecord.CO2 = mean;
                                break;

                            case FileSection.MinRelativeHumidity:
                                ecoRecord.MinRH = mean;
                                break;

                            case FileSection.MaxRelativeHumidity:
                                ecoRecord.MaxRH = mean;
                                break;

                            case FileSection.RelativeHumidity:
                                ecoRecord.RH = mean;
                                break;

                            case FileSection.SpecificHumidity:
                                ecoRecord.SpecificHumidity = mean;
                                break;

                            case FileSection.PET:
                                ecoRecord.PET = mean;
                                break;

                            case FileSection.PAR:
                                ecoRecord.PAR = mean;
                                break;

                            case FileSection.Ozone:
                                ecoRecord.Ozone = mean;
                                break;

                            case FileSection.ShortWaveRadiation:
                                ecoRecord.ShortWaveRadiation = mean;
                                break;
                        }
                    }
                }
            }

            reader.Close();

            // ** 
            // basic data checks

            for (var i = 0; i < precipClimateRecords.Count; ++i)
            {
                var ecoRecords = precipClimateRecords[i][ecoRegionIndexMap[0]];     // check the first eco region provided in the file

                if (climateTimeStep == TimeSeriesTimeStep.Monthly && ecoRecords.Count != 12)
                    throw new ApplicationException($"Error in ClimateDataConvertor: Precip/Tmax/Tmin, etc. Monthly data for year {precipYearKeys[i]} in climate file '{climateFile}' do not have 12 records. The year has {ecoRecords.Count} records.");

                if (climateTimeStep == TimeSeriesTimeStep.Daily && ecoRecords.Count != 365 && ecoRecords.Count != 366)
                    throw new ApplicationException($"Error in ClimateDataConvertor: Precip/Tmax/Tmin, etc. Daily data for year {precipYearKeys[i]} in climate file '{climateFile}' do not have 365 or 366 records. The year has {ecoRecords.Count} records.");
            }

            // if wind data exist, check them, too
            if (groupSectionCounts[1] > 0)
            {
                for (var i = 0; i < windClimateRecords.Count; ++i)
                {
                    var ecoRecords = windClimateRecords[i][ecoRegionIndexMap[0]];     // check the first eco region provided in the file

                    if (climateTimeStep == TimeSeriesTimeStep.Monthly && ecoRecords.Count != 12)
                        throw new ApplicationException($"Error in ClimateDataConvertor: Wind Monthly data for year {windYearKeys[i]} in climate file '{climateFile}' do not have 12 records. The year has {ecoRecords.Count} records.");

                    if (climateTimeStep == TimeSeriesTimeStep.Daily && ecoRecords.Count != 365 && ecoRecords.Count != 366)
                        throw new ApplicationException($"Error in ClimateDataConvertor: Wind Daily data for year {windYearKeys[i]} in climate file '{climateFile}' do not have 365 or 366 records. The year has {ecoRecords.Count} records.");
                }

                // also check that the number of years matches that of the precip data
                if (precipClimateRecords.Count != windClimateRecords.Count)
                    throw new ApplicationException($"Error in ClimateDataConvertor: The number of years ({precipClimateRecords.Count}) of Precip/Tmax/Tmin, etc. data does not equal the number of years ({windClimateRecords.Count}) of Wind data in climate file '{climateFile}'.");
            }

            // **
            // if wind data exist, combine into precip data
            if (groupSectionCounts[1] > 0)
            {
                for (var i = 0; i < precipClimateRecords.Count; ++i)
                {
                    for (var j = 0; j < _modelCore.Ecoregions.Count; ++j)
                    {
                        for (var k = 0; k < precipClimateRecords[i][j].Count; ++k)
                        {
                            var precipRecord = precipClimateRecords[i][j][k];
                            var windRecord = windClimateRecords[i][j][k];

                            precipRecord.Temp = windRecord.Temp;

                            precipRecord.WindDirection = windRecord.WindDirection;
                            precipRecord.WindSpeed = windRecord.WindSpeed;
                            precipRecord.WindEasting = windRecord.WindEasting;
                            precipRecord.WindNorthing = windRecord.WindNorthing;

                            precipRecord.NDeposition = windRecord.NDeposition;
                            precipRecord.CO2 = windRecord.CO2;

                            precipRecord.MinRH = windRecord.MinRH;
                            precipRecord.MaxRH = windRecord.MaxRH;
                            precipRecord.RH = windRecord.RH;
                            precipRecord.SpecificHumidity = windRecord.SpecificHumidity;

                            precipRecord.PET = windRecord.PET;
                            precipRecord.PAR = windRecord.PAR;
                            precipRecord.Ozone = windRecord.Ozone;
                            precipRecord.ShortWaveRadiation = windRecord.ShortWaveRadiation;
                        }
                    }
                }
            }


            // **
            // normalize daily data for leap years into 365 days
            if (climateTimeStep == TimeSeriesTimeStep.Daily)
            {
                foreach (var yEcoRecords in precipClimateRecords)
                {
                    foreach (var ecoRecords in yEcoRecords)
                    {
                        if (ecoRecords.Count == 366)
                        {
                            var feb28Record = ecoRecords[58]; // get data for Feb. 28 (day 59).
                            var feb29Record = ecoRecords[59]; // get data for Feb. 29 (day 60).
                            ecoRecords.RemoveAt(59); // remove Feb. 29 from the ecoRecords

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
                                ecoRecords[j].Precip += avgPptIncrement;
                                ecoRecords[j].NDeposition += avgNDepositionIncrement;
                            }
                        }
                    }
                }
            }

            
            // **
            // calculate missing data from other data when possible
            foreach (var rec in precipClimateRecords.SelectMany(x => x.SelectMany(y => y)))
            {
                // if Temp is missing, then average MinTemp and MaxTemp
                if (double.IsNaN(rec.Temp)) rec.Temp = 0.5 * (rec.MinTemp + rec.MaxTemp);

                // if WindEasting and WindNorthing exist, calculate WindDirection and WindSpeed
                if ((double.IsNaN(rec.WindDirection) || double.IsNaN(rec.WindSpeed)) && !double.IsNaN(rec.WindEasting) && !double.IsNaN(rec.WindNorthing))
                {
                    rec.WindSpeed = Math.Sqrt(rec.WindEasting * rec.WindEasting + rec.WindNorthing * rec.WindNorthing) * format.WindSpeedTransformation;
                    var t = Math.Atan2(-rec.WindNorthing, rec.WindEasting) * 180.0 / Math.PI + 90.0;
                    if (t < 0.0) t += 360.0;
                    rec.WindDirection = t;
                }

                // if RH is missing then calculate from MinRH and MaxRH or from SpecificHumidity
                if (double.IsNaN(rec.RH))
                {
                    if (!double.IsNaN(rec.MinRH) && !double.IsNaN(rec.MaxRH))
                        rec.RH = 0.5 * (rec.MinRH + rec.MaxRH);
                    else if (!double.IsNaN(rec.SpecificHumidity))
                        rec.RH = CalculateRelativeHumidity(rec.SpecificHumidity, rec.Temp);
                }
            }

            // **
            // calculate fire weather data for daily input data
            if (climateTimeStep == TimeSeriesTimeStep.Daily && ConfigParameters.UsingFireClimate)
            {
                foreach (var yearRecords in precipClimateRecords.SelectMany(x => x.Select(y => y)))
                {
                    CalculateDailyFireWeather(yearRecords);
                }
            }

            // **
            // final data structures to return

            // calendarYears is the list of years in the file, e.g. 1950, 1951, etc. taken from the precip timesteps
            calendarYears = precipYearKeys;
            climateRecords = precipClimateRecords.ToArray();

            // swap indexing to [ecoregion][yearIndex] -> List<ClimateRecord>
            var swappedClimateRecords = new List<ClimateRecord>[_modelCore.Ecoregions.Count][];
            for (var i = 0; i < _modelCore.Ecoregions.Count; ++i)
            {
                swappedClimateRecords[i] = new List<ClimateRecord>[calendarYears.Count];
                for (var j = 0; j < calendarYears.Count; ++j)
                {
                    swappedClimateRecords[i][j] = climateRecords[j][i];
                }
            }

            climateRecords = swappedClimateRecords;
        }

        private static double CalculateRelativeHumidity(double specificHumidity, double temp)
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
    }
}
