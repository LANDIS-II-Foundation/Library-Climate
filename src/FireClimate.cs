//  Authors:  Amin Almassian, Robert M. Scheller, John McNabb, Melissa Lucash, Vincent Schuster
// Derived from 
// Forestry Canada Fire Danger Group 1992. Development and structure of
// the Canadian Forest Fire Behavior Prediction System. Forestry
// Canada, Science and Sustainable Development Directorate,
// Information Report ST-X-3, Ottawa, Ontario, Canada.
// 
// Van Wagner, C. E. 1987. Development and structure of the Canadian
// Forest Fire Weather Index System. Canadian Forest Service, Ottawa,
// Ontario. Forestry Technical Report 35.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Landis.Core;

namespace Landis.Library.Climate
{
    public static class FireClimate
    {
        //public static bool UsingFireClimate = false;


        //private static double RHSlopeAdjust;
        //private static int SpringStart;
        //private static int WinterStart;
        //private static double FireWeatherIndex;
        //private static double FineFuelMoistureCode;
        //private static double DuffMoistureCode;
        //private static double DroughtCode;
        //private static double BuildUpIndex;
        private static double WindSpeedVelocity;
        private static double WindAzimuth;

        //private static double[] _fireWeatherIndex;
        private static double[] _fineFuelMoistureCode;
        private static double[] _duffMoistureCode;
        private static double[] _droughtCode;
        //private static double[] _buildUpIndex;


        public static void CalculateFireWeather(int year, ClimateRecord[][] TimestepData)
        {
            double fineFuelMoistureCodeYesterday, duffMoistureCodeYesterday, droughtCodeYesterday;
            var springStart = Climate.ConfigParameters.SpringStart;
            var winterStart = Climate.ConfigParameters.WinterStart;

            int maxtimestep = 12;
            if (Climate.AllData_granularity == TemporalGranularity.Daily)
                maxtimestep = 365;
            WindSpeedVelocity = -9999.0;
            WindAzimuth = -9999.0;
            var temperature = -9999.0;
            var precipitation = -9999.0;
            var relativeHumidity = -9999.0;

            // initialize arrays to save ecoregion data
            if (_fineFuelMoistureCode == null)
            {
                _fineFuelMoistureCode = new double[Climate.ModelCore.Ecoregions.Count];
                _duffMoistureCode = new double[Climate.ModelCore.Ecoregions.Count];
                _droughtCode = new double[Climate.ModelCore.Ecoregions.Count];
            }

            foreach (IEcoregion ecoregion in Climate.ModelCore.Ecoregions)
            {
                if (ecoregion.Active)
                {

                    // These are seed values for the beginning of the fire season                    
                    //double FineFuelMoistureCode_yesterday = 85; 
                    //double FineFuelMoistureCode_yesterday = 88;  //AK value                    
                    //double DuffMoistureCode_yesterday = 6;  
                    //double DuffMoistureCode_yesterday = 59;  //AK value                    
                    //double DroughtCode_yesterday = 15; 
                    //double DroughtCode_yesterday = 136;  //AK value
                    //for (int month = 0; month < 12; month++)
                    for (int timestep = 0; timestep < maxtimestep; timestep++)
                    {
                        if (timestep >= springStart && timestep < winterStart)
                        {
                            //temperature = (TimestepData[ecoregion.Index][timestep].AvgMaxTemp + TimestepData[ecoregion.Index][timestep].AvgMinTemp) / 2;
                            temperature = TimestepData[ecoregion.Index][timestep].Temp == -99.0 ? (TimestepData[ecoregion.Index][timestep].AvgMinTemp + TimestepData[ecoregion.Index][timestep].AvgMaxTemp) / 2.0 : TimestepData[ecoregion.Index][timestep].Temp;
                            precipitation = TimestepData[ecoregion.Index][timestep].AvgPpt;
                            WindSpeedVelocity = TimestepData[ecoregion.Index][timestep].AvgWindSpeed;
                            WindAzimuth = TimestepData[ecoregion.Index][timestep].AvgWindDirection;
                            //relativeHumidity = (TimestepData[ecoregion.Index][timestep].AvgMaxRH + TimestepData[ecoregion.Index][timestep].AvgMinRH) / 2;
                            //relativeHumidity = TimestepData[ecoregion.Index][timestep].AvgRH;
                            if (TimestepData[ecoregion.Index][timestep].AvgMinRH != -99.0)
                                relativeHumidity = (TimestepData[ecoregion.Index][timestep].AvgMinRH + TimestepData[ecoregion.Index][timestep].AvgMaxRH) / 2.0;   // if minRH exists, then estimate as the average of min and max  
                            else if (TimestepData[ecoregion.Index][timestep].AvgSpecificHumidity != -99.0)
                                relativeHumidity = AnnualClimate_Daily.ConvertSHtoRH(TimestepData[ecoregion.Index][timestep].AvgSpecificHumidity, temperature);                                   // if specific humidity is present, then use it to calculate RH.
                            else
                                relativeHumidity = -99.0;


                            if (relativeHumidity > 100)
                            {
                                relativeHumidity = 100.0;
                            }

                            if (timestep != springStart) //for each day, this loop assigns yesterday's fire weather variables
                            {
                                fineFuelMoistureCodeYesterday = _fineFuelMoistureCode[ecoregion.Index];
                                duffMoistureCodeYesterday = _duffMoistureCode[ecoregion.Index];
                                droughtCodeYesterday = _droughtCode[ecoregion.Index];
                            }
                            else
                            {
                                fineFuelMoistureCodeYesterday = Climate.ConfigParameters.FineFuelMoistureCode_Yesterday;
                                duffMoistureCodeYesterday = Climate.ConfigParameters.DuffMoistureCode_Yesterday;
                                droughtCodeYesterday = Climate.ConfigParameters.DroughtCode_Yesterday;
                            }

                            double mo = Calculate_mo(fineFuelMoistureCodeYesterday);
                            double rf = Calculate_rf(precipitation);
                            double mr = Calculate_mr(mo, rf);
                            double Ed = Calculate_Ed(relativeHumidity, temperature);
                            double Ew = Calculate_Ew(relativeHumidity, temperature);
                            double ko = Calculate_ko(relativeHumidity, WindSpeedVelocity);
                            double kd = Calculate_kd(ko, temperature);
                            double kl = Calculate_kl(relativeHumidity, WindSpeedVelocity);
                            double kw = Calculate_kw(kl, temperature);
                            double m = Calculate_m(mo, Ed, kd, Ew, kw);
                            double re = Calculate_re(precipitation);
                            double Mo = Calculate_Mo(duffMoistureCodeYesterday);
                            double b = Calculate_b(duffMoistureCodeYesterday);
                            double Mr = Calculate_Mr(re, b, Mo);
                            double Pr = Calculate_Pr(Mr);
                            int month = Calculate_month(timestep);
                            double Le1 = Calculate_Le1(month);
                            double Le2 = Calculate_Le2(month);
                            double Le = Calculate_Le(Le1, Le2);
                            double K = Calculate_K(temperature, relativeHumidity, Le);
                            _duffMoistureCode[ecoregion.Index] = Calculate_DuffMoistureCode(precipitation, Pr, K, duffMoistureCodeYesterday);
                            double rd = Calculate_rd(precipitation);
                            double Qo = Calculate_Qo(droughtCodeYesterday);
                            double Qr = Calculate_Qr(Qo, rd);
                            double Dr = Calculate_Dr(Qr);
                            double Lf = Calculate_Lf(month);
                            double V = Calculate_V(temperature, Lf);
                            _droughtCode[ecoregion.Index] = Calculate_DroughtCode(precipitation, Dr, V, droughtCodeYesterday);
                            double WindFunction_ISI = Calculate_WindFunction_ISI(WindSpeedVelocity);
                            double FineFuelMoistureFunction_ISI = Calculate_FineFuelMoistureFunction_ISI(m);
                            double InitialSpreadIndex = Calculate_InitialSpreadIndex(WindFunction_ISI, FineFuelMoistureFunction_ISI);
                            var buildUpIndex = Calculate_BuildUpIndex(_duffMoistureCode[ecoregion.Index], _droughtCode[ecoregion.Index]);
                            double fD = Calculate_fD(buildUpIndex);
                            double B = Calculate_B(InitialSpreadIndex, fD);
                            var fireWeatherIndex = Calculate_FireWeatherIndex(B);
                            double I_scale = Calculate_I_scale(fireWeatherIndex);
                            double DSR = Calculate_DSR(fireWeatherIndex);
                            _fineFuelMoistureCode[ecoregion.Index] = Calculate_FineFuelMoistureCode(m);

                            TimestepData[ecoregion.Index][timestep].DuffMoistureCode = _duffMoistureCode[ecoregion.Index];
                            TimestepData[ecoregion.Index][timestep].DroughtCode = _droughtCode[ecoregion.Index];
                            TimestepData[ecoregion.Index][timestep].BuildUpIndex = buildUpIndex;
                            TimestepData[ecoregion.Index][timestep].FineFuelMoistureCode = _fineFuelMoistureCode[ecoregion.Index];
                            TimestepData[ecoregion.Index][timestep].AvgFWI = fireWeatherIndex;
                        }
                        else
                        {
                            TimestepData[ecoregion.Index][timestep].AvgFWI = 0;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculations for CalculateFireWeather(double, int, int, int, ClimateRecord[][])
        /// </summary>
        private static void CheckData(double temperature, double precipitation, double relative_humidity)
        {
            if (WindSpeedVelocity == -9999.0)
            {
                throw new UninitializedClimateData("WindSpeedVelocity");
            }
            else if (WindAzimuth == -9999.0)
            {
                throw new UninitializedClimateData("WindAzimuth");
            }
            else if (temperature == -9999.0)
            {
                throw new UninitializedClimateData("temperature");
            }
            else if (relative_humidity == -9999.0)
            {
                throw new UninitializedClimateData("relative_humidity");
            }
            else if (precipitation == -9999.0)
            {
                throw new UninitializedClimateData("precipitation");
            }
        }

        private static double Calculate_mo(double FineFuelMoistureCode_yesterday)
        {
            double mo = 0;
            try
            {
                mo = 147.2 * (101.0 - FineFuelMoistureCode_yesterday) / (59.5 + FineFuelMoistureCode_yesterday);  //This used to be an explicit seed value for FFMC
            }
            catch(Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }
            

            return mo;
        }

        private static double Calculate_rf(double precipitation)
        {
            double rf = 0.0;

            try
            {
                rf = precipitation - 0.5;

                if (rf < 0)
                {
                    rf = 0.0;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return rf;
        }

        private static double Calculate_mr(double mo, double rf)
        {
            double mr = 0.0;
            try
            {
                if (mo <= 150.0)
                {

                    if (rf > 0)
                    {
                        mr = mo + 42.5 * rf * Math.Exp(-100.0 / (251.0 - mo)) * (1 - Math.Exp(-6.93 / rf));
                    }
                    else
                    {
                        mr = mo;
                    }
                }
                else
                {
                    if (rf > 0)
                    {
                        mr = mo + 42.5 * rf * Math.Exp(-100.0 / (251.0 - mo)) * (1 - Math.Exp(-6.93 / rf)) + 0.0015 * Math.Pow((mo - 150.0), 2) * Math.Pow(rf, 0.5);
                    }
                    else
                    {
                        mr = mo;
                    }
                }

                if (mr > 250)
                {
                    mr = 250;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return mr;
        }

        private static double Calculate_Ed(double relative_humidity, double temperature)
        {
            double Ed = 0.0;
            try
            {
                Ed = 0.942 * Math.Pow(relative_humidity, 0.679) + 11.0 * Math.Exp((relative_humidity - 100.0) / 10.0) + 0.18 * (21.1 - temperature) * (1.0 - Math.Exp(-0.115 * relative_humidity));
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Ed;
        }

        private static double Calculate_Ew(double relative_humidity, double temperature)
        {
            double Ew = 0.0;
            try
            {
                Ew = 0.618 * Math.Pow(relative_humidity, 0.753) + 10.0 * Math.Exp((relative_humidity - 100.0) / 10.0) + 0.18 * (21.1 - temperature) * (1.0 - Math.Exp(-0.115 * relative_humidity));                          //selfs
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Ew;
        }

        private static double Calculate_ko(double relative_humidity, double WindSpeedVelocity)
        {
            double ko = 0.0;
            try
            {
                ko = 0.424 * (1.0 - Math.Pow((relative_humidity / 100.0), 1.7)) + 0.0694 * Math.Pow(WindSpeedVelocity, 0.5) * (1.0 - Math.Pow((relative_humidity / 100.0), 8));
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return ko;
        }

        private static double Calculate_kd(double ko, double temperature)
        {
            double kd = 0.0;
            try
            {

                kd = ko * 0.581 * Math.Exp(0.0365 * temperature);
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return kd;
        }

        private static double Calculate_kl(double relative_humidity, double WindSpeedVelocity)
        {
            double kl = 0.0;
            try
            {
                kl = 0.424 * (1.0 - Math.Pow(((100.0 - relative_humidity) / 100.0), 1.7)) + 0.0694 * Math.Pow(WindSpeedVelocity, 0.5) * (1.0 - Math.Pow(((100.0 - relative_humidity) / 100.0), 8));
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return kl;
        }

        private static double Calculate_kw(double kl, double temperature)
        {
            double kw = 0.0;
            try
            {
                kw = kl * 0.581 * Math.Exp(0.0365 * temperature);
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return kw;
        }

        private static double Calculate_m(double mo, double Ed, double kd, double Ew, double kw)
        {
            double m = 0.0;
            try
            {
                if (mo > Ed)
                {
                    m = Ed + (mo - Ed) * Math.Pow(10.0, (-kd));
                }
                else
                {
                    if (mo < Ed)
                    {
                        if (mo < Ew)
                        {
                            m = Ew - (Ew - mo) * Math.Pow(10.0, (-kw));
                        }
                        else
                        {
                            m = mo;
                        }
                    }
                    else
                    {
                        m = mo;
                    }
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return m;
        }

        private static double Calculate_FineFuelMoistureCode(double m)
        {
            try
            {
                var fineFuelMoistureCode = 59.5 * (250.0 - m) / (147.2 + m);

                if (fineFuelMoistureCode > 100.0)
                {
                    fineFuelMoistureCode = 100.0;
                }

                return fineFuelMoistureCode;
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }
        }

        private static double Calculate_re(double precipitation)
        {
            double re = 0.0;
            try
            {
                if (precipitation > 1.5)
                {
                    re = 0.92 * precipitation - 1.27;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return re;
        }

        private static double Calculate_Mo(double DuffMoistureCode_yesterday)
        {
            double Mo = 0.0;
            try
            {
                Mo = 20.0 + Math.Exp(5.6348 - DuffMoistureCode_yesterday / 43.43);
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Mo;
        }

        private static double Calculate_b(double DuffMoistureCode_yesterday) //, int DMC_start
        {
            double b = 0.0;

            try
            {
                if (DuffMoistureCode_yesterday <= 33)
                {
                    b = 100 / (0.5 + 0.3 * DuffMoistureCode_yesterday);
                }

                else if (DuffMoistureCode_yesterday > 65)
                {
                    b = 6.2 * Math.Log(DuffMoistureCode_yesterday) - 17.2;
                }

                else
                {
                    b = 14.0 - 1.3 * Math.Log(DuffMoistureCode_yesterday);
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return b;
        }

        private static double Calculate_Mr(double re, double b, double Mo)
        {
            double Mr = 0.0;
            try
            {
                Mr = Mo + 1000.0 * re / (48.77 + b * re);
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Mr;
        }

        private static double Calculate_Pr(double Mr)
        {
            double Pr = 0.0;
            /* VS: Why the if statement
            if (day == 91)
            {
                Pr = 244.72 - 43.43 * Math.Log(Mr - 20.0);
                if (Pr < 0.0)
                {
                    Pr = 0.0;
                }
            }
            else
            {
               Pr = 244.72 - 43.43 * Math.Log(Mr - 20.0);

                if (Pr < 0.0)
                {
                    Pr = 0.0;
                }
            }
            */

            try
            {
                Pr = 244.72 - 43.43 * Math.Log(Mr - 20.0);
                if (Pr < 0.0)
                {
                    Pr = 0.0;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Pr;
        }

        private static int Calculate_month(int d)
        {
            int month = 0;

            try
            {
                if (d <= 31)
                {
                    month = 1;
                }
                else if (d > 31 && d <= 60)
                {
                    month = 2;
                }
                else if (d > 60 && d <= 91)
                {
                    month = 3;
                }
                else if (d > 91 && d <= 121)
                {
                    month = 4;
                }
                else if (d > 121 && d <= 152)
                {
                    month = 5;
                }
                else if (d > 152 && d <= 182)
                {
                    month = 6;
                }
                else if (d > 182 && d <= 213)
                {
                    month = 7;
                }
                else if (d > 213 && d <= 244)
                {
                    month = 8;
                }
                else if (d > 244 && d <= 274)
                {
                    month = 9;
                }
                else if (d > 274 && d <= 305)
                {
                    month = 10;
                }
                else if (d > 305 && d <= 335)
                {
                    month = 11;
                }

                else
                {
                    month = 12;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return month;
        }

        private static double Calculate_Le1(int month)
        {
            double Le1 = 0.0;

            try
            {
                if (month == 1)
                {
                    Le1 = 6.5;
                }
                else if (month == 2)
                {
                    Le1 = 7.5;
                }
                else if (month == 3)
                {
                    Le1 = 9.0;
                }
                else if (month == 4)
                {
                    Le1 = 12.8;
                }
                else if (month == 5)
                {
                    Le1 = 13.9;
                }
                else if (month == 6)
                {
                    Le1 = 13.9;
                }
                else if (month == 7)
                {
                    Le1 = 12.4;
                }
                else if (month == 8)
                {
                    Le1 = 10.9;
                }
                else
                {
                    Le1 = 0.0;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Le1;
        }

        private static double Calculate_Le2(int month)
        {
            double Le2 = 0.0;

            try
            {
                if (month == 9)
                {
                    Le2 = 9.2;
                }
                else if (month == 10)
                {
                    Le2 = 8.0;
                }
                else if (month == 11)
                {
                    Le2 = 7.0;
                }
                else if (month == 12)
                {
                    Le2 = 6.0;
                }

                else
                {
                    Le2 = 0.0;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Le2;
        }

        private static double Calculate_Le(double Le1, double Le2)
        {
            double Le = 0.0;

            try
            {
                if (Le1 == 0.0)
                {
                    Le = Le2;
                }
                else
                {
                    Le = Le1;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Le;
        }

        private static double Calculate_K(double temperature, double relative_humidity, double Le)
        {
            double K = 0.0;

            try
            {
                if (temperature < -1.1)
                {
                    K = 0.0;
                }

                else
                {
                    K = 1.894 * (temperature + 1.1) * (100.0 - relative_humidity) * Le * Math.Pow(10.0, -6.0);
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return K;
        }

        private static double Calculate_DuffMoistureCode(double precipitation, double Pr, double K, double DuffMoistureCode_yesterday) //int spring_start, int winter_start, double DMC_start
        {
            try
            {
                return precipitation > 1.5 ? Pr + 100.0 * K : DuffMoistureCode_yesterday + 100.0 * K;
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }
        }

        private static double Calculate_rd(double precipitation)
        {
            double rd = 0.0;

            try
            {
                if (precipitation > 2.8)
                {
                    rd = 0.83 * precipitation - 1.27;
                }

                else
                {
                    rd = 0;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return rd;
        }

        private static double Calculate_Qo(double DroughtCode_yesterday) //, int DC_start
        {
            double Qo = 0.0;

            try
            {
                Qo = 800.0 * Math.Exp(-DroughtCode_yesterday / 400.0);
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Qo;
        }

        private static double Calculate_Qr(double Qo, double rd)
        {
            double Qr = 0.0;

            try
            {
                Qr = Qo + 3.937 * rd;
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Qr;
        }

        private static double Calculate_Dr(double Qr)
        {
            double Dr = 0.0;

            try
            {
                Dr = 400.0 * Math.Log(800.0 / Qr);

                if (Dr < 0)
                {
                    Dr = 0.0;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Dr;
        }

        private static double Calculate_Lf(int month)
        {
            double Lf = 0.0;
            try
            {

                if (month <= 3)
                {
                    Lf = 1.6;
                }
                else if (month == 4.0)
                {
                    Lf = 0.9;
                }
                else if (month == 5.0)
                {
                    Lf = 3.8;
                }
                else if (month == 6.0)
                {
                    Lf = 5.8;
                }
                else if (month == 7.0)
                {
                    Lf = 6.4;
                }
                else if (month == 8.0)
                {
                    Lf = 5.0;
                }
                else if (month == 9.0)
                {
                    Lf = 2.4;
                }
                else if (month == 10.0)
                {
                    Lf = 0.4;
                }
                else
                {
                    Lf = -1.6;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return Lf;
        }

        private static double Calculate_V(double temperature, double Lf)
        {
            double V = 0.0;

            try
            {
                if (temperature < -2.8)
                {
                    V = 0.36 * (-2.8 + 2.8) + Lf;
                }
                else
                {
                    V = 0.36 * (temperature + 2.8) + Lf;
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return V;
        }

        private static double Calculate_DroughtCode(double precipitation, double Dr, double V, double droughtCodeYesterday)
        {

            try
            {
                return precipitation > 2.8 ? Dr + 0.5 * V : droughtCodeYesterday + 0.5 * V;
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }
        }

        private static double Calculate_WindFunction_ISI(double WindSpeedVelocity)
        {
            double WindFunction_ISI = 0.0;
            try
            {

                WindFunction_ISI = Math.Exp(0.05039 * WindSpeedVelocity);
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return WindFunction_ISI;
        }

        private static double Calculate_FineFuelMoistureFunction_ISI(double m)
        {
            double FineFuelMoistureFunction_ISI = 0.0;

            try
            {
                FineFuelMoistureFunction_ISI = 91.9 * Math.Exp(-0.1386 * m) * (1.0 + Math.Pow(m, 5.31) / (4.93 * Math.Pow(10.0, 7.0)));

            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }
            return FineFuelMoistureFunction_ISI;
        }

        private static double Calculate_InitialSpreadIndex(double WindFunction_ISI, double FineFuelMoistureFunction_ISI)
        {
            double InitialSpreadIndex = 0.0;

            try
            {
                InitialSpreadIndex = 0.208 * WindFunction_ISI * FineFuelMoistureFunction_ISI;
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return InitialSpreadIndex;
        }

        private static double Calculate_BuildUpIndex(double DuffMoistureCode, double DroughtCode)  //int spring_start, int winter_start,
        {
            try
            {
                if (DuffMoistureCode <= (0.4 * DroughtCode))
                {
                    return 0.8 * DuffMoistureCode * DroughtCode / (DuffMoistureCode + 0.4 * DroughtCode);
                }
                else
                {
                    return DuffMoistureCode - (1.0 - 0.8 * DroughtCode / (DuffMoistureCode + 0.4 * DroughtCode)) * (0.92 + (0.0114 * Math.Pow(DuffMoistureCode, 1.7)));
                }
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }
        }

        private static double Calculate_fD(double BuildUpIndex)
        {
            double fD = 0.0;

            try
            {
                if (BuildUpIndex <= 80.0)
                {
                    fD = 0.626 * Math.Pow(BuildUpIndex, 0.809) + 2.0;
                }
                else
                {
                    fD = 1000.0 / (25.0 + 108.64 * Math.Exp(-0.023 * BuildUpIndex));
                }

            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }
            return fD;
        }

        private static double Calculate_B(double InitialSpreadIndex, double fD)
        {
            double B = 0.0;

            try
            {
                B = 0.1 * InitialSpreadIndex * fD;
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return B;
        }

        private static double Calculate_FireWeatherIndex(double B) //int spring_start, int winter_start, 
        {
            try
            {
                return B > 1.0 ? Math.Exp(2.72 * Math.Pow((0.434 * Math.Log(B)), 0.647)) : B;
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }
        }

        private static double Calculate_I_scale(double FireWeatherIndex)
        {
            double I_scale = 0.0;

            try
            {
                I_scale = (1.0 / 0.289) * (Math.Exp(0.98 * (Math.Pow(Math.Log(FireWeatherIndex), 1.546))));
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return I_scale;
        }

        private static double Calculate_DSR(double FireWeatherIndex)
        {
            double DSR = 0.0;

            try
            {
                DSR = 0.0272 * Math.Pow(FireWeatherIndex, 1.77);
            }
            catch (Exception ex)
            {
                // Fetch the name of the function
                string meathodName = ex.TargetSite?.Name;
                throw new FireWeatherCalculationException(meathodName);
            }

            return DSR;
        }
    }
}
