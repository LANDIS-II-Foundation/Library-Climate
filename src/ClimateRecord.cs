//  Authors:  Amin Almassian, Robert M. Scheller, John McNabb, Melissa Lucash

using System.Collections.Generic;

namespace Landis.Library.Climate
{
    /// <summary>
    /// Weather parameters for each month/day
    /// </summary>
    public class ClimateRecord
    {
        private double avgMinTemp;
        private double avgMaxTemp;
        private double stdDevTemp;
        private double varTemp;
        private double avgPpt;
        private double varPpt;
        private double stdDevPpt;       
        private double avgWindDirection;
        private double stdDevWindDirection;
        private double varWindDirection;
        private double avgWindSpeed;
        private double stdDevWindSpeed;
        private double varWindSpeed;
        private double avgWindEasting;
        private double stdDevWindVectors;
        private double varWindVectors;
        private double avgWindNorthing;        
        private double avgNDeposition;
        private double stdDevNDeposition;
        private double varNDeposition;
        private double avgCO2;
        private double stdDevCO2;
        private double varCO2;
        private double avgRH;
        private double avgMinRH;
        private double avgMaxRH;
        private double stdDevRH;
        private double varRH;
        private double avgSpecificHumidity;
        private double avgPAR;
        private double stdDevPAR;
        private double varPAR;
        private double avgOzone;
        private double stdDevOzone;
        private double varOzone;
        private double avgShortWaveRadiation;
        private double stdDevShortWaveRadiation;
        private double varShortWaveRadiation;
        private double temp;
        private double avgFWI;    
        
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
       
        public double VarTemp
        {
            get
            {
                return varTemp;
            }
            set
            {
                varTemp = value;
            }
        }
        public double VarPpt
        {
            get
            {
                return varPpt;
            }
            set
            {
                varPpt = value;
            }
        }

        public double AvgWindDirection
        {
            get
            {
                return avgWindDirection;
            }
            set
            {
                avgWindDirection = value;
            }
        }
        public double StdDevWindDirection
        {
            get
            {
                return stdDevWindDirection;
            }
            set
            {
                stdDevWindDirection = value;
            }
        }
        public double VarWindDirection
        {
            get
            {
                return varWindDirection;
            }
            set
            {
                varWindDirection = value;
            }
        }

        public double AvgWindSpeed
        {
            get
            {
                return avgWindSpeed;
            }
            set
            {
                avgWindSpeed = value;
            }
        }
        public double StdDevWindSpeed
        {
            get
            {
                return stdDevWindSpeed;
            }
            set
            {
                stdDevWindSpeed = value;
            }
        }
        public double VarWindSpeed
        {
            get
            {
                return varWindSpeed;
            }
            set
            {
                varWindSpeed = value;
            }
        }
        public double AvgWindEasting
        {
            get
            {
                return avgWindEasting;
            }
            set
            {
                avgWindEasting = value;
            }
        }
        public double StdDevWindVectors
        {
            get
            {
                return stdDevWindVectors;
            }
            set
            {
                stdDevWindVectors = value;
            }
        }
        public double VarWindVectors
        {
            get
            {
                return varWindVectors;
            }
            set
            {
                varWindVectors = value;
            }
        }

        public double AvgWindNorthing
        {
            get
            {
                return avgWindNorthing;
            }
            set
            {
                avgWindNorthing = value;
            }
        }        

        public double AvgNDeposition
        {
            get
            {
                return avgNDeposition;
            }
            set
            {
                avgNDeposition = value;
            }
        }

        public double VarNDeposition
        {
            get
            {
                return varNDeposition;
            }
            set
            {
                varNDeposition = value;
            }
        }

        public double StdDevNDeposition
        {
            get
            {
                return stdDevNDeposition;
            }
            set
            {
                stdDevNDeposition = value;
            }
        }
        public double AvgCO2
        {
            get
            {
                return avgCO2;
            }
            set
            {
                avgCO2 = value;
            }
        }
        public double VarCO2
        {
            get
            {
                return varCO2;
            }
            set
            {
                varCO2 = value;
            }
        }
        public double StdDevCO2
        {
            get
            {
                return stdDevCO2;
            }
            set
            {
                stdDevCO2 = value;
            }
        }

        public double AvgRH
        {
            get
            {
                return avgRH;
            }
            set
            {
                avgRH = value;
            }
        }

        public double AvgMinRH
        {
            get
            {
                return avgMinRH;
            }
            set
            {
                avgMinRH = value;
            }
        }

        public double AvgMaxRH
        {
            get
            {
                return avgMaxRH;
            }
            set
            {
                avgMaxRH = value;
            }
        }

        public double StdDevRH
        {
            get
            {
                return stdDevRH;
            }
            set
            {
                stdDevRH = value;
            }
        }
        public double VarRH
        {
            get
            {
                return varRH;
            }
            set
            {
                varRH = value;
            }
        }
        public double AvgSpecificHumidity
        {
            get
            {
                return avgSpecificHumidity;
            }
            set
            {
                avgSpecificHumidity = value;
            }
        }
        public double AvgPAR
        {
            get
            {
                return avgPAR;
            }
            set
            {
                avgPAR = value;
            }
        }
        public double StdDevPAR
        {
            get
            {
                return stdDevPAR;
            }
            set
            {
                stdDevPAR = value;
            }
        }
        public double VarPAR
        {
            get
            {
                return varPAR;
            }
            set
            {
                varPAR = value;
            }
        }
        public double AvgOzone
        {
            get
            {
                return avgOzone;
            }
            set
            {
                avgOzone = value;
            }
        }
        public double StdDevOzone
        {
            get
            {
                return stdDevOzone;
            }
            set
            {
                stdDevOzone = value;
            }
        }
        public double VarOzone
        {
            get
            {
                return varOzone;
            }
            set
            {
                varOzone = value;
            }
        }

        public double AvgShortWaveRadiation
        {
            get
            {
                return avgShortWaveRadiation;
            }
            set
            {
                avgShortWaveRadiation = value;
            }
        }
        public double StdDevShortWaveRadiation
        {
            get
            {
                return stdDevShortWaveRadiation;
            }
            set
            {
                stdDevShortWaveRadiation = value;
            }
        }
        public double VarShortWaveRadiation
        {
            get
            {
                return varShortWaveRadiation;
            }
            set
            {
                varShortWaveRadiation = value;
            }
        }

        public double Temp
        {
            get
            {
                return temp;
            }
            set
            {
                temp = value;
            }
        }

        public double AvgFWI
        {
            get
            {
                return avgFWI;
            }
            set
            {
                avgFWI = value;
            }
        }

        public ClimateRecord(double avgMinTemp, double avgMaxTemp, double stdDevTemp, double avgPpt, double stdDevPpt, double varTemp, double varPpt, double avgWindDirection,
                            double varWindDirection, double stdDevWindDirection, double avgWindSpeed, double varWindSpeed, double stdDevWindSpeed, double avgWindEasting, double avgWindNorthing, double stdDevWindVectors, double varWindVectors, double avgNDeposition, double varNDeposition, 
                            double stdDevNDeposition, double avgCO2, double varCO2, double stdDevCO2, double avgRH, double avgMinRH, double avgMaxRH, double stdDevRH, double varRH, double avgSpecificHumidity, double avgPAR, double stdDevPAR, double varPAR,
                            double avgOzone, double VarOzone, double stdDevOzone, double avgShortWaveRadiation, double varShortWaveRadiation, double stdDevShortWaveRadiation,double temp,double avgFWI)
        {
            this.avgMinTemp = avgMinTemp;
            this.avgMaxTemp = avgMaxTemp;
            this.stdDevTemp = stdDevTemp;
            this.avgPpt = avgPpt;
            this.stdDevPpt = stdDevPpt;
            this.varTemp = VarTemp;
            this.varPpt = VarPpt;
            this.avgWindDirection = avgWindDirection;
            this.varWindDirection = varWindDirection;
            this.stdDevWindDirection = stdDevWindDirection;
            this.avgWindSpeed = avgWindSpeed;
            this.varWindSpeed = varWindSpeed;
            this.stdDevWindSpeed = stdDevWindSpeed;
            this.avgWindEasting = avgWindEasting;
            this.avgWindNorthing = avgWindNorthing;            
            this.stdDevWindVectors = stdDevWindVectors;
            this.varWindVectors = varWindVectors;
            this.avgNDeposition = avgNDeposition;
            this.varNDeposition = varNDeposition;
            this.stdDevNDeposition = stdDevNDeposition;
            this.avgCO2 = avgCO2;
            this.varCO2 = varCO2;
            this.stdDevCO2 = stdDevCO2;
            this.avgRH = avgRH;
            this.avgMinRH = avgMinRH;
            this.avgMaxRH = avgMaxRH;
            this.stdDevRH = stdDevRH;
            this.varRH = VarRH;
            this.avgSpecificHumidity = avgSpecificHumidity;
            this.avgPAR = avgPAR;
            this.stdDevPAR = stdDevPAR;
            this.varPAR = VarPAR;
            this.avgOzone = avgOzone;
            this.stdDevOzone = stdDevOzone;
            this.varOzone = VarOzone;
            this.avgShortWaveRadiation = avgShortWaveRadiation;
            this.stdDevShortWaveRadiation = stdDevShortWaveRadiation;
            this.varShortWaveRadiation = varShortWaveRadiation;
            this.temp = temp;
            this.avgFWI = avgFWI;
        }
        
        public ClimateRecord()
        {
            this.avgMinTemp = -99.0;
            this.avgMaxTemp = -99.0;
            this.stdDevTemp = -99.0;
            this.avgPpt = -99.0;
            this.stdDevPpt = -99.0;            
            this.varTemp = -99.0;
            this.varPpt = -99.0;
            this.avgWindDirection = -99.0;
            this.varWindDirection = -99.0;
            this.stdDevWindDirection = -99.0;
            this.avgWindEasting = -99.0;
            this.avgWindNorthing = -99.0;
            this.stdDevWindVectors = -99.0;
            this.varWindVectors = -99.0;     
            this.avgWindSpeed = -99.0;
            this.varWindSpeed = -99.0;
            this.stdDevWindSpeed = -99.0;
            this.avgNDeposition = -99.0;
            this.varNDeposition = -99.0;
            this.stdDevNDeposition = -99.0;
            this.avgCO2 = -99.0;
            this.varCO2 = -99.0;
            this.stdDevCO2 = -99.0;
            this.avgRH = -99.0;
            this.avgMinRH = -99.0;
            this.avgMaxRH = -99.0;
            this.stdDevRH = -99.0;
            this.varRH = -99.0;
            this.avgSpecificHumidity = -99.0;
            this.avgPAR = -99.0;
            this.stdDevPAR = -99.0;
            this.varPAR = -99.0;
            this.avgOzone = -99.0;
            this.stdDevOzone = -99.0;
            this.varOzone = -99.0;
            this.avgShortWaveRadiation = -99.0;
            this.stdDevShortWaveRadiation = -99.0;
            this.varShortWaveRadiation = -99.0;
            this.varShortWaveRadiation = -99.0;
            this.temp = -99.0;
            this.avgFWI = -99.0;
        }
    }
}
