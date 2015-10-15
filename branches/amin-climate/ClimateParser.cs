//  Copyright 2009-2010 Portland State University, Conservation Biology Institute
//  Authors:  Robert M. Scheller

using Edu.Wisc.Forest.Flel.Util;
using Landis.Core;
using System.Collections.Generic;
using System.Text;



namespace Landis.Library.Climate
{
    /// <summary>
    /// A parser that reads the tool parameters from text input.
    /// </summary>
    public class ClimateParser
        : TextParser<Dictionary<int, IClimateRecord[,]>>
    {

        //private IEcoregionDataset ecoregionDataset;
        //---------------------------------------------------------------------
        private string _LandisDataValue = "Climate Data";

        public override string LandisDataValue 
        { 
            get { 
                return _LandisDataValue; 
            } 
        }
        //---------------------------------------------------------------------

        public ClimateParser()
        {
            //this.ecoregionDataset = ecoregionDataset;
        }

        //---------------------------------------------------------------------

        protected override Dictionary<int, IClimateRecord[,]> Parse()
        {

            InputVar<string> landisData = new InputVar<string>("LandisData");
            ReadVar(landisData);
            if (landisData.Value.Actual != LandisDataValue)
                throw new InputValueException(landisData.Value.String, "The value is not \"{0}\"", LandisDataValue);
            
            Dictionary<int, IClimateRecord[,]> allData = new Dictionary<int, IClimateRecord[,]>();

            const string nextTableName = "ClimateTable";

            
            //---------------------------------------------------------------------
            //Read in climate data:

            ReadName(nextTableName);

            InputVar<string> ecoregionName = new InputVar<string>("Ecoregion");
            //InputVar<int> ecoregionIndex = new InputVar<int>("Ecoregion Index");
            InputVar<int>    year       = new InputVar<int>("Time step for updating the climate");
            InputVar<int>    month      = new InputVar<int>("The Month");
            InputVar<double> avgMinTemp = new InputVar<double>("Monthly Minimum Temperature Value");
            InputVar<double> avgMaxTemp = new InputVar<double>("Monthly Maximum Temperature Value");
            InputVar<double> stdDevTemp = new InputVar<double>("Monthly Std Deviation Temperature Value");
            InputVar<double> avgPpt     = new InputVar<double>("Monthly Precipitation Value");
            InputVar<double> stdDevPpt  = new InputVar<double>("Monthly Std Deviation Precipitation Value");
            InputVar<double> par = new InputVar<double>("Monthly Photosynthetically Active Radiation Value");
            InputVar<double> avgVarTemp = new InputVar<double>("Monthly Variance Temperature Value");
            InputVar<double> avgPptVarTemp = new InputVar<double>("Monthly Precipitation Variance Temperature Value");
            
            while (! AtEndOfInput)
            {
                StringReader currentLine = new StringReader(CurrentLine);

                ReadValue(ecoregionName, currentLine);
                //ReadValue(ecoregionIndex, currentLine);

                IEcoregion ecoregion = GetEcoregion(ecoregionName.Value);
                
                ReadValue(year, currentLine);
                int yr = year.Value.Actual;
                
                if(!allData.ContainsKey(yr))
                {
                    IClimateRecord[,] climateTable = new IClimateRecord[Climate.ModelCore.Ecoregions.Count, 12];
                    allData.Add(yr, climateTable);
                    //UI.WriteLine("  Climate Parser:  Add new year = {0}.", yr);
                }

                ReadValue(month, currentLine);
                int mo = month.Value.Actual;

                IClimateRecord climateRecord = new ClimateRecord();
                
                ReadValue(avgMinTemp, currentLine);
                climateRecord.AvgMinTemp = avgMinTemp.Value;
                
                ReadValue(avgMaxTemp, currentLine);
                climateRecord.AvgMaxTemp = avgMaxTemp.Value;

                ReadValue(stdDevTemp, currentLine);
                climateRecord.StdDevTemp = stdDevTemp.Value;
                
                ReadValue(avgPpt, currentLine);
                climateRecord.AvgPpt = avgPpt.Value;
                
                ReadValue(stdDevPpt, currentLine);
                climateRecord.StdDevPpt = stdDevPpt.Value;
                
                ReadValue(par, currentLine);
                climateRecord.PAR = par.Value;

                //if (currentLine.ToString().ToLower().Contains("avgvartemp"))
                //{
                //if(!Climate.ConfigParameters.ClimateFileFormat.ToLower().Contains("standard"))
                //{
                try
                {
                    ReadValue(avgVarTemp, currentLine);
                    climateRecord.AvgVarTemp = avgVarTemp.Value;

                    ReadValue(avgPptVarTemp, currentLine);
                    climateRecord.AvgPptVarTemp = avgPptVarTemp.Value;

                    allData[yr][ecoregion.Index, mo - 1] = climateRecord;

                    CheckNoDataAfter("the " + avgPptVarTemp.Name + " column",
                                     currentLine);
                }
                catch (InputVariableException ex)
                {


                    if (ex is InputVariableException) // This we know how to handle.
                    {
                        allData[yr][ecoregion.Index, mo - 1] = climateRecord;

                        CheckNoDataAfter("the " + par.Name + " column",
                            currentLine);
                    }
                }

                //if(!Climate.ConfigParameters.ClimateFileFormat.ToLower().Contains("standard"))
                ////{
                //    ReadValue(avgVarTemp, currentLine);
                //    climateRecord.AvgVarTemp = avgVarTemp.Value;

                //    ReadValue(avgPptVarTemp, currentLine);
                //    climateRecord.AvgPptVarTemp = avgPptVarTemp.Value;

                //    allData[yr][ecoregion.Index, mo - 1] = climateRecord;
                    
                //    CheckNoDataAfter("the " + avgPptVarTemp.Name + " column",
                //                     currentLine);
                //}

                //else
                //{
                //    allData[yr][ecoregion.Index, mo - 1] = climateRecord;

                //    CheckNoDataAfter("the " + par.Name + " column",
                //                     currentLine);
                //}

                GetNextLine();
                
            }

            return allData;
        }

        //---------------------------------------------------------------------

        private IEcoregion GetEcoregion(InputValue<string>      ecoregionName)
        {
            IEcoregion ecoregion = Climate.ModelCore.Ecoregions[ecoregionName.Actual];
            if (ecoregion == null)
                throw new InputValueException(ecoregionName.String,
                                              "{0} is not an ecoregion name.",
                                              ecoregionName.String);
            
            return ecoregion;
        }

    }
}
