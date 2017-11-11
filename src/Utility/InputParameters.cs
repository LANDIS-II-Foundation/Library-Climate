//  Authors:  Amin Almassian, Robert M. Scheller, John McNabb, Melissa Lucash

using Edu.Wisc.Forest.Flel.Util;

namespace Landis.Library.Climate
{
    /// <summary>
    /// The parameters for biomass succession.
    /// </summary>
    public class InputParameters
         : IInputParameters
       
    {

        private string climateConfigFile;
        private string climateTimeSeries;
        private string climateFileFormat;
        private string climateFile;
        private string spinUpClimateFileFormat;
        private string spinUpClimateFile;
        private string spinUpClimateTimeSeries;
        private bool climateFire = false;
        private double rHSlopeAdjust;
        private int springStart;
        private int winterStart;


        //---------------------------------------------------------------------
        public string ClimateConfigFile
        {
            get
            {
                return climateConfigFile;
            }
            set
            {

                climateConfigFile = value;
            }
        }
        
        //---------------------------------------------------------------------
        /// <summary>
        /// Path to the required file with climatedata.
        /// </summary>
        /// 

        public string ClimateTimeSeries
        {
            get
            {
                return climateTimeSeries;
            }
            set
            {

                climateTimeSeries = value;
            }
        }

        public string ClimateFile
        {
            get {
                return climateFile;
            }
            set {
                string path = value;
                if (path.Trim(null).Length == 0)
                    throw new InputValueException(path, "\"{0}\" is not a valid path.", path);
                climateFile = value;
            }
        }

        public string ClimateFileFormat
        {
            get
            {
                return climateFileFormat;
            }
            set
            {

                climateFileFormat = value;
            }
        }

        public string SpinUpClimateTimeSeries  
        {
            get
            {
                return spinUpClimateTimeSeries;
            }
            set
            {

                spinUpClimateTimeSeries = value;
            }
        }

        public string SpinUpClimateFileFormat
        {
            get
            {
                return spinUpClimateFileFormat;
            }
            set
            {

                spinUpClimateFileFormat = value;
            }
        }

        public string SpinUpClimateFile			
        {
            get
            {
                return spinUpClimateFile;
            }
            set
            {
                string path = value;
                if (spinUpClimateFileFormat != "no" && path.Trim(null).Length == 0)
                    throw new InputValueException(path, "\"{0}\" is not a valid path.", path);
                spinUpClimateFile = value;
            }
        }
        //---------------------------------------------------------------------
        public bool UsingFireClimate
        {
            get
            {
                return climateFire;
            }
            set
            {

                climateFire = value;
            }
        }
        //---------------------------------------------------------------------
        public double RHSlopeAdjust
        {
            get
            {
                return rHSlopeAdjust;
            }
            set
            {

                rHSlopeAdjust = value;
            }
        }

        //---------------------------------------------------------------------
        public int SpringStart
        {
            get
            {
                return springStart;
            }
            set
            {
                if(value < 0 || value > 365)
                    throw new InputValueException(value.ToString(), "\"{0}\" must be a valid Julian day of year.", value);
                springStart = value;
            }
        }
        //---------------------------------------------------------------------
        public int WinterStart
        {
            get
            {
                return winterStart;
            }
            set
            {
                if (value < 0 || value > 365 || value < SpringStart)
                    throw new InputValueException(value.ToString(), "\"{0}\" must be a valid Julian day of year AND > spring start.", value);
                winterStart = value;
            }
        }
    }
}
