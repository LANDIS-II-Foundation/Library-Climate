//  Copyright 2009 Conservation Biology Institute
//  Authors:  Robert M. Scheller
//  License:  Available at  
//  http://www.landis-ii.org/developers/LANDIS-IISourceCodeLicenseAgreement.pdf

using Edu.Wisc.Forest.Flel.Util;
using Landis.Util;
//using Landis.Ecoregions;

using System.Collections.Generic;
using System.Text;



namespace Landis.Library.Climate
{
    /// <summary>
    /// A parser that reads the tool parameters from text input.
    /// </summary>
    public class ClimateParser
        : Landis.TextParser<Dictionary<int, IClimateRecord[,]>>
    {
        
        //private Ecoregions.IDataset ecoregionDataset;
        private List<int> ecoregionDataset;
        
        //---------------------------------------------------------------------
        public override string LandisDataValue
        {
            get {
                return "Climate Data";
            }
        }

        //---------------------------------------------------------------------
        //public ClimateParser(Ecoregions.IDataset ecoregionDataset)
        public ClimateParser(List<int> ecoregionDataset)
        {
            this.ecoregionDataset = ecoregionDataset;
        }

        //---------------------------------------------------------------------

        protected override Dictionary<int, IClimateRecord[,]> Parse()
        {

            ReadLandisDataVar();
            
            Dictionary<int, IClimateRecord[,]> allData = new Dictionary<int, IClimateRecord[,]>();

            const string nextTableName = "ClimateTable";

            
            //---------------------------------------------------------------------
            //Read in climate data:

            ReadName(nextTableName);

            //InputVar<string> ecoregionName = new InputVar<string>("Ecoregion");
            InputVar<int> ecoregionIndex = new InputVar<int>("Ecoregion Index");
            InputVar<int>    year       = new InputVar<int>("Time step for updating the climate");
            InputVar<int>    month      = new InputVar<int>("The Month");
            InputVar<double> avgMinTemp = new InputVar<double>("Monthly Minimum Temperature Value");
            InputVar<double> avgMaxTemp = new InputVar<double>("Monthly Maximum Temperature Value");
            InputVar<double> stdDevTemp = new InputVar<double>("Monthly Std Deviation Temperature Value");
            InputVar<double> avgPpt     = new InputVar<double>("Monthly Precipitation Value");
            InputVar<double> stdDevPpt  = new InputVar<double>("Monthly Std Deviation Precipitation Value");
            InputVar<double> par        = new InputVar<double>("Monthly Photosynthetically Active Radiation Value");
            
            while (! AtEndOfInput)
            {
                StringReader currentLine = new StringReader(CurrentLine);

                //ReadValue(ecoregionName, currentLine);
                ReadValue(ecoregionIndex, currentLine);

                //IEcoregion ecoregion = GetEcoregion(ecoregionName.Value);
                
                ReadValue(year, currentLine);
                int yr = year.Value.Actual;
                
                if(!allData.ContainsKey(yr))
                {
                    IClimateRecord[,] climateTable = new IClimateRecord[ecoregionDataset.Count, 12];
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
                
                allData[yr][ecoregionIndex.Value, mo-1] = climateRecord;
                
                //UI.WriteLine(" climateTable avgPpt={0:0.0}.", climateTable[ecoregion.Index, mo-1].AvgPpt);
                //UI.WriteLine(" allData yr={0}, mo={1}, avgPpt={2:0.0}.", yr, mo, allData[yr][ecoregion.Index, mo-1].AvgPpt);
                
                CheckNoDataAfter("the " + par.Name + " column",
                                 currentLine);

                GetNextLine();
                
            }

            return allData;
        }

        //---------------------------------------------------------------------
/*
        private IEcoregion GetEcoregion(InputValue<string>      ecoregionName)
        {
            IEcoregion ecoregion = ecoregionDataset[ecoregionName.Actual];
            if (ecoregion == null)
                throw new InputValueException(ecoregionName.String,
                                              "{0} is not an ecoregion name.",
                                              ecoregionName.String);
            
            return ecoregion;
        }*/

    }
}
