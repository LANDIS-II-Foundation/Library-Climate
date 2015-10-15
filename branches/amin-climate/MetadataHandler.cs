using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Landis.Library.Metadata;
using Landis.Core;
using Edu.Wisc.Forest.Flel.Util;

namespace Landis.Library.Climate
{
    public class MetadataHandler
    {
        
        public static ExtensionMetadata Extension {get; set;}

        public static void InitializeMetadata(int timestep, ICore mCore)
        {
            ScenarioReplicationMetadata scenRep = new ScenarioReplicationMetadata() {
                RasterOutCellArea = Climate.ModelCore.CellArea,
                TimeMin = Climate.ModelCore.StartTime,
                TimeMax = Climate.ModelCore.EndTime,
            };

            Extension = new ExtensionMetadata(mCore){
                Name = "Climate-Library",
                TimeInterval = timestep, 
                ScenarioReplicationMetadata = scenRep
            };

            //---------------------------------------
            //          table outputs:   
            //---------------------------------------

            Climate.PdsiLog = new MetadataTable<PDSI_Log>("Climate-PDSI-log.csv");
            Climate.MonthlyLog = new MetadataTable<MonthlyLog>("Climate-monthly-log.csv");

            OutputMetadata tblOut_monthly = new OutputMetadata()
            {
                Type = OutputType.Table,
                Name = "MonthlyLog",
                FilePath = Climate.MonthlyLog.FilePath,
                Visualize = true,
            };
            tblOut_monthly.RetriveFields(typeof(MonthlyLog));
            Extension.OutputMetadatas.Add(tblOut_monthly);

            OutputMetadata tblOut_pdsi = new OutputMetadata()
            {
                Type = OutputType.Table,
                Name = "PDSILog",
                FilePath = Climate.PdsiLog.FilePath,
                Visualize = false,
            };
            tblOut_pdsi.RetriveFields(typeof(PDSI_Log));
            Extension.OutputMetadatas.Add(tblOut_pdsi);

            //---------------------------------------            
            //          map outputs:         
            //---------------------------------------

            // NONE

            //---------------------------------------
            MetadataProvider mp = new MetadataProvider(Extension);
            mp.WriteMetadataToXMLFile("Metadata", Extension.Name, Extension.Name);




        }
    }
}
