//  Authors:  Amin Almassian, Robert M. Scheller, John McNabb, Melissa Lucash

using System;
//using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Landis.Core;

namespace Landis.Library.Climate
{
    public class ClimateDataConvertor
    {        
        // JM: private enum used in parsing data
        private enum FileSection
        {
            Precipitation = 1,
            MaxTemperature = 2,
            MinTemperature = 3,
            NDeposition = 6,
            Winddirection = 4,
            Windspeed = 5,
            CO2 = 7,
            MaxRelativeHumidity = 8,
            MinRelativeHumidity = 9,
            PAR = 10,
            Ozone = 11,
            ShortWaveRadiation = 12
        }

        public static void Convert_USGS_to_ClimateData_FillAlldata(TemporalGranularity timeStep, string climateFile, string climateFileFormat, Climate.Phase climatePhase)
        {

            // **
            // John McNabb:  new parsing code
            List<int> yearKeys;
            List<List<ClimateRecord>[]> climateRecords;

            Convert_USGS_to_ClimateData2(timeStep, climateFile, climateFileFormat, out yearKeys, out climateRecords);

            Dictionary<int, ClimateRecord[][]> allDataRef = null; //this dictionary is filled out either by Daily data or Monthly
            if (climatePhase == Climate.Phase.Future_Climate)
                allDataRef = Climate.Future_AllData;

            if (climatePhase == Climate.Phase.SpinUp_Climate)
                allDataRef = Climate.Spinup_AllData;

            if (allDataRef == null)
                allDataRef = new Dictionary<int, ClimateRecord[][]>();
            else
                allDataRef.Clear();

            for (var i = 0; i < yearKeys.Count; ++i)
            {
                var ecoRecords = new ClimateRecord[Climate.ModelCore.Ecoregions.Count][];
                allDataRef[yearKeys[i]] = ecoRecords;

                for (var j = 0; j < Climate.ModelCore.Ecoregions.Count; ++j)
                {
                    // convert the parsed climateRecords for this year from List<ClimateRecord>[] to ClimateRecord[][]
                    ecoRecords[j] = climateRecords[i][j].ToArray();
                }
            }
        }

        // no longer used
        private static void Convert_USGS_to_ClimateData(TemporalGranularity sourceTemporalGranularity, string climateFile, string climateFileFormat, out List<string> timeStamps, out List<ClimateRecord>[] climateRecords)
        {
            // each item in timeStamps is the timeStamp 'key' for the data
            // each item in climateRecords is of length Climate.ModelCore.Ecoregions.Count, and is filled in with data from the input file, indexed by Ecoregion.Index.

            timeStamps = new List<string>();
            climateRecords = new List<ClimateRecord>[Climate.ModelCore.Ecoregions.Count];
            for (var i = 0; i < Climate.ModelCore.Ecoregions.Count; ++i)
                climateRecords[i] = new List<ClimateRecord>();

            // get trigger words for parsing based on file format
            ClimateFileFormatProvider format = new ClimateFileFormatProvider(climateFileFormat);

            if (!File.Exists(climateFile))
            {
                throw new ApplicationException("Error in ClimateDataConvertor: Cannot open climate file" + climateFile);
            }

            var reader = File.OpenText(climateFile);

            Climate.TextLog.WriteLine("   Converting raw data from text file: {0}, Format={1}, Temporal={2}.", climateFile, climateFileFormat.ToLower(), sourceTemporalGranularity);

            // maps from ecoregion column index in the input file to the ecoregion.index for the region
            int[] ecoRegionIndexMap = null;
            var ecoRegionCount = 0;

            var rowIndex = -1;
            var sectionIndex = -1;
            FileSection section = 0;

            string row;

            while ((row = reader.ReadLine()) != null)
            {
                var fields = row.Replace(" ", "").Split(',').ToList();    // JM: don't know if stripping blanks is needed, but just in case

                // check for trigger word
                if (fields[0].StartsWith("#"))
                {
                    // determine which section we're in
                    var triggerWord = fields[0].TrimStart('#');   // remove the leading "#"

                    if (format.PrecipTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                        section = FileSection.Precipitation;
                    else if (format.MaxTempTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                        section = FileSection.MaxTemperature;
                    else if (format.MinTempTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                        section = FileSection.MinTemperature;
                    else if (format.NDepositionTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                        section = FileSection.NDeposition;
                    else if (format.WindDirectionTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                        section = FileSection.Winddirection;
                    else if (format.WindSpeedTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                        section = FileSection.Windspeed;
                    else if (format.CO2TriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                        section = FileSection.CO2;
                    else if (format.MaxRHTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                        section = FileSection.MaxRelativeHumidity;
                    else if (format.MinRHTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                        section = FileSection.MinRelativeHumidity;
                    else if (format.PARTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                        section = FileSection.PAR;
                    else if (format.OzoneTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                        section = FileSection.Ozone;
                    else if (format.ShortWaveRadiationTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                        section = FileSection.ShortWaveRadiation;
                    else
                        throw new ApplicationException(string.Format("Error in ClimateDataConvertor: Unrecognized trigger word '{0}' in climate file '{1}'.", triggerWord, climateFile));

                    sectionIndex++;

                    // if this is the first section then parse the ecoregions, etc.
                    if (sectionIndex == 0)
                    {
                        // read next line to get ecoregion headers
                        var ecoRegionHeaders = reader.ReadLine().Replace(" ", "").Split(',').ToList();
                        ecoRegionHeaders.RemoveAt(0);   // remove blank cell at the beginning of ecoregion header row

                        // JM: the next line assumes all input files have exactly three groups of columns: Mean, Variance, Std_dev
                        ecoRegionCount = ecoRegionHeaders.Count / 3;

                        if (ecoRegionCount == 0)
                            throw new ApplicationException(string.Format("Error in ClimateDataConvertor: climate file '{0}' contains no ecoregion data.", climateFile));

                        var modelCoreActiveEcoRegionCount = Climate.ModelCore.Ecoregions.Count(x => x.Active);

                        if (ecoRegionCount != modelCoreActiveEcoRegionCount)
                            throw new ApplicationException(string.Format("Error in ClimateDataConvertor: climate file '{0}' contains data for {1} ecoregions, but the simulation has {2} active ecoregions.", climateFile, ecoRegionCount, modelCoreActiveEcoRegionCount));

                        // determine the map from ecoregions in this file to ecoregion indices in ModelCore
                        ecoRegionIndexMap = new int[ecoRegionCount];
                        for (var i = 0; i < ecoRegionCount; ++i)
                        {
                            IEcoregion eco = Climate.ModelCore.Ecoregions[ecoRegionHeaders[i]];     // JM:  Ecoregions appear to be indexed by string name, but I don't know if it is case-sensitive.
                            if (eco != null && eco.Active)
                                ecoRegionIndexMap[i] = eco.Index;
                            else
                                throw new ApplicationException(string.Format("Error in ClimateDataConvertor: Ecoregion name '{0}' in climate file '{1}' is not recognized or is inactive", ecoRegionHeaders[i], climateFile));
                        }
                    }
                    else
                        // skip ecoregion header line
                        reader.ReadLine();

                    // skip data headers
                    reader.ReadLine();

                    // get next line as first line of data
                    fields = reader.ReadLine().Replace(" ", "").Split(',').ToList();

                    // reset row index
                    rowIndex = -1;
                }


                // **
                // process line of data
                
                ++rowIndex;

                // grab the key as the first field and remove it from the data
                var key = fields[0];
                fields.RemoveAt(0);

                // if this is the first section then add key to timestamps and add new climate records for this rowIndex to the ecoregion array
                if (sectionIndex == 0)
                {
                    timeStamps.Add(key);
                    for (var i = 0; i < Climate.ModelCore.Ecoregions.Count; ++i)
                        climateRecords[i].Add(new ClimateRecord());
                }
                else
                {
                    // check that the timestamp key order matches
                    if (key != timeStamps[rowIndex])
                        throw new ApplicationException(string.Format("Error in ClimateDataConvertor: Timestamp order mismatch in section '{0}' timestamp '{1}' in climate file '{2}'.", section, key, climateFile));
                }

                for (var i = 0; i < ecoRegionCount; ++i)
                {
                    var ecoIndex = ecoRegionIndexMap[i];

                    var ecoRecords = climateRecords[ecoIndex];

                    // JM: the next line assumes all input files have exactly three groups of columns: Mean, Variance, Std_dev
                    var mean = double.Parse(fields[i]);
                    var variance = double.Parse(fields[ecoRegionCount + i]);
                    var stdev = double.Parse(fields[2 * ecoRegionCount + i]);

                    switch (section)
                    {
                        case FileSection.Precipitation:
                            ecoRecords[rowIndex].AvgPpt = mean * format.PrecipTransformation;
                            ecoRecords[rowIndex].StdDevPpt = stdev * format.PrecipTransformation;
                            break;

                        case FileSection.MaxTemperature:
                        case FileSection.MinTemperature:

                            mean += format.TemperatureTransformation;


                            if (section == FileSection.MaxTemperature)
                                ecoRecords[rowIndex].AvgMaxTemp = mean;
                            else
                                ecoRecords[rowIndex].AvgMinTemp = mean;

                            // for temperature variance wait until both min and max have been read before calculating the final value
                            if (ecoRecords[rowIndex].AvgVarTemp == -99.0)
                                ecoRecords[rowIndex].AvgVarTemp = variance;        // set AvgVarTemp to the first value we have (min or max)
                            else
                                // have both min and max, so average the variance
                                ecoRecords[rowIndex].AvgVarTemp = (ecoRecords[rowIndex].AvgVarTemp + variance) / 2.0;

                            ecoRecords[rowIndex].StdDevTemp = System.Math.Sqrt(ecoRecords[rowIndex].AvgVarTemp);      // this will set the st dev even if the data file only has one temperature section
                            break;

                        case FileSection.Winddirection:
                            mean += format.WindDirectionTransformation;
                            if (mean > 360.0) mean -= 360;
                            ecoRecords[rowIndex].AvgWindDirection = mean;
                                                       
                            ecoRecords[rowIndex].AvgVarWindDirection = variance;
                            ecoRecords[rowIndex].StdDevWindDirection = stdev;
                            break;

                        case FileSection.Windspeed:
                            ecoRecords[rowIndex].AvgWindSpeed = mean * format.WindSpeedTransformation;
                            ecoRecords[rowIndex].AvgVarWindSpeed = variance;
                            ecoRecords[rowIndex].StdDevWindSpeed = stdev;
                            break;

                        case FileSection.NDeposition:
                            ecoRecords[rowIndex].AvgNDeposition = mean;
                            ecoRecords[rowIndex].AvgVarNDeposition = variance;
                            ecoRecords[rowIndex].StdDevNDeposition = stdev;
                            break;


                        case FileSection.CO2:
                            ecoRecords[rowIndex].AvgCO2 = mean;
                            ecoRecords[rowIndex].AvgVarCO2 = variance;
                            ecoRecords[rowIndex].StdDevCO2 = stdev;
                            break;
                        case FileSection.PAR:
                            ecoRecords[rowIndex].AvgPAR = mean;
                            ecoRecords[rowIndex].AvgVarPAR = variance;
                            ecoRecords[rowIndex].StdDevPAR = stdev;
                            break;
                        case FileSection.Ozone:
                            ecoRecords[rowIndex].AvgOzone = mean;
                            ecoRecords[rowIndex].AvgVarOzone = variance;
                            ecoRecords[rowIndex].StdDevOzone = stdev;
                            break;
                        case FileSection.ShortWaveRadiation:
                            ecoRecords[rowIndex].AvgShortWaveRadiation = mean;
                            ecoRecords[rowIndex].AvgVarShortWaveRadiation = variance;
                            ecoRecords[rowIndex].StdDevShortWaveRadiation = stdev;
                            break;
                    }
                }                
            }
        }

        private static void Convert_USGS_to_ClimateData2(TemporalGranularity sourceTemporalGranularity, string climateFile, string climateFileFormat, out List<int> yearKeys, out List<List<ClimateRecord>[]> climateRecords)
        {
            var precipYearKeys = new List<int>();
            var windYearKeys = new List<int>();

            // indexing of precip and wind ClimateRecords:  [yearIndex][ecoregion][i], where yearIndex is [0..n] corresponding to the yearKeys index, i.e. index 0 is for 1950, index 1 for 1951, etc.
            var precipClimateRecords = new List<List<ClimateRecord>[]>();
            var windClimateRecords = new List<List<ClimateRecord>[]>();
        

            // get trigger words for parsing based on file format
            ClimateFileFormatProvider format = new ClimateFileFormatProvider(climateFileFormat);

            if (!File.Exists(climateFile))
            {
                throw new ApplicationException("Error in ClimateDataConvertor: Cannot open climate file" + climateFile);
            }

            var reader = File.OpenText(climateFile);

            Climate.TextLog.WriteLine("   Converting raw data from text file: {0}, Format={1}, Temporal={2}.", climateFile, climateFileFormat.ToLower(), sourceTemporalGranularity);

            // maps from ecoregion column index in the input file to the ecoregion.index for the region
            int[] ecoRegionIndexMap = null;
            var ecoRegionCount = 0;

            var numberOfGroups = 2;     // the number of allowed groups. Presently:  (1) precip, tmin, tmax, Ndep, CO2;  (2) winddirection, windspeed,  minRH, maxRH
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

                    if (format.PrecipTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.Precipitation;
                        groupIndex = 0;
                    }
                    else if (format.MaxTempTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.MaxTemperature;
                        groupIndex = 0;
                    }
                    else if (format.MinTempTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.MinTemperature;
                        groupIndex = 0;
                    }
                    else if (format.NDepositionTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.NDeposition;
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
                    else if (format.CO2TriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.CO2;
                        groupIndex = 0;
                    }
                    else if (format.MaxRHTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.MaxRelativeHumidity;
                        groupIndex = 1;
                    }
                    else if (format.MinRHTriggerWord.FindIndex(x => x.Equals(triggerWord, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        section = FileSection.MinRelativeHumidity;
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


                    else
                        throw new ApplicationException(string.Format("Error in ClimateDataConvertor: Unrecognized trigger word '{0}' in climate file '{1}'.", triggerWord, climateFile));

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
                            throw new ApplicationException(string.Format("Error in ClimateDataConvertor: climate file '{0}' contains no ecoregion data.", climateFile));

                        var modelCoreActiveEcoRegionCount = Climate.ModelCore.Ecoregions.Count(x => x.Active);

                        if (ecoRegionCount != modelCoreActiveEcoRegionCount)
                            throw new ApplicationException(string.Format("Error in ClimateDataConvertor: climate file '{0}' contains data for {1} ecoregions, but the simulation has {2} active ecoregions.", climateFile, ecoRegionCount, modelCoreActiveEcoRegionCount));

                        // determine the map from ecoregions in this file to ecoregion indices in ModelCore
                        ecoRegionIndexMap = new int[ecoRegionCount];
                        for (var i = 0; i < ecoRegionCount; ++i)
                        {
                            IEcoregion eco = Climate.ModelCore.Ecoregions[ecoRegionHeaders[i]];     // JM:  Ecoregions appear to be indexed by string name, but I don't know if it is case-sensitive.
                            if (eco != null && eco.Active)
                                ecoRegionIndexMap[i] = eco.Index;
                            else
                                throw new ApplicationException(string.Format("Error in ClimateDataConvertor: Ecoregion name '{0}' in climate file '{1}' is not recognized or is inactive", ecoRegionHeaders[i], climateFile));
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
                    throw new ApplicationException(string.Format("Error in ClimateDataConvertor: Timestamp order mismatch in section '{0}', timestamp '{1}', sectionRowIndex {2}, in climate file '{3}'.", section, timeStep, sectionRowIndex, climateFile));
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
                        yearEcoRecords = new List<ClimateRecord>[Climate.ModelCore.Ecoregions.Count];
                        for (var i = 0; i < Climate.ModelCore.Ecoregions.Count; ++i)
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
                    for (var i = 0; i < Climate.ModelCore.Ecoregions.Count; ++i)
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
                        // "precip" group
                        switch (section)
                        {
                            case FileSection.Precipitation:
                                ecoRecord.AvgPpt = mean * format.PrecipTransformation;
                                ecoRecord.StdDevPpt = stdev * format.PrecipTransformation;
                                break;

                            case FileSection.MaxTemperature:
                            case FileSection.MinTemperature:

                                mean += format.TemperatureTransformation;


                                if (section == FileSection.MaxTemperature)
                                    ecoRecord.AvgMaxTemp = mean;
                                else
                                    ecoRecord.AvgMinTemp = mean;

                                // for temperature variance wait until both min and max have been read before calculating the final value
                                if (ecoRecord.AvgVarTemp == -99.0)
                                    ecoRecord.AvgVarTemp = variance; // set AvgVarTemp to the first value we have (min or max)
                                else
                                    // have both min and max, so average the variance
                                    ecoRecord.AvgVarTemp = (ecoRecord.AvgVarTemp + variance) / 2.0;

                                ecoRecord.StdDevTemp = System.Math.Sqrt(ecoRecord.AvgVarTemp); // this will set the st dev even if the data file only has one temperature section
                                break;

                            case FileSection.NDeposition:
                                ecoRecord.AvgNDeposition = mean;
                                ecoRecord.AvgVarNDeposition = variance;
                                ecoRecord.StdDevNDeposition = stdev;
                                break;

                            case FileSection.CO2:
                                ecoRecord.AvgCO2 = mean;
                                ecoRecord.AvgVarCO2 = variance;
                                ecoRecord.StdDevCO2 = stdev;
                                break;
                        }
                    }
                    else if (groupIndex == 1)
                    {
                        // "wind and RH" group
                        switch (section)
                        {
                            case FileSection.Winddirection:
                                mean += format.WindDirectionTransformation;
                                if (mean > 360.0) mean -= 360;
                                ecoRecord.AvgWindDirection = mean;

                                ecoRecord.AvgVarWindDirection = variance;
                                ecoRecord.StdDevWindDirection = stdev;
                                break;

                            case FileSection.Windspeed:
                                ecoRecord.AvgWindSpeed = mean * format.WindSpeedTransformation;
                                ecoRecord.AvgVarWindSpeed = variance;
                                ecoRecord.StdDevWindSpeed = stdev;
                                break;

                            case FileSection.MaxRelativeHumidity:
                            case FileSection.MinRelativeHumidity:

                                if (section == FileSection.MaxRelativeHumidity)
                                    ecoRecord.AvgMaxRH = mean;
                                else
                                    ecoRecord.AvgMinRH = mean;

                                ecoRecord.AvgVarRH = variance;
                                ecoRecord.StdDevRH = stdev;

                                break;

                            case FileSection.PAR:
                                ecoRecord.AvgPAR = mean;
                                ecoRecord.AvgVarPAR = variance;
                                ecoRecord.StdDevPAR = stdev;
                                break;
                            case FileSection.Ozone:
                                ecoRecord.AvgOzone = mean;
                                ecoRecord.AvgVarOzone = variance;
                                ecoRecord.StdDevOzone = stdev;
                                break;
                            case FileSection.ShortWaveRadiation:
                                ecoRecord.AvgShortWaveRadiation = mean;
                                ecoRecord.AvgVarShortWaveRadiation = variance;
                                ecoRecord.StdDevShortWaveRadiation = stdev;
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

                if (sourceTemporalGranularity == TemporalGranularity.Monthly && ecoRecords.Count != 12)
                    throw new ApplicationException(string.Format("Error in ClimateDataConvertor: Precip/Tmax/Tmin, etc. Monthly data for year {0} in climate file '{1}' do not have 12 records. The year has {2} records.", precipYearKeys[i], climateFile, ecoRecords.Count));

                if (sourceTemporalGranularity == TemporalGranularity.Daily && ecoRecords.Count != 365 && ecoRecords.Count != 366)
                    throw new ApplicationException(string.Format("Error in ClimateDataConvertor: Precip/Tmax/Tmin, etc. Daily data for year {0} in climate file '{1}' do not have 365 or 366 records. The year has {2} records.", precipYearKeys[i], climateFile, ecoRecords.Count));
            }

            // if wind data exist, check them, too
            if (groupSectionCounts[1] > 0)
            {
                for (var i = 0; i < windClimateRecords.Count; ++i)
                {
                    var ecoRecords = windClimateRecords[i][ecoRegionIndexMap[0]];     // check the first eco region provided in the file

                    if (sourceTemporalGranularity == TemporalGranularity.Monthly && ecoRecords.Count != 12)
                        throw new ApplicationException(string.Format("Error in ClimateDataConvertor: Wind Monthly data for year {0} in climate file '{1}' do not have 12 records. The year has {2} records.", windYearKeys[i], climateFile, ecoRecords.Count));

                    if (sourceTemporalGranularity == TemporalGranularity.Daily && ecoRecords.Count != 365 && ecoRecords.Count != 366)
                        throw new ApplicationException(string.Format("Error in ClimateDataConvertor: Wind Daily data for year {0} in climate file '{1}' do not have 365 or 366 records. The year has {2} records.", windYearKeys[i], climateFile, ecoRecords.Count));
                }

                // also check that the number of years matches that of the precip data
                if (precipClimateRecords.Count != windClimateRecords.Count)
                    throw new ApplicationException(string.Format("Error in ClimateDataConvertor: The number of years ({0}) of Precip/Tmax/Tmin, etc. data does not equal the number of years ({1}) of Wind data in climate file '{2}'.", precipClimateRecords.Count, windClimateRecords.Count, climateFile));
            }

            // **
            // normalize daily data for leap years into 365 days
            if (sourceTemporalGranularity == TemporalGranularity.Daily)
            {
                // precip data first
                foreach (var yEcoRecords in precipClimateRecords)
                    foreach (var ecoRecords in yEcoRecords)
                        if (ecoRecords.Count == 366)
                        {
                            var feb28Record = ecoRecords[58];      // get data for Feb. 28 (day 59).
                            var feb29Record = ecoRecords[59];      // get data for Feb. 29 (day 60).
                            ecoRecords.RemoveAt(59);               // remove Feb. 29 from the ecoRecords

                            // ignore std. dev. and variance data from Feb. 29.

                            // average some Feb. 29 values with their corresponding Feb. 28 values
                            feb28Record.AvgMinTemp = 0.5 * (feb28Record.AvgMinTemp + feb29Record.AvgMinTemp);
                            feb28Record.AvgMaxTemp = 0.5 * (feb28Record.AvgMaxTemp + feb29Record.AvgMaxTemp);
                            feb28Record.AvgMinRH = 0.5 * (feb28Record.AvgMinRH + feb29Record.AvgMinRH);
                            feb28Record.AvgMaxRH = 0.5 * (feb28Record.AvgMaxRH + feb29Record.AvgMaxRH);
                            feb28Record.AvgCO2 = 0.5 * (feb28Record.AvgCO2 + feb29Record.AvgCO2);
                            feb28Record.AvgPAR = 0.5 * (feb28Record.AvgPAR + feb29Record.AvgPAR);

                            // amortize (spread out) some Feb. 29 values over the entire month so that a monthly total still contains the Feb. 29 value.
                            //  do this rather than simply adding the Feb. 28 and Feb. 29 values, which would leave a spike in the final Feb. 28 data.
                            var avgPptIncrement = feb28Record.AvgPpt / 28.0;
                            var avgNDepositionIncrement = feb28Record.AvgNDeposition / 28.0;

                            var feb1 = 31;      // Feb. 1 index (day 32)                      
                            for (var j = feb1; j < feb1 + 28; ++j)
                            {
                                ecoRecords[j].AvgPpt += avgPptIncrement;
                                ecoRecords[j].AvgNDeposition += avgNDepositionIncrement;
                            }
                        }

                // wind data next (if it exists)
                if (groupSectionCounts[1] > 0)
                    foreach (var yEcoRecords in windClimateRecords)
                        foreach (var ecoRecords in yEcoRecords)
                            if (ecoRecords.Count == 366)
                            {
                                var feb28Record = ecoRecords[58];      // get data for Feb. 28 (day 59).
                                var feb29Record = ecoRecords[59];      // get data for Feb. 29 (day 60).
                                ecoRecords.RemoveAt(59);               // remove Feb. 29 from the ecoRecords

                                // ignore std. dev. and variance data from Feb. 29.

                                // average some Feb. 29 values with their corresponding Feb. 28 values
                                feb28Record.AvgWindDirection = 0.5 * (feb28Record.AvgWindDirection + feb29Record.AvgWindDirection);
                                feb28Record.AvgWindSpeed = 0.5 * (feb28Record.AvgWindSpeed + feb29Record.AvgWindSpeed);
                                feb28Record.AvgMinRH = 0.5 * (feb28Record.AvgMinRH + feb29Record.AvgMinRH);
                                feb28Record.AvgMaxRH = 0.5 * (feb28Record.AvgMaxRH + feb29Record.AvgMaxRH);
                            }

            }


            // **
            // if wind data exist, combine them with precip data
            if (groupSectionCounts[1] > 0)
                for (var i = 0; i < precipClimateRecords.Count; ++i)
                    for (var j = 0; j < Climate.ModelCore.Ecoregions.Count; ++j)
                        for (var k = 0; k < precipClimateRecords[i][j].Count; ++k)
                        {
                            var precipRecord = precipClimateRecords[i][j][k];
                            var windRecord = windClimateRecords[i][j][k];

                            precipRecord.AvgWindDirection = windRecord.AvgWindDirection;
                            precipRecord.AvgVarWindDirection = windRecord.AvgVarWindDirection;
                            precipRecord.StdDevWindDirection = windRecord.StdDevWindDirection;

                            precipRecord.AvgWindSpeed = windRecord.AvgWindSpeed;
                            precipRecord.AvgVarWindSpeed = windRecord.AvgVarWindSpeed;
                            precipRecord.StdDevWindSpeed = windRecord.StdDevWindSpeed;

                            precipRecord.AvgMinRH = windRecord.AvgMinRH;
                            precipRecord.AvgMaxRH = windRecord.AvgMaxRH;
                            precipRecord.AvgVarRH = windRecord.AvgVarRH;
                            precipRecord.StdDevRH = windRecord.StdDevRH;

                            precipRecord.AvgPAR = windRecord.AvgPAR;
                            precipRecord.StdDevPAR = windRecord.StdDevPAR;
                            precipRecord.AvgVarPAR = windRecord.AvgVarPAR;

                            precipRecord.AvgOzone = windRecord.AvgOzone;
                            precipRecord.AvgVarOzone = windRecord.AvgVarOzone;
                            precipRecord.StdDevOzone = windRecord.StdDevOzone;

                            precipRecord.AvgShortWaveRadiation = windRecord.AvgShortWaveRadiation;
                            precipRecord.AvgVarShortWaveRadiation = windRecord.AvgVarShortWaveRadiation;
                            precipRecord.StdDevShortWaveRadiation = windRecord.StdDevShortWaveRadiation;
                        }

            // **
            // final data structures to return

            // yearKeys is the list of years in the file, e.g. 1950, 1951, etc. taken from the precip timesteps
            yearKeys = precipYearKeys;
            climateRecords = precipClimateRecords;
        }      
    }
}

