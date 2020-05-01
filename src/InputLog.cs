//  Authors:  Amin Almassian, Robert M. Scheller, John McNabb, Melissa Lucash

using Landis.Library.Metadata;

namespace Landis.Library.Climate
{
    public class InputLog
    {
        
        [DataFieldAttribute(Unit = FieldUnits.Year, Desc = "Input Year")]
        public int Year {set; get;}

        [DataFieldAttribute(Desc = "Input Timestep")]
        public int Timestep { set; get; }

        [DataFieldAttribute(Desc = "Ecoregion Name")]
        public string EcoregionName { set; get; }

        [DataFieldAttribute(Desc = "Ecoregion Index")]
        public int EcoregionIndex { set; get; }

        [DataFieldAttribute(Desc = "Precipitation (units variable)", Format = "0.00")]
        public double ppt {get; set;}

        [DataFieldAttribute(Desc = "Average Minimum Air Temperature (units variable)", Format = "0.00")]
        public double min_airtemp { get; set; }

        [DataFieldAttribute(Desc = "Average Maximum Air Temperature (units variable)", Format = "0.00")]
        public double max_airtemp { get; set; }

        [DataFieldAttribute(Desc = "Standard Deviation Precipitation (units variable)", Format = "0.00")]
        public double std_ppt { get; set; }

        [DataFieldAttribute(Desc = "Standard Deviation Temperature (units variable)", Format = "0.00")]
        public double std_temp { get; set; }
        
        [DataFieldAttribute(Desc = "Average Wind Direction (units variable)", Format = "0.00")]
        public double winddirection { get; set; }
       
        [DataFieldAttribute(Desc = "Average Wind Speed (units variable)", Format = "0.00")]
        public double windspeed { get; set; }
       
        [DataFieldAttribute(Desc = "Average Nitrogen Deposition (units variable)", Format = "0.00")]
        public double ndeposition { get; set; }

        [DataFieldAttribute(Desc = "Average CO2 concentration (units variable)", Format = "0.00")]
        public double co2 { get; set; }

        [DataFieldAttribute(Desc = "Average Relative Humidity (units variable)", Format = "0.00")]
        public double relativehumidity { get; set; }

        [DataFieldAttribute(Desc = "Average Minimum Relative Humidity (units variable)", Format = "0.00")]
        public double min_relativehumidity { get; set; }

        [DataFieldAttribute(Desc = "Average Maximum Air Humidity (units variable)", Format = "0.00")]
        public double max_relativehumidity { get; set; }

        [DataFieldAttribute(Desc = "Average Specific Humidity (units variable)", Format = "0.000000")]
        public double specifichumidty { get; set; }        

        [DataFieldAttribute(Desc = "Average PAR (units variable)", Format = "0.00")]
        public double par { get; set; }

        [DataFieldAttribute(Desc = "Average Ozone (units variable)", Format = "0.00")]
        public double ozone { get; set; }

        [DataFieldAttribute(Desc = "Average Shortwave Radiation (units variable)", Format = "0.00")]
        public double shortwave { get; set; }

        [DataFieldAttribute(Desc = "Average Temperature (units variable)", Format = "0.00")]
        public double temperature { get; set; }

        [DataFieldAttribute(Desc = "Average Fire Weather Index (units variable)", Format = "0.00")]
        public double FWI { get; set; }
    }
}
