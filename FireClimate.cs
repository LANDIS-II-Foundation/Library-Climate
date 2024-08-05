using System;
using System.Collections.Generic;

namespace Landis.Library.Climate
{
    public partial class Climate
    {
        #region fields

        private static readonly double[] Lf = { 1.6, 1.6, 1.6, 0.9, 3.8, 5.8, 6.4, 5.0, 2.4, 0.4, -1.6, -1.6 };
        private static readonly double[] Le = { 6.5, 7.5, 9.0, 12.8, 13.9, 13.9, 12.4, 10.9, 9.2, 8.0, 7.0, 6.0 };

        #endregion

        #region private methods

        private static void CalculateDailyFireWeather(List<ClimateRecord> yearRecords)
        {
            double fineFuelMoistureCodeYesterday = ConfigParameters.FineFuelMoistureCode_Yesterday;
            double duffMoistureCodeYesterday = ConfigParameters.DuffMoistureCode_Yesterday;
            double droughtCodeYesterday = ConfigParameters.DroughtCode_Yesterday;

            if (ConfigParameters.SpringStart < 0 || ConfigParameters.SpringStart >= yearRecords.Count ||
                ConfigParameters.WinterStart < 0 || ConfigParameters.WinterStart >= yearRecords.Count ||
                ConfigParameters.SpringStart > ConfigParameters.WinterStart) return;

            // loop over days from SpringStart to WinterStart
            for (var d = ConfigParameters.SpringStart; d < ConfigParameters.WinterStart; ++d)
            {
                var record = yearRecords[d];

                var mo = Calculate_mo(fineFuelMoistureCodeYesterday);
                var Ed = Calculate_Ed(record.RH, record.Temp);
                var Ew = Calculate_Ew(record.RH, record.Temp);
                var ko = Calculate_ko(record.RH, record.WindSpeed);
                var kd = Calculate_kd(ko, record.Temp);
                var kl = Calculate_kl(record.RH, record.WindSpeed);
                var kw = Calculate_kw(kl, record.Temp);
                var m = Calculate_m(mo, Ed, kd, Ew, kw);
                var re = Calculate_re(record.Precip);
                var Mo = Calculate_Mo(duffMoistureCodeYesterday);
                var b = Calculate_b(duffMoistureCodeYesterday);
                var Mr = Calculate_Mr(re, b, Mo);
                var Pr = Calculate_Pr(Mr);
                var month = Climate.MonthOfYear(d);
                var K = Calculate_K(record.Temp, record.RH, Le[month]);
                var duffMoistureCode = Calculate_DuffMoistureCode(record.Precip, Pr, K, duffMoistureCodeYesterday);
                var rd = Calculate_rd(record.Precip);
                var Qo = Calculate_Qo(droughtCodeYesterday);
                var Qr = Calculate_Qr(Qo, rd);
                var Dr = Calculate_Dr(Qr);
                var V = Calculate_V(record.Temp, Lf[month]);
                var droughtCode = Calculate_DroughtCode(record.Precip, Dr, V, droughtCodeYesterday);
                var windFunction_ISI = Calculate_WindFunction_ISI(record.WindSpeed);
                var fineFuelMoistureFunction_ISI = Calculate_FineFuelMoistureFunction_ISI(m);
                var initialSpreadIndex = Calculate_InitialSpreadIndex(windFunction_ISI, fineFuelMoistureFunction_ISI);
                var buildUpIndex = Calculate_BuildUpIndex(duffMoistureCode, droughtCode);
                var fD = Calculate_fD(buildUpIndex);
                var B = Calculate_B(initialSpreadIndex, fD);

                record.DuffMoistureCode = duffMoistureCodeYesterday = duffMoistureCode;
                record.DroughtCode = droughtCodeYesterday = droughtCode;
                record.BuildUpIndex = buildUpIndex;
                record.FineFuelMoistureCode = fineFuelMoistureCodeYesterday = Calculate_FineFuelMoistureCode(m);
                record.FireWeatherIndex = Calculate_FireWeatherIndex(B); 
            }
        }

        private static double Calculate_mo(double fineFuelMoistureCode) => 147.2 * (101.0 - fineFuelMoistureCode) / (59.5 + fineFuelMoistureCode);  //This used to be an explicit seed value for FFMC

        private static double Calculate_Ed(double rh, double temp) => 0.942 * Math.Pow(rh, 0.679) + 11.0 * Math.Exp((rh - 100.0) / 10.0) + 0.18 * (21.1 - temp) * (1.0 - Math.Exp(-0.115 * rh));

        private static double Calculate_Ew(double rh, double temp) => 0.618 * Math.Pow(rh, 0.753) + 10.0 * Math.Exp((rh - 100.0) / 10.0) + 0.18 * (21.1 - temp) * (1.0 - Math.Exp(-0.115 * rh));  //selfs

        private static double Calculate_ko(double rh, double windSpeed) => 0.424 * (1.0 - Math.Pow(rh / 100.0, 1.7)) + 0.0694 * Math.Pow(windSpeed, 0.5) * (1.0 - Math.Pow((rh / 100.0), 8));

        private static double Calculate_kd(double ko, double temp) => ko * 0.581 * Math.Exp(0.0365 * temp);

        private static double Calculate_kl(double rh, double windSpeed) => 0.424 * (1.0 - Math.Pow((100.0 - rh) / 100.0, 1.7)) + 0.0694 * Math.Pow(windSpeed, 0.5) * (1.0 - Math.Pow(((100.0 - rh) / 100.0), 8));

        private static double Calculate_kw(double kl, double temp) => kl * 0.581 * Math.Exp(0.0365 * temp);

        private static double Calculate_m(double mo, double Ed, double kd, double Ew, double kw)
        {
            if (mo > Ed)
                return Ed + (mo - Ed) * Math.Pow(10.0, -kd);

            return (mo < Ed && mo < Ew) ? (Ew - mo) * Math.Pow(10.0, -kw) : mo;
        }

        private static double Calculate_FineFuelMoistureCode(double m) => Math.Min(100.0, 59.5 * (250.0 - m) / (147.2 + m));

        private static double Calculate_re(double precip) => precip > 1.5 ? 0.92 * precip - 1.27 : 0.0;

        private static double Calculate_Mo(double duffMoistureCode) => 20.0 + Math.Exp(5.6348 - duffMoistureCode / 43.43);

        private static double Calculate_b(double duffMoistureCode)
        {
            if (duffMoistureCode <= 33.0)
            { 
                return 100.0 / (0.5 + 0.3 * duffMoistureCode);
            }

            if (duffMoistureCode > 65.0)
            {
                return 6.2 * Math.Log(duffMoistureCode) - 17.2;
            }

            return 14.0 - 1.3 * Math.Log(duffMoistureCode);
        }

        private static double Calculate_Mr(double re, double b, double Mo) => Mo + 1000.0 * re / (48.77 + b * re);

        private static double Calculate_Pr(double Mr) => Math.Max(0.0, 244.72 - 43.43 * Math.Log(Mr - 20.0));

        private static double Calculate_K(double temp, double rh, double Le) => temp < -1.1 ? 0.0 : 1.894 * (temp + 1.1) * (100.0 - rh) * Le * Math.Pow(10.0, -6.0);

        private static double Calculate_DuffMoistureCode(double precip, double Pr, double K, double duffMoistureCode) => precip > 1.5 ? Pr + 100.0 * K : duffMoistureCode + 100.0 * K;

        private static double Calculate_rd(double precip) => precip > 2.8 ? 0.83 * precip - 1.27 : 0.0;

        private static double Calculate_Qo(double droughtCode) => 800.0 * Math.Exp(-droughtCode / 400.0);

        private static double Calculate_Qr(double Qo, double rd) => Qo + 3.937 * rd;

        private static double Calculate_Dr(double Qr) => Math.Max(0.0, 400.0 * Math.Log(800.0 / Qr));

        private static double Calculate_V(double temp, double Lf) => temp < -2.8 ? Lf : 0.36 * (temp + 2.8) + Lf;

        private static double Calculate_DroughtCode(double precip, double Dr, double V, double droughtCodeYesterday) => precip > 2.8 ? Dr + 0.5 * V : droughtCodeYesterday + 0.5 * V;

        private static double Calculate_WindFunction_ISI(double windSpeed) => Math.Exp(0.05039 * windSpeed);

        private static double Calculate_FineFuelMoistureFunction_ISI(double m) => 91.9 * Math.Exp(-0.1386 * m) * (1.0 + Math.Pow(m, 5.31) / (4.93 * Math.Pow(10.0, 7.0)));

        private static double Calculate_InitialSpreadIndex(double windFunction_ISI, double fineFuelMoistureFunction_ISI) => 0.208 * windFunction_ISI * fineFuelMoistureFunction_ISI;

        private static double Calculate_BuildUpIndex(double duffMoistureCode, double droughtCode) => duffMoistureCode <= 0.4 * droughtCode ? 0.8 * duffMoistureCode * droughtCode / (duffMoistureCode + 0.4 * droughtCode) : duffMoistureCode - (1.0 - 0.8 * droughtCode / (duffMoistureCode + 0.4 * droughtCode)) * (0.92 + 0.0114 * Math.Pow(duffMoistureCode, 1.7));

        private static double Calculate_fD(double buildUpIndex) => buildUpIndex <= 80.0 ? 0.626 * Math.Pow(buildUpIndex, 0.809) + 2.0 : 1000.0 / (25.0 + 108.64 * Math.Exp(-0.023 * buildUpIndex));

        private static double Calculate_B(double initialSpreadIndex, double fD) => 0.1 * initialSpreadIndex * fD;

        private static double Calculate_FireWeatherIndex(double B) => B > 1.0 ? Math.Exp(2.72 * Math.Pow(0.434 * Math.Log(B), 0.647)) : B; 

        #endregion
    }
}
