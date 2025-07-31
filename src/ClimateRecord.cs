namespace Landis.Library.Climate
{
    internal class ClimateRecord
    {
        public double MinTemp { get; set; } = double.NaN;               // [C] required
        public double MaxTemp { get; set; } = double.NaN;               // [C] required
        public double Temp { get; set; } = double.NaN;                  // [C]
        public double Precip { get; set; } = double.NaN;                // [cm] required

        public double WindDirection { get; set; } = double.NaN;         // Compass heading that the wind is blowing to. input data are the heading the wind is blowing from.
        public double WindSpeed { get; set; } = double.NaN;             // [km/hr]. Input data are in [m/s].

        public double NDeposition { get; set; } = double.NaN;           // [g/m2]
        public double CO2 { get; set; } = double.NaN;                   // [ppm]
        
        public double MinRH { get; set; } = double.NaN;                 // [%]
        public double MaxRH { get; set; } = double.NaN;                 // [%]
        public double RH { get; set; } = double.NaN;                    // [%]
        public double SpecificHumidity { get; set; } = double.NaN;      // [unitless], e.g. [kg/kg]
        public double DewPoint { get; set; } = double.NaN;              // [C]

        public double PET { get; set; } = double.NaN;                   // [cm]
        public double PAR { get; set; } = double.NaN;                   // [umol]
        public double Ozone { get; set; } = double.NaN;                 // [ppm]
        public double ShortWaveRadiation { get; set; } = double.NaN;    // [W/m2]

        // fire weather data calculated from WindSpeed, RH, and Precip from daily input data
        public double DuffMoistureCode { get; set; }
        public double DroughtCode { get; set; }
        public double BuildUpIndex { get; set; }
        public double FineFuelMoistureCode { get; set; }
        public double FireWeatherIndex { get; set; }

        // intermediate data
        public double WindEasting { get; set; } = double.NaN;           // if present, will be transformed into WindDirection & WindSpeed
        public double WindNorthing { get; set; } = double.NaN;          // if present, will be transformed into WindDirection & WindSpeed
    }
}
