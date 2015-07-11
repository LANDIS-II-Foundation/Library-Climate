using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Landis.Library.Metadata;

namespace Landis.Library.Climate
{
    public class MonthlyLog
    {
        [DataFieldAttribute(Desc = "Simulation Period")]
        public string SimulationPeriod { set; get; }
        
        [DataFieldAttribute(Unit = FieldUnits.Year, Desc = "Simulation Year")]
        public int Time {set; get;}

        [DataFieldAttribute(Unit = FieldUnits.Month, Desc = "Simulation Month")]
        public int Month { set; get; }

        [DataFieldAttribute(Desc = "Ecoregion Name")]
        public string EcoregionName { set; get; }

        [DataFieldAttribute(Desc = "Ecoregion Index")]
        public int EcoregionIndex { set; get; }

        [DataFieldAttribute(Unit = FieldUnits.cm, Desc = "Monthly Precipitation", Format = "0.00")]
        public double ppt {get; set;}

        [DataFieldAttribute(Unit = FieldUnits.DegreeC, Desc = "Average Minimum Air Temperature", Format = "0.00")]
        public double min_airtemp { get; set; }

        [DataFieldAttribute(Unit = FieldUnits.DegreeC, Desc = "Average Maximum Air Temperature", Format = "0.00")]
        public double max_airtemp { get; set; }

        [DataFieldAttribute(Unit = FieldUnits.cm, Desc = "Standard Deviation Precipitation", Format = "0.00")]
        public double std_ppt { get; set; }

        [DataFieldAttribute(Unit = FieldUnits.DegreeC, Desc = "Standard Deviation Temperature", Format = "0.00")]
        public double std_temp { get; set; }
    }
}
