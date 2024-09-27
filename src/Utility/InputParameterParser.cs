//  Authors:  Amin Almassian, Robert M. Scheller, John McNabb, Melissa Lucash

using Landis.Core;
using Landis.SpatialModeling;
using Landis.Utilities;
using System.Collections.Generic;
using System;

namespace Landis.Library.Climate
{
    /// <summary>
    /// A parser that reads biomass succession parameters from text input.
    /// </summary>
    public class InputParametersParser
        : TextParser<IInputParameters>
    {
        private string landisDataValue;

        public override string LandisDataValue
        {
            get
            {
                return landisDataValue;  
            }
        }


        public static class Names
        {
            //public const string Timestep = "Timestep";            
            public const string LandisData = "LandisData";
            public const string ClimateConfigFile = "ClimateConfigFile";
            public const string ClimateTimeSeries = "ClimateTimeSeries";
            public const string ClimateFile = "ClimateFile";
            //public const string ClimateFileFormat = "ClimateFileFormat";
            public const string SpinUpClimateTimeSeries = "SpinUpClimateTimeSeries";
            public const string SpinUpClimateFile = "SpinUpClimateFile";
            //public const string SpinUpClimateFileFormat = "SpinUpClimateFileFormat";
            //public const string RHSlopeAdjust = "RelativeHumiditySlopeAdjust";
        }

        //---------------------------------------------------------------------

        static InputParametersParser()
        {
        }

        //---------------------------------------------------------------------

        public InputParametersParser()
        {
            this.landisDataValue = "Climate Config";
        }

        //---------------------------------------------------------------------

        protected override IInputParameters Parse()
        {
            InputVar<string> landisData = new InputVar<string>("LandisData");
            ReadVar(landisData);
            if (landisData.Value.Actual != "Climate Config")
                throw new InputValueException(landisData.Value.String, "The value is not \"{0}\"", "Climate Config");

            InputParameters parameters = new InputParameters();

            string climateTimeSeries_PossibleValues = "Monthly_AverageAllYears, Monthly_AverageWithVariation, Monthly_RandomYears, Daily_RandomYears, Daily_AverageAllYears, Daily_SequencedYears, Monthly_SequencedYears";


            InputVar<string> climateTimeSeries = new InputVar<string>(Names.ClimateTimeSeries);
            ReadVar(climateTimeSeries);
            parameters.ClimateTimeSeries = climateTimeSeries.Value;

            InputVar<string> climateFile = new InputVar<string>(Names.ClimateFile);
            ReadVar(climateFile);
            parameters.ClimateFile = climateFile.Value;

            InputVar<string> spinUpClimateTimeSeries = new InputVar<string>(Names.SpinUpClimateTimeSeries);
            ReadVar(spinUpClimateTimeSeries);
            parameters.SpinUpClimateTimeSeries = spinUpClimateTimeSeries.Value;

            InputVar<string> spinUpClimateFile = new InputVar<string>(Names.SpinUpClimateFile);
            ReadVar(spinUpClimateFile);
            parameters.SpinUpClimateFile = spinUpClimateFile.Value;

            if (!climateTimeSeries_PossibleValues.ToLower().Contains(parameters.ClimateTimeSeries.ToLower()) || !climateTimeSeries_PossibleValues.ToLower().Contains(parameters.SpinUpClimateTimeSeries.ToLower()))
            {
                throw new ApplicationException("Error in parsing climate-generator input file: invalid value for ClimateTimeSeries or SpinupTimeSeries provided. Possible values are: " + climateTimeSeries_PossibleValues);
            }

            var generateClimateOutputFiles = new InputVar<bool>("GenerateClimateOutputFiles");
            if (ReadOptionalVar(generateClimateOutputFiles))
                parameters.GenerateClimateOutputFiles = generateClimateOutputFiles.Value;

            InputVar<bool> climateFire = new InputVar<bool>("UsingFireClimate");
            if (ReadOptionalVar(climateFire))
                parameters.UsingFireClimate = climateFire.Value;

            if (parameters.UsingFireClimate)
            {
                InputVar<int> FFMoistCode = new InputVar<int>("FineFuelMoistureCode");
                ReadVar(FFMoistCode);
                parameters.FineFuelMoistureCode_Yesterday = FFMoistCode.Value;

                InputVar<int> DuffMoistCode = new InputVar<int>("DuffMoistureCode");
                ReadVar(DuffMoistCode);
                parameters.DuffMoistureCode_Yesterday = DuffMoistCode.Value;

                InputVar<int> DroughtCode = new InputVar<int>("DroughtCode");
                ReadVar(DroughtCode);
                parameters.DroughtCode_Yesterday = DroughtCode.Value;

                InputVar<int> sStart = new InputVar<int>("FirstDayFire");
                ReadVar(sStart);
                parameters.SpringStart = sStart.Value;

                InputVar<int> wStart = new InputVar<int>("LastDayFire");
                ReadVar(wStart);
                parameters.WinterStart = wStart.Value;
            }

            InputVar<int> atmPressure = new InputVar<int>("AtmosphericPressure");
            if (ReadOptionalVar(atmPressure))
                parameters.AtmPressure = atmPressure.Value;

            return parameters; 
        }         
    }
}
