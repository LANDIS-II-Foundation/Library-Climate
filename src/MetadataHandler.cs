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


            if (ConfigParameters.GenerateClimateOutputFiles)
            {
                _spinupMonthlyInputLog = new MetadataTable<MonthlyInputLog>("Climate-spinup-monthly-input-log.csv");
                var spinupMonthlyInputTable = new OutputMetadata()
                {
                    Type = OutputType.Table,
                    Name = "Spinup-Input-Log",
                    FilePath = _spinupMonthlyInputLog.FilePath,
                    Visualize = false,
                };
                spinupMonthlyInputTable.RetriveFields(typeof(MonthlyInputLog));
                Extension.OutputMetadatas.Add(spinupMonthlyInputTable);

                _spinupAnnualLog = new MetadataTable<AnnualLog>("Climate-spinup-annual-input-log.csv");
                var spinupAnnualTable = new OutputMetadata()
                {
                    Type = OutputType.Table,
                    Name = "Spinup-Annual-Log",
                    FilePath = _spinupAnnualLog.FilePath,
                    Visualize = false,
                };
                spinupAnnualTable.RetriveFields(typeof(AnnualLog));
                Extension.OutputMetadatas.Add(spinupAnnualTable);


                _futureMonthlyInputLog = new MetadataTable<MonthlyInputLog>("Climate-future-monthly-input-log.csv");
                var futureMonthlyInputTable = new OutputMetadata()
                {
                    Type = OutputType.Table,
                    Name = "Future-Input-Log",
                    FilePath = _futureMonthlyInputLog.FilePath,
                    Visualize = false,
                };
                futureMonthlyInputTable.RetriveFields(typeof(MonthlyInputLog));
                Extension.OutputMetadatas.Add(futureMonthlyInputTable);

                if (FutureTimeStep == TimeSeriesTimeStep.Daily)
                {
                    _futureDailyInputLog = new MetadataTable<DailyInputLog>("Climate-future-daily-input-log.csv");
                    var futureDailyInputTable = new OutputMetadata()
                    {
                        Type = OutputType.Table,
                        Name = "Future-Daily-Input-Log",
                        FilePath = _futureDailyInputLog.FilePath,
                        Visualize = false,
                    };
                    futureDailyInputTable.RetriveFields(typeof(DailyInputLog));
                    Extension.OutputMetadatas.Add(futureDailyInputTable);
                }

                _futureAnnualLog = new MetadataTable<AnnualLog>("Climate-future-annual-input-log.csv");
                var futureAnnualTable = new OutputMetadata()
                {
                    Type = OutputType.Table,
                    Name = "Future-Annual-Log",
                    FilePath = _futureAnnualLog.FilePath,
                    Visualize = false,
                };
                futureAnnualTable.RetriveFields(typeof(AnnualLog));
                Extension.OutputMetadatas.Add(futureAnnualTable);
            }

            // todo: is this needed?
            var mp = new MetadataProvider(Extension);
            mp.WriteMetadataToXMLFile("Metadata", Extension.Name, Extension.Name);
        }

        #endregion
    }
}
