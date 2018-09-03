using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Fluid {
    public class FFT {
        private const float PIMultply2 = Mathf.PI* 2;

        private uint m_DataLength;
        private uint m_Log2N;
        private Complex[] m_TempList = null;
        private Complex[] m_TempOutputList = null;

        public FFT(uint pLog2N) {
            this.m_Log2N = pLog2N;
            this.m_DataLength = (uint)(Math.Pow(2, pLog2N));
            this.m_TempList = Utils.getInstance().createComplexArray(this.m_DataLength);
            this.m_TempOutputList = Utils.getInstance().createComplexArray(this.m_DataLength);
        }

        public void caculateFFT(Complex[] pInputData, Complex[] pOutputData, int pStride, int pOffset) {
            for (uint dataIndex = 0; dataIndex < this.m_DataLength; ++dataIndex) {
                this.m_TempList[dataIndex].setComplex(
                    pInputData[dataIndex * pStride + pOffset].Real, 
                    pInputData[dataIndex * pStride + pOffset].Imaginary);
            }

            for(uint dataIndex = 0; dataIndex < this.m_DataLength; ++dataIndex) {
                uint validIndex = this.reverseBit(dataIndex, this.m_Log2N);
                this.m_TempOutputList[validIndex].setComplex(
                    this.m_TempList[dataIndex].Real,
                    this.m_TempList[dataIndex].Imaginary);
            }

            uint heightM = 1;
            for (uint heightIndex = 1; heightIndex <= this.m_Log2N; ++heightIndex) {
                heightM <<= 1;
                Complex omigaM = this.caculateExpXAndN(1, heightM);
                uint maxK = this.m_DataLength / heightM;
                for(int k = 0; k < maxK; ++k) {
                    Complex cacualteOmiga = new Complex(1, 0);

                    uint MDivide2 = heightM / 2;
                    uint offset = heightIndex * heightM;
                    for (int j = 0; j < MDivide2; ++j) {
                        Complex t = cacualteOmiga * this.m_TempOutputList[j + offset + MDivide2];
                        Complex u = new Complex(this.m_TempOutputList[offset + j].Real, this.m_TempOutputList[offset + j].Imaginary);
                        this.m_TempOutputList[offset + j].setComplex(t.Real + u.Real, t.Imaginary + u.Imaginary);
                        this.m_TempOutputList[offset + j + MDivide2].setComplex(u.Real - t.Real, u.Imaginary - t.Imaginary);

                        cacualteOmiga = cacualteOmiga * omigaM;
                    }
                }
            }

            for (uint dataIndex = 0; dataIndex < this.m_DataLength; ++dataIndex) {
                pOutputData[dataIndex * pStride + pOffset].setComplex(this.m_TempOutputList[dataIndex].Real, this.m_TempOutputList[dataIndex].Imaginary);
            }
        }

        /// <summary>
        /// calculate reverse bit of number.
        /// for example: pNum is 4 (Binary format is 100) as input number and pLog2N is 3, the returns number is 1 (binary format is 001). 
        /// </summary>
        /// <param name="pNum">number to be reversed</param>
        /// <param name="pLog2N">the length of lowst bits that will be reversed.</param>
        /// <returns></returns>
        private uint reverseBit(uint pNum, uint pLog2N) {
            uint reverseNum = 0;
            for (int bitIndex = 0; bitIndex < pLog2N; ++bitIndex) {
                reverseNum = (reverseNum << 1) + (pNum & 1);
                pNum >>= 1;
            }
            return reverseNum;
        }

        private Complex caculateExpXAndN(uint pValue, uint pHeightM) {
            return new Complex(Mathf.Cos(FFT.PIMultply2 * pValue / pHeightM), Mathf.Sin(FFT.PIMultply2 * pValue / pHeightM));
        }
    }
}
