//  Copyright 2007 Conservation Biology Institute
//  Authors:  Robert M. Scheller
//  License:  Available at  
//  http://www.landis-ii.org/developers/LANDIS-IISourceCodeLicenseAgreement.pdf

using System;
namespace Landis.Library.Climate
{
    /// <summary>
    /// A random number generator.
    /// </summary>
    /// <remarks>
    /// This class is implemented using the random-number generator
    /// in the core framework.
    /// </remarks>
    public class RandomNumberGenerator
        : Troschuetz.Random.Generator
    {
        private static RandomNumberGenerator singleton;

        //---------------------------------------------------------------------

        private RandomNumberGenerator()
            : base()
        {
        }

        //---------------------------------------------------------------------

        public override bool CanReset
        {
            get {
                return false;
            }
        }

        //---------------------------------------------------------------------

        public static RandomNumberGenerator Singleton
        {
            get {
                if (singleton == null)
                    singleton = new RandomNumberGenerator();
                return singleton;
            }
        }

        //---------------------------------------------------------------------

        public override int Next()
        {
            throw new System.NotImplementedException();
        }

        //---------------------------------------------------------------------

        public override int Next(int maxValue)
        {
            throw new System.NotImplementedException();
        }

        //---------------------------------------------------------------------

        public override int Next(int minValue,
                                 int maxValue)
        {
            throw new System.NotImplementedException();
        }

        //---------------------------------------------------------------------

        public override bool NextBoolean()
        {
            throw new System.NotImplementedException();
        }

        //---------------------------------------------------------------------

        public override void NextBytes(byte[] buffer)
        {
            throw new System.NotImplementedException();
        }

        //---------------------------------------------------------------------

        public override double NextDouble()
        {
            double doubleRand = Landis.Util.Random.GenerateUniform();
            return doubleRand;
        }

        //---------------------------------------------------------------------

        public override double NextDouble(double maxValue)
        {
            throw new System.NotImplementedException();
        }

        //---------------------------------------------------------------------

        public override double NextDouble(double minValue,
                                          double maxValue)
        {
            throw new System.NotImplementedException();
        }

        //---------------------------------------------------------------------

        public override bool Reset()
        {
            return false;
        }
    }
}
