using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fluid {
    /// <summary>
    /// 
    /// </summary>
    public class FFTWavesCaculater {
        public Vector3[] MeshVertices { get; private set; }                            // vertices of ocean
        public Vector3[] MeshNormals { get; private set; }                              // normals of ocean
        public Vector4[] MeshTangents { get; private set; }                             // tangents of ocean
        public int[] MeshTriangles { get; private set; }                                // triangles indices of ocean

        private VertexOcean[] m_OceanVertices = null;                       // vertices buffer for calculate

        private FFT_2 m_FFTInstance;                                          // Fast Fourier Transform instance

        private float m_Gravity;                                            // gravity constant
        private uint m_Log2N;                                               // log of dimension by 2
        private uint m_Dimension, m_DimensionPlus1;                         // dimension -- N should be a power of 2
        private Vector2 m_Wind;                                             // wind parameter
        private float m_Amplitude;                                   // Amplitude of waves
        private float m_Length;                                             // length of waves
        private float m_FFTPeriod;                                           // period of waves
        private float m_DisplacementLambda = 0;                             // lambda of displacement

        private Complex[] h_tilde = null;                          
        private Complex[] h_tilde_slopex = null;
        private Complex[] h_tilde_slopez = null;
        private Complex[] h_tilde_dx = null;
        private Complex[] h_tilde_dz = null; 

        public FFTWavesCaculater(uint pLog2N, Vector2 pWind, float pAmplitude, float pLength, float pFFTPeriod, float pDisplacementLambda) {
            this.m_Gravity = 9.81f;
            this.m_Log2N = pLog2N;
            this.m_Dimension = (uint)(Math.Pow(2, pLog2N));
            this.m_DimensionPlus1 = this.m_Dimension + 1;
            this.m_Amplitude = pAmplitude;
            this.m_Wind = pWind;
            this.m_Length = pLength;
            this.m_FFTPeriod = pFFTPeriod;
            this.m_DisplacementLambda = pDisplacementLambda;

            this.m_FFTInstance = new FFT_2(pLog2N);
            this.m_OceanVertices = new VertexOcean[this.m_DimensionPlus1 * this.m_DimensionPlus1];
            
            this.MeshVertices = new Vector3[this.m_DimensionPlus1 * this.m_DimensionPlus1];
            this.MeshNormals = new Vector3[this.m_DimensionPlus1 * this.m_DimensionPlus1];
            this.MeshTriangles = new int[this.m_Dimension * this.m_Dimension * 6];

            h_tilde = Utils.getInstance().createComplexArray(this.m_Dimension * this.m_Dimension);
            h_tilde_slopex = Utils.getInstance().createComplexArray(this.m_Dimension * this.m_Dimension);
            h_tilde_slopez = Utils.getInstance().createComplexArray(this.m_Dimension * this.m_Dimension);
            h_tilde_dx = Utils.getInstance().createComplexArray(this.m_Dimension * this.m_Dimension);
            h_tilde_dz = Utils.getInstance().createComplexArray(this.m_Dimension * this.m_Dimension);

            int verticeIndex;
            Complex htilde0, htilde0mk_conj;
            for (int m_prime = 0; m_prime < this.m_DimensionPlus1; m_prime++) {
                for (int n_prime = 0; n_prime < this.m_DimensionPlus1; n_prime++) {
                    verticeIndex = (int) (m_prime * this.m_DimensionPlus1 + n_prime);

                    htilde0 = hTilde_0(n_prime, m_prime);
                    htilde0mk_conj = hTilde_0(-n_prime, -m_prime).Conjugate();

                    this.m_OceanVertices[verticeIndex] = new VertexOcean();
                    this.m_OceanVertices[verticeIndex].Real = htilde0.Real;
                    this.m_OceanVertices[verticeIndex].Imaginary = htilde0.Imaginary;
                    this.m_OceanVertices[verticeIndex]._Real = htilde0mk_conj.Real;
                    this.m_OceanVertices[verticeIndex]._Imaginary = htilde0mk_conj.Imaginary;

                    float originX = (n_prime - this.m_Dimension / 2.0f) * this.m_Length / this.m_Dimension;
                    float originY = 0.0f;
                    float originZ = (m_prime - this.m_Dimension / 2.0f) * this.m_Length / this.m_Dimension;
                    this.m_OceanVertices[verticeIndex].ox = originX;
                    this.m_OceanVertices[verticeIndex].oy = originY;
                    this.m_OceanVertices[verticeIndex].oz = originZ;

                    this.MeshVertices[verticeIndex] = new Vector3(originX, originY, originZ);
                    this.MeshNormals[verticeIndex] = new Vector3(0.0f, 1.0f, 0.0f);
                }
            }

            int indicesCount = 0;
            int indiceIndex = 0;
            for (int m_prime = 0; m_prime < this.m_Dimension; m_prime++) {
                for (int n_prime = 0; n_prime < this.m_Dimension; n_prime++) {
                    indiceIndex = (int)(m_prime * this.m_DimensionPlus1 + n_prime);

                    this.MeshTriangles[indicesCount++] = indiceIndex;               // two triangles
                    this.MeshTriangles[indicesCount++] = (int)(indiceIndex + this.m_DimensionPlus1);
                    this.MeshTriangles[indicesCount++] = (int)(indiceIndex + this.m_DimensionPlus1 + 1);
                    this.MeshTriangles[indicesCount++] = indiceIndex;
                    this.MeshTriangles[indicesCount++] = (int)(indiceIndex + this.m_DimensionPlus1 + 1);
                    this.MeshTriangles[indicesCount++] = indiceIndex + 1;
                }
            }
        }

        private float dispersion(int pXPrime, int pZPrime) {
            //float w_0 = 2.0f * Mathf.PI / 200.0f;
            float w_0 = 2.0f * Mathf.PI / this.m_FFTPeriod;
            float kx = Mathf.PI * (2 * pXPrime - this.m_Dimension) / this.m_Length;
            float kz = Mathf.PI * (2 * pZPrime - this.m_Dimension) / this.m_Length;
            return Mathf.Floor(Mathf.Sqrt(this.m_Gravity * Mathf.Sqrt(kx * kx + kz * kz)) / w_0) * w_0;
        }

        /// <summary>
        /// calculate phillips spectrum
        /// </summary>
        /// <param name="pXPrime"></param>
        /// <param name="pZPrime"></param>
        /// <returns></returns>
        private float phillips(int pXPrime, int pZPrime) {
            Vector2 k = new Vector2((float)(Math.PI* (2 * pXPrime - this.m_Dimension) / this.m_Length), (float)(Math.PI * (2 * pZPrime - this.m_Dimension) / this.m_Length));
            float k_length = k.magnitude;
            if (k_length < FFTWavesConfig.Eplision) return 0;

            float k_length2 = k_length * k_length;
            float k_length4 = k_length2 * k_length2;

            float k_dot_w = Vector2.Dot(k, this.m_Wind);
            float k_dot_w2 = k_dot_w * k_dot_w;

            float w_length = this.m_Wind.magnitude;
            float L = w_length * w_length / this.m_Gravity;
            float L2 = L * L;

            float l2 = L2 * FFTWavesConfig.Damping * FFTWavesConfig.Damping;

            return this.m_Amplitude * Mathf.Exp(-1.0f / (k_length2 * L2)) / k_length4 * k_dot_w2 * Mathf.Exp(-k_length2 * l2);
        }

        private Complex hTilde_0(int pXPrime, int pZPrime) {
            Complex r = Utils.getInstance().gaussianRandomVariable();
            return r * Mathf.Sqrt(phillips(pXPrime, pZPrime) / 2.0f);
        }

        private Complex hTilde(float t, int pXPrime, int pZPrime) {
            int index = (int)(pZPrime * this.m_DimensionPlus1 + pXPrime);

            Complex htilde0 = new Complex(this.m_OceanVertices[index].Real, m_OceanVertices[index].Imaginary);
            Complex htilde0mkconj = new Complex(m_OceanVertices[index]._Real, m_OceanVertices[index]._Imaginary);

            float omegat = dispersion(pXPrime, pZPrime) * t;

            float cos_ = Mathf.Cos(omegat);
            float sin_ = Mathf.Sin(omegat);

            Complex c0 = new Complex(cos_, sin_);
            Complex c1 = new Complex(cos_, -sin_);

            Complex res = htilde0 * c0 + htilde0mkconj * c1;

            return res;
        }   

        private void caculateWaveVerticeStruct(Vector2 pVerticePoint, float t, out WaveVerticeStruct pOutputData) {
            Complex height = new Complex(0.0f, 0.0f);
            Vector2 displacement = new Vector2(0.0f, 0.0f);
            Vector3 normal = new Vector3(0.0f, 0.0f, 0.0f);

            Complex c, htilde_c;
            Vector2 k;
            float kx, kz, k_length, k_dot_x;

            for (int primeZIndex = 0; primeZIndex < this.m_Dimension; primeZIndex++) {
                kz = 2.0f * Mathf.PI * (primeZIndex - this.m_Dimension / 2.0f) / this.m_Length;
                for (int primeXIndex = 0; primeXIndex < this.m_Dimension; primeXIndex++) {
                    kx = 2.0f * Mathf.PI * (primeXIndex - this.m_Dimension / 2.0f) / this.m_Length;
                    k = new Vector2(kx, kz);

                    k_length = k.magnitude;
                    k_dot_x = Vector2.Dot(k , pVerticePoint);

                    c = new Complex(Mathf.Cos(k_dot_x), Mathf.Sin(k_dot_x));
                    htilde_c = hTilde(t, primeXIndex, primeZIndex) * c;

                    height = height + htilde_c;

                    normal = normal + new Vector3(-kx * htilde_c.Imaginary, 0.0f, -kz * htilde_c.Imaginary);

                    if (k.magnitude < FFTWavesConfig.Eplision) {
                        continue;
                    }
                        
                    displacement = displacement + new Vector2(kx / k_length * htilde_c.Imaginary, kz / k_length * htilde_c.Imaginary);
                }
            }

            normal = (new Vector3(0.0f, 1.0f, 0.0f) - normal).normalized;

            pOutputData.Height = height;
            pOutputData.Displacement = displacement;
            pOutputData.Normal = normal;
        }

        public void evaluateWaves(float t) {
            int index;
            Vector2 verticePoint = Vector2.zero;
            WaveVerticeStruct oneVerticeData = new WaveVerticeStruct(new Complex(), Vector2.zero, Vector3.zero);
            for (int primeZIndex = 0; primeZIndex < this.m_Dimension; primeZIndex++) {
                for (int primeXIndex = 0; primeXIndex < this.m_Dimension; primeXIndex++) {
                    index = (int)(primeZIndex * this.m_DimensionPlus1 + primeXIndex);

                    verticePoint.Set(this.MeshVertices[index].x, this.MeshVertices[index].z);

                    this.caculateWaveVerticeStruct(verticePoint, t, out oneVerticeData);

                    this.MeshVertices[index].Set(
                        this.m_OceanVertices[index].ox + this.m_DisplacementLambda * oneVerticeData.Displacement.x,
                        oneVerticeData.Height.Real,
                        this.m_OceanVertices[index].oz + this.m_DisplacementLambda * oneVerticeData.Displacement.y);
                    this.MeshNormals[index].Set(oneVerticeData.Normal.x, oneVerticeData.Normal.y, oneVerticeData.Normal.z);

                    if (primeXIndex == 0 && primeZIndex == 0) {
                        this.MeshVertices[index + this.m_Dimension + this.m_DimensionPlus1 * this.m_Dimension].Set(
                            this.m_OceanVertices[index + this.m_Dimension + this.m_DimensionPlus1 * this.m_Dimension].ox + this.m_DisplacementLambda * oneVerticeData.Displacement.x,
                            oneVerticeData.Height.Real,
                            this.m_OceanVertices[index + this.m_Dimension + this.m_DimensionPlus1 * this.m_Dimension].oz + this.m_DisplacementLambda * oneVerticeData.Displacement.y);
                        this.MeshNormals[index + this.m_Dimension + this.m_DimensionPlus1 * this.m_Dimension].Set(oneVerticeData.Normal.x, oneVerticeData.Normal.y, oneVerticeData.Normal.z);
                    }
                    if (primeXIndex == 0) {
                        this.MeshVertices[index + this.m_Dimension].Set(
                            this.m_OceanVertices[index + this.m_Dimension].ox + this.m_DisplacementLambda * oneVerticeData.Displacement.x,
                            oneVerticeData.Height.Real,
                            this.m_OceanVertices[index + this.m_Dimension].oz + this.m_DisplacementLambda * oneVerticeData.Displacement.y);
                        this.MeshNormals[index + this.m_Dimension].Set(oneVerticeData.Normal.x, oneVerticeData.Normal.y, oneVerticeData.Normal.z);
                    }
                    if (primeZIndex == 0) {
                        this.MeshVertices[index + this.m_DimensionPlus1 * this.m_Dimension].Set(
                                this.m_OceanVertices[index + this.m_DimensionPlus1 * this.m_Dimension].ox + this.m_DisplacementLambda * oneVerticeData.Displacement.x,
                                oneVerticeData.Height.Real,
                                this.m_OceanVertices[index + this.m_DimensionPlus1 * this.m_Dimension].oz + this.m_DisplacementLambda * oneVerticeData.Displacement.y);
                        this.MeshNormals[index + this.m_DimensionPlus1 * this.m_Dimension].Set(oneVerticeData.Normal.x, oneVerticeData.Normal.y, oneVerticeData.Normal.z);
                    }
                }
            }
        }

        public void evaluateWavesFFT(float t) {
            float kx, kz, len;
            int index, index1;

            this.initFFTWaves(t);

            for (int m_prime = 0; m_prime < this.m_Dimension; m_prime++) {
                this.m_FFTInstance.caculateFFT(h_tilde, h_tilde, 1, (int)(m_prime * this.m_Dimension));
                this.m_FFTInstance.caculateFFT(h_tilde_slopex, h_tilde_slopex, 1, (int)(m_prime * this.m_Dimension));
                this.m_FFTInstance.caculateFFT(h_tilde_slopez, h_tilde_slopez, 1, (int)(m_prime * this.m_Dimension));
                this.m_FFTInstance.caculateFFT(h_tilde_dx, h_tilde_dx, 1, (int)(m_prime * this.m_Dimension));
                this.m_FFTInstance.caculateFFT(h_tilde_dz, h_tilde_dz, 1, (int)(m_prime * this.m_Dimension));
            }
            for (int n_prime = 0; n_prime < this.m_Dimension; n_prime++) {
                this.m_FFTInstance.caculateFFT(h_tilde, h_tilde, (int)this.m_Dimension, n_prime);
                this.m_FFTInstance.caculateFFT(h_tilde_slopex, h_tilde_slopex, (int)this.m_Dimension, n_prime);
                this.m_FFTInstance.caculateFFT(h_tilde_slopez, h_tilde_slopez, (int)this.m_Dimension, n_prime);
                this.m_FFTInstance.caculateFFT(h_tilde_dx, h_tilde_dx, (int)this.m_Dimension, n_prime);
                this.m_FFTInstance.caculateFFT(h_tilde_dz, h_tilde_dz, (int)this.m_Dimension, n_prime);
            }

            int sign;
            int[] signs = new int[2] { 1, -1 };
            Vector3 n;
            for (int primeZIndex = 0; primeZIndex < this.m_Dimension; primeZIndex++) {
                for (int primeXIndex = 0; primeXIndex < this.m_Dimension; primeXIndex++) {
                    index = (int)(primeZIndex * this.m_Dimension + primeXIndex);      // index into h_tilde..
                    index1 = (int)(primeZIndex * this.m_DimensionPlus1 + primeXIndex);    // index into vertices

                    sign = signs[(primeXIndex + primeZIndex) & 1];

                    h_tilde[index] = h_tilde[index] * sign;

                    // height
                    this.MeshVertices[index1].y = h_tilde[index].Real;

                    // displacement
                    h_tilde_dx[index] = h_tilde_dx[index] * sign;
                    h_tilde_dz[index] = h_tilde_dz[index] * sign;
                    this.MeshVertices[index1].x = this.m_OceanVertices[index1].ox + h_tilde_dx[index].Real * FFTWavesConfig.DefaultDisplacement;
                    this.MeshVertices[index1].z = this.m_OceanVertices[index1].oz + h_tilde_dz[index].Real * FFTWavesConfig.DefaultDisplacement;

                    // normal
                    h_tilde_slopex[index] = h_tilde_slopex[index] * sign;
                    h_tilde_slopez[index] = h_tilde_slopez[index] * sign;
                    n = new Vector3(-h_tilde_slopex[index].Real, 1.0f, -h_tilde_slopez[index].Real).normalized;
                    this.MeshNormals[index1].Set(n.x, n.y, n.z);

                    // for tiling
                    if (primeXIndex == 0 && primeZIndex == 0) {
                        this.MeshVertices[index1 + this.m_Dimension + this.m_DimensionPlus1 * this.m_Dimension].Set(
                            this.m_OceanVertices[index1 + this.m_Dimension + this.m_DimensionPlus1 * this.m_Dimension].ox + this.m_DisplacementLambda * h_tilde_dx[index].Real,
                            h_tilde[index].Real,
                            this.m_OceanVertices[index1 + this.m_Dimension + this.m_DimensionPlus1 * this.m_Dimension].oz + this.m_DisplacementLambda * h_tilde_dz[index].Real);
                        this.MeshNormals[index1 + this.m_Dimension + this.m_DimensionPlus1 * this.m_Dimension].Set(n.x, n.y, n.z);
                    }
                    if (primeXIndex == 0) {
                        this.MeshVertices[index1 + this.m_Dimension].Set(
                            this.m_OceanVertices[index1 + this.m_Dimension].ox + this.m_DisplacementLambda * h_tilde_dx[index].Real,
                            h_tilde[index].Real,
                            this.m_OceanVertices[index1 + this.m_Dimension].oz + this.m_DisplacementLambda * h_tilde_dz[index].Real);
                        this.MeshNormals[index1 + this.m_Dimension].Set(n.x, n.y, n.z);
                    }
                    if (primeZIndex == 0) {
                        this.MeshVertices[index1 + this.m_DimensionPlus1 * this.m_Dimension].Set(
                                this.m_OceanVertices[index1 + this.m_DimensionPlus1 * this.m_Dimension].ox + this.m_DisplacementLambda * h_tilde_dx[index].Real,
                                h_tilde[index].Real,
                                this.m_OceanVertices[index1 + this.m_DimensionPlus1 * this.m_Dimension].oz + this.m_DisplacementLambda * h_tilde_dz[index].Real);
                        this.MeshNormals[index1 + this.m_DimensionPlus1 * this.m_Dimension].Set(n.x, n.y, n.z);
                    }
                }
            }
        }

        private void initFFTWaves(float t) {
            float kx, kz, len;

            for (int primeZIndex = 0; primeZIndex < this.m_Dimension; primeZIndex++) {
                kz = Mathf.PI * (2.0f * primeZIndex - this.m_Dimension) / this.m_Length;
                for (int primeXIndex = 0; primeXIndex < this.m_Dimension; primeXIndex++) {
                    kx = Mathf.PI * (2 * primeXIndex - this.m_Dimension) / this.m_Length;
                    len = Mathf.Sqrt(kx * kx + kz * kz);
                    int index = (int)(primeZIndex * this.m_Dimension + primeXIndex);

                    h_tilde[index] = hTilde(t, primeXIndex, primeZIndex);
                    h_tilde_slopex[index] = h_tilde[index] * new Complex(0, kx);
                    h_tilde_slopez[index] = h_tilde[index] * new Complex(0, kz);
                    if (len < FFTWavesConfig.Eplision) {
                        h_tilde_dx[index] = new Complex(0.0f, 0.0f);
                        h_tilde_dz[index] = new Complex(0.0f, 0.0f);
                    } else {
                        h_tilde_dx[index] = h_tilde[index] * new Complex(0, -kx / len);
                        h_tilde_dz[index] = h_tilde[index] * new Complex(0, -kz / len);
                    }
                }
            }
        }

        public void renderWaves(float pCurrentTime, bool pFFTWave) {
            if(pFFTWave) {
                evaluateWavesFFT(pCurrentTime);
            } else {
                evaluateWaves(pCurrentTime);
            }
            
            
        }
    }
}

