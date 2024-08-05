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

            var annualTable = new OutputMetadata()
            {
                Type = OutputType.Table,
                Name = "Future-Annual-Log",
                FilePath = _futureAnnualLog.FilePath,
                Visualize = false,
            };
            annualTable.RetriveFields(typeof(AnnualLog));
            Extension.OutputMetadatas.Add(annualTable);

            // todo: is this needed?
            var mp = new MetadataProvider(Extension);
            mp.WriteMetadataToXMLFile("Metadata", Extension.Name, Extension.Name);
        }

        #endregion
    }
}
