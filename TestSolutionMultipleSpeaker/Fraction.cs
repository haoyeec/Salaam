using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestSolutionMultipleSpeaker
{
    public class Fraction
    {
        public int num { get; set; }
        public int den { get; set; }

        public Fraction()
        {
            num = 0;
            den = 0;
        }
        public Fraction(int numer, int denom)
        {
            num = numer;
            den = denom;
        }

        public double toDecimal()
        {
            if (den == 0) return 0.0;
            return Convert.ToDouble(num) / Convert.ToDouble(den);
        }

        public override string ToString()
        {
            return num + "//" + den;
        }

        public static Fraction operator +(Fraction f1, Fraction f2)
        {
            return new Fraction(f1.num + f2.num, f1.den + f2.den);
        }
    }
}
