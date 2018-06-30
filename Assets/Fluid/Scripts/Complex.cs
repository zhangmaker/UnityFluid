using System;
using System.Collections.Generic;
using System.Text;

namespace Fluid {
    /// <summary>
    /// definition of complex
    /// </summary>
    public struct Complex {
        public float Real { get; private set; }
        public float Imaginary { get; private set; }

        public Complex(float pReal, float pImaginary) {
            this.Real = pReal;
            this.Imaginary = pImaginary;
        }

        public Complex Conjugate() {
            return new Complex(this.Real, -this.Imaginary);
        }

        public void setComplex(float pReal, float pImaginary) {
            this.Real = pReal;
            this.Imaginary = pImaginary;
        }

        public static Complex operator * (Complex pComplexA, Complex pComplexB) {
            return new Complex(pComplexA.Real * pComplexB.Real - pComplexA.Imaginary * pComplexB.Imaginary,
                pComplexA.Real * pComplexB.Imaginary + pComplexA.Imaginary * pComplexB.Real);
        }

        public static Complex operator * (Complex pComplexA, float pFactor) {
            return new Complex(pComplexA.Real * pFactor, pComplexA.Imaginary * pFactor);
        }

        public static Complex operator + (Complex pComplexA, Complex pComplexB) {
            return new Complex(pComplexA.Real + pComplexB.Real , pComplexA.Imaginary + pComplexB.Imaginary);
        }

        public static Complex operator - (Complex pComplexA, Complex pComplexB) {
            return new Complex(pComplexA.Real - pComplexB.Real, pComplexA.Imaginary - pComplexB.Imaginary);
        }
    }
}
