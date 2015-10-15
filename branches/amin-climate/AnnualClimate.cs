//  Copyright: Portland State University 2009-2014
//  Authors:  Robert M. Scheller, Amin Almassian

using System.Collections.Generic;
using System.IO;
using System;
using Landis.Core;

namespace Landis.Library.Climate
{

    public abstract class AnnualClimate
    {
        protected Climate.Phase climatePhase;
        protected int beginGrowing;
        protected int endGrowing;
        protected int growingDegreeDays;

        public int BeginGrowing {get {return this.beginGrowing;}}
        public int EndGrowing { get { return this.endGrowing; } }
        public int GrowingDegreeDays { get { return this.growingDegreeDays; } }

        public double AnnualPrecip;
        public double JJAtemperature;
        //public double AnnualN;
        public double AnnualAET;  // Actual Evapotranspiration
        public double Snow;
        public int Year;
        public static double stdDevTempGenerator;
        public static double stdDevPptGenerator;
        //public static IClimateRecord[,] avgEcoClimate_future;
        //public static IClimateRecord[,] avgEcoClimate_spinUp;
        public double Latitude { get; set; }

        //by Amin
        public IEcoregion Ecoregion { get; set; }
        public int TimeStep { get; set; }
        public double PDSI { get; set; }

        //---------------------------------------------------------------------

        public static void AnnualClimateInitialize()
        {
            stdDevTempGenerator = (Climate.ModelCore.GenerateUniform() * 2.0 - 1.0);
            stdDevPptGenerator = (Climate.ModelCore.GenerateUniform() * 2.0 - 1.0);
        }

        public virtual string Write() 
        {
            throw new NotImplementedException("implemented in sub classes.");
        }

        //---------------------------------------------------------------------------
        public static int DaysInMonth(int month, int currentYear)
        //This will return the number of days in a month given the month number where
        //January is 1.
        {
            switch (month + 1)
            {
                //Thirty days hath September, April, June && November
                case 9:
                case 4:
                case 6:
                case 11: return 30;
                //...all the rest have 31...
                case 1:
                case 3:
                case 5:
                case 7:
                case 8:
                case 10:
                case 12: return 31;
                //...save February, etc.
                case 2: if (currentYear % 4 == 0)
                        return 29;
                    else
                        return 28;
            }
            return 0;
        }

        protected static double CalculateDayNightLength(int month, double latitude)
        {
            double DOY = 0.0;
            if (Climate.AllData_granularity == TemporalGranularity.Daily)
                DOY = month;
            else if(Climate.AllData_granularity == TemporalGranularity.Monthly)
                DOY = (double)DayOfYear(month);
            double LatRad = latitude * (2.0 * Math.PI) / 360;
            double r = 1.0 - 0.0167 * Math.Cos(0.0172 * (DOY - 3));       //radius vector of the sun
            double z = 0.39785 * Math.Sin(4.868961 + 0.017203 * DOY + 0.033446 * Math.Sin(6.224111 + 0.017202 * DOY));

            double decl = 0.0;
            if (Math.Abs(z) < 0.7)
            {
                decl = Math.Atan(z / (Math.Sqrt(1.0 - Math.Pow(z, 2.0))));
            }
            else
            {
                decl = Math.PI / 2.0 - Math.Atan(Math.Sqrt(1.0 - Math.Pow(z, 2.0)) / z);
            }
            if (Math.Abs(LatRad) >= Math.PI / 2.0)
                LatRad = Math.Sign(latitude) * (Math.PI / 2.0 - 0.01);

            double z2 = -Math.Tan(decl) * Math.Tan(LatRad);                      //temporary variable
            double h = 0.0;

            if (z2 >= 1) //sun stays below horizon
            {
                h = 0;
            }
            else if (z2 <= -1) //sun stays above the horizon
            {
                h = Math.PI;
            }
            else
            {
                h = ZCos(z2);
            }//End if

            //Iomax = isc * (86400 / (3.1416 * r ^ 2)) * (h * Sin(Lat) * Sin(decl) + Cos(LatRad) * Cos(decl) * Sin(h)) //potential insolation, J/m2

            double hr = 2.0 * (h * 24) / (2 * 3.1416);               // length of day in hours


            return hr;
        }

        //---------------------------------------------------------------------------


        public static int DayOfYear(int month)
        {

            if (month < 0 || month > 11)
                throw new System.ApplicationException("Error: Day of Year not found.  Bad month data");


            if (month == 0) return 15;
            if (month == 1) return 46;
            if (month == 2) return 76;
            if (month == 3) return 107;
            if (month == 4) return 137;
            if (month == 5) return 168;
            if (month == 6) return 198;
            if (month == 7) return 229;
            if (month == 8) return 259;
            if (month == 9) return 290;
            if (month == 10) return 321;
            if (month == 11) return 351;

            return 0;
        }

        public static double LatitudeCorrection(int month, double latitude)
        {
            double latitudeCorrection = 0;
            int latIndex = 0;
            double[,] latCorrect = new double[27, 13]
                {
                    {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                    {0, .93, .89, 1.03, 1.06, 1.15, 1.14, 1.17, 1.12, 1.02, .99, .91, .91},
                    {0, .92, .88, 1.03, 1.06, 1.15, 1.15, 1.17, 1.12, 1.02, .99, .91, .91},
                    {0, .92, .88, 1.03, 1.07, 1.16, 1.15, 1.18, 1.13, 1.02, .99, .90, .90},
                    {0, .91, .88, 1.03, 1.07, 1.16, 1.16, 1.18, 1.13, 1.02, .98, .90, .90},
                    {0, .91, .87, 1.03, 1.07, 1.17, 1.16, 1.19, 1.13, 1.03, .98, .90, .89},
                    {0, .90, .87, 1.03, 1.08, 1.18, 1.17, 1.20, 1.14, 1.03, .98, .89, .88},
                    {0, .90, .87, 1.03, 1.08, 1.18, 1.18, 1.20, 1.14, 1.03, .98, .89, .88},
                    {0, .89, .86, 1.03, 1.08, 1.19, 1.19, 1.21, 1.15, 1.03, .98, .88, .87},
                    {0, .88, .86, 1.03, 1.09, 1.19, 1.20, 1.22, 1.15, 1.03, .97, .88, .86},
                    {0, .88, .85, 1.03, 1.09, 1.20, 1.20, 1.22, 1.16, 1.03, .97, .87, .86},
                    {0, .87, .85, 1.03, 1.09, 1.21, 1.21, 1.23, 1.16, 1.03, .97, .86, .85},
                    {0, .87, .85, 1.03, 1.10, 1.21, 1.22, 1.24, 1.16, 1.03, .97, .86, .84},
                    {0, .86, .84, 1.03, 1.10, 1.22, 1.23, 1.25, 1.17, 1.03, .97, .85, .83},
                    {0, .85, .84, 1.03, 1.10, 1.23, 1.24, 1.25, 1.17, 1.04, .96, .84, .83},
                    {0, .85, .84, 1.03, 1.11, 1.23, 1.24, 1.26, 1.18, 1.04, .96, .84, .82},
                    {0, .84, .83, 1.03, 1.11, 1.24, 1.25, 1.27, 1.18, 1.04, .96, .83, .81},
                    {0, .83, .83, 1.03, 1.11, 1.25, 1.26, 1.27, 1.19, 1.04, .96, .82, .80},
                    {0, .82, .83, 1.03, 1.12, 1.26, 1.27, 1.28, 1.19, 1.04, .95, .82, .79},
                    {0, .81, .82, 1.02, 1.12, 1.26, 1.28, 1.29, 1.20, 1.04, .95, .81, .77},
                    {0, .81, .82, 1.02, 1.13, 1.27, 1.29, 1.30, 1.20, 1.04, .95, .80, .76},
                    {0, .80, .81, 1.02, 1.13, 1.28, 1.29, 1.31, 1.21, 1.04, .94, .79, .75},
                    {0, .79, .81, 1.02, 1.13, 1.29, 1.31, 1.32, 1.22, 1.04, .94, .79, .74},
                    {0, .77, .80, 1.02, 1.14, 1.30, 1.32, 1.32, 1.22, 1.04, .93, .78, .73},
                    {0, .76, .80, 1.02, 1.14, 1.31, 1.33, 1.34, 1.23, 1.05, .93, .77, .72},
                    {0, .75, .79, 1.02, 1.14, 1.32, 1.34, 1.35, 1.24, 1.05, .93, .76, .71},
                    {0, .74, .78, 1.02, 1.15, 1.33, 1.36, 1.37, 1.25, 1.06, .92, .76, .70}};

            latIndex = (int)(latitude + 0.5) - 24;
            if (latIndex < 1)
            {
                String msg = String.Format("Error: Latitude of {0} generated an incorrect index:  {1}.", latitude, latIndex);
                throw new System.ApplicationException(msg);
            }
            if (latIndex > 26)
                latIndex = 26;

            latitudeCorrection = latCorrect[latIndex, month];
            return latitudeCorrection;
        }

        protected static double ZCos(double T)
        {
            double TA = Math.Abs(T);
            if (TA > 1.0)
            {
                throw new System.ApplicationException("|arg| for arccos > 1");
            }
            double AC = 0.0;
            double ZCos = 0.0;

            if (TA < 0.7)
                AC = 1.570796 - Math.Atan(TA / Math.Sqrt(1 - TA * TA));
            else
                AC = Math.Atan(Math.Sqrt(1.0 - TA * TA) / TA);

            if (T < 0)
                ZCos = 3.141593 - AC;
            else
                ZCos = AC;

            return ZCos;
        }

        //---------------------------------------------------------------------------
        //has to be implemented in subclasses
        public virtual void WriteToLogFile() 
        {
            throw new NotImplementedException("Error in calling WriteToLogFile() in AnnualClimate: the WriteToLogFile() should not be called directly and it has to be implemented in subclasses");
        }



    }
}
