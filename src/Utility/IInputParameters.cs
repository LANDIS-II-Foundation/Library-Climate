//  Authors:  Amin Almassian, Robert M. Scheller, John McNabb, Melissa Lucash

namespace Landis.Library.Climate
{

    //public enum TimeSeriesNames { Monthly_AverageAllYears, Monthly_AverageWithVariation, Monthly_RandomYear, Monthly_SequencedYears, Daily_RandomYear, Daily_AverageAllYears, Daily_SequencedYears };

    /// <summary>
    /// The parameters for biomass succession.
    /// </summary>
    public interface IInputParameters
    {
        string ClimateTimeSeries { get; set; }
        string ClimateFile { get; set; }
        string ClimateFileFormat { get; set; }
        string SpinUpClimateTimeSeries { get; set; }
        string SpinUpClimateFile { get; set; }
        string SpinUpClimateFileFormat { get; set; }
        double RHSlopeAdjust { get; set; }
        int SpringStart { get; set; }
        int WinterStart { get; set; }
        
    }
}
