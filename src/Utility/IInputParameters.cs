//  Authors:  Amin Almassian, Robert M. Scheller, John McNabb, Melissa Lucash

namespace Landis.Library.Climate
{

    public interface IInputParameters
    {
        string ClimateTimeSeries { get; set; }
        string ClimateFile { get; set; }
        //string ClimateFileFormat { get; set; }
        string SpinUpClimateTimeSeries { get; set; }
        string SpinUpClimateFile { get; set; }
        //string SpinUpClimateFileFormat { get; set; }
        bool GenerateClimateOutputFiles { get; set; }
        bool UsingFireClimate { get; set; }
        //double RHSlopeAdjust { get; set; }
        int FineFuelMoistureCode_Yesterday { get; set; }
        int DuffMoistureCode_Yesterday { get; set; }
        int DroughtCode_Yesterday { get; set; }
        int SpringStart { get; set; }
        int WinterStart { get; set; }
        int AtmPressure { get; set; }

    }
}
