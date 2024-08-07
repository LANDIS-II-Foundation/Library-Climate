using Landis.Library.Metadata;

namespace Landis.Library.Climate
{
    public static partial class Climate
    {
        #region fields

        private static ExtensionMetadata Extension { get; set; }

        #endregion

        #region private methods

        private static void InitializeMetadata()
        {
            var scenRep = new ScenarioReplicationMetadata()
            {
                RasterOutCellArea = _modelCore.CellArea,
                TimeMin = _modelCore.StartTime,
                TimeMax = _modelCore.EndTime,
            };

            Extension = new ExtensionMetadata(_modelCore)
            {
                Name = "Climate-Library",
                TimeInterval = 1,
                ScenarioReplicationMetadata = scenRep
            };


            _spinupInputLog = new MetadataTable<InputLog>("Climate-spinup-input-log.csv");
            _spinupAnnualLog = new MetadataTable<AnnualLog>("Climate-spinup-annual-log.csv");

            var spinupInputTable = new OutputMetadata()
            {
                Type = OutputType.Table,
                Name = "Spinup-Input-Log",
                FilePath = _spinupInputLog.FilePath,
                Visualize = false,
            };
            spinupInputTable.RetriveFields(typeof(InputLog));
            Extension.OutputMetadatas.Add(spinupInputTable);

            var spinupAnnualTable = new OutputMetadata()
            {
                Type = OutputType.Table,
                Name = "Spinup-Annual-Log",
                FilePath = _spinupAnnualLog.FilePath,
                Visualize = false,
            };
            spinupAnnualTable.RetriveFields(typeof(AnnualLog));
            Extension.OutputMetadatas.Add(spinupAnnualTable);


            _futureInputLog = new MetadataTable<InputLog>("Climate-future-input-log.csv");
            _futureAnnualLog = new MetadataTable<AnnualLog>("Climate-future-annual-log.csv");

            var futureInputTable = new OutputMetadata()
            {
                Type = OutputType.Table,
                Name = "Future-Input-Log",
                FilePath = _futureInputLog.FilePath,
                Visualize = false,
            };
            futureInputTable.RetriveFields(typeof(InputLog));
            Extension.OutputMetadatas.Add(futureInputTable);

            var futureAnnualTable = new OutputMetadata()
            {
                Type = OutputType.Table,
                Name = "Future-Annual-Log",
                FilePath = _futureAnnualLog.FilePath,
                Visualize = false,
            };
            futureAnnualTable.RetriveFields(typeof(AnnualLog));
            Extension.OutputMetadatas.Add(futureAnnualTable);

            // todo: is this needed?
            var mp = new MetadataProvider(Extension);
            mp.WriteMetadataToXMLFile("Metadata", Extension.Name, Extension.Name);
        }

        #endregion
    }
}
