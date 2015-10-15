
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Landis.Library.Climate
{
    public class ClimateDataConvertor
    {

        private static string maxTriggerWord = "maxtemp";
        private static string minTriggerWord = "mintemp";
        private static string prcpTriggerWord = "ppt";

        public static string Convert_USGS_to_ClimateData(Period period, string climateFile)
        {

            string path = climateFile;
            StreamReader sreader;
            string centuryPath = "";
            // monthly and daily climates should be filled before in order to chack weather input climatefile can be processed as daily or monthly
            //List<string> montlyClimates;
            //List<string> DailyClimate;

            #region GCM Input file is Daily--- convert to monthly
            if (period == Period.Daily)
            {

                //string path = @"D:\PSU\Landis_II\amin-branch\USGS_Data\Hayhoe_Climate_Data1.csv";
                sreader = new StreamReader(path);
                string line;
                string[] fields;
                //string tempScenarioName = "";
                DataTable _dataTableDataByTime = new DataTable();
                int numberOfAllEcorigions = 0;
                line = sreader.ReadLine();
                fields = line.Split(',');
                //tempScenarioName = fields[0].Substring(1, fields[0].LastIndexOf("t") - 2);
                line = sreader.ReadLine();
                fields = line.Split(',');
                //int totalRows = 0;
                //string[,] wholedata;
                //string CurrentScenarioName = "";

                string CurrentScenarioType = "";
                Dictionary<string, double[]> century_climate_Dic = new Dictionary<string, double[]>();

                //string currentT;
                //string currentSTD;
                //string currentPart = "";
                //int totalRow = 0;
                string key = "";
                int IndexMax_MeanT = 0;
                //int IndexMax_MaxT = 1;
                int IndexMax_Var = 1;
                int IndexMax_STD = 2;
                int IndexMin_MeanT = 3;
                //int IndexMin_MaxT = 5;
                int IndexMin_Var = 4;
                int IndexMin_STD = 5;
                int IndexPrcp_MeanT = 6;
                //int IndexPrcp_MaxT = 9;
                int IndexPrcp_Var = 7;
                int IndexPrcp_STD = 8;

                //bool firstFlag = false;
                string currentYear = "";
                int currentTimeS = 0;
                int currentMonth = 1;
                int tempEco = 1;
                double AverageMax = 0;

                //double AverageMaxSTD = 0;
                double AverageMin = 0;
                //double AverageMinSTD = 0;
                double AveragePrecp = 0;

                double AverageSTDT = 0;
                double StdDevPpt = 0;
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

                foreach (string field in fields)
                {
                    if (field != "" && Convert.ToInt16(field) > numberOfAllEcorigions)
                    {
                        numberOfAllEcorigions = Convert.ToInt16(field);
                    }
                }
                //12 beacuse for each ecoriogn we need Max_MinT,Max_MaxT,Max_Var Max_Std, Min_MinT,Min_MaxT,Min_Var Min_Std, Prcp_MinT,Prcp_MaxT,Prcp_Var Prcp_Std
                int dicSize = numberOfAllEcorigions * 9;
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
                            //tempScenarioName = CurrentScenarioName;
                            if (field.ToLower().Contains(maxTriggerWord) || field.ToLower().Contains(minTriggerWord))
                            {
                                //CurrentScenarioName = field.Substring(1, field.LastIndexOf("t") - 2);
                                if (field.ToLower().Contains(maxTriggerWord))
                                    CurrentScenarioType = maxTriggerWord;
                                if (field.ToLower().Contains(minTriggerWord))
                                    CurrentScenarioType = minTriggerWord;
                            }
                            if (field.ToLower().Contains(prcpTriggerWord))
                            {
                                //CurrentScenarioName = field.Substring(1, field.LastIndexOf("p") - 2);
                                CurrentScenarioType = prcpTriggerWord;

                            }

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
                    if (!fields[0].Contains("#"))
                    //if (CurrentScenarioName == tempScenarioName && !fields[0].Contains("#"))
                    {

                        key = fields[0].ToString();
                        if (CurrentScenarioType.Contains(maxTriggerWord))
                        {
                            IndexMax_MeanT = 0;
                            //IndexMax_MaxT = 1;
                            IndexMax_Var = 1;
                            IndexMax_STD = 2;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcorigions);
                            if (!century_climate_Dic.Keys.Contains(key))
                                century_climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });

                            //set index of max and maxSTD for each ecorigion
                            for (int i = 1; i <= numberOfAllEcorigions; i++)
                            {
                                //currentT = fields[i];
                                //if (indexofSTD < 26)
                                //{

                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[i]), IndexMax_MeanT);
                                updatedIndex += i + numberOfAllEcorigions;
                                //century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexMax_MaxT);
                                //updatedIndex +=  numberOfAllEcorigions;
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexMax_Var);
                                updatedIndex += numberOfAllEcorigions;
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexMax_STD);
                                IndexMax_MeanT = IndexMax_MeanT + 9;
                                //IndexMax_MaxT = IndexMax_MaxT + 12;
                                IndexMax_Var = IndexMax_Var + 9;
                                IndexMax_STD = IndexMax_STD + 9;
                                updatedIndex = 0;

                                //indexofSTD++;
                                //}

                            }
                        }
                        else if (CurrentScenarioType.Contains(minTriggerWord))
                        {
                            IndexMin_MeanT = 3;
                            //IndexMin_MaxT = 5;
                            IndexMin_Var = 4;
                            IndexMin_STD = 5;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcorigions);

                            // century_climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });
                            if (!century_climate_Dic.Keys.Contains(key))
                                century_climate_Dic.Add(key, new double[dicSize]);
                            //set index of max and maxSTD for each ecorigion
                            for (int i = 1; i <= numberOfAllEcorigions; i++)
                            {
                                //currentT = fields[i];
                                //if (indexofSTD < 26)
                                //{
                                //currentSTD = fields[indexofSTD];
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[i]), IndexMin_MeanT);
                                updatedIndex += i + numberOfAllEcorigions;
                                //century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexMin_MaxT);
                                //updatedIndex += numberOfAllEcorigions;
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexMin_Var);
                                updatedIndex += numberOfAllEcorigions;
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexMin_STD);


                                //century_climate_Dic[key].SetValue(Convert.ToDouble(currentSTD), IndexSTD);
                                IndexMin_MeanT = IndexMin_MeanT + 9;
                                //IndexMin_MaxT = IndexMin_MaxT + 12;
                                IndexMin_Var = IndexMin_Var + 9;
                                IndexMin_STD = IndexMin_STD + 9;
                                updatedIndex = 0;
                                //    IndexSTD = IndexSTD + 6;
                                //    indexofSTD++;
                                //}

                            }
                        }
                        else if (CurrentScenarioType.Contains(prcpTriggerWord))
                        {
                            IndexPrcp_MeanT = 6;
                            //IndexPrcp_MaxT = 9;
                            IndexPrcp_Var = 7;
                            IndexPrcp_STD = 8;

                            //IndexSTD = 5;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcorigions);

                            // century_climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });
                            if (!century_climate_Dic.Keys.Contains(key))
                                century_climate_Dic.Add(key, new double[dicSize]);
                            //set index of max and maxSTD for each ecorigion
                            for (int i = 1; i <= numberOfAllEcorigions; i++)
                            {
                                //currentT = fields[i];
                                //if (indexofSTD < 26)
                                //{
                                //currentSTD = fields[indexofSTD];
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[i]), IndexPrcp_MeanT);
                                updatedIndex += i + numberOfAllEcorigions;
                                //century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexPrcp_MaxT);
                                //updatedIndex += numberOfAllEcorigions;
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexPrcp_Var);
                                updatedIndex += numberOfAllEcorigions;
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexPrcp_STD);


                                //century_climate_Dic[key].SetValue(Convert.ToDouble(currentSTD), IndexSTD);
                                IndexPrcp_MeanT = IndexPrcp_MeanT + 9;
                                //IndexPrcp_MaxT = IndexPrcp_MaxT + 12;
                                IndexPrcp_Var = IndexPrcp_Var + 9;
                                IndexPrcp_STD = IndexPrcp_STD + 9;
                                updatedIndex = 0;
                                //IndexSTD = IndexSTD + 6;
                                //indexofSTD++;
                            }

                        }

                    }

                    //if (CurrentScenarioName != tempScenarioName || reader.EndOfStream)
                    if (reader.EndOfStream)
                    {
                        //tempScenarioName = CurrentScenarioName;
                        //Print file for one scenario then clear dictionary to use for another scenario

                        //Daily peiod
                        centuryPath = "Century_Climate_Inputs_Monthly.txt";
                        //int AverageMaxT = 0;
                        //int AverageMaxSTD = 1;
                        //int AverageMinT = 2;
                        //int AverageMinSTD = 3;
                        //int AveragePrec = 4;
                        //int AveragePrecSTD = 5;
                        IndexMax_MeanT = 0;
                        //IndexMax_MaxT = 1;
                        IndexMax_Var = 1;
                        IndexMax_STD = 2;
                        IndexMin_MeanT = 3;
                        //IndexMin_MaxT = 5;
                        IndexMin_Var = 4;
                        IndexMin_STD = 5;
                        IndexPrcp_MeanT = 6;
                        //IndexPrcp_MaxT = 9;
                        IndexPrcp_Var = 7;
                        IndexPrcp_STD = 8;


                        //int AverageMaxT = 0;
                        //int AverageMaxSTD = 1;
                        //int AverageMinT = 1;
                        //int AverageMinSTD = 3;
                        //int AveragePrec = 2;
                        //int AveragePrecSTD = 5;
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(centuryPath, emptytxt))
                        {
                            file.WriteLine("LandisData" + " \"Climate Data\" \n");
                            file.WriteLine("ClimateTable \n");
                            //file.WriteLine(tempScenarioName + "\n");
                            file.WriteLine(">>Eco" + "\t" + "Time" + "\t" + "Month" + "\t" + "AvgMinT" + "\t" + "AvgMaxT" + "\t" + "StdDevT" + "\t" + "AvgPpt" + "\t" + "StdDev" + "\t" + "PAR" + "\t" + "VarT" + "\t" + "VarPpt" + "\n");
                            file.WriteLine(">>Name" + "\t" + "Step" + "\t" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(k)" + "\t" + "(cm)" + "\t" + "Ppt" + "\t" + "µmol m-2 s-1" + "\t" + "(cm)" + "\t" + "(cm)" + "\n");
                            //file.WriteLine(">>Eco" + "\t" + "Time" + "\t" + "\t" + "AvgMaxT" + "\t" + "StdMaxT" + "\t" + "AvgMinT" + "\t" + "StdMinT" + "\t" + "AvgPpt" + "\t" + "StdDev" + "\n");
                            // file.WriteLine(">>Name" + "\t" + "Step" + "\t" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(C)" + "\n");
                            //initialize currentYear and month
                            currentYear = century_climate_Dic.First().Key.Substring(0, 4).ToString();
                            //starting timestep
                            currentTimeS = 0;
                            currentMonth = Convert.ToInt16(century_climate_Dic.First().Key.Substring(5, 2).ToString());
                            tempEco = 1;

                            int lastYear = century_climate_Dic.AsEnumerable().Select(ax => Convert.ToInt32(ax.Key.Substring(0, 4).ToString())).Distinct().ToList().Max();
                            int firstYear = century_climate_Dic.AsEnumerable().Select(ai => Convert.ToInt32(ai.Key.Substring(0, 4).ToString())).Distinct().ToList().Min();
                            if ((double)century_climate_Dic.Count / 12 > (double)lastYear - firstYear)
                                lastYear = lastYear - 1;

                            for (int j = firstYear; j <= lastYear; j++)
                            {
                                for (int i = 1; i <= numberOfAllEcorigions; i++)
                                {
                                    currentYear = j.ToString();

                                    foreach (KeyValuePair<string, double[]> row in century_climate_Dic)
                                    {

                                        if (currentYear == row.Key.Substring(0, 4).ToString())
                                        {

                                            if (currentMonth == Convert.ToInt16(row.Key.Substring(5, 2)))
                                            {

                                                //(row.Value[IndexMax_MaxT] + row.Value[IndexMax_MeanT])/2
                                                //AverageMin += (row.Value[IndexMin_MaxT] + row.Value[IndexMin_MeanT]) / 2;
                                                //AverageMax += (row.Value[IndexMax_MaxT] + row.Value[IndexMax_MeanT]) / 2;
                                                //AveragePrecp += (row.Value[IndexPrcp_MaxT] + row.Value[IndexPrcp_MeanT]) / 2;
                                                //AverageSTDT += (row.Value[IndexMax_Var] + row.Value[IndexMin_Var]) / 2;
                                                //AverageMaxSTD += Math.Round(Convert.ToDouble(row.Value[2]), 2);
                                                AverageMin += Math.Round(row.Value[IndexMin_MeanT], 2);
                                                AverageMax += Math.Round(row.Value[IndexMax_MeanT], 2);
                                                AveragePrecp += Math.Round(row.Value[IndexPrcp_MeanT], 2);
                                                AverageSTDT += Math.Round((row.Value[IndexMax_Var] + row.Value[IndexMin_Var]) / 2, 2);
                                                StdDevPpt += Convert.ToDouble(row.Value[IndexPrcp_Var]);


                                                numberOfDays++;

                                            }


                                            else
                                            {

                                                file.WriteLine("eco" + i.ToString() + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(StdDevPpt), 2) + "\t" + "0.0" + "\t" + Math.Round(AverageSTDT / numberOfDays, 2) + "\t" + Math.Round(StdDevPpt / numberOfDays, 2) + "\n");
                                                currentMonth = Convert.ToInt16(row.Key.Substring(5, 2));
                                                //if (tempMonth != currentMonth)

                                                AverageMax = 0;
                                                AverageMin = 0;
                                                AveragePrecp = 0;
                                                AverageSTDT = 0;
                                                StdDevPpt = 0;

                                                numberOfDays = 0;
                                                AverageMin += Math.Round(row.Value[IndexMin_MeanT], 2);
                                                AverageMax += Math.Round(row.Value[IndexMax_MeanT], 2);
                                                AveragePrecp += Math.Round(row.Value[IndexPrcp_MeanT], 2);
                                                AverageSTDT += Math.Round((row.Value[IndexMax_Var] + row.Value[IndexMin_Var]) / 2, 2);
                                                StdDevPpt += Convert.ToDouble(row.Value[IndexPrcp_Var]);

                                                numberOfDays++;
                                            }

                                        }
                                        else
                                        {
                                            //If ecorigion has been changed
                                            //if (tempEco != i && currentMonth == 12)
                                            //{
                                            //    file.WriteLine("eco" + tempEco.ToString() + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(StdDevPpt), 2) + "\t" + "0.0" + "\n");
                                            //    currentTimeS = 0;
                                            //}
                                            if (currentMonth == 12)
                                            {
                                                file.WriteLine("eco" + i.ToString() + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(StdDevPpt), 2) + "\t" + "0.0" + "\t" + Math.Round(AverageSTDT / numberOfDays, 2) + "\t" + Math.Round(StdDevPpt / numberOfDays, 2) + "\n");
                                            }
                                            //else if (tempEco != i)
                                            //    currentTimeS = 0;


                                            //file.WriteLine("eco1" + "\t" + currentYear + "\t" + currentMonth + "\t" + Math.Round(AverageMaxT / numberOfDays, 2) + "\t" + Math.Round(AverageMaxSTD / numberOfDays, 2) + "\t" + Math.Round(AverageMinT / numberOfDays, 2) + "\t" + Math.Round(AverageMinSTD / numberOfDays, 2) + "\t" + Math.Round(AveragePrec / numberOfDays, 2) + "\t" + Math.Round(AveragePrecSTD / numberOfDays, 2) + "\n");

                                            //currentYear = row.Key.Substring(0, 4).ToString();

                                            tempEco = i;
                                            currentMonth = 1;
                                            AverageMax = 0;
                                            //AverageMaxSTD = 0;
                                            AverageMin = 0;
                                            //AverageMinSTD = 0;
                                            AveragePrecp = 0;
                                            //AveragePrecSTD = 0;

                                            AverageSTDT = 0;
                                            StdDevPpt = 0;

                                            numberOfDays = 0;
                                            AverageMin += Math.Round(row.Value[IndexMin_MeanT], 2);
                                            AverageMax += Math.Round(row.Value[IndexMax_MeanT], 2);
                                            AveragePrecp += Math.Round(row.Value[IndexPrcp_MeanT], 2);
                                            AverageSTDT += Math.Round((row.Value[IndexMax_Var] + row.Value[IndexMin_Var]) / 2, 2);
                                            StdDevPpt += Convert.ToDouble(row.Value[IndexPrcp_Var]);
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
                                    AveragePrecp = 0;
                                    //AveragePrecSTD = 0;

                                    AverageSTDT = 0;
                                    StdDevPpt = 0;

                                    IndexMax_MeanT = IndexMax_MeanT + 9;
                                    //IndexMax_MaxT = IndexMax_MaxT + 12;
                                    IndexMax_Var = IndexMax_Var + 9;
                                    IndexMax_STD = IndexMax_STD + 9;
                                    IndexMin_MeanT = IndexMin_MeanT + 9;
                                    //IndexMin_MaxT = IndexMin_MaxT + 12;
                                    IndexMin_Var = IndexMin_Var + 9;
                                    IndexMin_STD = IndexMin_STD + 9;
                                    IndexPrcp_MeanT = IndexPrcp_MeanT + 9;
                                    //IndexPrcp_MaxT = IndexPrcp_MaxT + 12;
                                    IndexPrcp_Var = IndexPrcp_Var + 9;
                                    IndexPrcp_STD = IndexPrcp_STD + 9;
                                }
                                //file.WriteLine("eco" + numberOfAllEcorigions.ToString() + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(StdDevPpt), 2) + "\t" + "0.0" + "\n");

                                tempEco = 1;
                                currentTimeS = currentTimeS + 1;
                                IndexMax_MeanT = 0;
                                //IndexMax_MaxT = 1;
                                IndexMax_Var = 1;
                                IndexMax_STD = 2;
                                IndexMin_MeanT = 3;
                                //IndexMin_MaxT = 5;
                                IndexMin_Var = 4;
                                IndexMin_STD = 5;
                                IndexPrcp_MeanT = 6;
                                //IndexPrcp_MaxT = 9;
                                IndexPrcp_Var = 7;
                                IndexPrcp_STD = 8;
                                currentMonth = 1;
                                AverageMax = 0;
                                //AverageMaxSTD = 0;
                                AverageMin = 0;
                                //AverageMinSTD = 0;
                                AveragePrecp = 0;
                                //AveragePrecSTD = 0;

                                AverageSTDT = 0;
                                StdDevPpt = 0;
                            }

                        }

                        //If file contains more than one scenario then these setting will be needed
                        century_climate_Dic.Clear();
                        emptytxt = true;
                        //tempScenarioName = CurrentScenarioName;

                    }
                }




            }
            #endregion


            #region PRISM Data
            else if (period == Period.Monthly)
            {


                //string path = @"D:\PSU\Landis_II\amin-branch\USGS_Data\Hayhoe_Climate_Data1.csv";
                sreader = new StreamReader(path);
                string line;
                string[] fields;
                //string tempScenarioName = "";
                DataTable _dataTableDataByTime = new DataTable();
                int numberOfAllEcorigions = 0;
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
                Dictionary<string, double[]> century_climate_Dic = new Dictionary<string, double[]>();

                //string currentT;
                //string currentSTD;
                //string currentPart = "";
                //int totalRow = 0;
                string key = "";
                int IndexMax_MeanT = 0;
                //int IndexMax_MaxT = 1;
                int IndexMax_Var = 1;
                int IndexMax_STD = 2;
                int IndexMin_MeanT = 3;
                //int IndexMin_MaxT = 5;
                int IndexMin_Var = 4;
                int IndexMin_STD = 5;
                int IndexPrcp_MeanT = 6;
                //int IndexPrcp_MaxT = 9;
                int IndexPrcp_Var = 7;
                int IndexPrcp_STD = 8;

                //bool firstFlag = false;
                string currentYear = "";
                //starting timestep
                int currentTimeS = 0;
                int currentMonth = 1;
                int tempEco = 1;
                double AverageMax = 0;

                //double AverageMaxSTD = 0;
                double AverageMin = 0;
                //double AverageMinSTD = 0;
                double AveragePrecp = 0;

                double AverageSTDT = 0;
                double StdDevPpt = 0;
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

                foreach (string field in fields)
                {
                    if (field != "" && Convert.ToInt16(field) > numberOfAllEcorigions)
                    {
                        numberOfAllEcorigions = Convert.ToInt16(field);
                    }
                }
                //12 beacuse for each ecoriogn we need Max_MinT,Max_MaxT,Max_Var Max_Std, Min_MinT,Min_MaxT,Min_Var Min_Std, Prcp_MinT,Prcp_MaxT,Prcp_Var Prcp_Std
                int dicSize = numberOfAllEcorigions * 9;
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
                            //tempScenarioName = CurrentScenarioName;
                            if (field.ToLower().Contains(prcpTriggerWord))
                            {
                                //CurrentScenarioName = field.Substring(1, 4);
                                CurrentScenarioType = prcpTriggerWord;

                            }

                            else if (field.ToLower().Contains(maxTriggerWord) || field.Contains(minTriggerWord))
                            {
                                //CurrentScenarioName = field.Substring(1, 4);
                                if (field.ToLower().Contains(maxTriggerWord))
                                    CurrentScenarioType = maxTriggerWord;
                                if (field.ToLower().Contains(minTriggerWord))
                                    CurrentScenarioType = minTriggerWord;
                            }


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
                        if (CurrentScenarioType.Contains(prcpTriggerWord))
                        {
                            IndexPrcp_MeanT = 6;
                            //IndexPrcp_MaxT = 9;
                            IndexPrcp_Var = 7;
                            IndexPrcp_STD = 8;

                            //IndexSTD = 5;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcorigions);

                            // century_climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });
                            if (!century_climate_Dic.Keys.Contains(key))
                                century_climate_Dic.Add(key, new double[dicSize]);
                            //set index of max and maxSTD for each ecorigion
                            for (int i = 1; i <= numberOfAllEcorigions; i++)
                            {
                                //currentT = fields[i];
                                //if (indexofSTD < 26)
                                //{
                                //currentSTD = fields[indexofSTD];
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[i]), IndexPrcp_MeanT);
                                updatedIndex += i + numberOfAllEcorigions;
                                //century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexPrcp_MaxT);
                                //updatedIndex += numberOfAllEcorigions;
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexPrcp_Var);
                                updatedIndex += numberOfAllEcorigions;
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexPrcp_STD);


                                //century_climate_Dic[key].SetValue(Convert.ToDouble(currentSTD), IndexSTD);
                                IndexPrcp_MeanT = IndexPrcp_MeanT + 9;
                                //IndexPrcp_MaxT = IndexPrcp_MaxT + 12;
                                IndexPrcp_Var = IndexPrcp_Var + 9;
                                IndexPrcp_STD = IndexPrcp_STD + 9;
                                updatedIndex = 0;
                                //IndexSTD = IndexSTD + 6;
                                //indexofSTD++;
                            }

                        }

                        else if (CurrentScenarioType.Contains(maxTriggerWord))
                        {
                            IndexMax_MeanT = 0;
                            //IndexMax_MaxT = 1;
                            IndexMax_Var = 1;
                            IndexMax_STD = 2;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcorigions);
                            if (!century_climate_Dic.Keys.Contains(key))
                                century_climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });

                            //set index of max and maxSTD for each ecorigion
                            for (int i = 1; i <= numberOfAllEcorigions; i++)
                            {
                                //currentT = fields[i];
                                //if (indexofSTD < 26)
                                //{

                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[i]), IndexMax_MeanT);
                                updatedIndex += i + numberOfAllEcorigions;
                                //century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexMax_MaxT);
                                //updatedIndex +=  numberOfAllEcorigions;
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexMax_Var);
                                updatedIndex += numberOfAllEcorigions;
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexMax_STD);
                                IndexMax_MeanT = IndexMax_MeanT + 9;
                                //IndexMax_MaxT = IndexMax_MaxT + 12;
                                IndexMax_Var = IndexMax_Var + 9;
                                IndexMax_STD = IndexMax_STD + 9;
                                updatedIndex = 0;

                                //indexofSTD++;
                                //}

                            }
                        }
                        else if (CurrentScenarioType.Contains(minTriggerWord))
                        {
                            IndexMin_MeanT = 3;
                            //IndexMin_MaxT = 5;
                            IndexMin_Var = 4;
                            IndexMin_STD = 5;
                            //int indexofSTD = 0;
                            //indexofSTD = fields.Length - (numberOfAllEcorigions);

                            // century_climate_Dic.Add(key, new double[dicSize]);//{ currentT, currentSTD, 0, 0, 0, 0 });

                            //set index of max and maxSTD for each ecorigion
                            if (!century_climate_Dic.Keys.Contains(key))
                                century_climate_Dic.Add(key, new double[dicSize]);
                            for (int i = 1; i <= numberOfAllEcorigions; i++)
                            {
                                //currentT = fields[i];
                                //if (indexofSTD < 26)
                                //{
                                //currentSTD = fields[indexofSTD];
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[i]), IndexMin_MeanT);
                                updatedIndex += i + numberOfAllEcorigions;
                                //century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexMin_MaxT);
                                //updatedIndex += numberOfAllEcorigions;
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexMin_Var);
                                updatedIndex += numberOfAllEcorigions;
                                century_climate_Dic[key].SetValue(Convert.ToDouble(fields[updatedIndex]), IndexMin_STD);


                                //century_climate_Dic[key].SetValue(Convert.ToDouble(currentSTD), IndexSTD);
                                IndexMin_MeanT = IndexMin_MeanT + 9;
                                //IndexMin_MaxT = IndexMin_MaxT + 12;
                                IndexMin_Var = IndexMin_Var + 9;
                                IndexMin_STD = IndexMin_STD + 9;
                                updatedIndex = 0;
                                //    IndexSTD = IndexSTD + 6;
                                //    indexofSTD++;
                                //}

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
                        IndexMax_MeanT = 0;
                        //IndexMax_MaxT = 1;
                        IndexMax_Var = 1;
                        IndexMax_STD = 2;
                        IndexMin_MeanT = 3;
                        //IndexMin_MaxT = 5;
                        IndexMin_Var = 4;
                        IndexMin_STD = 5;
                        IndexPrcp_MeanT = 6;
                        //IndexPrcp_MaxT = 9;
                        IndexPrcp_Var = 7;
                        IndexPrcp_STD = 8;


                        //int AverageMaxT = 0;
                        //int AverageMaxSTD = 1;
                        //int AverageMinT = 1;
                        //int AverageMinSTD = 3;
                        //int AveragePrec = 2;
                        //int AveragePrecSTD = 5;
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(centuryPath, emptytxt))
                        {
                            file.WriteLine("LandisData" + " \"Climate Data\" \n");
                            file.WriteLine("ClimateTable \n");
                            //file.WriteLine(tempScenarioName + "\n");

                            file.WriteLine(">>Eco" + "\t" + "Time" + "\t" + "Month" + "\t" + "AvgMinT" + "\t" + "AvgMaxT" + "\t" + "StdDevT" + "\t" + "AvgPpt" + "\t" + "StdDev" + "\t" + "PAR" + "\t" + "VarT" + "\t" + "VarPpt" + "\n");
                            file.WriteLine(">>Name" + "\t" + "Step" + "\t" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(k)" + "\t" + "(cm)" + "\t" + "Ppt" + "\t" + "µmol m-2 s-1" + "\t" + "(cm)" + "\t" + "(cm)" + "\n");
                            //file.WriteLine(">>Eco" + "\t" + "Time" + "\t" + "Month" + "\t" + "AvgMinT" + "\t" + "AvgMaxT" + "\t" + "StdDevT" + "\t" + "AvgPpt" + "\t" + "StdDev" + "\t" + "PAR" + "\n");
                            //file.WriteLine(">>Name" + "\t" + "Step" + "\t" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(k)" + "\t" + "(cm)" + "\t" + "Ppt" + "\t" + "µmol m-2 s-1" + "\n");
                            //file.WriteLine(">>Eco" + "\t" + "Time" + "\t" + "\t" + "AvgMaxT" + "\t" + "StdMaxT" + "\t" + "AvgMinT" + "\t" + "StdMinT" + "\t" + "AvgPpt" + "\t" + "StdDev" + "\n");
                            // file.WriteLine(">>Name" + "\t" + "Step" + "\t" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(C)" + "\t" + "(C)" + "\n");
                            //initialize currentYear and month
                            currentYear = century_climate_Dic.First().Key.Substring(0, 4).ToString();
                            //starting timestep
                            currentTimeS = 0;
                            currentMonth = Convert.ToInt16(century_climate_Dic.First().Key.Substring(5, 2).ToString());
                            tempEco = 1;
                            int lastYear = century_climate_Dic.AsEnumerable().Select(ax => Convert.ToInt32(ax.Key.Substring(0, 4).ToString())).Distinct().ToList().Max();
                            int firstYear = century_climate_Dic.AsEnumerable().Select(ai => Convert.ToInt32(ai.Key.Substring(0, 4).ToString())).Distinct().ToList().Min();
                            if ((double)century_climate_Dic.Count / 12 > (double)lastYear - firstYear)
                                lastYear = lastYear - 1;
                            for (int j = firstYear; j <= lastYear; j++)
                            {
                                for (int i = 1; i <= numberOfAllEcorigions; i++)
                                {
                                    currentYear = j.ToString();
                                    foreach (KeyValuePair<string, double[]> row in century_climate_Dic)
                                    {

                                        if (currentYear == row.Key.Substring(0, 4).ToString())
                                        {
                                            if (currentMonth == Convert.ToInt16(row.Key.Substring(5, 2)))
                                            {
                                                AverageMin += Math.Round(row.Value[IndexMin_MeanT], 2);
                                                AverageMax += Math.Round(row.Value[IndexMax_MeanT], 2);
                                                AveragePrecp += Math.Round(row.Value[IndexPrcp_MeanT], 2);
                                                AverageSTDT += Math.Round((row.Value[IndexMax_Var] + row.Value[IndexMin_Var]) / 2, 2);
                                                StdDevPpt += Convert.ToDouble(row.Value[IndexPrcp_STD]);
                                                numberOfDays++;

                                            }
                                            else
                                            {
                                                file.WriteLine("eco" + i.ToString() + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(StdDevPpt, 2) + "\t" + "0.0" + "\t" + Math.Round(AverageSTDT / numberOfDays, 2) + "\t" + Math.Round(StdDevPpt / numberOfDays, 2) + "\n");
                                                //file.WriteLine("eco1" + "\t" + currentYear + "\t" + currentMonth + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(AverageMaxSTD / numberOfDays, 2) + "\t" + Math.Round(AverageMinT / numberOfDays, 2) + "\t" + Math.Round(AverageMinSTD / numberOfDays, 2) + "\t" + Math.Round(AveragePrec / numberOfDays, 2) + "\t" + Math.Round(AveragePrecSTD / numberOfDays, 2) + "\n");
                                                //tempMonth = currentMonth;
                                                currentMonth = Convert.ToInt16(row.Key.Substring(5, 2));
                                                //if (tempMonth != currentMonth)

                                                AverageMax = 0;
                                                //AverageMaxSTD = 0;
                                                AverageMin = 0;
                                                //AverageMinSTD = 0;
                                                AveragePrecp = 0;
                                                //AveragePrecSTD = 0;
                                                AverageSTDT = 0;
                                                StdDevPpt = 0;
                                                numberOfDays = 0;
                                                AverageMin += Math.Round(row.Value[IndexMin_MeanT], 2);
                                                AverageMax += Math.Round(row.Value[IndexMax_MeanT], 2);
                                                AveragePrecp += Math.Round(row.Value[IndexPrcp_MeanT], 2);
                                                AverageSTDT += Math.Round((row.Value[IndexMax_Var] + row.Value[IndexMin_Var]) / 2, 2);
                                                StdDevPpt += Convert.ToDouble(row.Value[IndexPrcp_STD]);
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

                                            if (currentMonth == 12)
                                                file.WriteLine("eco" + i.ToString() + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(StdDevPpt, 2) + "\t" + "0.0" + "\t" + Math.Round(AverageSTDT / numberOfDays, 2) + "\t" + Math.Round(StdDevPpt / numberOfDays, 2) + "\n");
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
                                            AveragePrecp = 0;
                                            //AveragePrecSTD = 0;

                                            AverageSTDT = 0;
                                            StdDevPpt = 0;

                                            numberOfDays = 0;

                                        }
                                    }
                                    tempEco = i;
                                    currentMonth = 1;
                                    AverageMax = 0;
                                    //AverageMaxSTD = 0;
                                    AverageMin = 0;
                                    //AverageMinSTD = 0;
                                    AveragePrecp = 0;
                                    //AveragePrecSTD = 0;

                                    AverageSTDT = 0;
                                    StdDevPpt = 0;
                                    IndexMax_MeanT = IndexMax_MeanT + 9;
                                    //IndexMax_MaxT = IndexMax_MaxT + 12;
                                    IndexMax_Var = IndexMax_Var + 9;
                                    IndexMax_STD = IndexMax_STD + 9;
                                    IndexMin_MeanT = IndexMin_MeanT + 9;
                                    //IndexMin_MaxT = IndexMin_MaxT + 12;
                                    IndexMin_Var = IndexMin_Var + 9;
                                    IndexMin_STD = IndexMin_STD + 9;
                                    IndexPrcp_MeanT = IndexPrcp_MeanT + 9;
                                    //IndexPrcp_MaxT = IndexPrcp_MaxT + 12;
                                    IndexPrcp_Var = IndexPrcp_Var + 9;
                                    IndexPrcp_STD = IndexPrcp_STD + 9;

                                }

                                tempEco = 1;
                                currentTimeS = currentTimeS + 1;
                                IndexMax_MeanT = 0;
                                //IndexMax_MaxT = 1;
                                IndexMax_Var = 1;
                                IndexMax_STD = 2;
                                IndexMin_MeanT = 3;
                                //IndexMin_MaxT = 5;
                                IndexMin_Var = 4;
                                IndexMin_STD = 5;
                                IndexPrcp_MeanT = 6;
                                //IndexPrcp_MaxT = 9;
                                IndexPrcp_Var = 7;
                                IndexPrcp_STD = 8;
                                currentMonth = 1;
                                AverageMax = 0;
                                //AverageMaxSTD = 0;
                                AverageMin = 0;
                                //AverageMinSTD = 0;
                                AveragePrecp = 0;
                                //AveragePrecSTD = 0;

                                AverageSTDT = 0;
                                StdDevPpt = 0;
                            }


                            //    for (int i = 1; i <= numberOfAllEcorigions; i++)
                            //    {

                            //        foreach (KeyValuePair<string, double[]> row in century_climate_Dic)
                            //        {

                            //            //file.WriteLine("eco" + i.ToString() + "\t" + row.Key.Remove(10) + "\t" + Math.Round(row.Value[AverageMaxT], 2) +  "\t" + Math.Round(row.Value[AverageMinT], 2) +  "\t" + Math.Round(row.Value[AveragePrec], 2)  + "\n");
                            //            //file.WriteLine("eco" + i.ToString() + "\t" + row.Key.Remove(10) + "\t" + Math.Round(row.Value[AverageMaxT], 2) + "\t" + Math.Round(row.Value[AverageMaxSTD], 2) + "\t" + Math.Round(row.Value[AverageMinT], 2) + "\t" + Math.Round(row.Value[AverageMinSTD], 2) + "\t" + Math.Round(row.Value[AveragePrec], 2) + "\t" + Math.Round(row.Value[AveragePrecSTD], 2) + "\n");

                            //                if (currentYear == row.Key.Substring(0, 4).ToString())
                            //                {

                            //                    if (currentMonth == Convert.ToInt16(row.Key.Substring(5, 2)))
                            //                    {

                            //                        //(row.Value[IndexMax_MaxT] + row.Value[IndexMax_MeanT])/2
                            //                        //AverageMin += (row.Value[IndexMin_MaxT] + row.Value[IndexMin_MeanT]) / 2;
                            //                        //AverageMax += (row.Value[IndexMax_MaxT] + row.Value[IndexMax_MeanT]) / 2;
                            //                        //AveragePrecp += (row.Value[IndexPrcp_MaxT] + row.Value[IndexPrcp_MeanT]) / 2;
                            //                        //AverageSTDT += (row.Value[IndexMax_Var] + row.Value[IndexMin_Var]) / 2;
                            //                        //AverageMaxSTD += Math.Round(Convert.ToDouble(row.Value[2]), 2);
                            //                        AverageMin += Math.Round(row.Value[IndexMin_MeanT], 2);
                            //                        AverageMax += Math.Round(row.Value[IndexMax_MeanT], 2);
                            //                        AveragePrecp += Math.Round(row.Value[IndexPrcp_MeanT], 2);
                            //                        AverageSTDT += Math.Round((row.Value[IndexMax_Var] + row.Value[IndexMin_Var]) / 2, 2);
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
                            //                        AverageMin += Math.Round(row.Value[IndexMin_MeanT], 2);
                            //                        AverageMax += Math.Round(row.Value[IndexMax_MeanT], 2);
                            //                        AveragePrecp += Math.Round(row.Value[IndexPrcp_MeanT], 2);
                            //                        AverageSTDT += Math.Round((row.Value[IndexMax_Var] + row.Value[IndexMin_Var]) / 2, 2);
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
                            //                    AverageMin += Math.Round(row.Value[IndexMin_MeanT], 2);
                            //                    AverageMax += Math.Round(row.Value[IndexMax_MeanT], 2);
                            //                    AveragePrecp += Math.Round(row.Value[IndexPrcp_MeanT], 2);
                            //                    AverageSTDT += Math.Round((row.Value[IndexMax_Var] + row.Value[IndexMin_Var]) / 2, 2);
                            //                    StdDevPpt += Convert.ToDouble(row.Value[IndexPrcp_STD]);
                            //                    //sums = 0;
                            //                    //stdTemp = 0;
                            //                    //prpSums = 0;
                            //                    //stdPrp = 0;
                            //                    numberOfDays++;
                            //                }


                            //        }

                            //        IndexMax_MeanT = IndexMax_MeanT + 9;
                            //        //IndexMax_MaxT = IndexMax_MaxT + 12;
                            //        IndexMax_Var = IndexMax_Var + 9;
                            //        IndexMax_STD = IndexMax_STD + 9;
                            //        IndexMin_MeanT = IndexMin_MeanT + 9;
                            //        //IndexMin_MaxT = IndexMin_MaxT + 12;
                            //        IndexMin_Var = IndexMin_Var + 9;
                            //        IndexMin_STD = IndexMin_STD + 9;
                            //        IndexPrcp_MeanT = IndexPrcp_MeanT + 9;
                            //        //IndexPrcp_MaxT = IndexPrcp_MaxT + 12;
                            //        IndexPrcp_Var = IndexPrcp_Var + 9;
                            //        IndexPrcp_STD = IndexPrcp_STD + 9;
                            //    }
                            //    file.WriteLine("eco" + numberOfAllEcorigions.ToString() + "\t" + currentTimeS + "\t" + currentMonth + "\t" + Math.Round(AverageMin / numberOfDays, 2) + "\t" + Math.Round(AverageMax / numberOfDays, 2) + "\t" + Math.Round(Math.Sqrt(AverageSTDT / numberOfDays), 2) + "\t" + Math.Round(AveragePrecp / numberOfDays, 2) + "\t" + Math.Round(StdDevPpt, 2) + "\t" + "0.0" + "\n");



                        }


                        //If file contains more than one scenario then these setting will be needed
                        //century_climate_Dic.Clear();
                        //emptytxt = true;
                        //tempScenarioName = CurrentScenarioName;

                    }
                }





            }

            #endregion

            return centuryPath;

        }
    }
}
