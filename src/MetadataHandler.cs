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

            //Climate.PdsiLog = new MetadataTable<PDSI_Log>("Climate-PDSI-log.csv");
            Climate.SpinupInputLog = new MetadataTable<InputLog>("Climate-spinup-input-log.csv");
            Climate.FutureInputLog = new MetadataTable<InputLog>("Climate-future-input-log.csv");
            Climate.AnnualLog = new MetadataTable<AnnualLog>("Climate-annual-log.csv");

            OutputMetadata tblOut_spinupInput = new OutputMetadata()
            {
                Type = OutputType.Table,
                Name = "Spinup-Input-Log",
                FilePath = Climate.SpinupInputLog.FilePath,
                Visualize = false,
            };
            tblOut_spinupInput.RetriveFields(typeof(InputLog));
            Extension.OutputMetadatas.Add(tblOut_spinupInput);

            OutputMetadata tblOut_futureInput = new OutputMetadata()
            {
                Type = OutputType.Table,
                Name = "Future-Input-Log",
                FilePath = Climate.FutureInputLog.FilePath,
                Visualize = false,
            };
            tblOut_futureInput.RetriveFields(typeof(InputLog));
            Extension.OutputMetadatas.Add(tblOut_futureInput);

            OutputMetadata tblOut_annual = new OutputMetadata()
            {
                Type = OutputType.Table,
                Name = "Annual-Log",
                FilePath = Climate.AnnualLog.FilePath,
                Visualize = false,
            };
            tblOut_annual.RetriveFields(typeof(AnnualLog));
            Extension.OutputMetadatas.Add(tblOut_annual);

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
