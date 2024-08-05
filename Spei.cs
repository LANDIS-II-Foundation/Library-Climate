using System;
using System.Collections.Generic;
using System.Linq;

namespace Landis.Library.Climate
{
    public static partial class Climate
    {
        #region private methods

        private static void CalculateMonthlySpei(List<AnnualClimate> annualClimate, int scale)
        {
            // annualClimate is sequenced by input years

            var precipByYearAndMonth = annualClimate.Select(x => x.MonthlyPrecip).ToList();
            var petByYearAndMonth = annualClimate.Select(x => x.MonthlyPET).ToList();

            var yearCount = annualClimate.Count;

            // calculate deficit D = P - PET
            var deficit = new List<double>();
            for (var i = 0; i < yearCount; ++i)
            {
                for (var j = 0; j < 12; ++j)
                {
                    deficit.Add(precipByYearAndMonth[i][j] - petByYearAndMonth[i][j]);
                }
            }

            // accumulate deficits over the scale window into a [month][year] array
            var cumDecifitByMonthAndYear = Enumerable.Range(0, 12).Select(x => new double[yearCount]).ToArray();
            for (var k = 0; k < deficit.Count; ++k)
            {
                if (k >= scale - 1)
                {
                    var s = 0.0;
                    for (var m = k - scale + 1; m <= k; ++m)
                    {
                        s += deficit[m];
                    }
                    cumDecifitByMonthAndYear[k % 12][k / 12] = s;
                }
                else
                {
                    cumDecifitByMonthAndYear[k % 12][k / 12] = double.NaN;
                }
            }

            // fit each month's cum decifits and calculate spei values
            var speiByYearAndMonth = Enumerable.Range(0, yearCount).Select(x => new double[12]).ToArray();
            for (var j = 0; j < 12; ++j)
            {
                // sort each month's data and find the unbiased probability-weighted moments
                var sortedData = cumDecifitByMonthAndYear[j].Where(x => !double.IsNaN(x)).OrderBy(x => x).ToArray();
                var pwms = ProbabilityWeightedMoments(sortedData);

                // fit pwms to log-logistic
                var logLogisticParams = LogLogisticFit(pwms);

                for (var i = 0; i < yearCount; ++i)
                {
                    if (double.IsNaN(cumDecifitByMonthAndYear[j][i]))
                    {
                        speiByYearAndMonth[i][j] = 0.0;
                    }
                    else
                    {
                        var llCdf = LogLogisticCdf(cumDecifitByMonthAndYear[j][i], logLogisticParams);
                        speiByYearAndMonth[i][j] = NormStandardInv(llCdf);
                    }
                }
            }

            // attach spei data to input data
            for (var i = 0; i < yearCount; ++i)
            {
                annualClimate[i].MonthlySpei = speiByYearAndMonth[i];
            }
        }

        private static double[] ProbabilityWeightedMoments(double[] sortedData)
        {
            // compute alpha pwms
            var acum = new double[3];
            var n = sortedData.Length;
            for (var i = 1; i <= n; ++i)
            {
                acum[0] += sortedData[i - 1];
                acum[1] += sortedData[i - 1] * (n - i);
                acum[2] += sortedData[i - 1] * (n - i) * (n - i - 1);
            }

            return new[] { acum[0] / n, acum[1] / n / (n - 1), acum[2] / n / ((n - 1) * (n - 2)) };
        }

        private static double[] LogLogisticFit(double[] beta)
        {
            // Estimates the parameters of a 3-parameter Gamma distribution function

            var logLogisticParams = new double[3];

            // estimate gamma parameter
            logLogisticParams[2] = (2 * beta[1] - beta[0]) / (6 * beta[1] - beta[0] - 6 * beta[2]);

            var g1 = Math.Exp(GammaLn(1.0 + 1.0 / logLogisticParams[2]));
            var g2 = Math.Exp(GammaLn(1.0 - 1.0 / logLogisticParams[2]));

            // estimate alpha parameter
            logLogisticParams[1] = (beta[0] - 2 * beta[1]) * logLogisticParams[2] / (g1 * g2);

            // estimate beta parameter
            logLogisticParams[0] = beta[0] - logLogisticParams[1] * g1 * g2;

            return logLogisticParams;
        }

        private static double LogLogisticCdf(double p, double[] llParams)
        {
            return 1.0 / (1.0 + System.Math.Pow(llParams[1] / (p - llParams[0]), llParams[2]));
        }

        #endregion

        #region numeric functions needed for SPEI

        /// <summary>
        /// Returns the inverse Cumulative Distribution Function of the standard Normal distribution.
        /// </summary>
        private static double NormStandardInv(double probability)
        {
            const double epsilon = 1e-62;
            const double sqrt2Pi = 2.506628274631;

            if (double.IsNaN(probability))
            {
                return double.NaN;
            }

            const double a0 = 3.38713287279637;
            const double a1 = 133.141667891784;
            const double a2 = 1971.59095030655;
            const double a3 = 13731.6937655095;
            const double a4 = 45921.9539315499;
            const double a5 = 67265.709270087;
            const double a6 = 33430.5755835881;
            const double a7 = 2509.08092873012;

            const double b1 = 42.3133307016009;
            const double b2 = 687.187007492058;
            const double b3 = 5394.19602142475;
            const double b4 = 21213.7943015866;
            const double b5 = 39307.8958000927;
            const double b6 = 28729.0857357219;
            const double b7 = 5226.49527885285;

            const double c0 = 1.42343711074968;
            const double c1 = 4.63033784615655;
            const double c2 = 5.76949722146069;
            const double c3 = 3.6478483247632;
            const double c4 = 1.27045825245237;
            const double c5 = 0.241780725177451;
            const double c6 = 0.0227238449892692;
            const double c7 = 0.000774545014278341;
            const double d1 = 2.05319162663776;
            const double d2 = 1.6763848301838;
            const double d3 = 0.6897673349851;
            const double d4 = 0.14810397642748;
            const double d5 = 0.0151986665636165;
            const double d6 = 0.000547593808499535;
            const double d7 = 1.05075007164442E-09;

            const double e0 = 6.6579046435011;
            const double e1 = 5.46378491116411;
            const double e2 = 1.78482653991729;
            const double e3 = 0.296560571828505;
            const double e4 = 0.0265321895265761;
            const double e5 = 0.00124266094738808;
            const double e6 = 2.71155556874349E-05;
            const double e7 = 2.01033439929229E-07;
            const double f1 = 0.599832206555888;
            const double f2 = 0.136929880922736;
            const double f3 = 0.0148753612908506;
            const double f4 = 0.000786869131145613;
            const double f5 = 1.84631831751005E-05;
            const double f6 = 1.42151175831645E-07;
            const double f7 = 2.04426310338994E-15;

            const double half = 0.5;
            const double one = 1.0;
            const double zero = 0.0;

            const double split1 = 0.425;
            const double split2 = 5.0;

            const double const1 = 0.180625;
            const double const2 = 1.6;

            if (probability < epsilon)
            {
                return double.NegativeInfinity;
            }

            if (probability - 1.0 > -epsilon)
            {
                return double.PositiveInfinity;
            }

            double ppnd16;
            double r;

            var q = probability - half;
            if (Math.Abs(q) <= split1)
            {
                r = const1 - q * q;
                ppnd16 = q * (((((((a7 * r + a6) * r + a5) * r + a4) * r + a3) * r + a2) * r + a1) * r + a0) / (((((((b7 * r + b6) * r + b5) * r + b4) * r + b3) * r + b2) * r + b1) * r + one);

                //' One iteration of
                //' Halley() 's rational method (third order) gives full machine precision.

                var e = 0.5 * Erfc(-ppnd16 / sqrt2Pi) - probability;
                var u = e * sqrt2Pi * Math.Exp(ppnd16 * ppnd16 / 2.0);
                ppnd16 = ppnd16 - u / (1.0 + ppnd16 * u / 2.0);
            }
            else
            {
                if (q < zero)
                {
                    r = probability;
                }
                else
                {
                    r = one - probability;
                }

                if (r <= zero)
                {
                    ppnd16 = zero;
                    return ppnd16;
                }
                r = Math.Sqrt(-Math.Log(r));
                if (r <= split2)
                {
                    r = r - const2;
                    ppnd16 = (((((((c7 * r + c6) * r + c5) * r + c4) * r + c3) * r + c2) * r + c1) * r + c0) / (((((((d7 * r + d6) * r + d5) * r + d4) * r + d3) * r + d2) * r + d1) * r + one);
                }
                else
                {
                    r = r - split2;
                    ppnd16 = (((((((e7 * r + e6) * r + e5) * r + e4) * r + e3) * r + e2) * r + e1) * r + e0) / (((((((f7 * r + f6) * r + f5) * r + f4) * r + f3) * r + f2) * r + f1) * r + one);
                }


                if (q < zero)
                {
                    ppnd16 = -ppnd16;
                }
            }

            return ppnd16;
        }

        private static readonly double[] ErfQ3 = { 0.0106209230528468, 0.19130892610783, 1.05167510706793, 1.98733201817135, 1.0 };
        private static readonly double[] ErfP3 = { -0.00299610707703542, -0.0494730910623251, -0.226956593539687, -0.278661308609648, -0.0223192459734185 };
        private static readonly double[] ErfQ2 = { 300.459260956983, 790.950925327898, 931.35409485061, 638.980264465631, 277.585444743988, 77.0001529352295, 12.7827273196294, 1.0 };
        private static readonly double[] ErfP2 = { 300.459261020162, 451.918953711873, 339.320816734344, 152.98928504694, 43.1622272220567, 7.21175825088309, 0.564195517478974, -1.36864857382717E-07 };
        private static readonly double[] ErfQ1 = { 215.058875869861, 91.1649054045149, 15.0827976304078, 1.0 };
        private static readonly double[] ErfP1 = { 242.667955230532, 21.9792616182941, 6.99638348861914, -0.0356098437018154 };

        /// <summary>
        /// Returns the complementary error function Erfc(x).
        /// </summary>
        private static double Erfc(double x)
        {
            const double invSqrtPi = 0.56418958354775628;

            double a;
            double b;
            double y;
            double retval;

            var v = Math.Abs(x);

            if (v <= 0.46875)
            {
                y = v * v;
                a = ErfP1[3];
                b = ErfQ1[3];
                for (var j = 2; j >= 0; --j)
                {
                    a = a * y + ErfP1[j];
                    b = b * y + ErfQ1[j];
                }


                return 1.0 - x * a / b;
            }

            if (v <= 4.0)
            {
                a = ErfP2[7];
                b = ErfQ2[7];

                for (var j = 6; j >= 0; --j)
                {
                    a = a * v + ErfP2[j];
                    b = b * v + ErfQ2[j];
                }


                retval = Math.Exp(-v * v) * a / b;
            }
            else if (v <= 10.0)
            {
                y = 1.0 / (v * v);
                a = ErfP3[4];
                b = ErfQ3[4];

                for (var j = 3; j >= 0; --j)
                {
                    a = a * y + ErfP3[j];
                    b = b * y + ErfQ3[j];
                }


                retval = Math.Exp(-v * v) * (invSqrtPi + y * a / b) / v;
            }
            else
            {
                retval = 0.0;
            }

            if (x <= 0.0)
            {
                retval = 2.0 - retval;
            }

            return retval;
        }

        private static readonly double[] GammaLanczos =
        {
            0.99999999999999709182,
            57.156235665862923517,
            -59.597960355475491248,
            14.136097974741747174,
            -0.49191381609762019978,
            .33994649984811888699e-4,
            .46523628927048575665e-4,
            -.98374475304879564677e-4,
            .15808870322491248884e-3,
            -.21026444172410488319e-3,
            .21743961811521264320e-3,
            -.16431810653676389022e-3,
            .84418223983852743293e-4,
            -.26190838401581408670e-4,
            .36899182659531622704e-5
        };

        /// <summary>
        /// Ln of gamma function of s. Valid for s > 0.0.
        /// </summary>
        private static double GammaLn(double s)
        {
            const double log2Pi = 1.83787706640935;

            if (s < double.Epsilon)
            {
                return double.NaN;
            }

            var sum = 0.0;
            for (var i = GammaLanczos.Length - 1; i > 0; --i)
            {
                sum += GammaLanczos[i] / (s + i);
            }

            sum += GammaLanczos[0];

            var tmp = s + 607.0 / 128.0 + .5;
            return (s + .5) * Math.Log(tmp) - tmp +
                   0.5 * log2Pi + Math.Log(sum / s);
        }

        #endregion
    }
}
