//  Copyright: Portland State University 2009-2014
//  Authors:  Robert M. Scheller, Amin Almassian

using System.Collections.Generic;

namespace Landis.Library.Climate
{
    /// <summary>
    /// Weather parameters for each month/day
    /// </summary>
    public interface IClimateRecord
    {

        double AvgMinTemp{get;set;}
        double AvgMaxTemp{get;set;}
        double StdDevTemp{get;set;}
        double AvgPpt{get;set;}
        double StdDevPpt{get;set;}
        double PAR{get;set;}
        double AvgVarTemp { get; set; }
        double AvgPptVarTemp { get; set; }
        double RHMean { get; set; }
        double RHVar { get; set; }
        double RHSTD { get; set; }
        double WindSpeedMean { get; set; }
        double WindSpeedVar { get; set; }
        double WindSpeedSTD { get; set; }

        
        
    }

    public class ClimateRecord
    : IClimateRecord
    {

        private double avgMinTemp;
        private double avgMaxTemp;
        private double stdDevTemp;
        private double avgPpt;
        private double stdDevPpt;
        private double par;
        private double avgVarTemp;
        private double avgPptVarTemp;
        private double rhMean;
        private double rhVar;
        private double rhSTD;
        private double windSpeedMean;
        private double windSpeedVar;
        private double windSpeedSTD;

        
        public double AvgMinTemp
        {
            get {
                return avgMinTemp;
            }
            set {
                avgMinTemp = value;
            }
        }

        public double AvgMaxTemp
        {
            get {
                return avgMaxTemp;
            }
            set {
                avgMaxTemp = value;
            }
        }
        public double StdDevTemp
        {
            get {
                return stdDevTemp;
            }
            set {
                stdDevTemp = value;
            }
        }
        public double AvgPpt
        {
            get {
                return avgPpt;
            }
            set {
                avgPpt = value;
            }
        }
        public double StdDevPpt
        {
            get {
                return stdDevPpt;
            }
            set {
                stdDevPpt = value;
            }
        }
        public double PAR
        {
            get {
                return par;
            }
            set {
                par = value;
            }
        }

        public double AvgVarTemp
        {
            get
            {
                return avgVarTemp;
            }
            set
            {
                avgVarTemp = value;
            }
        }
        public double AvgPptVarTemp
        {
            get
            {
                return avgPptVarTemp;
            }
            set
            {
                avgPptVarTemp = value;
            }
        }

        public double RHMean
        {
            get
            {
                return rhMean;
            }
            set
            {
                rhMean = value;
            }
        }
        public double RHVar
        {
            get
            {
                return rhVar;
            }
            set
            {
                rhVar = value;
            }
        }
        public double RHSTD
        {
            get
            {
                return rhSTD;
            }
            set
            {
                rhSTD = value;
            }
        }

        public double WindSpeedMean
        {
            get
            {
                return windSpeedMean;
            }
            set
            {
                windSpeedMean = value;
            }
        }
        public double WindSpeedVar
        {
            get
            {
                return windSpeedVar;
            }
            set
            {
                windSpeedVar = value;
            }
        }
        public double WindSpeedSTD
        {
            get
            {
                return windSpeedSTD;
            }
            set
            {
                windSpeedSTD = value;
            }
        }
        
      

        public ClimateRecord(
                            double avgMinTemp,
                            double avgMaxTemp,
                            double stdDevTemp,
                            double avgPpt,
                            double stdDevPpt,
                            double par,
                            double avgVarTemp,
                            double avgPptVarTemp,
                            double rhMean,
                            double rhVar,
                            double rhSTD,
                            double windSpeedMean,
                            double windSpeedVar,
                            double windSpeedSTD 
                            
                            )
        {
            this.avgMinTemp = avgMinTemp;
            this.avgMaxTemp = avgMaxTemp;
            this.stdDevTemp = stdDevTemp;
            this.avgPpt = avgPpt;
            this.stdDevPpt = stdDevPpt;
            this.par = par;
            this.avgVarTemp = avgVarTemp;
            this.avgPptVarTemp = avgPptVarTemp;
            this.rhMean = rhMean;
            this.rhVar = rhVar;
            this.rhSTD = rhSTD;
            this.windSpeedMean = windSpeedMean;
            this.windSpeedVar = windSpeedVar;
            this.windSpeedSTD = windSpeedSTD;


                          

        }
        
        public ClimateRecord()
        {
            this.avgMinTemp = -99.0;
            this.avgMaxTemp = -99.0;
            this.stdDevTemp = -99.0;
            this.avgPpt = -99.0;
            this.stdDevPpt = -99.0;
            this.par = -99.0;
            this.avgVarTemp = -99.0;
            this.avgPptVarTemp = -99.0;
            this.rhMean = -99.0;
            this.rhVar = -99.0;
            this.rhSTD = -99.0;
            this.windSpeedMean = -99.0;
            this.windSpeedVar = -99.0;
            this.windSpeedSTD = -99.0;


        }



     
    }
}
