//  Copyright: Portland State University 2009-2014
//  Authors:  Robert M. Scheller, Amin Almassian

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Landis.Core;


namespace Landis.Library.Climate
{
    public class PDSI_Calculator
    {
        //private static AnnualClimate_Monthly annualClimate;
        //public static double[] mon_T_normal;
        private static Potential potential;// = new Potential[12];
        public static int Verbose; 
        //private static AnnualClimate_Monthly annClimate;

        //===========================================PDSI Attributes=====================================================
        public const double MISSING = -99.00;

        //preserve period_length and num_of_periods for multiple week PDSI's.
        private static int period_length = 1;        //set to 1 for monthly, otherwise, length of period
        private static int num_of_periods = 12;       

        // The variables used as flags to the pdsi class
        private static int bug = 0;
        public static int e_year;
        private static int metric = 0;
        private static bool south; //whethere the location is in Southern Hemisphere (If TLA is positive, we assume location is in NORTHERN Hemisphere.
        //private static int nadss = 0;

        // Various constants used in calculations
        private static double TLA; // The negative tangent of latitude is used in calculating PE
        private static double AWC; // The soils water capacity
        private static double I;   // Thornthwaites heat index
        private static double A;   // Thornthwaites exponent
        private static double tolerance = 0.00001; // The tolerance for various comparisons

        // The arrays used to read in the normal temp data, a years worth of 
        // actual temp data, and a years worth of precipitation data
        //private static double[] TNorm = new double[12];
        private static double[] T = new double[12];
        private static double[] P = new double[12];
        private static double[] PDSI_Monthly = new double[12];

        // These variables are used in calculation to store the current period's
        // potential and actual water balance variables as well as the soil
        // moisture levels
        private static double ET;            // Actual evapotranspiration
        private static double R;             // Actual soil recharge 
        private static double L;             // Actual loss
        private static double RO;            // Actual runoff
        private static double PE;            // Potential evapotranspiration
        private static double PR;            // Potential soil recharge
        private static double PL;            // Potential Loss
        private static double PRO;           // Potential runoff
        private static double Su;            // Underlying soil moisture
        private static double Ss;            // Surface soil moisture

        // These arrays are used to store the monthly or weekly sums of the 8 key 
        // water balance variables and the precipitation
        private static double[] ETSum = new double[12];
        private static double[] RSum = new double[12];
        private static double[] LSum = new double[12];
        private static double[] ROSum = new double[12];
        private static double[] PESum = new double[12];
        private static double[] PRSum = new double[12];
        private static double[] PLSum = new double[12];
        private static double[] PROSum = new double[12];
        private static double[] PSum = new double[12];

        // These arrays store the monthly or weekly water balance coefficeints 
        private static double[] Alpha = new double[12];
        private static double[] Beta = new double[12];
        private static double[] Gamma = new double[12];
        private static double[] Delta = new double[12];

        // The CAFEC percipitation
        private static double Phat;

        // These variables are used in calculating the z index
        private static double d;     // Departure from normal for a period
        private static double[] D = new double[12]; // Sum of the absolute value of all d values by period
        private static double[] k = new double[12]; // Palmer's k' constant by period
        private static double K;     // The final K value for a period
        private static double Z = 9999.0;     // The z index for a period (Z=K*d)

        // These variables are used in calculating the PDSI from the Z
        // index.  They determine how much of an effect the z value has on 
        // the PDSI based on the climate of the region.  
        // They are calculated using CalcDurFact()
        private static double drym;
        private static double dryb;
        private static double wetm;
        private static double wetb;

        //these two variables weight the climate characteristic in the 
        //calibration process
        //double dry_ratio;
        //double wet_ratio;

        // The X variables are used in book keeping for the computation of
        // the pdsi
        private static double X1;    // Wet index for a month/week
        private static double X2;    // Dry index for a month/week
        private static double X3;    // Index for an established wet or dry spell
        //double X;     // Current period's pdsi value before backtracking

        // These variables are used in calculating the probability of a wet
        // or dry spell ending
        private static double Prob;  // Prob=V/Q*100
        private static double V;     // Sumation of effective wetness/dryness
        private static double Q;     // Z needed for an end plus last period's V

        // These variables are statistical variables computed and output in 
        // Verbose mode
        private static double[] DSSqr = new double[12];
        private static double[] DEPSum = new double[12];
        private static double DKSum;
        private static double SD;
        private static double SD2;

        // linked lists to store X values for backtracking when computing X
        //private static Dictionary<int, double[]> XDic = new Dictionary<int, double[]>();//final list of PDSI values
        //private static Dictionary<int, double[]> PDSI_Dic = new Dictionary<int, double[]>();//final list of PDSI values
        private static LinkedList<double> Xlist = new LinkedList<double>();//final list of PDSI values
        private static LinkedList<double> altX1 = new LinkedList<double>();//list of X1 values
        private static LinkedList<double> altX2 = new LinkedList<double>();//list of X2 values

        // These linked lists store the Z, Prob, and 3 X values for
        // outputing the Z index, Hydro Palmer, and Weighted Palmer
        //private static LinkedList<double> XL1 = new LinkedList<double>();
        //private static LinkedList<double> XL2 = new LinkedList<double>();
        //private static LinkedList<double> XL3 = new LinkedList<double>();
        //private static LinkedList<double> ProbL = new LinkedList<double>();
        //private static LinkedList<double> ZIND = new LinkedList<double>();

        static PDSI_Calculator()
        {
        }
        //===============================================================================================================
        //Methods called by SumAll()
        //-------------------------------------------------------

        // CalcPR calculates the Potential Recharge of the soil for one period of the 
        // year being examined.  PR = Soils Max Capacity - Soils Current Capacity or
        // AWC - (SU + Ss)
        //-----------------------------------------------------------------------------
        private static void CalcPR()
        {
            PR = AWC - (Su + Ss);
        }
        //-----------------------------------------------------------------------------
        // CalcPRO calculates the Potential Runoff for a given period of the year being
        // examined.  PRO = Potential Precip - PR. Palmer arbitrarily set the Potential
        // Precip to the AWC making PRO = AWC - (AWC - (Su + Ss)). This then simplifies
        // to PRO = Su + Ss
        //-----------------------------------------------------------------------------
        private static void CalcPRO()
        {
            PRO = Ss + Su;
        }
        //-----------------------------------------------------------------------------
        // CalcPL calculates the Potential Loss of moisture in the soil for a period of
        // one period of the year being examined. If the Ss capacity is enough to
        // handle all PE, PL is simple PE.  Otherwise, potential loss from Su occurs at
        // the rate of (PE-Ss)/AWC*Su.  This means PL = Su*(PE - Ss)/AWC + Ss
        //-----------------------------------------------------------------------------
        private static void CalcPL()
        {
            if (Ss >= PE)
                PL = PE;
            else
            {
                PL = ((PE - Ss) * Su) / (AWC) + Ss;
                if (PL > PRO)  // If PL>PRO then PL>water in the soil.  This isn't
                    PL = PRO;   // possible so PL is set to the water in the soil
            }
        }

        //-----------------------------------------------------------------------------
        // CalcActual calculates the actual values of evapotranspiration,soil recharge,
        // runoff, and soil moisture loss.  It also updates the soil moisture in both
        // layers for the next period depending on current weather conditions.
        //-----------------------------------------------------------------------------
        private static void CalcActual(int per)
        {
            double R_surface = 0.0;   // recharge of the surface layer
            double R_under = 0.0;    // recharge of the underlying layer
            double surface_L = 0.0;   // loss from surface layer
            double under_L = 0.0;    // loss from underlying layer
            double new_Su, new_Ss;    // new soil moisture values


            if (P[per] >= PE)
            {
                // The precipitation exceeded the maximum possible evapotranspiration
                // (excess moisture)
                ET = PE;   // Enough moisture for all potential evapotranspiration to occur
                L = 0.0;   // with no actual loss of soil moisture

                if ((P[per] - PE) > (1.0 - Ss))
                {
                    // The excess precip will recharge both layers. Note: (1.0 - SS) is the 
                    // amount of water needed to saturate the top layer of soil assuming it
                    // can only hold 1 in. of water.
                    R_surface = 1.0 - Ss;
                    new_Ss = 1.0;

                    if ((P[per] - PE - R_surface) < ((AWC - 1.0) - Su))
                    {
                        // The entire amount of precip can be absorbed by the soil (no runoff)
                        // and the underlying layer will receive whats left after the top layer
                        // Note: (AWC - 1.0) is the amount able to be stored in lower layer
                        R_under = (P[per] - PE - R_surface);
                        RO = 0.0;
                    }
                    else
                    {
                        // The underlying layer is fully recharged and some runoff will occur
                        R_under = (AWC - 1.0) - Su;
                        RO = P[per] - PE - (R_surface + R_under);
                    }
                    new_Su = Su + R_under;
                    R = R_surface + R_under;//total recharge
                }
                else
                {
                    // There is only enough moisture to recharge some of the top layer.
                    R = P[per] - PE;
                    new_Ss = Ss + R;
                    new_Su = Su;
                    RO = 0.0;
                }
            }// End of if(P[per] >= PE)
            else
            {
                // The evapotranspiration is greater than the precipitation received.  This
                // means some moisture loss will occur from the soil.
                if (Ss > (PE - P[per]))
                {
                    // The moisture from the top layer is enough to meet the remaining PE so 
                    // only the top layer losses moisture.
                    surface_L = PE - P[per];
                    under_L = 0.0;
                    new_Ss = Ss - surface_L;
                    new_Su = Su;
                }
                else
                {
                    // The top layer is drained, so the underlying layer loses moisture also.
                    surface_L = Ss;
                    under_L = (PE - P[per] - surface_L) * Su / AWC;
                    if (Su < under_L)
                        under_L = Su;
                    new_Ss = 0.0;
                    new_Su = Su - under_L;
                }
                R = 0;// No recharge occurs
                L = under_L + surface_L;// Total loss
                RO = 0.0;// No extra moisture so no runoff
                ET = P[per] + L;// Total evapotranspiration
            }
            Ss = new_Ss;//update soil moisture values
            Su = new_Su;
        }//end of CalcActual(int per)

        //-----------Is called from SumAll()----------------
        private static void CalcMonPE(int month, int year)
        {
            double[] Phi = { -.3865982, -.2316132, -.0378180, .1715539, .3458803, .4308320, .3916645, .2452467, .0535511, -.15583436, -.3340551, -.4310691 };
            //these values of Phi[] come directly from the fortran program.
            int[] Days = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            double Dum, Dk;
            int offset;
            if (south)
                offset = 6;
            else
                offset = 0;

            if (T[month] <= 32)
                PE = 0;
            else
            {
                Dum = Phi[(month + offset) % 12] * TLA;
                Dk = Math.Atan(Math.Sqrt(1 - Dum * Dum) / Dum);
                if (Dk < 0)
                    Dk += 3.141593;
                Dk = (Dk + .0157) / 1.57;
                if (T[month] >= 80)
                {
                    PE = (Math.Sin(T[month] / 57.3 - .166) - .76) * Dk;
                }
                else
                {
                    Dum = Math.Log(T[month] - 32);
                    PE = (Math.Exp(-3.863233 + A * 1.715598 - A * Math.Log(I) + A * Dum)) * Dk;
                }
            }
            // This calculation of leap year follows the FORTRAN program 
            // It does not take into account factors of 100 or 400
            /*
            if (year%4==0 && month==1)
              PE=PE*29;
            else
              PE=PE*Days[month];
            */
            //this calculation has been updated to accurately follow leap years
            if (month == 1)
            {
                if (year % 400 == 0)
                    PE = PE * 29;
                else if (year % 4 == 0 && year % 100 != 0)
                    PE = PE * 29;
                else
                    PE = PE * 28;
            }
            else
                PE = PE * Days[month];

        }

        //===============================================================================================================
        /// <summary>
        /// Calculates original PDSI (NOT Self-Calibrating PDSI) for a given month (1-12)
        /// </summary>
        /// <param name="annualClimate"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        public static double CalculatePDSI(AnnualClimate_Monthly oneClimate, double[] mon_T_normal, double awc, double latitude, UnitSystem arsUnitSystem, IEcoregion ecoregion)
        {
            TLA = latitude;

            // Print AVERAGE T normal.
            //for (int mo = 0; mo < 12; mo++)
            //{
            //    Climate.ModelCore.UI.WriteLine("Month = {0}, Input Monthly T normal = {1}", mo, mon_T_normal[mo]);
            //}

            double[] corrected_mon_T_normal = new double[12];

            // -------------------------------------------------------------------
            //Converting Celcius temps to Fahrenheit by  Tf = (9/5)*Tc+32; formula
            //Converting precepitation from cm to inches by  in = cm * 0.39370 formula
            if (arsUnitSystem == UnitSystem.metrics)
            {
                for (int i = 0; i < 12; i++)
                {
                    corrected_mon_T_normal[i] = (9.0 / 5.0) * mon_T_normal[i] + 32.0;
                }

                //for (int i = 0; i < _AnnualClimates.Length; i++)
                //{
                for (int month = 0; month < 12; month++)
                {
                        T[month] = (9.0 / 5.0) * oneClimate.MonthlyTemp[month] + 32.0;
                        P[month] = oneClimate.MonthlyPrecip[month] * 0.39370;
                }

                //awt is passed and used in GetParam(awt)
                double awc_temp = AWC;
                AWC = awc_temp * 0.39370;
                //TLA = TLA * 0.39370;  //WHY IS TLA TRANSFORMED??
            }
            
            // -------------------------------------------------------------------
            //the negative of the tangent of the latitude  of the station - TLA
            if (TLA > 0)
                south = false; //true;
            else
                south = true; // false;
            //--------------------------------------------------

            //Set fieldCapacity and TLA
            SetSoilMoisture(awc);  //awc = available water content
            SetTLA();

            I = CalcMonThornI(corrected_mon_T_normal);

            A = CalcThornA(I);
            if (Verbose > 1)
            {
                Climate.ModelCore.UI.WriteLine("AWC = {0}  TLA = {1}", AWC, TLA);
                Climate.ModelCore.UI.WriteLine("HEAT INDEX, THORNTHWAITE A: {0} {1}", I, A);
            }


            //------------------------
            //--------Calculation-----
            //------------------------
            //totalyears = HistoricClimate.endyear - HistoricClimate.startyear + 1;
            // Output seen only in maximum Verbose mode
            if (Verbose > 1)
                Climate.ModelCore.UI.WriteLine("processing station 1\n");
            
            // SumAll is called to compute the sums for the 8 water balance variables
            PDSI_SumAll();

            // This outputs those sums to the screen
            if (Verbose > 1)
            {
                // Climate.ModelCore.UI.WriteLine("STATION = %5d %18c SUMMATION OF MONTHLY VALUES OVER %4d YEARS\n", 0, ' ', totalyears);
                Climate.ModelCore.UI.WriteLine("%36c CALIBRATION YEARS:\n", ' ');
                Climate.ModelCore.UI.WriteLine("PER P S PR PE PL ET R L RO DEP\n\n");
            }
            for (int i = 0; i < num_of_periods; i++)  // months
            {
                DEPSum[i] = ETSum[i] + RSum[i] - PESum[i] + ROSum[i];
                if (Verbose > 2)
                {
                    Climate.ModelCore.UI.WriteLine("{0}", (period_length * i) + 1);
                    Climate.ModelCore.UI.WriteLine("{0} {1} {2} {3} {4}", PSum[i], PROSum[i], PRSum[i], PESum[i], PLSum[i]);
                    Climate.ModelCore.UI.WriteLine("{0} {1} {2} {3} {4}", ETSum[i], RSum[i], LSum[i], ROSum[i], DEPSum[i]);
                    Climate.ModelCore.UI.WriteLine("\n");
                }
                DSSqr[i] = 0;
            }

            // CalcWBCoef is then called to calculate alpha, beta, gamma, and delta
            CalcWBCoef();

            // Next Calcd is called to calculate the monthly departures from normal
            CalcMonthlyDepartures();
            
            // Finally CalcK is called to compute the K and Z values.  CalcX is called within CalcK.
            CalcOrigK();

            double annualPDSI = 0;
            //double ecoAverage = 0;
            //LinkedListNode<double> node = Xlist.Last;

            for (int m = 0; m < 12; m++)
            {
                annualPDSI += PDSI_Monthly[m]; //Math.Round(node.Value, 2);
                //Climate.ModelCore.UI.WriteLine("Month = {0}, Output Monthly T normal = {1:0.00}, PDSI = {2:0.000}", m, mon_T_normal[m], PDSI_Monthly[m]);
            }

            annualPDSI /= 12.0;

            Climate.PdsiLog.Clear();
            PDSI_Log pl = new PDSI_Log();

            pl.Time = oneClimate.Year;
            pl.EcoregionName = ecoregion.Name;
            pl.EcoregionIndex = ecoregion.Index;
            pl.PDSI = annualPDSI;

            Climate.PdsiLog.AddObject(pl);
            Climate.PdsiLog.WriteToFile();

            return annualPDSI;
        }

        //Computes the sums for the 8 water balance variables
        private static void PDSI_SumAll()
        {
            //char[] Temp = new char[150], Precip = new char[150];
            //int actyear;
            double DEP = 0;
            SD = 0;
            SD2 = 0;

            // Initializes the sums to 0;
            for (int i = 0; i < num_of_periods; i++)
            {
                ETSum[i] = 0;
                RSum[i] = 0;
                LSum[i] = 0;
                ROSum[i] = 0;
                PSum[i] = 0;
                PESum[i] = 0;
                PRSum[i] = 0;
                PLSum[i] = 0;
                PROSum[i] = 0;
            }

            //totalyears = e_year - s_year + 1;
            //totalyears = e_year - s_year + 1;
            //totalyears = _AnnualClimates[e_year].Year - _AnnualClimates[s_year].Year;

            //_Potential = new Potential[totalyears];
            // This loop runs to read in and calculate the values for all years
            //for (int y = 1; y <= totalyears; y++)
            //{
                // Get a year's worth of temperature and precipitation data
                // Also, get the current year from the temperature file.
            //annClimate = annualClimate; // s[y - 1];
                //annClimateIndex = y - 1;
                //if(Weekly){
                //  actyear=GetTemp(input_temp, T, 52);
                //  GetPrecip(input_prec, P, 52);
                //}
                //else{
                //for (int i = 0; i < 12; i++)
                //{
                //    T[i] = _AnnualClimates[year].MonthlyTemp[i];  //calculate T[i] from historic, not target climate
                //    P[i] = _AnnualClimates[year].MonthlyPrecip[i];  //calculate P[i] from historic, not target climate
                //}
             //GetTemp(ref T, 12);//actyear = GetTemp(input_temp, T, 12);
             //GetPrecip(ref P, 12);//GetPrecip(input_prec, P, 12);
                //}

            Potential pot = new Potential();

                // This loop runs for each per in the year
                for (int per = 0; per < num_of_periods; per++)
                {
                    //if (P[per] >= 0 && T[per] != MISSING)
                    //{
                        // calculate the Potential Evapotranspiration first
                        // because it's needed in later calculations
                        CalcMonPE(per, 2012); // The year 2012 is a placeholder!
                        //_AnnualClimates[y - 1].Year); //Year is the given actual year like 2012

                        CalcPR();         // calculate Potential Recharge, Potential Runoff,
                        CalcPRO();        // and Potential Loss
                        CalcPL();
                        CalcActual(per);  // Calculate Evapotranspiration, Recharge, Runoff,
                        // and Loss

                        // Calculates some statistical variables for output 
                        // to the screen in the most Verbose mode (Verbose > 1)
                        if (per > 4 && per < 8)
                        {
                            DEP = DEP + P[per] + L - PE;
                            if (per == 7)
                            {
                                SD = SD + DEP;
                                SD2 = SD2 + DEP * DEP;
                                DEP = 0;
                            }
                        }


                        /* SG 6/5/06: add code to support a calibration interval */
                        /* SG 6/4/06: Allow for user-defined calibration interval by not Summing during
                        **            years before the calibration interval or after the end of the calibration
                        **            interval.  
                        */

                        // Update the sums by adding the current water balance values
                        ETSum[per] += ET;
                        RSum[per] += R;
                        ROSum[per] += RO;
                        LSum[per] += L;
                        PSum[per] += P[per];
                        PESum[per] += PE;
                        PRSum[per] += PR;
                        PROSum[per] += PRO;
                        PLSum[per] += PL;

                        pot.Year = 2012; // Placeholder! _AnnualClimates[y - 1].Year;
                        pot.Period[per] = (period_length * per) + 1;
                        pot.P[per] = P[per];
                        pot.PE[per] = PE;
                        pot.PR[per] = PR;
                        pot.PRO[per] = PRO;
                        pot.PL[per] = PL;
                        pot.P_PE[per] = P[per] - PE;
                        //_Potential[year] = new Potential()  { Year = _AnnualClimates[year].Year, Period[per] = (period_length * per) + 1, P[per] = P[per], PE[per] = PE, PR[per] = PR, PRO[per] = PRO, PL[per] = PL, P_PE[per] = P[per] - PE };
                    //}
                    //else
                    //{
                    //    pot.Year = 2012; // Placeholder! _AnnualClimates[y - 1].Year;
                    //    pot.Period[per] = (period_length * per) + 1;
                    //    pot.P[per] = MISSING;
                    //    pot.PE[per] = MISSING;
                    //    pot.PR[per] = MISSING;
                    //    pot.PRO[per] = MISSING;
                    //    pot.PL[per] = MISSING;
                    //    pot.P_PE[per] = MISSING;
                    //    //_Potential[year] = new Potential() { Year = _AnnualClimates[year].Year, Period = (period_length * per) + 1, P = MISSING, PE = MISSING, PR = MISSING, PRO = MISSING, PL = MISSING, P_PE = MISSING };
                    //}
                    potential = pot;
            }

        }

        //-----------------------------------------------------------------------------
        //This function calculates the Thornthwaite heat index I for monthly PDSI's.
        //-----------------------------------------------------------------------------
        private static double CalcMonThornI(double[] mon_T_normal)
        {
            double I = 0;
            int i = 0; //, j = 0;
            double[] TNorm = new double[12];
            
            // Then we move the temperatures to the TNorm array and calclulate I
            for (i = 0; i < 12; i++)
            {
                //if (metric == 1)
                //    TNorm[i] = mon_T_normal[i] * (9.0 / 5.0) + 32;//TNorm[i] = t[i]*(9.0/5.0)+32;
                //else
                TNorm[i] = mon_T_normal[i];//TNorm[i]=t[i];
                // Prints the normal temperatures to the screen
                if (Verbose > 1)
                    Climate.ModelCore.UI.WriteLine("{0}", TNorm[i]);
                // Adds the modified temp to heat if the temp is above freezing
                if (TNorm[i] > 32)
                    I = I + Math.Pow((TNorm[i] - 32) / 9, 1.514);
            }
            
            return I;
        }



        //-----------------------------------------------------------------------------
        // CalcThornA calculates the Thornthwaite exponent a based on the heat index I.
        //-----------------------------------------------------------------------------
        private static double CalcThornA(double I)
        {
            double A;
            A = 6.75 * (Math.Pow(I, 3)) / 10000000 - 7.71 * (Math.Pow(I, 2)) / 100000 + 0.0179 * I + 0.49;
            return A;
        }



        //-----------------------------------------------------------------------------
        // This function reads in the 2 initializing values of Su and TLA
        //-----------------------------------------------------------------------------
        private static void SetSoilMoisture(double awc)
        {
            //double lat;
            AWC = awc;
            //double PI = 3.1415926535;

            ////the negative of the tangent of the latitude  of the station - TLA


            if (metric == 1)
                AWC = AWC / 25.4;
            if (AWC <= 0)
            {
                throw new ApplicationException("Invalid value for AWC: " + Su);
            }
            Ss = 1.0;   //assume the top soil can hold 1 inch
            if (AWC < Ss)
            {
                //always assume the top layer of soil can 
                //hold at least the Ss value of 1 inch.
                AWC = Ss;
            }
            Su = AWC - Ss;
            if (Su < 0)
                Su = 0;
        }
        private static void SetTLA()
        {
                double lat = TLA;
                double PI = 3.1415926535;

                TLA = -Math.Tan(PI * lat / 180);
                if (lat >= 0)
                {
                    if (Verbose > 1)
                        Climate.ModelCore.UI.WriteLine("TLA is positive, assuming location is in Southern Hemisphere. TLA: %f\n", TLA);
                    south = false;//0;
                }
                else
                {
                    south = true;//1;
                    TLA = -TLA;
                }

                if (Verbose > 1)
                {
                    Climate.ModelCore.UI.WriteLine("AWC = {0}  TLA = {1}", AWC, TLA);
                    Climate.ModelCore.UI.WriteLine("HEAT INDEX, THORNTHWAITE A: {0} {1}", I, A);
                }
        }



        //-----------------------------------------------------------------------------
        // This function reads in a years worth of data from file In and places those
        // values in array A.  It's been modified to average the input data to the 
        // correct time scale.  Because of this modification, it only works for 
        // temperature data; precip data must be summed, not averaged.
        //-----------------------------------------------------------------------------
        public static void GetTemp(ref double[] A, int max) //int GetTemp(FILE *In, number *A, int max) 
        {
            double[] t = new double[12], t2 = new double[12];
            double temp;
            int i, j, bad_weeks;
            //char line[4096];
            //char letter;

            for (i = 0; i < 12; i++)
                A[i] = 0;

            //for (i = 0; i < max; i++)
            //    t[i] = annClimate.MonthlyTemp[i];

            //place values read into array t2 to be summarized
            //  if(read == max+1){
            //a full year's worth of data was read
            for (i = 0; i < max; i++)
                t2[i] = t[i];

            for (i = 0; i < num_of_periods; i++)
            {
                bad_weeks = 0;
                temp = 0;
                for (j = 0; j < period_length; j++)
                {
                    if (t2[i * period_length + j] != MISSING)
                        temp += t2[i * period_length + j];
                    else
                        bad_weeks++;
                }
                if (bad_weeks < period_length)
                    A[i] = temp / (period_length - bad_weeks);
                else
                    A[i] = MISSING;
            }
            //if (metric == 1)
            //{
            //    for (i = 0; i < num_of_periods; i++)
            //    {
            //        if (A[i] != MISSING)
            //            A[i] = A[i] * (9.0 / 5.0) + 32;
            //    }
            //}

            //return year;

        }
        //-----------------------------------------------------------------------------
        // This function is a modified version of GetTemp() function for precip only.
        //-----------------------------------------------------------------------------
        private static void GetPrecip(ref double[] A, int max) 
        {
            double[] t = new double[12], t2 = new double[12];
            double temp;
            int i, j, bad_weeks;
            //  char line[4096];
            //  char letter;

            for (i = 0; i < 12; i++)
                A[i] = 0;

            //for (i = 0; i < max; i++)
            //    t[i] = annClimate.MonthlyPrecip[i];


            for (i = 0; i < max; i++)
                t2[i] = t[i];

            //now summaraize data in t2 into A
            for (i = 0; i < num_of_periods; i++)
            {
                bad_weeks = 0;
                temp = 0;
                for (j = 0; j < period_length; j++)
                {
                    if (t2[i * period_length + j] != MISSING)
                        temp += t2[i * period_length + j];
                    else
                        bad_weeks++;
                }
                if (bad_weeks < period_length)
                    A[i] = temp;
                else
                    A[i] = MISSING;
            }

            if (metric == 1)
            {
                for (i = 0; i < num_of_periods; i++)
                {
                    if (A[i] != MISSING)
                        A[i] = A[i] / 25.4;
                }
            }

            //return year;
        }

        // ------------------------------------------------
        // Calculates alpha, beta, gamma, and delta
        private static void CalcWBCoef()
        {
            // The coefficients are calculated by per
            for (int per = 0; per < num_of_periods; per++)
            {
                //calculate alpha:
                if (PESum[per] != 0.0)
                    Alpha[per] = ETSum[per] / PESum[per];
                else if (ETSum[per] == 0.0)
                    Alpha[per] = 1.0;
                else
                    Alpha[per] = 0.0;

                //calculate beta:
                if (PRSum[per] != 0.0)
                    Beta[per] = RSum[per] / PRSum[per];
                else if (RSum[per] == 0.0)
                    Beta[per] = 1.0;
                else
                    Beta[per] = 0.0;

                //calculate gamma:
                if (PROSum[per] != 0.0)
                    Gamma[per] = ROSum[per] / PROSum[per];
                else if (ROSum[per] == 0.0)
                    Gamma[per] = 1.0;
                else
                    Gamma[per] = 0.0;

                //calculate delta:
                if (PLSum[per] != 0.0)
                    Delta[per] = LSum[per] / PLSum[per];
                else
                    Delta[per] = 0.0;
            }

        }

        // Calculates the monthly departures from normal
        private static void CalcMonthlyDepartures()
        {
            int per;           // The period in question
            //int yr;           // The year in question
            double p;         // The precip for that period
            int i = 0;
            
            // These variables are used in calculating terminal outputs and are not 
            // important to the final PDSI
            double[] D_sum = new double[12];
            double[] DSAct = new double[12];
            double[] SPhat = new double[12];

            for (i = 0; i < 12; i++)
            {
                D_sum[i] = 0.0;
                DSAct[i] = 0.0;
                SPhat[i] = 0.0;
            }


            //for (int y = 0; y < _AnnualClimates.Length; y++)
            //{
                for (per = 0; per < num_of_periods; per++)
                {
                    p = potential.P[per];//scn1;
                    PE = potential.PE[per];//scn2;
                    PR = potential.PR[per];//scn3;
                    PRO = potential.PRO[per];//scn4;
                    PL = potential.PL[per];//scn5;
                    //scn6 is P - PE, which can be ignored for calculations.

                    if (p != MISSING && PE != MISSING && PR != MISSING && PRO != MISSING && PL != MISSING)
                    {
                        // Then the calculations for Phat and d are done
                        Phat = (Alpha[per] * PE) + (Beta[per] * PR) + (Gamma[per] * PRO) - (Delta[per] * PL);
                        potential.d[per] = p - Phat;//d=p - Phat;


                        /* SG 6/5/06: Need to only update statistical values when in 
                        **            user defined calibration interval. When not used
                        **            nCalibrationYears==totalyears; hence no change then
                        */

                        //if (yr >= currentCalibrationStartYear && yr <= currentCalibrationEndYear)
                        //{
                        // D_sum is the sum of the absolute values of d
                        // and is used to find D
                        // D_sum[per] += abs(d);
                        if (potential.d[per] < 0.0)
                            D_sum[per] += -(potential.d[per]);
                        else
                            D_sum[per] += potential.d[per];


                        // The statistical values are updated
                        DSAct[per] += potential.d[per];
                        DSSqr[per] += potential.d[per] * potential.d[per];
                        SPhat[per] += Phat;
                    }
                    else
                    {
                        potential.d[per] = MISSING;//d = MISSING;
                    }
                }

            //}
            // If the user specifies, the various sums are output to the screen
            if (Verbose > 1)
            {
                Climate.ModelCore.UI.WriteLine("\n{0} CHECK SUMS OF ESTIMATED VARIABLES\n\n", ' ');
                Climate.ModelCore.UI.WriteLine("{0} {1} {2} {3}", "PER", "SCET", "SCR", "SCRO");
                Climate.ModelCore.UI.WriteLine("{0} {1} {2}\n\n", "SCL", "SCP", "SCD");
            }
            for (i = 0; i < num_of_periods; i++)
            {
                if (Verbose > 1)
                {
                    Climate.ModelCore.UI.WriteLine("{0}{1}{2}", (period_length * i) + 1, Alpha[i] * PESum[i], Beta[i] * PRSum[i]);
                    Climate.ModelCore.UI.WriteLine("{0}{1}", Gamma[i] * PROSum[i], Delta[i] * PLSum[i]);
                    Climate.ModelCore.UI.WriteLine("{0}{1}\n", SPhat[i], DSAct[i]);
                }
                // D becomes the mean of D_sum
                /* SG 6/5/06: changed totalyears to nCalibrationYears to support
                **            user defined calibration intervals. When not used
                **            nCalibrationYears==totalyears; hence no change then
                */

                D[i] = D_sum[i] / 1; // totalyears;//111// nCalibrationYears;
            }
        }



        //Computes the K and Z values.  CalcX is called within CalcK.
        private static void CalcOrigK()
        {
            int month; //, year;
            double sums;        //used to calc k
            //float dtemp;
            DKSum = 0;

            // Calculate k, which is K', or Palmer's second approximation of K
            for (int per = 0; per < num_of_periods; per++)
            {
                if (PSum[per] + LSum[per] == 0)
                    sums = 0;//prevent div by 0
                else
                    sums = (PESum[per] + RSum[per] + ROSum[per]) / (PSum[per] + LSum[per]);

                if (D[per] == 0)
                    k[per] = 0.5;//prevent div by 0
                else
                    k[per] = (1.5) * Math.Log10((sums + 2.8) / D[per]) + 0.5;

                DKSum += D[per] * k[per];
            }

            // Set duration factors to Palmer's original duration factors
            drym = .309;
            dryb = 2.691;
            wetm = drym;
            wetb = dryb;


            // Initializes the book keeping indices used in finding the PDSI
            Prob = 0.0;
            X1 = 0.0;
            X2 = 0.0;
            X3 = 0.0;
            //X = 0.0;
            V = 0.0;
            Q = 0.0;

            // Reads in all previously calclulated d values and calculates Z
            // then calls CalcX to compute the corresponding PDSI value

            for (int per = 0; per < num_of_periods; per++)
                {
                    month = per; //Since we assumed each period is one month
                    //                PeriodList.insert(month);
                    //                YearList.insert(year);

                    d = potential.d[per];//d = dtemp;
                    //                month--; //In this code the month is the indes of month ie. 0..11 for a year 
                    K = (17.67 / DKSum) * k[month];
                    if (d != MISSING)
                        Z = d * K;
                    else
                        Z = MISSING;

                    //                ZIND.insert(Z);
                    //CalcOneX(table, month, year);
                    CalcOnePDSI(month);
            }

        }


        //-----------------------------------------------------------------------------
        // This function calculates X, X1, X2, and X3
        //
        // X1 = severity index of a wet spell that is becoming "established"
        // X2 = severity index of a dry spell that is becoming "established"
        // X3 = severity index of any spell that is already "established"
        //
        // newPDSI is the name given to the pdsi value for the current month.
        // newPDSI will be one of X1, X2 and X3 depending on what the current 
        // spell is, or if there is an established spell at all.
        //-----------------------------------------------------------------------------
        private static void CalcOnePDSI(int month)//, int yearIndex)
        {

            double newV;    //These variables represent the values for 
            double newProb; //corresponding variables for the current period.
            //amin: I assigned 0 to avoid unassigend variable error
            double newPDSI = 0;  //They are kept seperate because many calculations
            double newX1 = 0;  //depend on last period's values.  
            double newX2 = 0;
            double newX3;
            double ZE;      //ZE is the Z value needed to end an established spell

            double m, b, c;

            int wd;        //wd is a sign changing flag.  It allows for use of the same
            //equations during both a wet or dry spell by adjusting the
            //appropriate signs.

            if (X3 >= 0)  // Seems unlikely given the above.
            {
                m = wetm;
                b = wetb;
            }
            else
            {
                m = drym;
                b = dryb;
            }
            c = 1 - (m / (m + b));


            if (Z != MISSING)
            {
                // This sets the wd flag by looking at X3
                if (X3 >= 0) wd = 1;
                else wd = -1;
                // If X3 is 0 then there is no reason to calculate Q or ZE, V and Prob
                // are reset to 0;
                if (X3 == 0)
                {
                    newX3 = 0;
                    newV = 0;
                    newProb = 0;
                    ChooseX(ref newPDSI, ref newX1, ref newX2, ref newX3, bug);
                }
                // Otherwise all calculations are needed.
                else
                {
                    newX3 = (c * X3 + Z / (m + b));
                    ZE = (m + b) * (wd * 0.5 - c * X3);
                    Q = ZE + V;
                    newV = Z - wd * (m * 0.5) + wd * Math.Min(wd * V + tolerance, 0);

                    if ((wd * newV) > 0)
                    {
                        newV = 0;
                        newProb = 0;
                        newX1 = 0;
                        newX2 = 0;
                        newPDSI = newX3;
                        while (altX1.Count > 0)//(!altX1.is_empty())
                            altX1.RemoveFirst();//altX1.head_remove();
                        while (altX2.Count > 0)//(!altX2.is_empty())
                            altX2.RemoveFirst();//altX2.head_remove();
                    }
                    else
                    {
                        newProb = (newV / Q) * 100;
                        if (newProb >= 100 - tolerance)
                        {
                            newX3 = 0;
                            newV = 0;
                            newProb = 100;
                        }
                        ChooseX(ref newPDSI, ref newX1, ref newX2, ref newX3, bug);
                    }
                }
                
                //update variables for next month:
                V = newV;
                Prob = newProb;
                X1 = newX1;
                X2 = newX2;
                X3 = newX3;

                PDSI_Monthly[month] = newPDSI;

                //-->
                //add newX to the list of pdsi values
                //if(month == 0)
                //    XDic.Add(yearIndex,new double[12]);
                //((double[])XDic[yearIndex])[month] = newX;

                //Xlist.AddFirst(newX);////Xlist.insert(newX);
                //XL1.AddFirst(X1);//XL1.insert(X1);
                //XL2.AddFirst(X2);//XL2.insert(X2);
                //XL3.AddFirst(X3);//XL3.insert(X3);
                //ProbL.AddFirst(Prob);//ProbL.insert(Prob);

                //Xlist.AddFirst(newX);//Xlist.insert(newX);
                //XL1.AddFirst(X1);//XL1.insert(X1);
                //XL2.AddFirst(X2);//XL2.insert(X2);
                //XL3.AddFirst(X3);//XL3.insert(X3);
                //ProbL.AddFirst(Prob);//ProbL.insert(Prob);
            }
            else
            {
                throw new ApplicationException (String.Format("Z-Value not correctly calculated for month = ", month));
                
                //This month's data is missing, so output MISSING as PDSI.  
                //All variables used in calculating the PDSI are kept from 
                //the previous month.  Only the linked lists are changed to make
                //sure that if backtracking occurs, a MISSING value is kept 
                //as the PDSI for this month.

                //if (month == 0)
                //    XDic.Add(yearIndex, new double[12]);
                //((double[])XDic[yearIndex])[month] = MISSING;

                //Xlist.AddFirst(MISSING);////Xlist.insert(newX);

                //XL1.AddFirst(MISSING);//XL1.insert(MISSING);
                //XL2.AddFirst(MISSING);//XL2.insert(MISSING);
                //XL3.AddFirst(MISSING);//XL3.insert(MISSING);
                //ProbL.AddFirst(MISSING);//ProbL.insert(MISSING);
            }

        }


        private static void ChooseX(ref double newX, ref double newX1, ref double newX2, ref double newX3, int bug)
        {
            double m, b;
            double wetc, dryc;

            if (X3 >= 0)
            {
                m = wetm;
                b = wetb;
            }
            else
            {
                m = drym;
                b = dryb;
            }

            wetc = 1 - (wetm / (wetm + wetb));
            dryc = 1 - (drym / (drym + wetb));

            newX1 = (wetc * X1 + Z / (wetm + wetb));
            if (newX1 < 0)
                newX1 = 0;
            newX2 = X2;

            if (bug == 0) // bug is always 0
            {
                newX2 = (dryc * X2 + Z / (drym + dryb));
                if (newX2 > 0)
                    newX2 = 0;
            }

            if ((newX1 >= 0.5) && (newX3 == 0))
            {
                Backtrack(newX1, newX2);
                newX = newX1;
                newX3 = newX1;
                newX1 = 0;
            }
            else
            {
                newX2 = (dryc * X2 + Z / (drym + dryb));
                if (newX2 > 0)
                    newX2 = 0;

                if ((newX2 <= -0.5) && (newX3 == 0))
                {
                    Backtrack(newX2, newX1);
                    newX = newX2;
                    newX3 = newX2;
                    newX2 = 0;
                }
                else if (newX3 == 0)
                {
                    if (newX1 == 0)
                    {
                        Backtrack(newX2, newX1);
                        newX = newX2;
                    }
                    else if (newX2 == 0)
                    {
                        Backtrack(newX1, newX2);
                        newX = newX1;
                    }
                    else
                    {
                        altX1.AddFirst(newX1);//altX1.insert(newX1);
                        altX2.AddFirst(newX2);//altX2.insert(newX2);
                        newX = newX3;
                    }
                }

                else
                {
                    //store X1 and X2 in their linked lists for possible use later
                    altX1.AddFirst(newX1);//altX1.insert(newX1);
                    altX2.AddFirst(newX2);//altX2.insert(newX2);
                    newX = newX3;
                }
            }
        }//end of chooseX

        private static void Backtrack(double X1, double X2)
        {
            double num1, num2;
            LinkedListNode<double> ptr = null;
            num1 = X1;
            while (altX1.Count > 0 && altX2.Count > 0)
            {
                if (num1 > 0)
                {
                    num1 = altX1.First(); altX1.RemoveFirst(); //num1=altX1.head_remove();
                    num2 = altX2.First(); altX2.RemoveLast(); //num2 = altX2.head_remove();
                }
                else
                {
                    num1 = altX2.Last(); altX2.RemoveLast(); //num1=altX2.head_remove();
                    num2 = altX1.Last(); altX1.RemoveLast(); //num2=altX1.head_remove();
                }
                if (-tolerance <= num1 && num1 <= tolerance)
                    num1 = num2;
                ptr = Xlist.SetNode<double>(num1, ptr);
                //Xlist = ptr.List;
            }
        }//end of backtrack()
    }
    //---------------------------
    class Potential
    {
        public Potential()
        {
            Period = new double[12];
            P = new double[12];
            PE = new double[12];
            PR = new double[12];
            PRO = new double[12];
            PL = new double[12];
            P_PE = new double[12];
            d = new double[12];
            x = new double[12];
        }
        public int Year { get; set; }
        public double[] Period { get; set; }
        public double[] P { get; set; }
        public double[] PE { get; set; }
        public double[] PR { get; set; }
        public double[] PRO { get; set; }
        public double[] PL { get; set; }
        public double[] P_PE { get; set; }

        public double[] d { get; set; }

        public double[] x { get; set; }
    }
}


namespace Landis.Library.Climate
{
    using System.Collections.Generic;
    public static class LinkedListExtentions
    {
        public static LinkedListNode<T> SetNode<T>(this LinkedList<T> linkedList, T value, LinkedListNode<T> set = null)
        {
            int error = 1;
            LinkedListNode<T> comparer;//node* comparer;

            if (set == null)
                set = linkedList.First;//set = head->next;
            comparer = linkedList.First;
            while (comparer != null)//while(comparer != head)
            {
                if (comparer == set)
                {
                    error = 0;
                    break;
                }
                comparer = comparer.Next;//comparer = comparer->next;
            }

            if (error == 1)
            {
                return null;
            }
            else
            {
                if (Convert.ToDouble(set.Value) != PDSI_Calculator.MISSING)
                {
                    set.Value = value;//set->key = x;
                    return set.Next;//return set->next;
                }
                else
                {
                    //if the node is MISSING, then don't replace
                    //that key.  instead, replace the first non-MISSING
                    //node you come to.
                    return SetNode(linkedList, value, set.Next);//return set_node(set->next, x);
                }
            }

            //set.Value = value;
            //return set;
        }
    }
}