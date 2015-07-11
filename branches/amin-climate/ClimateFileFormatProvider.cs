//  Copyright: Portland State University 2009-2014
//  Authors:  Robert M. Scheller, Amin Almassian

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Landis.Library.Climate
{
    public class ClimateFileFormatProvider
    {
        private string format;
        private TemporalGranularity timeStep;
        private string maxTempTriggerWord;
        private string minTempTriggerWord;
        private string precipTriggerWord;
        private string rhTriggerWord;
        private string windSpeedTriggerWord;

        
            
        //------
        public TemporalGranularity InputTimeStep { get { return this.timeStep; } }
        public string MaxTempTriggerWord { get { return this.maxTempTriggerWord; } }
        public string MinTempTriggerWord { get { return this.minTempTriggerWord; } }
        public string PrecipTriggerWord { get { return this.precipTriggerWord; } }
        public string RhTriggerWord { get { return this.rhTriggerWord; } }
        public string WindSpeedTriggerWord { get { return this.windSpeedTriggerWord; } }
        public string SelectedFormat { get { return format; } }
  
        //------
        public ClimateFileFormatProvider(string format)
        {
            this.format = format;
            this.maxTempTriggerWord = "maxtemp";
            this.minTempTriggerWord = "mintemp";
            this.precipTriggerWord = "ppt";
            this.rhTriggerWord = "rh";
            this.windSpeedTriggerWord = "windSpeed";

            //this.timeStep = ((this.format == "PRISM") ? TemporalGranularity.Monthly : TemporalGranularity.Daily);
            switch (this.format.ToLower())
            {
                case "ipcc3_daily":  //was 'gfdl_a1fi'
                {
                    this.timeStep = TemporalGranularity.Daily;
                    break;
                }
                case "ipcc3_monthly":  //ADD
                {
                    this.timeStep = TemporalGranularity.Monthly;
                    break;
                }
                case "ipcc5_monthly":
                {
                    this.timeStep = TemporalGranularity.Monthly;
                    break;
                }
                case "ipcc5_daily":  //ADD
                {
                    this.timeStep = TemporalGranularity.Daily;
                    break;
                }
                case "prism_monthly":  //was 'prism'
                {
                    this.timeStep = TemporalGranularity.Monthly;
                    break;
                }
                case "Mauer_daily":  //was griddedobserved
                {
                    this.timeStep = TemporalGranularity.Daily;
                    break;
                }
                default:
                {
                    Climate.ModelCore.UI.WriteLine("Error in ClimateFileFormatProvider: the given \"{0}\" file format is not supported.", this.format);
                    throw new ApplicationException("Error in ClimateFileFormatProvider: the given \"" + this.format + "\" file format is not supported.");
                    //break;
                }
            }
        }

       


    }
}
