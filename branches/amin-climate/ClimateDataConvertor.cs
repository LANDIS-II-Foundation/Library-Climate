//  Copyright: Portland State University 2009-2014
//  Authors:  Robert M. Scheller, Amin Almassian

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

        private static string maxTriggerWord; //= "Tmax";// "maxtemp";//
        private static string minTriggerWord; //= "Tmin";// "mintemp";//
        private static string prcpTriggerWord; //= "Prcp";// "ppt";//
        private static string rhTriggerWord;
        private static string windSpeedTriggerWord;

        private static Dictionary<string, double[]> climate_Dic;
        private static int firstYear;
        private static int lastYear;
        private static string currentYear;
        private static int currentMonth;
        private static SortedList<int, Core.IEcoregion> climateFileActiveEcoregions;
        private static bool exportToTxtFormatFile = true;

        //-------Indices--------
        private static int IndexMaxT_Mean = 0;
        private static int IndexMaxT_Var = 1;
        private static int IndexMaxT_STD = 2;
        private static int IndexMinT_Mean = 3;
        private static int IndexMinT_Var = 4;
        private static int IndexMinT_STD = 5;
        private static int IndexPrcp_Mean = 6;
        private static int IndexPrcp_Var = 7;
        private static int IndexPrcp_STD = 8;

        private static int IndexRH_Mean = 9;
        private static int IndexRH_Var = 10;
        private static int IndexRH_STD = 11;


        private static int IndexwindSpeed_Mean = 12;
        private static int IndexwindSpeed_Var = 13;
        private static int IndexwindSpeed_STD = 14;
        //----------------------

        public static Dictionary<string, double[]> Climate_Dic { get { return climate_Dic; } }
        private static Dictionary<int, int> yearsdays = new Dictionary<int, int>();
        public static int alldays = 0;

        //--------------------------------------------------------------------
        /// <summary>
        /// This function converts Monthly to Monthly and Daily to Monthly
        /// </summary>
        /// <returns>string: file name of the converted monthly file </returns>
        public static void Convert_USGS_to_ClimateData_FillAlldata(TemporalGranularity timeStep, string climateFile, string climateFileFormat, Climate.Phase climatePhase)
        {
            Dictionary<int, IClimateRecord[,]> allDataRef = null; //this dictionary is filled out either by Daily data or Monthly
            if (climatePhase == Climate.Phase.Future_Climate)
                allDataRef = Climate.Future_AllData;
            
            if (climatePhase == Climate.Phase.SpinUp_Climate)
                allDataRef = Climate.Spinup_AllData;

            //The Convert conversts the csv file data to a dictionary (of either daily or monthly data)
            //--------------------------------------------------------------------
            //string resultingFileName = 
                Convert_USGS_to_ClimateData(timeStep, climateFile, climateFileFormat);
            //-------------------------------------------------------------------- 
            //Then read from the dictionary and convert it to IClimate records 

            //if (timeStep == TimeStep.Daily)
            //{
            //setIndexed(checkRHWindSpeed);
            IndexMaxT_Mean = 0;
            IndexMaxT_Var = 1;
            IndexMaxT_STD = 2;
            IndexMinT_Mean = 3;
            IndexMinT_Var = 4;
            IndexMinT_STD = 5;
            IndexPrcp_Mean = 6;
            IndexPrcp_Var = 7;
            IndexPrcp_STD = 8;

            IndexRH_Mean = 9;
            IndexRH_Var = 10;
            IndexRH_STD = 11;

            IndexwindSpeed_Mean = 12;
            IndexwindSpeed_Var = 13;
            IndexwindSpeed_STD = 14;


            for (int j = firstYear; j <= lastYear; j++)//for each year
            {
                currentYear = j.ToString();

                //Dictionary<string, double[]> climate_Dic_currentYear = (Dictionary<string, double[]>)climate_Dic.Where(r => r.Key.Substring(0, 4).ToString() == currentYear);
                IEnumerable<KeyValuePair<string, double[]>> climate_Dic_currentYear = climate_Dic.Where(r => r.Key.Substring(0, 4).ToString() == currentYear);

                IClimateRecord[,] icrs = new IClimateRecord[Climate.ModelCore.Ecoregions.Count, climate_Dic_currentYear.Count()]; // climate_Dic_currentYear.Count: number of days/months in a year
                
                for (int i = 0; i < Climate.ModelCore.Ecoregions.Count; i++) //for each ecoregion either active or inactive
                {
                    //IClimateRecord icr;
                    List<IClimateRecord> icrList = new List<IClimateRecord>();
                    int icrCount = 0;
                    foreach (KeyValuePair<string, double[]> row in climate_Dic_currentYear) // foreach day/month in a certain year-ecoregion
                    {

                        IClimateRecord icr = new ClimateRecord(row.Value[IndexMinT_Mean], row.Value[IndexMaxT_Mean], (row.Value[IndexMinT_STD] + row.Value[IndexMaxT_STD]) / 2, row.Value[IndexPrcp_Mean], row.Value[IndexPrcp_STD], 0, (row.Value[IndexMinT_Var] + row.Value[IndexMaxT_Var]) / 2, 0, row.Value[IndexRH_Mean], row.Value[IndexRH_Var], row.Value[IndexRH_STD], row.Value[IndexwindSpeed_Mean], row.Value[IndexwindSpeed_Var], row.Value[IndexwindSpeed_STD]);
                        if (Climate.ModelCore.Ecoregions[i].Active)
                            icrs[i, icrCount++] = icr;//new KeyValuePair<int, IClimateRecord>(i, icr);

                    }

                }

                allDataRef.Add(j, icrs);
            }

            return;// resultingFileName;
        }


        public static void Convert_USGS_to_ClimateData(TemporalGranularity sourceTemporalGranularity, string climateFile, string climateFileFormat)
        {

            ClimateFileFormatProvider formatProvider = new ClimateFileFormatProvider(climateFileFormat);
            maxTriggerWord = formatProvider.MaxTempTriggerWord;
            minTriggerWord = formatProvider.MinTempTriggerWord;
            prcpTriggerWord = formatProvider.PrecipTriggerWord;
            rhTriggerWord = formatProvider.RhTriggerWord;
            windSpeedTriggerWord = formatProvider.WindSpeedTriggerWord;
            bool checkRHWindSpeed = false;


            string unmatched_TriggerWords;

            if (checkRHWindSpeed == false)
                unmatched_TriggerWords = maxTriggerWord + ", " + minTriggerWord + ", " + prcpTriggerWord;// +", " + rhTriggerWord + ", " + windSpeedTriggerWord;
            else
                unmatched_TriggerWords = maxTriggerWord + ", " + minTriggerWord + ", " + prcpTriggerWord + ", " + rhTriggerWord + ", " + windSpeedTriggerWord;

            int triggerWordsCheckingTime = 0;
            string path = climateFile;
            StreamReader sreader;
            string centuryPath = "";
            // monthly and daily climates should be filled before in order to chack weather input climatefile can be processed as daily or monthly
            //List<string> montlyClimates;
            //List<string> DailyClimate;

            climateFileFormat = climateFileFormat.ToLower();
            Climate.ModelCore.UI.WriteLine("   Converting raw data from text file: {0}, Format={1}, Temporal={2}.", climateFile, climateFileFormat, sourceTemporalGranularity);


            #region IPCC3 Input file 
            if (climateFileFormat == "ipcc3_daily" || climateFileFormat == "ipcc3_monthly") 
            {
                exportToTxtFormatFile = false;
                sreader = new StreamReader(path);
                string line;
                string[] fields;
                DataTable _dataTableDataByTime = new DataTable();
                line = sreader.ReadLine();
                fields = line.Split(',');
                line = sreader.ReadLine();
                fields = line.Split(',');

                string CurrentScenarioType = "";
                climate_Dic = new Dictionary<string, double[]>();


                string key = "";
                currentYear = "";
                int currentTimeS = 0;
                currentMonth = 1;
                int tempEco = 1;
                double AverageMax = 0;
                double AverageMin = 0;
                double SumPrecp = 0;

                double AverageSTDT = 0;
                double SumVarPpt = 0;
                int numberOfDays = 0;
                double[] tempSum = new double[31];
                double[] tempPrp = new double[31];
                //bool emptytxt = false;
                int updatedIndex = 0;

                //int ecoIndex = -1;
                climateFileActiveEcoregions = new SortedList<int, Core.IEcoregion>();

                foreach (string field in fields)
                {

                    if (field != "")
                    {
                        //if (Convert.ToInt16(field) > ecoIndex)
                        //{
                        //    ecoIndex = Convert.ToInt16(field);
                            IEcoregion eco = Climate.ModelCore.Ecoregions[field]; //.Where(P => P.Index == ecoIndex).FirstOrDefault();
                            //numberOfAllEcoregions = Convert.ToInt16(field);
                            if (eco != null && eco.Active)
                            {
                                if (!climateFileActiveEcoregions.ContainsKey(eco.Index))
                                    climateFileActiveEcoregions.Add(eco.Index, eco);
                            }
                            else
                            {
                                //Climate.ModelCore.UI.WriteLine("ClimateDataConvertor: Number of active ecoregions does not match the input file. The climate data for ecoregion with index {0} was ignored.", numberOfAllEcoregions);
                                Climate.ModelCore.UI.WriteLine("Error in ClimateDataConvertor: Converting {0} file into standard format; Number of active ecoregions does not match the input file.", climateFile);
                                throw new ApplicationException("Error in ClimateDataConvertor: Converting" + climateFile + "file into standard format; Number of active ecoregions does not match the input file.");
                            }

                        }
                }


                //12 beacuse for each ecoriogn we need Max_MinT,Max_MaxT,Max_Var Max_Std, Min_MinT,Min_MaxT,Min_Var Min_Std, Prcp_MinT,Prcp_MaxT,Prcp_Var Prcp_Std
                int dicSize = climateFileActiveEcoregions.Count * 15; 
                sreader.Close();
                StreamReader reader = new StreamReader(path);

                while (reader.Peek() >= 0)
                {
                    line = reader.ReadLine();
                    fields = line.Split(',');
                    foreach (string field in fields)
                    {
                        if (field.Contains("#"))
                        {
                            triggerWordsCheckingTime++;
                            if (triggerWordsCheckingTime > 1)
                                if (unmatched_TriggerWords == maxTriggerWord + ", " + minTriggerWord + ", " + prcpTriggerWord + ", " + rhTriggerWord + ", " + windSpeedTriggerWord && checkRHWindSpeed == true)
                                {
                                    Climate.ModelCore.UI.WriteLine("Error in ClimateDataConvertor: Converting {0} file into standard format; The following trigger words did not match the triggerwords in the given file: {1}." + "selected format: \"{2}\"", climateFile, unmatched_TriggerWords, formatProvider.SelectedFormat);
                                    throw new ApplicationException("Error in ClimateDataConvertor: Converting " + climateFile + " file into standard format; The following triggerWords did not match the triggerwords in the given file: " + unmatched_TriggerWords + "." + "selected format: \"" + formatProvider.SelectedFormat + "\"");
                                }
                                else if (unmatched_TriggerWords == maxTriggerWord + ", " + minTriggerWord + ", " + prcpTriggerWord && checkRHWindSpeed == false)
                                {
                                    Climate.ModelCore.UI.WriteLine("Error in ClimateDataConvertor: Converting {0} file into standard format; The following trigger words did not match the triggerwords in the given file: {1}." + "selected format: \"{2}\"", climateFile, unmatched_TriggerWords, formatProvider.SelectedFormat);
                                    throw new ApplicationException("Error in ClimateDataConvertor: Converting " + climateFile + " file into standard format; The following triggerWords did not match the triggerwords in the given file: " + unmatched_TriggerWords + "." + "selected format: \"" + formatProvider.SelectedFormat + "\"");
                                }

                            //tempScenarioName = CurrentScenarioName;
                            //if (field.ToLower().Contains(maxTriggerWord) || field.ToLower().Contains(minTriggerWord))
                            //{
                            //CurrentScenarioName = field.Substring(1, field.LastIndexOf("t") - 2);
                            if (field.ToLower().Contains(maxTriggerWord.ToLower()))
                            {
                                CurrentScenarioType = maxTriggerWord;
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(maxTriggerWord, "");
                            }
                            else if (field.ToLower().Contains(minTriggerWord.ToLower()))
                            {
                                CurrentScenarioType = minTriggerWord;
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(", " + minTriggerWord, "");
                            }
                            else if (field.ToLower().Contains(prcpTriggerWord.ToLower()))
                            {
                                //CurrentScenarioName = field.Substring(1, field.LastIndexOf("p") - 2);
                                CurrentScenarioType = prcpTriggerWord;
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(", " + prcpTriggerWord, "");
                            }
                            else if (field.ToLower().Contains(rhTriggerWord.ToLower()))
                            {
                                CurrentScenarioType = rhTriggerWord;
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(", " + rhTriggerWord, "");
                            }
                            else if (field.ToLower().Contains(windSpeedTriggerWord.ToLower()))
                            {
                                CurrentScenarioType = windSpeedTriggerWord;
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(", " + windSpeedTriggerWord, "");
                            }


                        }



                    }


                    if (fields[0] == string.Empty && !fields[0].Contains("#"))
                    {
                        line = reader.ReadLine();
                        fields = line.Split(',');

                        if (fields[0].Contains("TIME"))
                        {
                            line = reader.ReadLine();
                            fields = line.Split(',');

                            //now fill array 
                            //Get the lenght of array according to the number of ecorigions/
                            //

                        }
                    }
                    if (!fields[0].Contains("#"))
                    //if (CurrentScenarioName == tempScenarioName && !fields[0].Contains("#"))
                    {

                        key = fields[0].ToString();
                        if (CurrentScenarioType.ToLower().Contains(maxTriggerWord.ToLower()))
                        {
                            IndexMaxT_Mean = 0;
                            //IndexMax_MaxT = 1;
                            IndexMaxT_Var = 1;
                            IndexMaxT_STD = 2;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcoregions);
                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });

                            //set index of max and maxSTD for each ecorigion
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {
                                //currentT = fields[i+1];
                                //if (indexofSTD < 26)
                                //{

                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]), IndexMaxT_Mean);
                                updatedIndex += i + climateFileActiveEcoregions.Count;
                                //climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexMax_MaxT);
                                //updatedIndex +=  numberOfAllEcoregions;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexMaxT_Var);
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexMaxT_STD);
                                IndexMaxT_Mean = IndexMaxT_Mean + 9;
                                //IndexMax_MaxT = IndexMax_MaxT + 12;
                                IndexMaxT_Var = IndexMaxT_Var + 9;
                                IndexMaxT_STD = IndexMaxT_STD + 9;
                                updatedIndex = 0;

                                //indexofSTD++;
                                //}

                            }
                        }
                        else if (CurrentScenarioType.ToLower().Contains(minTriggerWord.ToLower()))
                        {
                            IndexMinT_Mean = 3;
                            //IndexMin_MaxT = 5;
                            IndexMinT_Var = 4;
                            IndexMinT_STD = 5;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcoregions);

                            // climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });
                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);
                            //set index of max and maxSTD for each ecorigion
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {
                                //currentT = fields[i+1];
                                //if (indexofSTD < 26)
                                //{
                                //currentSTD = fields[indexofSTD];
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]), IndexMinT_Mean);
                                updatedIndex += i + climateFileActiveEcoregions.Count;
                                //climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexMin_MaxT);
                                //updatedIndex += numberOfAllEcoregions;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexMinT_Var);
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexMinT_STD);


                                //climate_Dic[key].SetValue(Convert.ToDouble(currentSTD), IndexSTD);
                                IndexMinT_Mean = IndexMinT_Mean + 9;
                                //IndexMin_MaxT = IndexMin_MaxT + 12;
                                IndexMinT_Var = IndexMinT_Var + 9;
                                IndexMinT_STD = IndexMinT_STD + 9;
                                updatedIndex = 0;
                                //    IndexSTD = IndexSTD + 6;
                                //    indexofSTD++;
                                //}

                            }
                        }
                        else if (CurrentScenarioType.ToLower().Contains(prcpTriggerWord.ToLower()))
                        {
                            IndexPrcp_Mean = 6;
                            //IndexPrcp_MaxT = 9;
                            IndexPrcp_Var = 7;
                            IndexPrcp_STD = 8;

                            //IndexSTD = 5;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcoregions);

                            // climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });
                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);
                            
                            //set index of Precipitation and STD for each ecoregion
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {
                                //currentT = fields[i+1];
                                //if (indexofSTD < 26)
                                //{
                                //currentSTD = fields[indexofSTD];
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]) / 10, IndexPrcp_Mean); //  /10 is for mm to cm conversion
                                updatedIndex += i + climateFileActiveEcoregions.Count;
                                //climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexPrcp_MaxT);
                                //updatedIndex += numberOfAllEcoregions;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]) / 10, IndexPrcp_Var); 
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]) / 10, IndexPrcp_STD); //NOTE: this might be a wrong conversion for converting IndexPrcp_STD from mm to cm because STD is calculate using root square.


                                //climate_Dic[key].SetValue(Convert.ToDouble(currentSTD), IndexSTD);
                                IndexPrcp_Mean = IndexPrcp_Mean + 9;
                                //IndexPrcp_MaxT = IndexPrcp_MaxT + 12;
                                IndexPrcp_Var = IndexPrcp_Var + 9;
                                IndexPrcp_STD = IndexPrcp_STD + 9;
                                updatedIndex = 0;
                                //IndexSTD = IndexSTD + 6;
                                //indexofSTD++;
                            }

                        }


                        //-----
                        else if (CurrentScenarioType.ToLower().Contains(rhTriggerWord.ToLower()))
                        {
                            IndexRH_Mean = 9;
                            IndexRH_Var = 10;
                            IndexRH_STD = 11;

                            //IndexSTD = 5;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcoregions);

                            // climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });
                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);
                            //set index of max and maxSTD for each ecorigion
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {

                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]), IndexRH_Mean);
                                updatedIndex += i + climateFileActiveEcoregions.Count;

                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexRH_Var);
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexRH_STD);


                                IndexRH_Mean = IndexRH_Mean + 9;
                                IndexRH_Var = IndexRH_Var + 9;
                                IndexRH_STD = IndexRH_STD + 9;
                                updatedIndex = 0;

                            }

                        }

                        //-----
                        else if (CurrentScenarioType.ToLower().Contains(windSpeedTriggerWord.ToLower()))
                        {
                            IndexwindSpeed_Mean = 12;
                            IndexwindSpeed_Var = 13;
                            IndexwindSpeed_STD = 14;

                            //IndexSTD = 5;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcoregions);

                            // climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });
                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);
                            //set index of max and maxSTD for each ecorigion
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {

                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]), IndexwindSpeed_Mean);
                                updatedIndex += i + climateFileActiveEcoregions.Count;

                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexwindSpeed_Var);
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexwindSpeed_STD);


                                IndexwindSpeed_Mean = IndexwindSpeed_Mean + 9;
                                IndexwindSpeed_Var = IndexwindSpeed_Var + 9;
                                IndexwindSpeed_STD = IndexwindSpeed_STD + 9;
                                updatedIndex = 0;

                            }

                        }
                        //-----


                    }  //end while


                    //if (CurrentScenarioName != tempScenarioName || reader.EndOfStream)
                    if (reader.EndOfStream)
                    {
                        //Print file for one scenario then clear dictionary to use for another scenario

                        //Daily period
                        centuryPath = "Century_Climate_Inputs_Monthly.txt";
                        setIndexed();
                        //using (System.IO.StreamWriter file = new System.IO.StreamWriter(centuryPath, emptytxt))
                        //{
                        //    if (exportToTxtFormatFile)
                        //    {
                        //        file.WriteLine("LandisData" + " \"Climate Data\" \n");
                        //        file.WriteLine("ClimateTable \n");
                        //        //file.WriteLine(tempScenarioName + "\n");
                        //        file.WriteLine(">>Eco" + "\t\t" + "Time" + "\t" + "Month" + "\t" + "AvgMinT" + "\t" + "AvgMaxT" + "\t" + "StdDevT" + "\t" + "AvgPpt" + "\t" + "StdDevPpt" + "\t" + "PAR" + "\t" + "VarT" + "\t" + "VarPpt" + "\n");
                        //        file.WriteLine(">>Name" + "\t\t" + "Step" + "\t" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(k)" + "\t" + "(cm)" + "\t" + "Ppt" + "\t" + "µmol m-2 s-1" + "\t" + "(cm)" + "\t" + "(cm)" + "\n");

                        //    }
                        //    else
                        //    {
                        //        file.WriteLine("This file is not generated when converting a Daily Climate to Monthly.");
                        //        file.WriteLine("To enable the generation of this file:");
                        //        file.WriteLine("set 'exportToTxtFormatFile = true;' in 'if (sourceTemporalGranularity == TemporalGranularity.Daily) \n { \n \t exportToTxtFormatFile = false; ' in ClimateDataConvertor class.");
                        //        file.WriteLine("Caution: Note that the right AnnualCliamte_Monthly will not be generated if this generated Monthly-climate txt file is used as a climate input file.");
                        //    }
                            currentYear = climate_Dic.First().Key.Substring(0, 4).ToString();
                            //starting timestep
                            currentTimeS = 0;
                            currentMonth = Convert.ToInt16(climate_Dic.First().Key.Substring(5, 2).ToString());
                            tempEco = 1;


                            lastYear = climate_Dic.AsEnumerable().Select(ax => Convert.ToInt32(ax.Key.Substring(0, 4).ToString())).Distinct().ToList().Max();
                            firstYear = climate_Dic.AsEnumerable().Select(ai => Convert.ToInt32(ai.Key.Substring(0, 4).ToString())).Distinct().ToList().Min();
                            if ((double)climate_Dic.Count / 12 > (double)lastYear - firstYear)
                                lastYear = lastYear - 1;

                            for (int j = firstYear; j <= lastYear; j++)
                            {
                                for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                                {
                                    currentYear = j.ToString();

                                    foreach (KeyValuePair<string, double[]> row in climate_Dic)
                                    {
                                        if (currentYear == row.Key.Substring(0, 4).ToString())
                                        {

                                            if (currentMonth == Convert.ToInt16(row.Key.Substring(5, 2)))
                                            {

                                                //(row.Value[IndexMax_MaxT] + row.Value[IndexMaxT_Mean])/2
                                                //AverageMin += (row.Value[IndexMin_MaxT] + row.Value[IndexMinT_Mean]) / 2;
                                                //AverageMax += (row.Value[IndexMax_MaxT] + row.Value[IndexMaxT_Mean]) / 2;
                                                //AveragePrecp += (row.Value[IndexPrcp_MaxT] + row.Value[IndexPrcp_Mean]) / 2;
                                                //AverageSTDT += (row.Value[IndexMaxT_Var] + row.Value[IndexMinT_Var]) / 2;
                                                //AverageMaxSTD += Math.Round(Convert.ToDouble(row.Value[2]), 2);
                                                AverageMin += Math.Round(row.Value[IndexMinT_Mean], 2);
                                                AverageMax += Math.Round(row.Value[IndexMaxT_Mean], 2);
                                                SumPrecp += (Math.Round(row.Value[IndexPrcp_Mean], 2));
                                                AverageSTDT += Math.Round((row.Value[IndexMaxT_Var] + row.Value[IndexMinT_Var]) / 2, 2);
                                                SumVarPpt += Convert.ToDouble(row.Value[IndexPrcp_Var]);


                                                numberOfDays++;

                                            }



                                            else
                                            {
                                                //if (exportToTxtFormatFile)
                                                //    file.WriteLine(climateFileActiveEcoregions.ElementAt(i).Value.Name + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(SumPrecp, 2) + "\t" + Math.Round(Math.Sqrt(SumVarPpt), 2) + "\t" + "0.0" + "\t" + Math.Round(AverageSTDT / numberOfDays, 2) + "\t" + Math.Round(SumVarPpt, 2) + "\n");
                                                currentMonth = Convert.ToInt16(row.Key.Substring(5, 2));
                                                //if (tempMonth != currentMonth)
                                                alldays += numberOfDays;
                                                AverageMax = 0;
                                                AverageMin = 0;
                                                SumPrecp = 0;
                                                AverageSTDT = 0;
                                                SumVarPpt = 0;

                                                numberOfDays = 0;
                                                AverageMin += Math.Round(row.Value[IndexMinT_Mean], 2);
                                                AverageMax += Math.Round(row.Value[IndexMaxT_Mean], 2);
                                                SumPrecp += (Math.Round(row.Value[IndexPrcp_Mean], 2));
                                                AverageSTDT += Math.Round((row.Value[IndexMaxT_Var] + row.Value[IndexMinT_Var]) / 2, 2);
                                                SumVarPpt += Convert.ToDouble(row.Value[IndexPrcp_Var]);

                                                numberOfDays++;
                                            }


                                        }
                                        else
                                        {
                                            if (currentMonth == 12)
                                            {
                                                alldays += numberOfDays;
                                                if (!yearsdays.Keys.Contains(j))
                                                {
                                                    yearsdays.Add(j, alldays);
                                                    alldays = 0;
                                                }

                                            }
                                            

                                            alldays = 0;
                                            tempEco = i;
                                            currentMonth = 1;
                                            AverageMax = 0;
                                            //AverageMaxSTD = 0;
                                            AverageMin = 0;
                                            //AverageMinSTD = 0;
                                            SumPrecp = 0;
                                            //AveragePrecSTD = 0;

                                            AverageSTDT = 0;
                                            SumVarPpt = 0;

                                            numberOfDays = 0;
                                            AverageMin += Math.Round(row.Value[IndexMinT_Mean], 2);
                                            AverageMax += Math.Round(row.Value[IndexMaxT_Mean], 2);
                                            SumPrecp += (Math.Round(row.Value[IndexPrcp_Mean], 2));
                                            AverageSTDT += Math.Round((row.Value[IndexMaxT_Var] + row.Value[IndexMinT_Var]) / 2, 2);
                                            SumVarPpt += Convert.ToDouble(row.Value[IndexPrcp_Var]);
                                            //sums = 0;
                                            //stdTemp = 0;
                                            //prpSums = 0;
                                            //stdPrp = 0;
                                            numberOfDays++;
                                        }


                                    }
                                    tempEco = i;

                                    currentMonth = 1;
                                    AverageMax = 0;
                                    //AverageMaxSTD = 0;
                                    AverageMin = 0;
                                    //AverageMinSTD = 0;
                                    SumPrecp = 0;
                                    //AveragePrecSTD = 0;

                                    AverageSTDT = 0;
                                    SumVarPpt = 0;

                                    IndexMaxT_Mean = IndexMaxT_Mean + 9;
                                    //IndexMax_MaxT = IndexMax_MaxT + 12;
                                    IndexMaxT_Var = IndexMaxT_Var + 9;
                                    IndexMaxT_STD = IndexMaxT_STD + 9;
                                    IndexMinT_Mean = IndexMinT_Mean + 9;
                                    //IndexMin_MaxT = IndexMin_MaxT + 12;
                                    IndexMinT_Var = IndexMinT_Var + 9;
                                    IndexMinT_STD = IndexMinT_STD + 9;
                                    IndexPrcp_Mean = IndexPrcp_Mean + 9;
                                    //IndexPrcp_MaxT = IndexPrcp_MaxT + 12;
                                    IndexPrcp_Var = IndexPrcp_Var + 9;
                                    IndexPrcp_STD = IndexPrcp_STD + 9;


                                    IndexRH_Mean = IndexRH_Mean + 9;
                                    IndexRH_Var = IndexRH_Var + 9;
                                    IndexRH_STD = IndexRH_STD + 9;

                                    IndexwindSpeed_Mean = IndexwindSpeed_Mean + 9;
                                    IndexwindSpeed_Var = IndexwindSpeed_Var + 9;
                                    IndexwindSpeed_STD = IndexwindSpeed_STD + 9;

                                }
                                //file.WriteLine("eco" + numberOfAllEcoregions.ToString() + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(StdDevPpt), 2) + "\t" + "0.0" + "\n");

                                tempEco = 1;
                                currentTimeS = currentTimeS + 1;
                                IndexMaxT_Mean = 0;
                                //IndexMax_MaxT = 1;
                                IndexMaxT_Var = 1;
                                IndexMaxT_STD = 2;
                                IndexMinT_Mean = 3;
                                //IndexMin_MaxT = 5;
                                IndexMinT_Var = 4;
                                IndexMinT_STD = 5;
                                IndexPrcp_Mean = 6;
                                //IndexPrcp_MaxT = 9;
                                IndexPrcp_Var = 7;
                                IndexPrcp_STD = 8;

                                IndexRH_Mean = 9;
                                IndexRH_Var = 10;
                                IndexRH_STD = 11;

                                IndexwindSpeed_Mean = 12;
                                IndexwindSpeed_Var = 13;
                                IndexwindSpeed_STD = 14;

                                currentMonth = 1;
                                AverageMax = 0;
                                //AverageMaxSTD = 0;
                                AverageMin = 0;
                                //AverageMinSTD = 0;
                                SumPrecp = 0;
                                //AveragePrecSTD = 0;

                                AverageSTDT = 0;
                                SumVarPpt = 0;

                            }

                        }

                        //If file contains more than one scenario then these setting will be needed
                        //climate_Dic.Clear();
                        //emptytxt = true;
                        //tempScenarioName = CurrentScenarioName;

                    //}
                }
                //if (unmatched_TriggerWords != "")
                //{
                //    Climate.ModelCore.UI.WriteLine("Error in ClimateDataConvertor: Converting {0} file into standard format.", climateFile);
                //    Climate.ModelCore.UI.WriteLine("The following triggerWords did not match the triggerwords in the given file: {1}.  Selected format: {2}", unmatched_TriggerWords, formatProvider.SelectedFormat);
                //    throw new ApplicationException("Error in ClimateDataConvertor: See Log File."); //: Converting " + climateFile + " file into standard format; The following triggerWords did not match the triggerwords in the given file: " + unmatched_TriggerWords + "." + "selected format: \"" + formatProvider.SelectedFormat + "\"");
                //}

            }
            #endregion

            #region IPCC5 Input file
            else if (climateFileFormat == "ipcc5_monthly" || climateFileFormat == "ipcc5_daily") 
            {
                exportToTxtFormatFile = false;
                sreader = new StreamReader(path);
                string line;
                string[] fields;
                DataTable _dataTableDataByTime = new DataTable();
                line = sreader.ReadLine();
                fields = line.Split(',');
                line = sreader.ReadLine();
                fields = line.Split(',');

                string CurrentScenarioType = "";
                climate_Dic = new Dictionary<string, double[]>();


                string key = "";
                currentYear = "";
                int currentTimeS = 0;
                currentMonth = 1;
                int tempEco = 1;
                double AverageMax = 0;
                double AverageMin = 0;
                double SumPrecp = 0;

                double AverageSTDT = 0;
                double SumVarPpt = 0;
                int numberOfDays = 0;
                double[] tempSum = new double[31];
                double[] tempPrp = new double[31];
                //bool emptytxt = false;
                int updatedIndex = 0;

                //int ecoIndex = -1;
                climateFileActiveEcoregions = new SortedList<int, Core.IEcoregion>();

                foreach (string field in fields)
                {

                    if (field != "")
                        //if (Convert.ToInt16(field) > ecoIndex)
                        {
                            //ecoIndex = Convert.ToInt16(field);
                            IEcoregion eco = Climate.ModelCore.Ecoregions[field]; //.Where(P => P.Index == ecoIndex).FirstOrDefault();
                            if (eco != null && eco.Active)
                            {
                                if (!climateFileActiveEcoregions.ContainsKey(eco.Index))
                                    climateFileActiveEcoregions.Add(eco.Index, eco);
                            }
                            else
                            {
                                //Climate.ModelCore.UI.WriteLine("ClimateDataConvertor: Number of active ecoregions does not match the input file. The climate data for ecoregion with index {0} was ignored.", numberOfAllEcoregions);
                                Climate.ModelCore.UI.WriteLine("Error in ClimateDataConvertor: Converting {0} file into standard format; Number of active ecoregions does not match the input file.", climateFile);
                                throw new ApplicationException("Error in ClimateDataConvertor: Converting" + climateFile + "file into standard format; Number of active ecoregions does not match the input file.");
                            }

                        }
                }


                //12 beacuse for each ecoriogn we need Max_MinT,Max_MaxT,Max_Var Max_Std, Min_MinT,Min_MaxT,Min_Var Min_Std, Prcp_MinT,Prcp_MaxT,Prcp_Var Prcp_Std
                int dicSize = climateFileActiveEcoregions.Count * 15;
                sreader.Close();
                StreamReader reader = new StreamReader(path);

                while (reader.Peek() >= 0)
                {
                    line = reader.ReadLine();
                    fields = line.Split(',');
                    foreach (string field in fields)
                    {
                        if (field.Contains("#"))
                        {
                            triggerWordsCheckingTime++;
                            if (triggerWordsCheckingTime > 1)
                                if (unmatched_TriggerWords == maxTriggerWord + ", " + minTriggerWord + ", " + prcpTriggerWord + ", " + rhTriggerWord + ", " + windSpeedTriggerWord && checkRHWindSpeed == true)
                                {
                                    Climate.ModelCore.UI.WriteLine("Error in ClimateDataConvertor: Converting {0} file into standard format; The following triggerWords did not match the triggerwords in the given file: {1}." + "selected format: \"{2}\"", climateFile, unmatched_TriggerWords, formatProvider.SelectedFormat);
                                    throw new ApplicationException("Error in ClimateDataConvertor: Converting " + climateFile + " file into standard format; The following triggerWords did not match the triggerwords in the given file: " + unmatched_TriggerWords + "." + "selected format: \"" + formatProvider.SelectedFormat + "\"");
                                }
                                else if (unmatched_TriggerWords == maxTriggerWord + ", " + minTriggerWord + ", " + prcpTriggerWord && checkRHWindSpeed == false)
                                {
                                    Climate.ModelCore.UI.WriteLine("Error in ClimateDataConvertor: Converting {0} file into standard format; The following triggerWords did not match the triggerwords in the given file: {1}." + "selected format: \"{2}\"", climateFile, unmatched_TriggerWords, formatProvider.SelectedFormat);
                                    throw new ApplicationException("Error in ClimateDataConvertor: Converting " + climateFile + " file into standard format; The following triggerWords did not match the triggerwords in the given file: " + unmatched_TriggerWords + "." + "selected format: \"" + formatProvider.SelectedFormat + "\"");
                                }

                            //tempScenarioName = CurrentScenarioName;
                            //if (field.ToLower().Contains(maxTriggerWord) || field.ToLower().Contains(minTriggerWord))
                            //{
                            //CurrentScenarioName = field.Substring(1, field.LastIndexOf("t") - 2);
                            if (field.ToLower().Contains(maxTriggerWord.ToLower()))
                            {
                                CurrentScenarioType = maxTriggerWord;
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(maxTriggerWord, "");
                            }
                            else if (field.ToLower().Contains(minTriggerWord.ToLower()))
                            {
                                CurrentScenarioType = minTriggerWord;
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(", " + minTriggerWord, "");
                            }
                            else if (field.ToLower().Contains(prcpTriggerWord.ToLower()))
                            {
                                //CurrentScenarioName = field.Substring(1, field.LastIndexOf("p") - 2);
                                CurrentScenarioType = prcpTriggerWord;
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(", " + prcpTriggerWord, "");
                            }
                            else if (field.ToLower().Contains(rhTriggerWord.ToLower()))
                            {
                                CurrentScenarioType = rhTriggerWord;
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(", " + rhTriggerWord, "");
                            }
                            else if (field.ToLower().Contains(windSpeedTriggerWord.ToLower()))
                            {
                                CurrentScenarioType = windSpeedTriggerWord;
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(", " + windSpeedTriggerWord, "");
                            }


                        }



                    }


                    if (fields[0] == string.Empty && !fields[0].Contains("#"))
                    {
                        line = reader.ReadLine();
                        fields = line.Split(',');

                        if (fields[0].Contains("TIME"))
                        {
                            line = reader.ReadLine();
                            fields = line.Split(',');

                        }
                    }
                    if (!fields[0].Contains("#"))
                    {

                        key = fields[0].ToString();
                        if (CurrentScenarioType.ToLower().Contains(maxTriggerWord.ToLower()))
                        {
                            IndexMaxT_Mean = 0;
                            IndexMaxT_Var = 1;
                            IndexMaxT_STD = 2;
                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);

                            //set index of max and maxSTD for each ecorigion
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]) - 274.15, IndexMaxT_Mean);
                                updatedIndex += i + climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexMaxT_Var);
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexMaxT_STD);
                                IndexMaxT_Mean = IndexMaxT_Mean + 9;
                                IndexMaxT_Var = IndexMaxT_Var + 9;
                                IndexMaxT_STD = IndexMaxT_STD + 9;
                                updatedIndex = 0;


                            }
                        }
                        else if (CurrentScenarioType.ToLower().Contains(minTriggerWord.ToLower()))
                        {
                            IndexMinT_Mean = 3;
                            IndexMinT_Var = 4;
                            IndexMinT_STD = 5;

                            // climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });
                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);
                            //set index of max and maxSTD for each ecorigion
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]) - 274.15, IndexMinT_Mean);
                                updatedIndex += i + climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]) - 274.15, IndexMinT_Var);
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]) - 274.15, IndexMinT_STD);

                                IndexMinT_Mean = IndexMinT_Mean + 9;
                                IndexMinT_Var = IndexMinT_Var + 9;
                                IndexMinT_STD = IndexMinT_STD + 9;
                                updatedIndex = 0;

                            }
                        }
                        else if (CurrentScenarioType.ToLower().Contains(prcpTriggerWord.ToLower()))
                        {
                            IndexPrcp_Mean = 6;
                            IndexPrcp_Var = 7;
                            IndexPrcp_STD = 8;

                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);

                            //set index of Precipitation and STD for each ecoregion
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]) * 315360, IndexPrcp_Mean); 
                                updatedIndex += i + climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]) * 315360, IndexPrcp_Var);
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]) * 315360, IndexPrcp_STD); //NOTE: this might be a wrong conversion for converting IndexPrcp_STD from mm to cm because STD is calculate using root square.

                                IndexPrcp_Mean = IndexPrcp_Mean + 9;
                                IndexPrcp_Var = IndexPrcp_Var + 9;
                                IndexPrcp_STD = IndexPrcp_STD + 9;
                                updatedIndex = 0;
                            }

                        }


                        //-----
                        else if (CurrentScenarioType.ToLower().Contains(rhTriggerWord.ToLower()))
                        {
                            IndexRH_Mean = 9;
                            IndexRH_Var = 10;
                            IndexRH_STD = 11;

                            //IndexSTD = 5;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcoregions);

                            // climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });
                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);
                            //set index of max and maxSTD for each ecorigion
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {

                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]), IndexRH_Mean);
                                updatedIndex += i + climateFileActiveEcoregions.Count;

                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexRH_Var);
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexRH_STD);


                                IndexRH_Mean = IndexRH_Mean + 9;
                                IndexRH_Var = IndexRH_Var + 9;
                                IndexRH_STD = IndexRH_STD + 9;
                                updatedIndex = 0;

                            }

                        }

                        //-----
                        else if (CurrentScenarioType.ToLower().Contains(windSpeedTriggerWord.ToLower()))
                        {
                            IndexwindSpeed_Mean = 12;
                            IndexwindSpeed_Var = 13;
                            IndexwindSpeed_STD = 14;

                            //IndexSTD = 5;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcoregions);

                            // climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });
                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);
                            //set index of max and maxSTD for each ecorigion
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {

                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]), IndexwindSpeed_Mean);
                                updatedIndex += i + climateFileActiveEcoregions.Count;

                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexwindSpeed_Var);
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexwindSpeed_STD);


                                IndexwindSpeed_Mean = IndexwindSpeed_Mean + 9;
                                IndexwindSpeed_Var = IndexwindSpeed_Var + 9;
                                IndexwindSpeed_STD = IndexwindSpeed_STD + 9;
                                updatedIndex = 0;

                            }

                        }
                        //-----


                    }  //end while


                    //if (CurrentScenarioName != tempScenarioName || reader.EndOfStream)
                    if (reader.EndOfStream)
                    {
                        //Daily period
                        //centuryPath = "Century_Climate_Inputs_Monthly.txt";
                        setIndexed();
                        currentYear = climate_Dic.First().Key.Substring(0, 4).ToString();
                        //starting timestep
                        currentTimeS = 0;
                        currentMonth = Convert.ToInt16(climate_Dic.First().Key.Substring(5, 2).ToString());
                        tempEco = 1;


                        lastYear = climate_Dic.AsEnumerable().Select(ax => Convert.ToInt32(ax.Key.Substring(0, 4).ToString())).Distinct().ToList().Max();
                        firstYear = climate_Dic.AsEnumerable().Select(ai => Convert.ToInt32(ai.Key.Substring(0, 4).ToString())).Distinct().ToList().Min();
                        if ((double)climate_Dic.Count / 12 > (double)lastYear - firstYear)
                            lastYear = lastYear - 1;

                        for (int j = firstYear; j <= lastYear; j++)
                        {
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {
                                currentYear = j.ToString();

                                foreach (KeyValuePair<string, double[]> row in climate_Dic)
                                {
                                    if (currentYear == row.Key.Substring(0, 4).ToString())
                                    {

                                        if (currentMonth == Convert.ToInt16(row.Key.Substring(5, 2)))
                                        {

                                            AverageMin += Math.Round(row.Value[IndexMinT_Mean], 2);
                                            AverageMax += Math.Round(row.Value[IndexMaxT_Mean], 2);
                                            SumPrecp += (Math.Round(row.Value[IndexPrcp_Mean], 2));
                                            AverageSTDT += Math.Round((row.Value[IndexMaxT_Var] + row.Value[IndexMinT_Var]) / 2, 2);
                                            SumVarPpt += Convert.ToDouble(row.Value[IndexPrcp_Var]);
                                            numberOfDays++;

                                        }
                                        else
                                        {
                                            //if (exportToTxtFormatFile)
                                            //    file.WriteLine(climateFileActiveEcoregions.ElementAt(i).Value.Name + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(SumPrecp, 2) + "\t" + Math.Round(Math.Sqrt(SumVarPpt), 2) + "\t" + "0.0" + "\t" + Math.Round(AverageSTDT / numberOfDays, 2) + "\t" + Math.Round(SumVarPpt, 2) + "\n");
                                            currentMonth = Convert.ToInt16(row.Key.Substring(5, 2));
                                            //if (tempMonth != currentMonth)
                                            alldays += numberOfDays;
                                            AverageMax = 0;
                                            AverageMin = 0;
                                            SumPrecp = 0;
                                            AverageSTDT = 0;
                                            SumVarPpt = 0;

                                            numberOfDays = 0;
                                            AverageMin += Math.Round(row.Value[IndexMinT_Mean], 2);
                                            AverageMax += Math.Round(row.Value[IndexMaxT_Mean], 2);
                                            SumPrecp += (Math.Round(row.Value[IndexPrcp_Mean], 2));
                                            AverageSTDT += Math.Round((row.Value[IndexMaxT_Var] + row.Value[IndexMinT_Var]) / 2, 2);
                                            SumVarPpt += Convert.ToDouble(row.Value[IndexPrcp_Var]);

                                            numberOfDays++;
                                        }


                                    }
                                    else
                                    {
                                        if (currentMonth == 12)
                                        {
                                            alldays += numberOfDays;
                                            //if (exportToTxtFormatFile)
                                            //    file.WriteLine(climateFileActiveEcoregions.ElementAt(i).Value.Name + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(SumPrecp, 2) + "\t" + Math.Round(Math.Sqrt(SumVarPpt), 2) + "\t" + "0.0" + "\t" + Math.Round(AverageSTDT / numberOfDays, 2) + "\t" + Math.Round(SumVarPpt, 2) + "\n");
                                            if (!yearsdays.Keys.Contains(j))
                                            {
                                                yearsdays.Add(j, alldays);
                                                alldays = 0;
                                            }
                                        }


                                        alldays = 0;
                                        tempEco = i;
                                        currentMonth = 1;
                                        AverageMax = 0;
                                        AverageMin = 0;
                                        SumPrecp = 0;

                                        AverageSTDT = 0;
                                        SumVarPpt = 0;

                                        numberOfDays = 0;
                                        AverageMin += Math.Round(row.Value[IndexMinT_Mean], 2);
                                        AverageMax += Math.Round(row.Value[IndexMaxT_Mean], 2);
                                        SumPrecp += (Math.Round(row.Value[IndexPrcp_Mean], 2));
                                        AverageSTDT += Math.Round((row.Value[IndexMaxT_Var] + row.Value[IndexMinT_Var]) / 2, 2);
                                        SumVarPpt += Convert.ToDouble(row.Value[IndexPrcp_Var]);
                                        numberOfDays++;
                                    }


                                }
                                tempEco = i;

                                currentMonth = 1;
                                AverageMax = 0;
                                //AverageMaxSTD = 0;
                                AverageMin = 0;
                                //AverageMinSTD = 0;
                                SumPrecp = 0;
                                //AveragePrecSTD = 0;

                                AverageSTDT = 0;
                                SumVarPpt = 0;

                                IndexMaxT_Mean = IndexMaxT_Mean + 9;
                                //IndexMax_MaxT = IndexMax_MaxT + 12;
                                IndexMaxT_Var = IndexMaxT_Var + 9;
                                IndexMaxT_STD = IndexMaxT_STD + 9;
                                IndexMinT_Mean = IndexMinT_Mean + 9;
                                //IndexMin_MaxT = IndexMin_MaxT + 12;
                                IndexMinT_Var = IndexMinT_Var + 9;
                                IndexMinT_STD = IndexMinT_STD + 9;
                                IndexPrcp_Mean = IndexPrcp_Mean + 9;
                                //IndexPrcp_MaxT = IndexPrcp_MaxT + 12;
                                IndexPrcp_Var = IndexPrcp_Var + 9;
                                IndexPrcp_STD = IndexPrcp_STD + 9;


                                IndexRH_Mean = IndexRH_Mean + 9;
                                IndexRH_Var = IndexRH_Var + 9;
                                IndexRH_STD = IndexRH_STD + 9;

                                IndexwindSpeed_Mean = IndexwindSpeed_Mean + 9;
                                IndexwindSpeed_Var = IndexwindSpeed_Var + 9;
                                IndexwindSpeed_STD = IndexwindSpeed_STD + 9;

                            }
                            //file.WriteLine("eco" + numberOfAllEcoregions.ToString() + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(StdDevPpt), 2) + "\t" + "0.0" + "\n");

                            tempEco = 1;
                            currentTimeS = currentTimeS + 1;
                            IndexMaxT_Mean = 0;
                            //IndexMax_MaxT = 1;
                            IndexMaxT_Var = 1;
                            IndexMaxT_STD = 2;
                            IndexMinT_Mean = 3;
                            //IndexMin_MaxT = 5;
                            IndexMinT_Var = 4;
                            IndexMinT_STD = 5;
                            IndexPrcp_Mean = 6;
                            //IndexPrcp_MaxT = 9;
                            IndexPrcp_Var = 7;
                            IndexPrcp_STD = 8;

                            IndexRH_Mean = 9;
                            IndexRH_Var = 10;
                            IndexRH_STD = 11;

                            IndexwindSpeed_Mean = 12;
                            IndexwindSpeed_Var = 13;
                            IndexwindSpeed_STD = 14;

                            currentMonth = 1;
                            AverageMax = 0;
                            //AverageMaxSTD = 0;
                            AverageMin = 0;
                            //AverageMinSTD = 0;
                            SumPrecp = 0;
                            //AveragePrecSTD = 0;

                            AverageSTDT = 0;
                            SumVarPpt = 0;

                        }

                    }

                    //If file contains more than one scenario then these setting will be needed
                    //climate_Dic.Clear();
                    //emptytxt = true;
                    //tempScenarioName = CurrentScenarioName;

                    //}
                }
                //if (unmatched_TriggerWords != "")
                //{
                //    Climate.ModelCore.UI.WriteLine("Error in ClimateDataConvertor: Converting {0} file into standard format.", climateFile);
                //    Climate.ModelCore.UI.WriteLine("The following triggerWords did not match the triggerwords in the given file: {1}.  Selected format: {2}", unmatched_TriggerWords, formatProvider.SelectedFormat);
                //    throw new ApplicationException("Error in ClimateDataConvertor: See Log File."); //: Converting " + climateFile + " file into standard format; The following triggerWords did not match the triggerwords in the given file: " + unmatched_TriggerWords + "." + "selected format: \"" + formatProvider.SelectedFormat + "\"");
                //}

            }
            #endregion

            #region PRISM Monthly Data
            //else if (sourceTemporalGranularity == TemporalGranularity.Monthly)
            else if (climateFileFormat == "prism_monthly") 
            {

                //unmatched_TriggerWords = maxTriggerWord + ", " + minTriggerWord + ", " + prcpTriggerWord;// +", " + rhTriggerWord + ", " + windSpeedTriggerWord;

                sreader = new StreamReader(path);  // NEED TRY CATCH FILE OPEN HERE


                string line;
                string[] fields;
                checkRHWindSpeed = false;

                if (checkRHWindSpeed == false)
                    unmatched_TriggerWords = maxTriggerWord + ", " + minTriggerWord + ", " + prcpTriggerWord;// +", " + rhTriggerWord + ", " + windSpeedTriggerWord;
                else
                    unmatched_TriggerWords = maxTriggerWord + ", " + minTriggerWord + ", " + prcpTriggerWord + ", " + rhTriggerWord + ", " + windSpeedTriggerWord;

                //string tempScenarioName = "";
                DataTable _dataTableDataByTime = new DataTable();
                //int numberOfAllEcoregions = 0;
                line = sreader.ReadLine();
                fields = line.Split(',');
                //tempScenarioName = fields[0].Substring(1, fields[0].LastIndexOf("t") - 2);
                //tempScenarioName = fields[0].Substring(1, 4);
                line = sreader.ReadLine();
                fields = line.Split(',');
                //int totalRows = 0;
                //string[,] wholedata;
                //string CurrentScenarioName = "";

                string CurrentScenarioType = "";
                climate_Dic = new Dictionary<string, double[]>();

                //string currentT;
                //string currentSTD;
                //string currentPart = "";
                //int totalRow = 0;
                string key = "";
                int IndexMaxT_Mean = 0;
                //int IndexMax_MaxT = 1;
                int IndexMaxT_Var = 1;
                int IndexMaxT_STD = 2;
                int IndexMinT_Mean = 3;
                //int IndexMin_MaxT = 5;
                int IndexMinT_Var = 4;
                int IndexMinT_STD = 5;
                int IndexPrcp_Mean = 6;
                //int IndexPrcp_MaxT = 9;
                int IndexPrcp_Var = 7;
                int IndexPrcp_STD = 8;
                int IndexRH_Mean = 9;
                int IndexRH_Var = 10;
                int IndexRH_STD = 11;
                int IndexwindSpeed_Mean = 12;
                int IndexwindSpeed_Var = 13;
                int IndexwindSpeed_STD = 14;

                //bool firstFlag = false;
                currentYear = "";
                //starting timestep
                int currentTimeS = 0;
                currentMonth = 1;
                int tempEco = 1;
                double AverageMax = 0;

                //double AverageMaxSTD = 0;
                double AverageMin = 0;
                //double AverageMinSTD = 0;
                double SumPrecp = 0;

                double AverageSTDT = 0;
                double sumVarPpt = 0;
                //double AveragePrecSTD = 0;
                int numberOfDays = 0;
                double[] tempSum = new double[31];
                double[] tempPrp = new double[31];
                //double sums = 0;
                //double prpSums = 0;
                //double stdTemp = 0;
                //double stdPrp = 0;
                bool emptytxt = false;
                int updatedIndex = 0;


                // IEnumerable<Core.IEcoregion> activeEcoregions = Climate.ModelCore.Ecoregions.Where(P => P.Active);
                //int ecoIndex = -1;
                climateFileActiveEcoregions = new SortedList<int, Core.IEcoregion>();

                foreach (string field in fields)
                {
                    if (field != "")
                    {
                        //if (Convert.ToInt16(field) > ecoIndex)
                        //{
                        //    ecoIndex = Convert.ToInt16(field);
                            IEcoregion eco = Climate.ModelCore.Ecoregions[field]; //.Where(P => P.Index == ecoIndex).FirstOrDefault();
                            //numberOfAllEcoregions = Convert.ToInt16(field);
                            if (eco != null && eco.Active)
                            {
                                if (!climateFileActiveEcoregions.ContainsKey(eco.Index))
                                    climateFileActiveEcoregions.Add(eco.Index, eco);
                            }
                            else
                            {
                                //Climate.ModelCore.UI.WriteLine("ClimateDataConvertor: Number of active ecoregions does not match the input file. The climate data for ecoregion with index {0} was ignored.", numberOfAllEcoregions);
                                Climate.ModelCore.UI.WriteLine("Error in ClimateDataConvertor: Converting {0} file into standard format; Number of active ecoregions does not match the input file.", climateFile);
                                throw new ApplicationException("Error in ClimateDataConvertor: Converting" + climateFile + "file into standard format; Number of active ecoregions does not match the input file.");
                            }

                     }
                }


                //12 beacuse for each ecoriogn we need Max_MinT,Max_MaxT,Max_Var Max_Std, Min_MinT,Min_MaxT,Min_Var Min_Std, Prcp_MinT,Prcp_MaxT,Prcp_Var Prcp_Std
                int dicSize = climateFileActiveEcoregions.Count * 15;// Climate.ModelCore.Ecoregions.Count * 9;
                sreader.Close();
                StreamReader reader = new StreamReader(path);

                while (reader.Peek() >= 0)
                {
                    line = reader.ReadLine();
                    fields = line.Split(',');
                    foreach (string field in fields)
                    {
                        if (field.Contains("#"))
                        {

                            triggerWordsCheckingTime++;
                            if (triggerWordsCheckingTime > 1)
                                if (unmatched_TriggerWords == maxTriggerWord + ", " + minTriggerWord + ", " + prcpTriggerWord + ", " + rhTriggerWord + ", " + windSpeedTriggerWord && checkRHWindSpeed == true)
                                {
                                    Climate.ModelCore.UI.WriteLine("Error in ClimateDataConvertor: Converting {0} file into standard format; The following triggerWords did not match the triggerwords in the given file: {1}." + "selected format: \"{2}\"", climateFile, unmatched_TriggerWords, formatProvider.SelectedFormat);
                                    throw new ApplicationException("Error in ClimateDataConvertor: Converting " + climateFile + " file into standard format; The following triggerWords did not match the triggerwords in the given file: " + unmatched_TriggerWords + "." + "selected format: \"" + formatProvider.SelectedFormat + "\"");
                                }
                                else if (unmatched_TriggerWords == maxTriggerWord + ", " + minTriggerWord + ", " + prcpTriggerWord && checkRHWindSpeed == false)
                                {
                                    Climate.ModelCore.UI.WriteLine("Error in ClimateDataConvertor: Converting {0} file into standard format; The following triggerWords did not match the triggerwords in the given file: {1}." + "selected format: \"{2}\"", climateFile, unmatched_TriggerWords, formatProvider.SelectedFormat);
                                    throw new ApplicationException("Error in ClimateDataConvertor: Converting " + climateFile + " file into standard format; The following triggerWords did not match the triggerwords in the given file: " + unmatched_TriggerWords + "." + "selected format: \"" + formatProvider.SelectedFormat + "\"");
                                }


                            //tempScenarioName = CurrentScenarioName;
                            if (field.ToLower().Contains(prcpTriggerWord.ToLower()))
                            {
                                //CurrentScenarioName = field.Substring(1, 4);
                                CurrentScenarioType = prcpTriggerWord;
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(", " + prcpTriggerWord, "");
                            }

                            //else if (field.ToLower().Contains(maxTriggerWord.ToLower()) || field.ToLower().Contains(minTriggerWord.ToLower()))
                            //{
                            //CurrentScenarioName = field.Substring(1, 4);
                            else if (field.ToLower().Contains(maxTriggerWord.ToLower()))
                            {
                                CurrentScenarioType = maxTriggerWord;
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(maxTriggerWord, "");
                            }
                            else if (field.ToLower().Contains(minTriggerWord.ToLower()))
                            {
                                CurrentScenarioType = minTriggerWord.ToLower();
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(", " + minTriggerWord, "");
                            }
                            else if (field.ToLower().Contains(rhTriggerWord.ToLower()))
                            {
                                CurrentScenarioType = rhTriggerWord.ToLower();
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(", " + rhTriggerWord, "");
                            }
                            else if (field.ToLower().Contains(windSpeedTriggerWord.ToLower()))
                            {
                                CurrentScenarioType = windSpeedTriggerWord.ToLower();
                                unmatched_TriggerWords = unmatched_TriggerWords.Replace(", " + windSpeedTriggerWord, "");
                            }
                            //}


                            //if (tempScenarioName != CurrentScenarioName)// firstFlag == false)
                            //{
                            //    tempScenarioName = CurrentScenarioName;
                            //    //firstFlag = true;
                            //}



                            //line = reader.ReadLine();
                            //fields = line.Split(',');

                        }



                    }

                    if (fields[0] == string.Empty && !fields[0].Contains("#"))
                    {
                        line = reader.ReadLine();
                        fields = line.Split(',');

                        if (fields[0].Contains("TIME"))
                        {
                            line = reader.ReadLine();
                            fields = line.Split(',');

                            //now fill array 
                            //Get the lenght of array according to the number of ecorigions/
                            //

                        }
                    }
                    if (!fields[0].Contains("#"))//(CurrentScenarioName == tempScenarioName && !fields[0].Contains("#"))
                    {

                        key = fields[0].ToString();
                        if (CurrentScenarioType.ToLower().Contains(prcpTriggerWord.ToLower()))
                        {
                            IndexPrcp_Mean = 6;
                            //IndexPrcp_MaxT = 9;
                            IndexPrcp_Var = 7;
                            IndexPrcp_STD = 8;

                            //IndexSTD = 5;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcoregions);

                            // climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });
                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);
                            //set index of max and maxSTD for each ecorigion
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {
                                //currentT = fields[i+1];
                                //if (indexofSTD < 26)
                                //{
                                //currentSTD = fields[indexofSTD];
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]) / 10, IndexPrcp_Mean); // /10 is for mm to cm conversion
                                updatedIndex += i + climateFileActiveEcoregions.Count;
                                //climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexPrcp_MaxT);
                                //updatedIndex += numberOfAllEcoregions;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]) / 10, IndexPrcp_Var);
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]) / 10, IndexPrcp_STD);


                                //climate_Dic[key].SetValue(Convert.ToDouble(currentSTD), IndexSTD);
                                IndexPrcp_Mean = IndexPrcp_Mean + 9;
                                //IndexPrcp_MaxT = IndexPrcp_MaxT + 12;
                                IndexPrcp_Var = IndexPrcp_Var + 9;
                                IndexPrcp_STD = IndexPrcp_STD + 9;
                                updatedIndex = 0;
                                //IndexSTD = IndexSTD + 6;
                                //indexofSTD++;
                            }

                        }

                        else if (CurrentScenarioType.ToLower().Contains(maxTriggerWord.ToLower()))
                        {
                            IndexMaxT_Mean = 0;
                            //IndexMax_MaxT = 1;
                            IndexMaxT_Var = 1;
                            IndexMaxT_STD = 2;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcoregions);
                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });

                            //set index of max and maxSTD for each ecorigion
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {
                                //currentT = fields[i+1];
                                //if (indexofSTD < 26)
                                //{

                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]), IndexMaxT_Mean);
                                updatedIndex += i + climateFileActiveEcoregions.Count;
                                //climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexMax_MaxT);
                                //updatedIndex +=  numberOfAllEcoregions;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexMaxT_Var);
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexMaxT_STD);
                                IndexMaxT_Mean = IndexMaxT_Mean + 9;
                                //IndexMax_MaxT = IndexMax_MaxT + 12;
                                IndexMaxT_Var = IndexMaxT_Var + 9;
                                IndexMaxT_STD = IndexMaxT_STD + 9;
                                updatedIndex = 0;

                                //indexofSTD++;
                                //}

                            }
                        }
                        else if (CurrentScenarioType.ToLower().Contains(minTriggerWord.ToLower()))
                        {
                            IndexMinT_Mean = 3;
                            //IndexMin_MaxT = 5;
                            IndexMinT_Var = 4;
                            IndexMinT_STD = 5;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcoregions);

                            // climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });

                            //set index of max and maxSTD for each ecorigion
                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {

                                //currentT = fields[i+1];
                                //if (indexofSTD < 26)
                                //{
                                //currentSTD = fields[indexofSTD];
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]), IndexMinT_Mean);
                                updatedIndex += i + climateFileActiveEcoregions.Count;
                                //climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexMin_MaxT);
                                //updatedIndex += numberOfAllEcoregions;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexMinT_Var);
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexMinT_STD);


                                //climate_Dic[key].SetValue(Convert.ToDouble(currentSTD), IndexSTD);
                                IndexMinT_Mean = IndexMinT_Mean + 9;
                                //IndexMin_MaxT = IndexMin_MaxT + 12;
                                IndexMinT_Var = IndexMinT_Var + 9;
                                IndexMinT_STD = IndexMinT_STD + 9;
                                updatedIndex = 0;
                                //    IndexSTD = IndexSTD + 6;
                                //    indexofSTD++;
                                //}

                            }
                        }

                         //-----
                        else if (CurrentScenarioType.ToLower().Contains(rhTriggerWord.ToLower()))
                        {
                            IndexRH_Mean = 9;
                            IndexRH_Var = 10;
                            IndexRH_STD = 11;

                            //IndexSTD = 5;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcoregions);

                            // climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });
                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);
                            //set index of max and maxSTD for each ecorigion
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {

                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]), IndexRH_Mean);
                                updatedIndex += i + climateFileActiveEcoregions.Count;

                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexRH_Var);
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexRH_STD);


                                IndexRH_Mean = IndexRH_Mean + 9;
                                IndexRH_Var = IndexRH_Var + 9;
                                IndexRH_STD = IndexRH_STD + 9;
                                updatedIndex = 0;

                            }

                        }

                        //-----
                        else if (CurrentScenarioType.ToLower().Contains(windSpeedTriggerWord.ToLower()))
                        {
                            IndexwindSpeed_Mean = 12;
                            IndexwindSpeed_Var = 13;
                            IndexwindSpeed_STD = 14;

                            //IndexSTD = 5;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcoregions);

                            // climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });
                            if (!climate_Dic.Keys.Contains(key))
                                climate_Dic.Add(key, new double[dicSize]);
                            //set index of max and maxSTD for each ecorigion
                            for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                            {

                                climate_Dic[key].SetValue(Convert.ToDouble(fields[i + 1]), IndexwindSpeed_Mean);
                                updatedIndex += i + climateFileActiveEcoregions.Count;

                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexwindSpeed_Var);
                                updatedIndex += climateFileActiveEcoregions.Count;
                                climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex + 1]), IndexwindSpeed_STD);


                                IndexwindSpeed_Mean = IndexwindSpeed_Mean + 9;
                                IndexwindSpeed_Var = IndexwindSpeed_Var + 9;
                                IndexwindSpeed_STD = IndexwindSpeed_STD + 9;
                                updatedIndex = 0;

                            }

                        }


                    }

                    if (reader.EndOfStream)//CurrentScenarioName != tempScenarioName || reader.EndOfStream)
                    {
                        //tempScenarioName = CurrentScenarioName;
                        //Print file for one scenario then clear dictionary to use for another scenario

                        //Monthly peiod
                        centuryPath = "Century_Climate_Inputs_PRISM_Monthly.txt";
                        //int AverageMaxT = 0;
                        //int AverageMaxSTD = 1;
                        //int AverageMinT = 2;
                        //int AverageMinSTD = 3;
                        //int AveragePrec = 4;
                        //int AveragePrecSTD = 5;
                        IndexMaxT_Mean = 0;
                        //IndexMax_MaxT = 1;
                        IndexMaxT_Var = 1;
                        IndexMaxT_STD = 2;
                        IndexMinT_Mean = 3;
                        //IndexMin_MaxT = 5;
                        IndexMinT_Var = 4;
                        IndexMinT_STD = 5;
                        IndexPrcp_Mean = 6;
                        //IndexPrcp_MaxT = 9;
                        IndexPrcp_Var = 7;
                        IndexPrcp_STD = 8;
                        IndexRH_Mean = 9;
                        IndexRH_Var = 10;
                        IndexRH_STD = 11;

                        IndexwindSpeed_Mean = 12;
                        IndexwindSpeed_Var = 13;
                        IndexwindSpeed_STD = 14;


                        //int AverageMaxT = 0;
                        //int AverageMaxSTD = 1;
                        //int AverageMinT = 1;
                        //int AverageMinSTD = 3;
                        //int AveragePrec = 2;
                        //int AveragePrecSTD = 5;
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(centuryPath, emptytxt))
                        {
                            if (exportToTxtFormatFile)
                            {
                                file.WriteLine("LandisData" + " \"Climate Data\" \n");
                                file.WriteLine("ClimateTable \n");
                                //file.WriteLine(tempScenarioName + "\n");

                                file.WriteLine(">>Eco" + "\t\t" + "Time" + "\t" + "Month" + "\t" + "AvgMinT" + "\t" + "AvgMaxT" + "\t" + "StdDevT" + "\t" + "AvgPpt" + "\t" + "StdDevPpt" + "\t" + "PAR" + "\t" + "VarT" + "\t" + "VarPpt" + "\n");
                                file.WriteLine(">>Name" + "\t\t" + "Step" + "\t" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(k)" + "\t" + "(cm)" + "\t" + "Ppt" + "\t" + "µmol m-2 s-1" + "\t" + "(cm)" + "\t" + "(cm)" + "\n");
                                //file.WriteLine(">>Eco" + "\t" + "Time" + "\t" + "Month" + "\t" + "AvgMinT" + "\t" + "AvgMaxT" + "\t" + "StdDevT" + "\t" + "AvgPpt" + "\t" + "StdDev" + "\t" + "PAR" + "\n");
                                //file.WriteLine(">>Name" + "\t" + "Step" + "\t" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(k)" + "\t" + "(cm)" + "\t" + "Ppt" + "\t" + "µmol m-2 s-1" + "\n");
                                //file.WriteLine(">>Eco" + "\t" + "Time" + "\t" + "\t" + "AvgMaxT" + "\t" + "StdMaxT" + "\t" + "AvgMinT" + "\t" + "StdMinT" + "\t" + "AvgPpt" + "\t" + "StdDev" + "\n");
                                // file.WriteLine(">>Name" + "\t" + "Step" + "\t" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(C)" + "\n");
                            }
                            //initialize currentYear and month
                            currentYear = climate_Dic.First().Key.Substring(0, 4).ToString();
                            //starting timestep
                            currentTimeS = 0;
                            currentMonth = Convert.ToInt16(climate_Dic.First().Key.Substring(5, 2).ToString());
                            tempEco = 1;
                            lastYear = climate_Dic.AsEnumerable().Select(ax => Convert.ToInt32(ax.Key.Substring(0, 4).ToString())).Distinct().ToList().Max();
                            firstYear = climate_Dic.AsEnumerable().Select(ai => Convert.ToInt32(ai.Key.Substring(0, 4).ToString())).Distinct().ToList().Min();
                            if ((double)climate_Dic.Count / 12 > (double)lastYear - firstYear)
                                lastYear = lastYear - 1;
                            for (int j = firstYear; j <= lastYear; j++)
                            {
                                for (int i = 0; i < climateFileActiveEcoregions.Count; i++)
                                {
                                    currentYear = j.ToString();
                                    foreach (KeyValuePair<string, double[]> row in climate_Dic)
                                    {

                                        if (currentYear == row.Key.Substring(0, 4).ToString())
                                        {
                                            if (currentMonth == Convert.ToInt16(row.Key.Substring(5, 2)))
                                            {
                                                AverageMin += Math.Round(row.Value[IndexMinT_Mean], 2);
                                                AverageMax += Math.Round(row.Value[IndexMaxT_Mean], 2);
                                                SumPrecp += (Math.Round(row.Value[IndexPrcp_Mean], 2));
                                                AverageSTDT += Math.Round((row.Value[IndexMaxT_Var] + row.Value[IndexMinT_Var]) / 2, 2);
                                                sumVarPpt += Convert.ToDouble(row.Value[IndexPrcp_STD]);
                                                numberOfDays++;

                                            }
                                            else
                                            {
                                                if (exportToTxtFormatFile)
                                                    file.WriteLine(climateFileActiveEcoregions.ElementAt(i).Value.Name + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(SumPrecp, 2) + "\t" + Math.Round(sumVarPpt, 2) + "\t" + "0.0" + "\t" + Math.Round(AverageSTDT / numberOfDays, 2) + "\t" + Math.Round(sumVarPpt, 2) + "\n");
                                                //file.WriteLine("eco1" + "\t" + currentYear + "\t" + currentMonth + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(AverageMaxSTD / numberOfDays, 2) + "\t" + Math.Round(AverageMinT / numberOfDays, 2) + "\t" + Math.Round(AverageMinSTD / numberOfDays, 2) + "\t" + Math.Round(AveragePrec / numberOfDays, 2) + "\t" + Math.Round(AveragePrecSTD / numberOfDays, 2) + "\n");
                                                //tempMonth = currentMonth;
                                                currentMonth = Convert.ToInt16(row.Key.Substring(5, 2));
                                                //if (tempMonth != currentMonth)

                                                AverageMax = 0;
                                                //AverageMaxSTD = 0;
                                                AverageMin = 0;
                                                //AverageMinSTD = 0;
                                                SumPrecp = 0;
                                                //AveragePrecSTD = 0;
                                                AverageSTDT = 0;
                                                sumVarPpt = 0;
                                                numberOfDays = 0;
                                                AverageMin += Math.Round(row.Value[IndexMinT_Mean], 2);
                                                AverageMax += Math.Round(row.Value[IndexMaxT_Mean], 2);
                                                SumPrecp += (Math.Round(row.Value[IndexPrcp_Mean], 2)); 
                                                AverageSTDT += Math.Round((row.Value[IndexMaxT_Var] + row.Value[IndexMinT_Var]) / 2, 2);
                                                sumVarPpt += Convert.ToDouble(row.Value[IndexPrcp_STD]);
                                                //sums = 0;
                                                //stdTemp = 0;
                                                //prpSums = 0;
                                                //stdPrp = 0;
                                                numberOfDays++;
                                            }
                                        }
                                        else //  currentYear != row.Key.Substring(0, 4).ToString())
                                        {
                                            //if (tempEco != i && currentMonth == 12)
                                            //    file.WriteLine("eco" + tempEco.ToString() + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(StdDevPpt, 2) + "\t" + "0.0" + "\n");

                                            if (exportToTxtFormatFile)
                                                if (currentMonth == 12)
                                                    file.WriteLine(climateFileActiveEcoregions.ElementAt(i).Value.Name + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(SumPrecp, 2) + "\t" + Math.Round(sumVarPpt, 2) + "\t" + "0.0" + "\t" + Math.Round(AverageSTDT / numberOfDays, 2) + "\t" + Math.Round(sumVarPpt, 2) + "\n");
                                            ////if (currentTimeS == 0 && currentMonth == 12 && i==2)
                                            //    file.WriteLine("eco2" + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(StdDevPpt, 2) + "\t" + "0.0" + "\n");

                                            //else if (tempEco != i)
                                            //    currentTimeS = 0;
                                            //file.WriteLine("eco1" + "\t" + currentYear + "\t" + currentMonth + "\t" + Math.Round(AverageMaxT / numberOfDays, 2) + "\t" + Math.Round(AverageMaxSTD / numberOfDays, 2) + "\t" + Math.Round(AverageMinT / numberOfDays, 2) + "\t" + Math.Round(AverageMinSTD / numberOfDays, 2) + "\t" + Math.Round(AveragePrec / numberOfDays, 2) + "\t" + Math.Round(AveragePrecSTD / numberOfDays, 2) + "\n");

                                            //currentYear = row.Key.Substring(0, 4).ToString();
                                            //currentTimeS = currentTimeS + 1;
                                            tempEco = i;
                                            currentMonth = 1;
                                            AverageMax = 0;
                                            //AverageMaxSTD = 0;
                                            AverageMin = 0;
                                            //AverageMinSTD = 0;
                                            SumPrecp = 0;
                                            //AveragePrecSTD = 0;

                                            AverageSTDT = 0;
                                            sumVarPpt = 0;

                                            numberOfDays = 0;

                                        }
                                    }
                                    tempEco = i;
                                    currentMonth = 1;
                                    AverageMax = 0;
                                    //AverageMaxSTD = 0;
                                    AverageMin = 0;
                                    //AverageMinSTD = 0;
                                    SumPrecp = 0;
                                    //AveragePrecSTD = 0;

                                    AverageSTDT = 0;
                                    sumVarPpt = 0;
                                    IndexMaxT_Mean = IndexMaxT_Mean + 9;
                                    //IndexMax_MaxT = IndexMax_MaxT + 12;
                                    IndexMaxT_Var = IndexMaxT_Var + 9;
                                    IndexMaxT_STD = IndexMaxT_STD + 9;
                                    IndexMinT_Mean = IndexMinT_Mean + 9;
                                    //IndexMin_MaxT = IndexMin_MaxT + 12;
                                    IndexMinT_Var = IndexMinT_Var + 9;
                                    IndexMinT_STD = IndexMinT_STD + 9;
                                    IndexPrcp_Mean = IndexPrcp_Mean + 9;
                                    //IndexPrcp_MaxT = IndexPrcp_MaxT + 12;
                                    IndexPrcp_Var = IndexPrcp_Var + 9;
                                    IndexPrcp_STD = IndexPrcp_STD + 9;

                                    IndexRH_Mean = IndexRH_Mean + 9;
                                    IndexRH_Var = IndexRH_Var + 9;
                                    IndexRH_STD = IndexRH_STD + 9;

                                    IndexwindSpeed_Mean = IndexwindSpeed_Mean + 9;
                                    IndexwindSpeed_Var = IndexwindSpeed_Var + 9;
                                    IndexwindSpeed_STD = IndexwindSpeed_STD + 9;

                                }

                                tempEco = 1;
                                currentTimeS = currentTimeS + 1;
                                IndexMaxT_Mean = 0;
                                //IndexMax_MaxT = 1;
                                IndexMaxT_Var = 1;
                                IndexMaxT_STD = 2;
                                IndexMinT_Mean = 3;
                                //IndexMin_MaxT = 5;
                                IndexMinT_Var = 4;
                                IndexMinT_STD = 5;
                                IndexPrcp_Mean = 6;
                                //IndexPrcp_MaxT = 9;
                                IndexPrcp_Var = 7;
                                IndexPrcp_STD = 8;

                                IndexRH_Mean = 9;
                                IndexRH_Var = 10;
                                IndexRH_STD = 11;

                                IndexwindSpeed_Mean = 12;
                                IndexwindSpeed_Var = 13;
                                IndexwindSpeed_STD = 14;

                                currentMonth = 1;
                                AverageMax = 0;
                                //AverageMaxSTD = 0;
                                AverageMin = 0;
                                //AverageMinSTD = 0;
                                SumPrecp = 0;
                                //AveragePrecSTD = 0;

                                AverageSTDT = 0;
                                sumVarPpt = 0;
                            }


                            //    for (int i = 1; i <= numberOfAllEcoregions; i++)
                            //    {

                            //        foreach (KeyValuePair<string, double[]> row in climate_Dic)
                            //        {

                            //            //file.WriteLine("eco" + i.ToString() + "\t" + row.Key.Remove(10) + "\t" + Math.Round(row.Value[AverageMaxT], 2) +  "\t" + Math.Round(row.Value[AverageMinT], 2) +  "\t" + Math.Round(row.Value[AveragePrec], 2)  + "\n");
                            //            //file.WriteLine("eco" + i.ToString() + "\t" + row.Key.Remove(10) + "\t" + Math.Round(row.Value[AverageMaxT], 2) + "\t" + Math.Round(row.Value[AverageMaxSTD], 2) + "\t" + Math.Round(row.Value[AverageMinT], 2) + "\t" + Math.Round(row.Value[AverageMinSTD], 2) + "\t" + Math.Round(row.Value[AveragePrec], 2) + "\t" + Math.Round(row.Value[AveragePrecSTD], 2) + "\n");

                            //                if (currentYear == row.Key.Substring(0, 4).ToString())
                            //                {

                            //                    if (currentMonth == Convert.ToInt16(row.Key.Substring(5, 2)))
                            //                    {

                            //                        //(row.Value[IndexMax_MaxT] + row.Value[IndexMaxT_Mean])/2
                            //                        //AverageMin += (row.Value[IndexMin_MaxT] + row.Value[IndexMinT_Mean]) / 2;
                            //                        //AverageMax += (row.Value[IndexMax_MaxT] + row.Value[IndexMaxT_Mean]) / 2;
                            //                        //AveragePrecp += (row.Value[IndexPrcp_MaxT] + row.Value[IndexPrcp_Mean]) / 2;
                            //                        //AverageSTDT += (row.Value[IndexMaxT_Var] + row.Value[IndexMinT_Var]) / 2;
                            //                        //AverageMaxSTD += Math.Round(Convert.ToDouble(row.Value[2]), 2);
                            //                        AverageMin += Math.Round(row.Value[IndexMinT_Mean], 2);
                            //                        AverageMax += Math.Round(row.Value[IndexMaxT_Mean], 2);
                            //                        AveragePrecp += Math.Round(row.Value[IndexPrcp_Mean], 2);
                            //                        AverageSTDT += Math.Round((row.Value[IndexMaxT_Var] + row.Value[IndexMinT_Var]) / 2, 2);
                            //                        StdDevPpt += Convert.ToDouble(row.Value[IndexPrcp_STD]);

                            //                        //AverageMinSTD += Math.Round(Convert.ToDouble(row.Value[4]), 2);
                            //                        //AveragePrecp += Math.Round(row.Value[AveragePrec], 2);
                            //                        //AveragePrecSTD += Math.Round(Convert.ToDouble(row.Value[6]), 2);

                            //                        //Calculating STD of Tempeture
                            //                        //tempSum[numberOfDays] = (row.Value[AverageMaxT] + row.Value[AverageMinT]) / 2;
                            //                        //stdTemp = 0;
                            //                        //stdPrp = 0;


                            //                        //Calculating STD of Prp
                            //                        //tempPrp[numberOfDays] = row.Value[AveragePrec];

                            //                        numberOfDays++;

                            //                    }


                            //                    else
                            //                    {
                            //                        //for (int j = 0; j < numberOfDays; j++)
                            //                        //{
                            //                        //    sums += Math.Pow((tempSum[j] - (((AverageMax / numberOfDays) + (AverageMin / numberOfDays)) / 2)), 2);
                            //                        //    prpSums += Math.Pow(tempPrp[j] - (AveragePrec / numberOfDays), 2);
                            //                        //}

                            //                        //stdTemp = Math.Sqrt(sums / (numberOfDays - 1));
                            //                        //stdPrp = Math.Sqrt(prpSums / (numberOfDays - 1));
                            //                        file.WriteLine("eco" + i.ToString() + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(StdDevPpt, 2) + "\t" + "0.0" + "\n");
                            //                        //file.WriteLine("eco1" + "\t" + currentYear + "\t" + currentMonth + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(AverageMaxSTD / numberOfDays, 2) + "\t" + Math.Round(AverageMinT / numberOfDays, 2) + "\t" + Math.Round(AverageMinSTD / numberOfDays, 2) + "\t" + Math.Round(AveragePrec / numberOfDays, 2) + "\t" + Math.Round(AveragePrecSTD / numberOfDays, 2) + "\n");
                            //                        //tempMonth = currentMonth;
                            //                        currentMonth = Convert.ToInt16(row.Key.Substring(5, 2));
                            //                        //if (tempMonth != currentMonth)

                            //                        AverageMax = 0;
                            //                        //AverageMaxSTD = 0;
                            //                        AverageMin = 0;
                            //                        //AverageMinSTD = 0;
                            //                        AveragePrecp = 0;
                            //                        //AveragePrecSTD = 0;
                            //                        AverageSTDT = 0;
                            //                        StdDevPpt = 0;

                            //                        numberOfDays = 0;
                            //                        AverageMin += Math.Round(row.Value[IndexMinT_Mean], 2);
                            //                        AverageMax += Math.Round(row.Value[IndexMaxT_Mean], 2);
                            //                        AveragePrecp += Math.Round(row.Value[IndexPrcp_Mean], 2);
                            //                        AverageSTDT += Math.Round((row.Value[IndexMaxT_Var] + row.Value[IndexMinT_Var]) / 2, 2);
                            //                        StdDevPpt += Convert.ToDouble(row.Value[IndexPrcp_STD]);
                            //                        //sums = 0;
                            //                        //stdTemp = 0;
                            //                        //prpSums = 0;
                            //                        //stdPrp = 0;
                            //                        numberOfDays++;
                            //                    }

                            //                }
                            //                else
                            //                {
                            //                    //If ecorigion has been changed
                            //                    if (tempEco != i && currentMonth == 12)
                            //                    {
                            //                        file.WriteLine("eco" + tempEco.ToString() + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(StdDevPpt, 2) + "\t" + "0.0" + "\n");
                            //                        currentTimeS = 0;
                            //                    }

                            //                    else if (currentMonth == 12)
                            //                    {
                            //                        file.WriteLine("eco" + i.ToString() + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(StdDevPpt, 2) + "\t" + "0.0" + "\n");
                            //                        currentTimeS = currentTimeS + 1;
                            //                    }
                            //                    else if (tempEco != i)
                            //                        currentTimeS = 0;
                            //                    //file.WriteLine("eco1" + "\t" + currentYear + "\t" + currentMonth + "\t" + Math.Round(AverageMaxT / numberOfDays, 2) + "\t" + Math.Round(AverageMaxSTD / numberOfDays, 2) + "\t" + Math.Round(AverageMinT / numberOfDays, 2) + "\t" + Math.Round(AverageMinSTD / numberOfDays, 2) + "\t" + Math.Round(AveragePrec / numberOfDays, 2) + "\t" + Math.Round(AveragePrecSTD / numberOfDays, 2) + "\n");

                            //                    currentYear = row.Key.Substring(0, 4).ToString();
                            //                    //currentTimeS = currentTimeS + 1;
                            //                    tempEco = i;
                            //                    currentMonth = 1;
                            //                    AverageMax = 0;
                            //                    //AverageMaxSTD = 0;
                            //                    AverageMin = 0;
                            //                    //AverageMinSTD = 0;
                            //                    AveragePrecp = 0;
                            //                    //AveragePrecSTD = 0;

                            //                    AverageSTDT = 0;
                            //                    StdDevPpt = 0;

                            //                    numberOfDays = 0;
                            //                    AverageMin += Math.Round(row.Value[IndexMinT_Mean], 2);
                            //                    AverageMax += Math.Round(row.Value[IndexMaxT_Mean], 2);
                            //                    AveragePrecp += Math.Round(row.Value[IndexPrcp_Mean], 2);
                            //                    AverageSTDT += Math.Round((row.Value[IndexMaxT_Var] + row.Value[IndexMinT_Var]) / 2, 2);
                            //                    StdDevPpt += Convert.ToDouble(row.Value[IndexPrcp_STD]);
                            //                    //sums = 0;
                            //                    //stdTemp = 0;
                            //                    //prpSums = 0;
                            //                    //stdPrp = 0;
                            //                    numberOfDays++;
                            //                }


                            //        }

                            //        IndexMaxT_Mean = IndexMaxT_Mean + 9;
                            //        //IndexMax_MaxT = IndexMax_MaxT + 12;
                            //        IndexMaxT_Var = IndexMaxT_Var + 9;
                            //        IndexMaxT_STD = IndexMaxT_STD + 9;
                            //        IndexMinT_Mean = IndexMinT_Mean + 9;
                            //        //IndexMin_MaxT = IndexMin_MaxT + 12;
                            //        IndexMinT_Var = IndexMinT_Var + 9;
                            //        IndexMinT_STD = IndexMinT_STD + 9;
                            //        IndexPrcp_Mean = IndexPrcp_Mean + 9;
                            //        //IndexPrcp_MaxT = IndexPrcp_MaxT + 12;
                            //        IndexPrcp_Var = IndexPrcp_Var + 9;
                            //        IndexPrcp_STD = IndexPrcp_STD + 9;
                            //    }
                            //    file.WriteLine("eco" + numberOfAllEcoregions.ToString() + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(StdDevPpt, 2) + "\t" + "0.0" + "\n");



                        }


                        //If file contains more than one scenario then these setting will be needed
                        //climate_Dic.Clear();
                        //emptytxt = true;
                        //tempScenarioName = CurrentScenarioName;

                    }
                }

                if (unmatched_TriggerWords != "")
                {
                    Climate.ModelCore.UI.WriteLine("Error in ClimateDataConvertor: Converting {0} file into standard format; The following triggerWords did not match the triggerwords in the given file: {1}." + "selected format: \"{2}\"", climateFile, unmatched_TriggerWords, formatProvider.SelectedFormat);
                    throw new ApplicationException("Error in ClimateDataConvertor: Converting " + climateFile + " file into standard format; The following triggerWords did not match the triggerwords in the given file: " + unmatched_TriggerWords + "." + "selected format: \"" + formatProvider.SelectedFormat + "\"");
                }

            }

            #endregion

            return; // centuryPath;

        }

        private static void setIndexed()
        {

            IndexMaxT_Mean = 0;
            IndexMaxT_Var = 1;
            IndexMaxT_STD = 2;
            IndexMinT_Mean = 3;
            IndexMinT_Var = 4;
            IndexMinT_STD = 5;
            IndexPrcp_Mean = 6;
            IndexPrcp_Var = 7;
            IndexPrcp_STD = 8;
            IndexRH_Mean = 9;
            IndexRH_Var = 10;
            IndexRH_STD = 11;

            IndexwindSpeed_Mean = 12;
            IndexwindSpeed_Var = 13;
            IndexwindSpeed_STD = 14;

        }
    }
}

