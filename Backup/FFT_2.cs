using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Fluid {
    public class FFT_2 {
        private const float PIMultply2 = Mathf.PI* 2;

        private uint m_DataLength;
        private uint m_Log2N;
        private uint[] m_RevertedIndexs;
        private Complex[][] T;
        private int m_TempIndex = 0;
        private Complex[][] m_TempList;

        public FFT_2(uint pLog2N) {
            this.m_Log2N = pLog2N;
            this.m_DataLength = (uint)(Math.Pow(2, pLog2N));

            this.m_RevertedIndexs = new uint[this.m_DataLength];
            for(uint revertIndex = 0; revertIndex < this.m_DataLength; ++revertIndex) {
                this.m_RevertedIndexs[revertIndex] = this.reverseBit(revertIndex, this.m_Log2N);
            }

            this.T = new Complex[this.m_Log2N][];
            uint pow2 = 1;
            for (int tIndex = 0; tIndex < m_Log2N; ++tIndex) {
                T[tIndex] = new Complex[pow2];
                for (uint j = 0; j < pow2; j++) {
                    T[tIndex][j] = this.caculateExpXAndN(j, pow2 * 2);
                }
                    
                pow2 *= 2;
            }

            this.m_TempList = new Complex[2][];
            this.m_TempList[0] = Utils.getInstance().createComplexArray(this.m_DataLength);
            this.m_TempList[1] = Utils.getInstance().createComplexArray(this.m_DataLength);
        }

        public void caculateFFT(Complex[] pInputData, Complex[] pOutputData, int pStride, int pOffset) {
            this.m_TempIndex = 0;
            for (int i = 0; i < this.m_DataLength; i++) {
                this.m_TempList[m_TempIndex][i] = pInputData[this.m_RevertedIndexs[i] * pStride + pOffset];
            } 

            uint loops = this.m_DataLength >> 1;
            int size = 1 << 1;
            int size_over_2 = 1;
            int w_ = 0;
            for (int i = 1; i <= this.m_Log2N; i++) {
                m_TempIndex ^= 1;
                for (int j = 0; j < loops; j++) {
                    for (int k = 0; k < size_over_2; k++) {
                        this.m_TempList[m_TempIndex][size * j + k] = 
                                        this.m_TempList[m_TempIndex ^ 1][size * j + k] +
                                        this.m_TempList[m_TempIndex ^ 1][size * j + size_over_2 + k] * T[w_][k];
                    }

                    for (int k = size_over_2; k < size; k++) {
                        this.m_TempList[m_TempIndex][size * j + k] = 
                                        this.m_TempList[m_TempIndex ^ 1][size * j - size_over_2 + k] -
                                        this.m_TempList[m_TempIndex ^ 1][size * j + k] * T[w_][k - size_over_2];
                    }
                }
                loops >>= 1;
                size <<= 1;
                size_over_2 <<= 1;
                w_++;
            }

            for (int i = 0; i < this.m_DataLength; i++) {
                pOutputData[i * pStride + pOffset] = this.m_TempList[m_TempIndex][i];
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
            return new Complex(Mathf.Cos(FFT_2.PIMultply2 * pValue / pHeightM), Mathf.Sin(FFT_2.PIMultply2 * pValue / pHeightM));
        }
    }
}
