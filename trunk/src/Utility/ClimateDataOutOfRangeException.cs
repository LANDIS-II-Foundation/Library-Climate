using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Landis.Library.Climate
{
    public class ClimateDataOutOfRangeException : ApplicationException
    {
        public ClimateDataOutOfRangeException()
            : base()
        {
             this.Data.Add("message", "Exception: out of range Time-step or ecoregion.");
        }


        public ClimateDataOutOfRangeException(string message, Exception innerException): base(message, innerException)
        {
           
        }

        //System.Collections.Generic.KeyNotFoundException
    }
}
