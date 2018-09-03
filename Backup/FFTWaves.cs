using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Fluid {
    public struct VertexOcean {
        public float Real, Imaginary;                  // htilde0
        public float _Real, _Imaginary;               // htilde0mk conjugate
        public float ox, oy, oz;                    // original position
    };

    public struct WaveVerticeStruct {               // structure used with discrete flourier transform
        public Complex Height;                      // wave height
        public Vector2 Displacement;                // displacement
        public Vector3 Normal;                      // normal

        public WaveVerticeStruct(Complex pHeight, Vector2 pDisplacement, Vector3 pNormal) {
            this.Height = pHeight;
            this.Displacement = pDisplacement;
            this.Normal = pNormal;
        }
    };

    public class FFTWaves {
        private FFTWavesCaculater m_Caculater = null;

        private GameObject m_WaveObject = null;
        private Mesh m_WaveMesh = null;
        private Renderer m_WaveRender = null;
        private Vector2 m_Size = Vector2.zero;
        private Vector2 m_SizePerCell = Vector2.zero;
        private Vector3 m_OriginPositon = Vector3.zero;
        private Material m_WaterRenderMat = null;
        

        public FFTWaves(Transform pParentTrans, Vector2 pSizePerCell, Vector3 pOriginPositon, Material pWaterRenderMat,
            uint pLog2N, Vector2 pWind, float pAmplitude, float pLength, float pFFTPeriod, float pDisplacementLambda) {
            this.createFFTWaves(pParentTrans, pSizePerCell, pOriginPositon, pWaterRenderMat, pLog2N, pWind, pAmplitude, pLength, pFFTPeriod, pDisplacementLambda);
        }

        private void createFFTWaves(Transform pParentTrans, Vector2 pSizePerCell, Vector3 pOriginPositon, Material pWaterRenderMat, 
            uint pLog2N, Vector2 pWind, float pAmplitude, float pLength, float pFFTPeriod, float pDisplacementLambda) {
            this.m_Caculater = new FFTWavesCaculater(pLog2N, pWind, pAmplitude, pLength, pFFTPeriod, pDisplacementLambda);

            this.m_SizePerCell = pSizePerCell;
            this.m_OriginPositon = pOriginPositon;
            this.m_WaterRenderMat = pWaterRenderMat;

            int widthOrHeight = (int)Mathf.Pow(2, pLog2N);
            this.m_Size = new Vector2(widthOrHeight, widthOrHeight);

            this.m_WaveObject = Utils.getInstance().buildMesh(widthOrHeight, widthOrHeight, this.m_OriginPositon, this.m_SizePerCell);
            this.m_WaveMesh = this.m_WaveObject.GetComponent<MeshFilter>().mesh;
            this.m_WaveRender = this.m_WaveObject.GetComponent<Renderer>();
            this.m_WaveRender.material = this.m_WaterRenderMat;
            this.m_WaveObject.transform.SetParent(pParentTrans);

            this.m_WaveMesh.vertices = this.m_Caculater.MeshVertices;
            this.m_WaveMesh.triangles = this.m_Caculater.MeshTriangles;
            this.m_WaveMesh.normals = this.m_Caculater.MeshNormals;
            //this.m_WaveMesh.tangents = this.m_Caculater.MeshTangents;
        }

        public void updateWaves(float pCurrentTime, bool pUseFFT) {
            this.m_Caculater.renderWaves(pCurrentTime, pUseFFT);
            this.m_WaveMesh.vertices = this.m_Caculater.MeshVertices;
            this.m_WaveMesh.triangles = this.m_Caculater.MeshTriangles;
            this.m_WaveMesh.normals = this.m_Caculater.MeshNormals;
        }
    }
}
