//  Copyright 2009 Conservation Biology Institute (www.consbio.org)
//  Authors:  Robert M. Scheller
//  License:  Available at
//  http://www.landis-ii.org/developers/LANDIS-IISourceCodeLicenseAgreement.pdf

//  Creates monthly weather for one year based on averages and standard deviations, other info.

using System.Collections.Generic;
using System.IO;
using System;
//using Landis.Ecoregions;
using Landis.Util;

namespace Landis.Library.Climate
{

    public class AnnualClimate
    {

        public double[] MonthlyTemp     = new double[12];
        public double[] MonthlyMinTemp  = new double[12];
        public double[] MonthlyMaxTemp  = new double[12];
        public double[] MonthlyPrecip   = new double[12];
        public double[] MonthlyPAR      = new double[12];

        public double[] MonthlyPET      = new double[12];  // Potential Evapotranspiration
        public double[] MonthlyVPD      = new double[12];  // Vapor Pressure Deficit
        public double[] MonthlyNdeposition  = new double[12];
        public double[] MonthlyDayLength    = new double[12];
        public double[] MonthlyNightLength  = new double[12];
        public int[]    MonthlyGDD = new int[12];

        public int BeginGrowing;
        public int EndGrowing;
        public int GrowingDegreeDays;
        public double AnnualPrecip;
        public double JJAtemperature;
        public double AnnualN;
        public double AnnualAET;  // Actual Evapotranspiration
        public double Snow;
        public int Year;

        //---------------------------------------------------------------------

        //public AnnualClimate(IEcoregion ecoregion, int year, double latitude)
        public AnnualClimate(int ecoregionIndex, int year, double latitude)
        {


            IClimateRecord[] ecoClimate = new IClimateRecord[12];

            this.Year = year;
            this.AnnualPrecip = 0.0;
            this.AnnualN = 0.0;

            for(int mo = 0; mo < 12; mo++)
            {

                ecoClimate[mo] = Climate.TimestepData[ecoregionIndex, mo];

                double MonthlyAvgTemp = (ecoClimate[mo].AvgMinTemp + ecoClimate[mo].AvgMaxTemp) / 2.0;

                double standardDeviation = ecoClimate[mo].StdDevTemp * (Landis.Util.Random.GenerateUniform() * 2.0 - 1.0);

                this.MonthlyTemp[mo] = MonthlyAvgTemp + standardDeviation;
                this.MonthlyMinTemp[mo] = ecoClimate[mo].AvgMinTemp + standardDeviation;
                this.MonthlyMaxTemp[mo] = ecoClimate[mo].AvgMaxTemp + standardDeviation;
                this.MonthlyPrecip[mo] = Math.Max(0.0, ecoClimate[mo].AvgPpt + (ecoClimate[mo].StdDevPpt * (Landis.Util.Random.GenerateUniform()*2.0 - 1.0)));
                this.MonthlyPAR[mo] = ecoClimate[mo].PAR;

                this.AnnualPrecip += this.MonthlyPrecip[mo];

                if (this.MonthlyPrecip[mo] < 0)
                    this.MonthlyPrecip[mo] = 0;

                double hr = CalculateDayNightLength(mo, latitude);
                this.MonthlyDayLength[mo] = (60.0 * 60.0 * hr);                  // seconds of daylight/day
                this.MonthlyNightLength[mo] = (60.0 * 60.0 * (24 - hr));         // seconds of nighttime/day

                //this.DOY[mo] = DayOfYear(mo);
            }


            this.MonthlyPET = CalculatePotentialEvapotranspiration(ecoClimate);
            this.MonthlyVPD = CalculateVaporPressureDeficit(ecoClimate);
            this.MonthlyGDD = CalculatePnETGDD(this.MonthlyTemp, year);

            this.BeginGrowing       = CalculateBeginGrowingSeason(ecoClimate);
            this.EndGrowing         = CalculateEndGrowingSeason(ecoClimate);
            this.GrowingDegreeDays  = GrowSeasonDegreeDays(year);

            for(int mo = 5; mo < 8; mo++)
                this.JJAtemperature += this.MonthlyTemp[mo];
            this.JJAtemperature /= 3.0;


        }

        //---------------------------------------------------------------------------
        public string Write()
        {
            string s = String.Format(
                " Climate:  Year={0}, Number GDD={1}." +
                " AnnualPpt={2:000.0}," +
                " JanMinTemp={3:0.0}," +
                " JanMaxTemp={4:0.0}," +
                " JanPpt={5:0.0}",
                this.Year, this.GrowingDegreeDays,
                TotalAnnualPrecip(), this.MonthlyMinTemp[0], this.MonthlyMaxTemp[0], this.MonthlyPrecip[0]);
            return s;
        }
        //---------------------------------------------------------------------------
        public void SetAnnualN(double Nslope, double Nintercept)
        {

            AnnualN = CalculateAnnualN(AnnualPrecip, Nslope, Nintercept);
            for(int mo = 0; mo < 12; mo++)
                MonthlyNdeposition[mo] = AnnualN * MonthlyPrecip[mo] / AnnualPrecip;

        }
        //---------------------------------------------------------------------------
        private static double CalculateAnnualN(double annualPrecip, double Nslope, double Nintercept)
        {
            //wet fixation , rain in cm, not mm
            //dry fixation , rain in cm, not mm

            double annualN = 0.0;

            annualN = Nintercept + Nslope * annualPrecip;

            return annualN;
        }


        //---------------------------------------------------------------------------
        public int GrowSeasonDegreeDays(int currentYear)
        //Calc growing season degree days (Degree_Day) based on monthly temperatures
        //normally distributed around a specified mean with a specified standard
        //deviation.

        {
            //degDayBase is temperature (C) above which degree days (Degree_Day)
            //are counted
            double degDayBase = 5.56;      // 42F.

            double Deg_Days = 0.0;

            //Calc monthly temperatures (mean +/- normally distributed
            //random number times standard deviation) and
            //sum degree days for consecutve months.
            for (int i = 0; i < 12; i++) //12 months in year
            {
                if (MonthlyTemp[i] > degDayBase)
                    Deg_Days += (MonthlyTemp[i] - degDayBase) * DaysInMonth(i, currentYear);
            }
            return (int) Deg_Days;
        }


        //---------------------------------------------------------------------------
        public static int[] CalculatePnETGDD(double[] monthlyTemp, int currentYear)
        {
            //************************************************
            //  Heat Sum Routine
            //**********************

            int[] MonthlyGDD = new int[12];

            for (int i = 0; i < 12; i++) //12 months in year
            {
                double GDD = monthlyTemp[i] * DaysInMonth(i, currentYear);
                if (GDD < 0)
                    GDD = 0;
                MonthlyGDD[i] = (int) GDD;
                //GDDTot = GDDTot + GDD;
            }

            return MonthlyGDD;
        }


        //---------------------------------------------------------------------------
        private static int CalculateBeginGrowingSeason(IClimateRecord[] annualClimate)
        //Calculate Begin Growing Degree Day (Last Frost; Minimum = 0 degrees C):
        {
            NormalRandomVar randVar = new NormalRandomVar(0, 1);
            double lastMonthMinTemp = annualClimate[0].AvgMinTemp;
            int dayCnt = 15;  //the middle of February
            int beginGrowingSeason = -1;

            for (int i = 1; i < 7; i++)  //Begin looking in February (1).  Should be safe for at least 100 years.
            {

                int totalDays = (DaysInMonth(i, 3) + DaysInMonth(i-1, 3)) / 2;
                double MonthlyMinTemp = annualClimate[i].AvgMinTemp;// + (monthlyTempSD[i] * randVar.GenerateNumber());

                //Now interpolate between days:
                double degreeIncrement = System.Math.Abs(MonthlyMinTemp - lastMonthMinTemp) / (double) totalDays;
                double Tnight = MonthlyMinTemp;  //start from warmer month
                double TnightRandom = Tnight + (annualClimate[i].StdDevTemp * (Landis.Util.Random.GenerateUniform() * 2 - 1));

                for(int day = 1; day <= totalDays; day++)
                {
                    if(TnightRandom <= 0)
                        beginGrowingSeason = (dayCnt + day);
                    Tnight += degreeIncrement;  //work backwards to find last frost day.
                    TnightRandom = Tnight + (annualClimate[i].StdDevTemp * (Landis.Util.Random.GenerateUniform() * 2 - 1));
                }

                lastMonthMinTemp = MonthlyMinTemp;
                dayCnt += totalDays;  //new monthly mid-point
            }
            return beginGrowingSeason;
        }

        //---------------------------------------------------------------------------
        private static int CalculateEndGrowingSeason(IClimateRecord[] annualClimate)//, Random autoRand)
        //Calculate End Growing Degree Day (First frost; Minimum = 0 degrees C):
        {
            NormalRandomVar randVar = new NormalRandomVar(0, 1);

            //Defaults for the middle of July:
            double lastMonthTemp = annualClimate[6].AvgMinTemp;
            int dayCnt = 198;
            //int endGrowingSeason = 198;

            for (int i = 7; i < 12; i++)  //Begin looking in August.  Should be safe for at least 100 years.
            {
                int totalDays = (DaysInMonth(i, 3) + DaysInMonth(i-1, 3)) / 2;
                double MonthlyMinTemp = annualClimate[i].AvgMinTemp;

                //Now interpolate between days:
                double degreeIncrement = System.Math.Abs(lastMonthTemp - MonthlyMinTemp) / (double) totalDays;
                double Tnight = lastMonthTemp;  //start from warmer month

                    //double randomT = (2 * annualClimate[i].StdDevTemp * randVar.GenerateNumber(autoRand));
                    //Console.WriteLine("Night Temp random offset = {0}.", randomT);
                double TnightRandom = Tnight + (annualClimate[i].StdDevTemp * (Landis.Util.Random.GenerateUniform() * 2 - 1));

                for(int day = 1; day <= totalDays; day++)
                {
                    if(TnightRandom <= 0)
                        return (dayCnt + day);
                    Tnight -= degreeIncrement;  //work forwards to find first frost day.
                    TnightRandom = Tnight + (annualClimate[i].StdDevTemp * (Landis.Util.Random.GenerateUniform() * 2 - 1));
                    //Console.WriteLine("Tnight = {0}.", TnightRandom);
                }

                lastMonthTemp = MonthlyMinTemp;
                dayCnt += totalDays;  //new monthly mid-point
            }
            return 365;
        }


        //---------------------------------------------------------------------------
        private static double[] CalculateVaporPressureDeficit(IClimateRecord[] annualClimate)
        {
            // From PnET
            //Estimation of saturated vapor pressure from daily average temperature.

            //   calculates saturated vp and delta from temp
            //   from Murray J Applied Meteorol 6:203
            //   ?? are the < 0 equations from there also
            //   Tday    average air temperature, degC
            //   ES  saturated vapor pressure at Tday, kPa
            //   DELTA dES/dTA at TA, kPa/K which is the slope of the sat. vapor pressure curve
            //   Saturation equations are from:
            //       Murry, (1967). Journal of Applied Meteorology. 6:203.
            //
            //
            //
            double[] monthlyVPD = new double[12];

            for(int month = 0; month < 12; month++)
            {
                double Tmin = annualClimate[month].AvgMinTemp;
                double Tday = (annualClimate[month].AvgMinTemp + annualClimate[month].AvgMaxTemp) / 2.0;

                double es = 0.61078 * Math.Exp(17.26939 * Tday / (Tday + 237.3)); //kPa
                //double delta = 4098 * es / (Tday + 237.3) ^ 2.0;
                if (Tday < 0)
                {
                    es = 0.61078 * Math.Exp(21.87456 * Tday / (Tday + 265.5)); //kPa
                    //delta = 5808 * es / (Tday + 265.5) ^ 2
                }

                //Calculation of mean daily vapor pressure from minimum daily temperature.

                //   Tmin = minimum daily air temperature                  //degrees C
                //   emean = mean daily vapor pressure                     //kPa
                //   Vapor pressure equations are from:
                //       Murray (1967). Journal of Applied Meteorology. 6:203.

                double emean = 0.61078 * Math.Exp(17.26939 * Tmin / (Tmin + 237.3)); //kPa

                if (Tmin < 0)
                     emean = 0.61078 * Math.Exp(21.87456 * Tmin / (Tmin + 265.5));

                double VPD = es - emean;
                //if (VPD = 0)
                //  VPD = VPD;
                monthlyVPD[month] = VPD;
            }

            return monthlyVPD;
        }
        //---------------------------------------------------------------------------
        private static double[] CalculatePotentialEvapotranspiration(IClimateRecord[] annualClimate)
        {
            //Calculate potential evapotranspiration (pevap)
            //...Originally from pevap.f
            // FWLOSS(4) - Scaling factor for potential evapotranspiration (pevap).
            double waterLossFactor4 = 0.9;  //from Century v4.5


            // FINISH - from ecoregion data???
            double elev = 1.0;       //Definition?? Set elevation = 0???
            double sitlat = 0.0; // Site latitude???

            double highest = -40.0;
            double lowest = 100.0;

            for(int i = 0; i < 12; i++)
            {
                double avgTemp = (annualClimate[i].AvgMinTemp + annualClimate[i].AvgMaxTemp) / 2.0;
                highest = System.Math.Max(highest, avgTemp);
                lowest = System.Math.Min(lowest, avgTemp);
            }

            lowest = System.Math.Max(lowest, -10.0);

            //...Determine average temperature range
            double avgTempRange = System.Math.Abs(highest - lowest);

            double[] monthlyPET = new double[12];


            for(int month = 0; month < 12; month++)
            {

                //...Temperature range calculation
                double tr = annualClimate[month].AvgMaxTemp - System.Math.Max(-10.0, annualClimate[month].AvgMinTemp);

                double t = tr / 2.0 + annualClimate[month].AvgMinTemp;
                double tm = t + 0.006 * elev;
                double td = (0.0023 * elev) + (0.37 * t) + (0.53 * tr) + (0.35 * avgTempRange) - 10.9;
                double e = ((700.0 * tm / (100.0 - System.Math.Abs(sitlat))) + 15.0 * td) / (80.0 - t);
                double monpet = (e * 30.0)/10.0;

                if (monpet < 0.5)
                    monpet = 0.5;

                //...fwloss(4) is a modifier for PET loss.   vek may90
                monthlyPET[month] = monpet * waterLossFactor4;

            }

            return monthlyPET;
        }


        //---------------------------------------------------------------------------
        public static double CalculateAnnualActualEvapotranspiration(AnnualClimate annualClimate, double fieldCapacity)
        {
            // field capacity input as cm
            // variable with xVariableName indicate conversion to mm

            double xFieldCap = fieldCapacity * 10.0;

            double waterAvail = 0.0;
            double actualET = 0.0;
            double oldWaterAvail = 0.0;
            double accPotWaterLoss = 0.0;

            for(int month = 0; month < 12; month++)
            {

                double monthlyRain = annualClimate.MonthlyPrecip[month];
                double potentialET = annualClimate.MonthlyPET[month];


                //Calc potential water loss this month
                double potWaterLoss = monthlyRain - potentialET;

                //If monthlyRain doesn't satisfy potentialET, add this month's potential
                //water loss to accumulated water loss from soil
                if (potWaterLoss < 0.0)
                {
                    accPotWaterLoss += potWaterLoss;
                    double xAccPotWaterLoss = accPotWaterLoss * 10.0;

                    //Calc water retained in soil given so much accumulated potential
                    //water loss Pastor and Post. 1984.  Can. J. For. Res. 14:466:467.

                    waterAvail = fieldCapacity *
                                 System.Math.Exp((.000461 - 1.10559 / xFieldCap) * (-1.0 * xAccPotWaterLoss));

                    if (waterAvail < 0.0)
                        waterAvail = 0.0;

                    //changeSoilMoisture - during this month
                    double changeSoilMoisture = waterAvail - oldWaterAvail;

                    //Calc actual evapotranspiration (AET) if soil water is drawn down
                    actualET += (monthlyRain - changeSoilMoisture);
                }

                //If monthlyRain satisfies potentialET, don't draw down soil water
                else
                {
                    waterAvail = oldWaterAvail + potWaterLoss;
                    if (waterAvail >= fieldCapacity)
                        waterAvail = fieldCapacity;

                    double changeSoilMoisture = waterAvail - oldWaterAvail;

                    //If soil partially recharged, reduce accumulated potential
                    //water loss accordingly
                    accPotWaterLoss += changeSoilMoisture;

                    //If soil completely recharged, reset accumulated potential
                    //water loss to zero
                    if (waterAvail >= fieldCapacity)
                        accPotWaterLoss = 0.0;

                    //If soil water is not drawn upon, add potentialET to AET
                    actualET += potentialET;
                }

                oldWaterAvail = waterAvail;
            }

            return actualET;
        }

        //---------------------------------------------------------------------------
        public double MeanAnnualTemp(int currentYear)
        {
            double MAT = 0.0;
            //Calc monthly temperatures (mean +/- normally distributed
            //random number times  standard deviation) and
            //sum degree days for consecutve months.
            for (int i = 0; i < 12; i++) //12 months in year
            {
                int daysInMonth = DaysInMonth(i, currentYear);
                MAT += daysInMonth * MonthlyTemp[i];
            }
            MAT /= 365.0;
            return MAT;
        }

        //---------------------------------------------------------------------------
        public double TotalAnnualPrecip()
        {
            //Main loop for yearly water balance calculation by month   */
            double TAP = 0.0;
            for (int i = 0; i < 12; i++)
            {
                TAP += MonthlyPrecip[i];
            }
            return TAP;
        }

        //---------------------------------------------------------------------------
        public static int DaysInMonth(int month, int currentYear)
        //This will return the number of days in a month given the month number where
        //January is 1.

        {
            switch(month+1)
            {
                //Thirty days hath September, April, June && November
                case 9:  case 4:  case 6: case 11: return 30;
                //...all the rest have 31...
                case 1: case 3: case 5: case 7:
                case 8: case 10: case 12: return 31;
                //...save February, etc.
                case 2: if (currentYear%4 == 0)
                  return 29;
                else
                  return 28;
            }
            return 0;
        }

        private static double CalculateDayNightLength(int month, double latitude)
        {
            double DOY = (double) DayOfYear(month);
            double LatRad = latitude * (2.0 * Math.PI) / 360;
            double r = 1.0 - 0.0167 * Math.Cos(0.0172 * (DOY - 3));       //radius vector of the sun
            double z = 0.39785 * Math.Sin(4.868961 + 0.017203 * DOY + 0.033446 * Math.Sin(6.224111 + 0.017202 * DOY));

            double decl = 0.0;
            if (Math.Abs(z) < 0.7)
            {
                decl = Math.Atan(z / (Math.Sqrt(1.0 - Math.Pow(z,2.0))));
            } else
            {
                decl = Math.PI / 2.0 - Math.Atan(Math.Sqrt(1.0 - Math.Pow(z,2.0)) / z);
            }
            if (Math.Abs(LatRad) >= Math.PI / 2.0)
                LatRad = Math.Sign(latitude) * (Math.PI / 2.0 - 0.01);

            double z2 = -Math.Tan(decl) * Math.Tan(LatRad);                      //temporary variable
            double h=0.0;

            if (z2 >= 1) //sun stays below horizon
            {
                h = 0;
            } else if (z2 <= -1) //sun stays above the horizon
            {
                h = Math.PI;
            } else {
                h = ZCos(z2);
            }//End if

            //Iomax = isc * (86400 / (3.1416 * r ^ 2)) * (h * Sin(Lat) * Sin(decl) + Cos(LatRad) * Cos(decl) * Sin(h)) //potential insolation, J/m2

            double hr = 2.0 * (h * 24) / (2 * 3.1416);               // length of day in hours


            return hr;
        }

        //---------------------------------------------------------------------------
        public static double LatitudeCorrection(int month, double latitude)
        {
            double latitudeCorrection = 0;
            int  latIndex = 0;
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

            latIndex = (int) (latitude + 0.5) - 24;
            if (latIndex < 1)
                throw new System.ApplicationException("Error: Lat index too small.");
            if (latIndex > 26)
                latIndex = 26;

            latitudeCorrection = latCorrect[latIndex, month];
            return latitudeCorrection;
        }

        public static int DayOfYear(int month)
        {

            if(month < 0 || month > 11)
                throw new System.ApplicationException("Error: Day of Year not found.  Bad month data");


            if(month == 0) return 15;
            if(month == 1) return 46;
            if(month == 2) return 76;
            if(month == 3) return 107;
            if(month == 4) return 137;
            if(month == 5) return 168;
            if(month == 6) return 198;
            if(month == 7) return 229;
            if(month == 8) return 259;
            if(month == 9) return 290;
            if(month == 10) return 321;
            if(month == 11) return 351;

            return 0;
        }

        private static double ZCos(double T)
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


    }
}
