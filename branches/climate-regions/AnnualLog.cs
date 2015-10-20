using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Landis.Library.Metadata;

namespace Landis.Library.Climate
{
    public class AnnualLog
    {
        //[DataFieldAttribute(Desc = "Simulation Period")]
        //public string SimulationPeriod { set; get; }
        
        [DataFieldAttribute(Unit = FieldUnits.Year, Desc = "Simulation Year")]
        public int Time {set; get;}

        //[DataFieldAttribute(Unit = FieldUnits.Month, Desc = "Simulation Month")]
        //public int Month { set; get; }

        [DataFieldAttribute(Desc = "Ecoregion Name")]
        public string EcoregionName { set; get; }

        [DataFieldAttribute(Desc = "Ecoregion Index")]
        public int EcoregionIndex { set; get; }

        [DataFieldAttribute(Unit = FieldUnits.cm, Desc = "Total Annual Precipitation", Format = "0.00")]
        public double TAP {get; set;}

        [DataFieldAttribute(Unit = FieldUnits.DegreeC, Desc = "Mean Annual Temperature", Format = "0.00")]
        public double MAT { get; set; }

        [DataFieldAttribute(Desc = "Begin Growing Season Julian Day")]
        public int BeginGrow { get; set; }

        [DataFieldAttribute(Desc = "End Growing Season Julian Day")]
        public int EndGrow { get; set; }
        
        [DataFieldAttribute(Desc = "Palmer Drought Severity Index")]
        public double PDSI { set; get; }

    }
}
