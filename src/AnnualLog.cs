using Landis.Library.Metadata;

namespace Landis.Library.Climate
{
    public class AnnualLog
    {
        [DataFieldAttribute(Desc = "Simulation Year")]
        public int Year { set; get; }

        [DataFieldAttribute(Unit = FieldUnits.Year, Desc = "Calendar Year")]
        public int CalendarYear { set; get; }

        [DataFieldAttribute(Desc = "Ecoregion Name")]
        public string EcoregionName { set; get; }

        [DataFieldAttribute(Desc = "Total Annual Precipitation [cm]", Format = "0.00")]
        public double TAP { get; set; }

        [DataFieldAttribute(Desc = "Mean Annual Temperature [C]", Format = "0.00")]
        public double MAT { get; set; }

        [DataFieldAttribute(Desc = "Begin Growing Season Julian Day")]
        public int BeginGrow { get; set; }

        [DataFieldAttribute(Desc = "End Growing Season Julian Day")]
        public int EndGrow { get; set; }
    }
}
