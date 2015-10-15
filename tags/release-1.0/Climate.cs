//  Copyright 2009 Conservation Biology Institute
//  Authors:  Robert M. Scheller
//  License:  Available at  
//  http://www.landis-ii.org/developers/LANDIS-IISourceCodeLicenseAgreement.pdf

using System.Collections.Generic;
using System.IO;
using System;
//using Landis.Ecoregions;

namespace Landis.Library.Climate
{

    public class Climate
    {
        private static Dictionary<int, IClimateRecord[,]> allData;
        private static IClimateRecord[,] timestepData;
        
        public Climate()
        {
        }
        
        public static Dictionary<int, IClimateRecord[,]> AllData
        {
            get {
                return allData;
            }
        }
        //---------------------------------------------------------------------
        public static IClimateRecord[,] TimestepData
        {
            get {
                return timestepData;
            }
            set {
                timestepData = value;
            }
        }
        
        public static void Write(List<int> ecoregionDataset) //Ecoregions.IDataset 
        {
            //foreach(IEcoregion ecoregion in ecoregionDataset)
            foreach(int ecoregionIndex in ecoregionDataset)
            {
                for(int i = 0; i < 12; i++)
                {
                    UI.WriteLine("Eco={0}, Month={1}, AvgMinTemp={2:0.0}, AvgMaxTemp={3:0.0}, StdDevTemp={4:0.0}, AvgPpt={5:0.0}, StdDevPpt={6:0.0}.",
                        ecoregionIndex, i+1, 
                        TimestepData[ecoregionIndex,i].AvgMinTemp,
                        TimestepData[ecoregionIndex,i].AvgMaxTemp,
                        TimestepData[ecoregionIndex,i].StdDevTemp,
                        TimestepData[ecoregionIndex,i].AvgPpt,
                        TimestepData[ecoregionIndex,i].StdDevPpt
                        );
                }
            }
            
        }
        //---------------------------------------------------------------------
        public static void Initialize(string filename, List<int> ecoregionDataset, bool writeOutput) //Ecoregions.IDataset ecoregionDataset
        {
            UI.WriteLine("Loading weather data from file \"{0}\" ...", filename);
            ClimateParser parser = new ClimateParser(ecoregionDataset);
            allData = Data.Load<Dictionary<int, IClimateRecord[,]>>(filename, parser);
            
            timestepData = allData[0];
            
            if(writeOutput)
                Write(ecoregionDataset);

        }
    }

}
