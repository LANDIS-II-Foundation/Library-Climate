//  Copyright 2007-2010 Portland State University, University of Wisconsin-Madison
//  Author: Robert Scheller, Ben Sulman

//using Landis.Library.Succession;
using Edu.Wisc.Forest.Flel.Util;
using System.Collections.Generic;

namespace Landis.Library.Climate
{

    //public enum TimeSeriesNames { Monthly_AverageAllYears, Monthly_AverageWithVariation, Monthly_RandomYear, Monthly_SequencedYears, Daily_RandomYear, Daily_AverageAllYears, Daily_SequencedYears };

    /// <summary>
    /// The parameters for biomass succession.
    /// </summary>
    public interface IInputParameters
    {
        //int Timestep{ get;set;}
        //string LandisDataValue { }

        string ClimateTimeSeries { get; set; }
        string ClimateFile { get; set; }
        string ClimateFileFormat { get; set; }
        string SpinUpClimateTimeSeries { get; set; }
        string SpinUpClimateFile { get; set; }
        string SpinUpClimateFileFormat { get; set; }
 


        //---------------------------------------------------------------------



        
    }
}
