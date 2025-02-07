using Landis.Library.Metadata;

namespace Landis.Library.Climate
{
    public class DailyInputLog
    {
        [DataFieldAttribute(Desc = "Simulation Year")]
        public int Year { set; get; }

        [DataFieldAttribute(Unit = FieldUnits.Year, Desc = "Calendar Year")]
        public int CalendarYear { set; get; }

        [DataFieldAttribute(Desc = "Day")]
        public int Day { set; get; }

        [DataFieldAttribute(Desc = "Ecoregion Name")]
        public string EcoregionName { set; get; }

        [DataFieldAttribute(Desc = "Minimum Air Temperature [C])", Format = "0.00")]
        public double MinTemp { get; set; }

        [DataFieldAttribute(Desc = "Maximum Air Temperature [C])", Format = "0.00")]
        public double MaxTemp { get; set; }

        [DataFieldAttribute(Desc = "Air Temperature [C])", Format = "0.00")]
        public double Temp { get; set; }

        [DataFieldAttribute(Desc = "Precipitation [cm]", Format = "0.00")]
        public double Precip { get; set; }

        [DataFieldAttribute(Desc = "Wind Direction (Compass heading that the wind is blowing to)", Format = "0.00")]
        public double WindDirection { get; set; }

        [DataFieldAttribute(Desc = "Wind Speed [km/hr]", Format = "0.00")]
        public double WindSpeed { get; set; }

        [DataFieldAttribute(Desc = "Nitrogen Deposition [g/m2]", Format = "0.00")]
        public double NDeposition { get; set; }

        [DataFieldAttribute(Desc = "CO2 concentration [ppm]", Format = "0.00")]
        public double CO2 { get; set; }

        [DataFieldAttribute(Desc = "Minimum Relative Humidity [%]", Format = "0.00")]
        public double MinRH { get; set; }

        [DataFieldAttribute(Desc = "Maximum Relative Humidity [%]", Format = "0.00")]
        public double MaxRH { get; set; }

        [DataFieldAttribute(Desc = "Relative Humidity [%]", Format = "0.00")]
        public double RH { get; set; }

        [DataFieldAttribute(Desc = "Specific Humidity [unitless]", Format = "0.000000")]
        public double SpecificHumidity { get; set; }

        [DataFieldAttribute(Desc = "Potential Evapotranspiration [cm]", Format = "0.000000")]
        public double PET { get; set; }

        [DataFieldAttribute(Desc = "PAR [umol]", Format = "0.00")]
        public double PAR { get; set; }

        [DataFieldAttribute(Desc = "Ozone [ppm]", Format = "0.00")]
        public double Ozone { get; set; }

        [DataFieldAttribute(Desc = "Shortwave Radiation [W/m2]", Format = "0.00")]
        public double ShortWaveRadiation { get; set; }

        [DataFieldAttribute(Desc = "DuffMoistureCode [unitless]", Format = "0.00")]
        public double DuffMoistureCode { get; set; }

        [DataFieldAttribute(Desc = "DroughtCode [unitless]", Format = "0.00")]
        public double DroughtCode { get; set; }

        [DataFieldAttribute(Desc = "BuildUpIndex [unitless]", Format = "0.00")]
        public double BuildUpIndex { get; set; }

        [DataFieldAttribute(Desc = "FineFuelMoistureCode [unitless]", Format = "0.00")]
        public double FineFuelMoistureCode { get; set; }

        [DataFieldAttribute(Desc = "Fire Weather Index [unitless]", Format = "0.00")]
        public double FireWeatherindex { get; set; }
    }
}
