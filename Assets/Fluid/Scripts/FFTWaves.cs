using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Fluid {
    public class FFTWaves : MonoBehaviour {
        //----------- required parments. begin ---------
        public ComputeShader FFTComputeShader;
        public Material WavesRenderMat = null;
        public Vector2 SizePerCell = Vector2.zero;
        public Vector3 OriginPositon = Vector3.zero;
        public Vector2[] WavesOriginOffsetList = null;

        public uint Log2N = 0;
        //----------- required parments. end ---------

        //----------fft parments. begin --------------
        public Vector2 FFTWind = Vector2.zero;
        public float FFTAmplitude = 0;
        public float FFTWaveLength = 0;
        public float FFTWavePeriod = 0;
        public float FFTDisplacementLambda = 0;
        public float FFTPhillipsDamping = 0;
        //----------fft parments. end --------------

        //----------gerstner parments. begin --------------
        public Vector4 GerstnerWind = Vector2.zero;
        public Vector4 GerstnerAmplitude = Vector4.zero;
        public Vector4 GerstnerWaveLength = Vector4.zero;
        public Vector4 GerstnerSpeed = Vector4.zero;
        public Vector4 GerstnerSteepnessOfWaves = Vector4.zero;
        //----------gerstner parments. end --------------

        private bool m_ParmentValid = false;
        private int Dimension = 0;
        private int DimensionPlus1 = 0;
        private List<GameObject> m_WaveObjectList = null;
        private List<Mesh> m_WaveMeshList = null;
        private List<Vector3[]> m_MeshVerticesList = null;
        private List<Vector3[]> m_MeshNormalsList = null;

        private GameObject m_FlagWaveObject = null;
        private Mesh m_FlagWaveMesh = null;
        private Vector3[] m_FlagVerticesInitPosition = null;
        private Vector3[] m_FlagNormalInitValue = null;
        private Vector3[] m_FlagMeshVertices = null;
        private Vector3[] m_FlagMeshNormals = null;

        //----------fft parments. begin --------------
        private uint[] m_ShaderFFTRevertedIndexCacheElements;
        private InitElement[] m_ShaderInitElement = null;
        private CaculateElement[] m_ShaderCaculateElement = null;
        private CaculateElement[] m_ShaderOutputElement = null;
        private ComputeBuffer m_ShaderBufferFFTRevertedIndexCache = null;
        private ComputeBuffer m_ShaderBufferInitElement = null;
        private ComputeBuffer m_ShaderBufferCaculateElement = null;
        private int m_InitWavesKernelID = -1;
        private int m_HorizontalKernelID = -1;
        private int m_VerticalKernelID = -1;
        private int m_ThreadGroupCount = 0;
        //----------fft parments. end --------------

        void Awake() {
            this.m_ParmentValid = this.checkParmentValid();

            if(this.m_ParmentValid) {
                this.m_WaveObjectList = new List<GameObject>();
                this.m_WaveMeshList = new List<Mesh>();
                this.m_MeshVerticesList = new List<Vector3[]>();
                this.m_MeshNormalsList = new List<Vector3[]>();
            }
        }

        void Start() {
            if (this.m_ParmentValid) {
                if (this.WavesOriginOffsetList != null) {
                    this.Dimension = (int)Mathf.Pow(2, this.Log2N);
                    this.DimensionPlus1 = this.Dimension + 1;
                }

                this.initWaves();
            }
        }

        private void Update() {
            this.setWaves(Time.realtimeSinceStartup);
        }

        private bool checkParmentValid() {
            if(this.FFTComputeShader != null && this.WavesRenderMat != null && this.SizePerCell.magnitude > 0 && 
                this.FFTWind.magnitude > 0 && this.FFTAmplitude > 0 && this.FFTWaveLength > 0 &&  this.FFTWavePeriod > 0 && 
                this.GerstnerWind.magnitude > 0 && this.GerstnerAmplitude.magnitude > 0 && this.GerstnerWaveLength.magnitude > 0 && this.GerstnerSpeed.magnitude > 0) {
                return true;
            }

            return false;
        }

        private void initWaves() {
            if (this.m_ParmentValid) {
                //create mesh
                this.m_FlagWaveObject = Utils.getInstance().buildMesh(Dimension, Dimension, this.OriginPositon, this.SizePerCell);
                this.m_FlagWaveObject.GetComponent<Renderer>().material = this.WavesRenderMat;
                this.m_FlagWaveObject.transform.SetParent(transform);
                this.m_FlagWaveMesh = this.m_FlagWaveObject.GetComponent<MeshFilter>().mesh;

                for (int waveIndex = 0; waveIndex < this.WavesOriginOffsetList.Length; ++waveIndex) {
                    Vector2 waveOffset = this.WavesOriginOffsetList[waveIndex];
                    Vector3 validOrigin = new Vector3(waveOffset.x, 0, waveOffset.y) + this.OriginPositon;

                    GameObject oneWaveObject = Utils.getInstance().buildMesh(Dimension, Dimension, validOrigin, this.SizePerCell);
                    oneWaveObject.GetComponent<Renderer>().material = this.WavesRenderMat;
                    oneWaveObject.transform.SetParent(transform);
                    Mesh oneWaveMesh = oneWaveObject.GetComponent<MeshFilter>().mesh;

                    this.m_WaveObjectList.Add(oneWaveObject);
                    this.m_WaveMeshList.Add(oneWaveMesh);
                    this.m_MeshVerticesList.Add(Utils.getInstance().CreateArray<Vector3>(this.DimensionPlus1 * this.DimensionPlus1));
                    this.m_MeshNormalsList.Add(Utils.getInstance().CreateArray<Vector3>(this.DimensionPlus1 * this.DimensionPlus1));
                }

                this.setFFTParments(this.FFTWind, this.FFTAmplitude, this.FFTWaveLength, this.FFTWavePeriod, this.FFTDisplacementLambda, this.FFTPhillipsDamping);
                this.setGerstnerWavesParments(this.GerstnerWind, this.GerstnerAmplitude, this.GerstnerWaveLength, this.GerstnerSpeed, this.GerstnerSteepnessOfWaves);
            }
        }

        private void setFFTParments(Vector2 pFFTWind, float pFFTAmplitude, float pFFTWaveLength, float pFFTWavePeriod, float pFFTDisplacementLambda, float pFFTPhillipsDamping) {
            if (this.m_ParmentValid) {
                this.FFTWind = pFFTWind;
                this.FFTAmplitude = pFFTAmplitude;
                this.FFTWaveLength = pFFTWaveLength;
                this.FFTWavePeriod = pFFTWavePeriod;
                this.FFTDisplacementLambda = pFFTDisplacementLambda;
                this.FFTPhillipsDamping = pFFTPhillipsDamping;

                //init fft parments.
                this.m_ShaderFFTRevertedIndexCacheElements = new uint[this.Dimension];
                for (uint revertIndex = 0; revertIndex < this.Dimension; ++revertIndex) {
                    this.m_ShaderFFTRevertedIndexCacheElements[revertIndex] = this.reverseBit(revertIndex, this.Log2N);
                }

                int dimSquare = this.Dimension * this.Dimension;
                int vierticeSquare = this.DimensionPlus1 * this.DimensionPlus1;

                this.m_ShaderBufferFFTRevertedIndexCache = new ComputeBuffer(this.Dimension, FFTWavesConfig.FFTRevertedIndexCacheLength);
                this.m_ShaderBufferInitElement = new ComputeBuffer(dimSquare, FFTWavesConfig.InitElementLength);
                this.m_ShaderBufferCaculateElement = new ComputeBuffer(dimSquare, FFTWavesConfig.CaculateElementLength);

                this.m_ShaderInitElement = new InitElement[dimSquare];
                this.m_ShaderCaculateElement = new CaculateElement[dimSquare];
                this.m_ShaderOutputElement = new CaculateElement[dimSquare];
                for (int m_prime = 0; m_prime < this.Dimension; m_prime++) {
                    for (int n_prime = 0; n_prime < this.Dimension; n_prime++) {
                        int eleIndex = m_prime * this.Dimension + n_prime;

                        this.m_ShaderCaculateElement[eleIndex] = new CaculateElement();
                        this.m_ShaderCaculateElement[eleIndex].HeightData = Vector2.zero;
                        this.m_ShaderCaculateElement[eleIndex].HeightSlopeX = Vector2.zero;
                        this.m_ShaderCaculateElement[eleIndex].HeightSlopeY = Vector2.zero;
                        this.m_ShaderCaculateElement[eleIndex].HeightDX = Vector2.zero;
                        this.m_ShaderCaculateElement[eleIndex].HeightDY = Vector2.zero;

                        Complex htildeBegin = hTilde_0(n_prime, m_prime);
                        Complex htildeBeginConjugate = hTilde_0(-n_prime, -m_prime).Conjugate();

                        this.m_ShaderInitElement[eleIndex] = new InitElement();
                        this.m_ShaderInitElement[eleIndex].HeightTilde0 = new Vector2(htildeBegin.Real, htildeBegin.Imaginary);
                        this.m_ShaderInitElement[eleIndex].HeightTilde0Conjugate = new Vector2(htildeBeginConjugate.Real, htildeBeginConjugate.Imaginary);
                    }
                }

                this.m_FlagNormalInitValue = Utils.getInstance().CreateArray<Vector3>(vierticeSquare);
                this.m_FlagMeshVertices = Utils.getInstance().CreateArray<Vector3>(vierticeSquare);
                this.m_FlagMeshNormals = Utils.getInstance().CreateArray<Vector3>(vierticeSquare);

                this.m_FlagVerticesInitPosition = this.m_FlagWaveMesh.vertices;
                int verticeIndex;
                for (int m_prime = 0; m_prime < this.DimensionPlus1; m_prime++) {
                    for (int n_prime = 0; n_prime < this.DimensionPlus1; n_prime++) {
                        verticeIndex = (int)(m_prime * this.DimensionPlus1 + n_prime);

                        float originX = (n_prime - this.Dimension / 2.0f) * this.FFTWaveLength / this.Dimension + this.m_FlagVerticesInitPosition[verticeIndex].x;
                        float originY = this.m_FlagVerticesInitPosition[verticeIndex].y;
                        float originZ = (m_prime - this.Dimension / 2.0f) * this.FFTWaveLength / this.Dimension + this.m_FlagVerticesInitPosition[verticeIndex].z;
                        //float originX = this.m_FlagVerticesInitPosition[verticeIndex].x;
                        //float originY = this.m_FlagVerticesInitPosition[verticeIndex].y;
                        //float originZ = this.m_FlagVerticesInitPosition[verticeIndex].z;
                        this.m_FlagVerticesInitPosition[verticeIndex].Set(originX, originY, originZ);
                        this.m_FlagMeshVertices[verticeIndex].Set(originX, originY, originZ);
                        this.m_FlagNormalInitValue[verticeIndex] = new Vector3(0.0f, 1.0f, 0.0f);
                        this.m_FlagMeshNormals[verticeIndex] = new Vector3(0.0f, 1.0f, 0.0f);
                    }
                }

                //init computer parments.
                this.m_InitWavesKernelID = this.FFTComputeShader.FindKernel("InitFFTWaves");
                this.m_HorizontalKernelID = this.FFTComputeShader.FindKernel("HorizontalFFT");
                this.m_VerticalKernelID = this.FFTComputeShader.FindKernel("VerticalFFT");
                this.m_ThreadGroupCount = this.Dimension / FFTWavesConfig.ComputerShaderNumthreads;

                this.m_ShaderBufferFFTRevertedIndexCache.SetData(this.m_ShaderFFTRevertedIndexCacheElements);
                this.m_ShaderBufferInitElement.SetData(this.m_ShaderInitElement);
                this.m_ShaderBufferCaculateElement.SetData(this.m_ShaderCaculateElement);

                this.FFTComputeShader.SetInt("_Dimension", this.Dimension);
                this.FFTComputeShader.SetInt("_Log2N", (int)this.Log2N);
                this.FFTComputeShader.SetFloat("_WaveLength", this.FFTWaveLength);

                this.FFTComputeShader.SetBuffer(this.m_InitWavesKernelID, "InitBuff", this.m_ShaderBufferInitElement);
                this.FFTComputeShader.SetBuffer(this.m_InitWavesKernelID, "OutputBuff", this.m_ShaderBufferCaculateElement);

                this.FFTComputeShader.SetBuffer(this.m_HorizontalKernelID, "FFTRevertedIndexCache", this.m_ShaderBufferFFTRevertedIndexCache);
                this.FFTComputeShader.SetBuffer(this.m_HorizontalKernelID, "InitBuff", this.m_ShaderBufferInitElement);
                this.FFTComputeShader.SetBuffer(this.m_HorizontalKernelID, "OutputBuff", this.m_ShaderBufferCaculateElement);

                this.FFTComputeShader.SetBuffer(this.m_VerticalKernelID, "FFTRevertedIndexCache", this.m_ShaderBufferFFTRevertedIndexCache);
                this.FFTComputeShader.SetBuffer(this.m_VerticalKernelID, "InitBuff", this.m_ShaderBufferInitElement);
                this.FFTComputeShader.SetBuffer(this.m_VerticalKernelID, "OutputBuff", this.m_ShaderBufferCaculateElement);
            }
        }

        private void setGerstnerWavesParments(Vector4 pGerstnerWind, Vector4 pGerstnerAmplitude, Vector4 pGerstnerWaveLength, Vector4 pGerstnerSpeed, Vector4 pGerstnerSteepnessOfWaves) {
            if (this.m_ParmentValid) {
                this.GerstnerWind = pGerstnerWind;
                this.GerstnerAmplitude = pGerstnerAmplitude;
                this.GerstnerWaveLength = pGerstnerWaveLength;
                this.GerstnerSpeed = pGerstnerSpeed;
                this.GerstnerSteepnessOfWaves = pGerstnerSteepnessOfWaves;

                this.WavesRenderMat.SetVector("_Amplitude", pGerstnerAmplitude);
                this.WavesRenderMat.SetVector("_WaveLength", pGerstnerWaveLength);
                this.WavesRenderMat.SetVector("_Direction", pGerstnerWind);
                this.WavesRenderMat.SetVector("_Speed", pGerstnerSpeed);
                this.WavesRenderMat.SetVector("_SteepnessOfWaves", pGerstnerSteepnessOfWaves);
            }
        }

        public void setWaves(float pCurrentTime) {
            if (this.m_ParmentValid) {
                this.updateFFTWaves(pCurrentTime);

                this.WavesRenderMat.SetFloat("_CurrentTime", pCurrentTime);
            }
        }

        private void updateFFTWaves(float pCurrentTime) {
            if (this.m_ParmentValid) {
                /* disptch computer shader to compute fft. */
                //set time.
                this.FFTComputeShader.SetFloat("_CurrentTime", pCurrentTime);
                //init waves
                this.FFTComputeShader.Dispatch(this.m_InitWavesKernelID, this.m_ThreadGroupCount, this.m_ThreadGroupCount, 1);
                //horizontal fft.
                this.FFTComputeShader.Dispatch(this.m_HorizontalKernelID, this.m_ThreadGroupCount, 1, 1);
                //vertical fft.
                this.FFTComputeShader.Dispatch(this.m_VerticalKernelID, this.m_ThreadGroupCount, 1, 1);
                /* disptch computer shader to compute fft. */

                //get data from gpu and render waves.
                this.m_ShaderBufferCaculateElement.GetData(this.m_ShaderOutputElement);

                int[] signs = new int[2] { 1, -1 };
                for (int primeZIndex = 0; primeZIndex < this.Dimension; primeZIndex++) {
                    for (int primeXIndex = 0; primeXIndex < this.Dimension; primeXIndex++) {
                        int index = (int)(primeZIndex * this.Dimension + primeXIndex);      // index into h_tilde..
                        int indexVertice = (int)(primeZIndex * this.DimensionPlus1 + primeXIndex);    // index into vertices

                        int sign = signs[(primeXIndex + primeZIndex) & 1];

                        this.m_ShaderOutputElement[index].HeightData = this.m_ShaderOutputElement[index].HeightData * sign;

                        // height
                        this.m_FlagMeshVertices[indexVertice].y = this.m_ShaderOutputElement[index].HeightData.x;

                        // displacement
                        this.m_ShaderOutputElement[index].HeightDX *= sign;
                        this.m_ShaderOutputElement[index].HeightDY *= sign;
                        this.m_FlagMeshVertices[indexVertice].x = this.m_FlagVerticesInitPosition[indexVertice].x
                            + this.m_ShaderOutputElement[index].HeightDX.x * this.FFTDisplacementLambda;
                        this.m_FlagMeshVertices[indexVertice].z = this.m_FlagVerticesInitPosition[indexVertice].z +
                            this.m_ShaderOutputElement[index].HeightDY.x * this.FFTDisplacementLambda;

                        // normal
                        this.m_ShaderOutputElement[index].HeightSlopeX *= sign;
                        this.m_ShaderOutputElement[index].HeightSlopeY *= sign;
                        Vector3 n = new Vector3(-this.m_ShaderOutputElement[index].HeightSlopeX.x, 1.0f, -this.m_ShaderOutputElement[index].HeightSlopeY.x).normalized;
                        this.m_FlagMeshNormals[indexVertice].Set(n.x, n.y, n.z);

                        //for tiling
                        if (primeXIndex == 0 && primeZIndex == 0) {
                            int setIndex = indexVertice + this.Dimension + this.DimensionPlus1 * this.Dimension;
                            this.m_FlagMeshVertices[setIndex].Set(
                                this.m_FlagVerticesInitPosition[setIndex].x + this.m_ShaderOutputElement[index].HeightDX.x * this.FFTDisplacementLambda, 
                                this.m_FlagMeshVertices[indexVertice].y,
                                this.m_FlagVerticesInitPosition[setIndex].z + this.m_ShaderOutputElement[index].HeightDY.x * this.FFTDisplacementLambda);
                            this.m_FlagMeshNormals[setIndex].Set(n.x, n.y, n.z);
                        }
                        if (primeXIndex == 0) {
                            int setIndex = indexVertice + this.Dimension;
                            this.m_FlagMeshVertices[setIndex].Set(
                                this.m_FlagVerticesInitPosition[setIndex].x + this.m_ShaderOutputElement[index].HeightDX.x * this.FFTDisplacementLambda,
                                this.m_FlagMeshVertices[indexVertice].y,
                                this.m_FlagVerticesInitPosition[setIndex].z + this.m_ShaderOutputElement[index].HeightDY.x * this.FFTDisplacementLambda);
                            this.m_FlagMeshNormals[setIndex].Set(n.x, n.y, n.z);
                        }
                        if (primeZIndex == 0) {
                            int setIndex = indexVertice + this.DimensionPlus1 * this.Dimension;
                            this.m_FlagMeshVertices[setIndex].Set(
                                this.m_FlagVerticesInitPosition[setIndex].x + this.m_ShaderOutputElement[index].HeightDX.x * this.FFTDisplacementLambda,
                                this.m_FlagMeshVertices[indexVertice].y,
                                this.m_FlagVerticesInitPosition[setIndex].z + this.m_ShaderOutputElement[index].HeightDY.x * this.FFTDisplacementLambda);
                            this.m_FlagMeshNormals[setIndex].Set(n.x, n.y, n.z);
                        }

                        for (int offsetIndex = 0; offsetIndex < this.WavesOriginOffsetList.Length; ++offsetIndex) {
                            Vector3 verticeOffset = new Vector3(this.WavesOriginOffsetList[offsetIndex].x, 0, this.WavesOriginOffsetList[offsetIndex].y);
                            Vector3[] currentVerticesArray = this.m_MeshVerticesList[offsetIndex];
                            currentVerticesArray[indexVertice] = this.m_FlagMeshVertices[indexVertice] + verticeOffset;

                            //for tiling
                            if (primeXIndex == 0 && primeZIndex == 0) {
                                int setIndex = indexVertice + this.Dimension + this.DimensionPlus1 * this.Dimension;
                                currentVerticesArray[setIndex] = this.m_FlagMeshVertices[setIndex] + verticeOffset;
                            }
                            if (primeXIndex == 0) {
                                int setIndex = indexVertice + this.Dimension;
                                currentVerticesArray[setIndex] = this.m_FlagMeshVertices[setIndex] + verticeOffset;
                            }
                            if (primeZIndex == 0) {
                                int setIndex = indexVertice + this.DimensionPlus1 * this.Dimension;
                                currentVerticesArray[setIndex] = this.m_FlagMeshVertices[setIndex] + verticeOffset;
                            }
                        }
                    }
                }

                this.m_FlagWaveMesh.vertices = this.m_FlagMeshVertices;
                this.m_FlagWaveMesh.normals = this.m_FlagMeshNormals;
                for (int offsetIndex = 0; offsetIndex < this.WavesOriginOffsetList.Length; ++offsetIndex) {
                    this.m_WaveMeshList[offsetIndex].vertices = this.m_MeshVerticesList[offsetIndex];
                    this.m_WaveMeshList[offsetIndex].normals = this.m_FlagMeshNormals;
                }
            }
        }

        private float dispersion(int pXPrime, int pZPrime) {
            float w_0 = 2.0f * Mathf.PI / this.FFTWavePeriod;
            float kx = Mathf.PI * (2 * pXPrime - this.Dimension) / this.FFTWaveLength;
            float kz = Mathf.PI * (2 * pZPrime - this.Dimension) / this.FFTWaveLength;
            return Mathf.Floor(Mathf.Sqrt(FFTWavesConfig.Gravity * Mathf.Sqrt(kx * kx + kz * kz)) / w_0) * w_0;
        }

        /// <summary>
        /// calculate phillips spectrum
        /// </summary>
        /// <param name="pXPrime"></param>
        /// <param name="pZPrime"></param>
        /// <returns></returns>
        private float phillips(int pXPrime, int pZPrime) {
            Vector2 k = new Vector2((float)(Math.PI * (2 * pXPrime - this.Dimension) / this.FFTWaveLength),
                (float)(Math.PI * (2 * pZPrime - this.Dimension) / this.FFTWaveLength));
            float k_length = k.magnitude;
            if (k_length < FFTWavesConfig.Eplision) return 0;

            float k_length2 = k_length * k_length;
            float k_length4 = k_length2 * k_length2;

            float k_dot_w = Vector2.Dot(k.normalized, this.FFTWind.normalized);
            float k_dot_w2 = k_dot_w * k_dot_w;

            float w_length = this.FFTWind.magnitude;
            float L = w_length * w_length / FFTWavesConfig.Gravity;
            float L2 = L * L;

            float l2 = L2 * this.FFTPhillipsDamping * this.FFTPhillipsDamping;

            return this.FFTAmplitude * Mathf.Exp(-1.0f / (k_length2 * L2)) / k_length4 * k_dot_w2 * Mathf.Exp(-k_length2 * l2);
        }

        private Complex hTilde_0(int pXPrime, int pZPrime) {
            Complex r = Utils.getInstance().gaussianRandomVariable();
            return r * Mathf.Sqrt(phillips(pXPrime, pZPrime) / 2.0f);
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
    }
}
