using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Fluid {
    public class GerstnerWaves : MonoBehaviour {
        public Material WavesRenderMat = null;
        public Vector2 Size = Vector2.zero;
        public Vector2 SizePerCell = Vector2.zero;
        public Vector3 OriginPositon = Vector3.zero;
        public Vector4 Wind = Vector2.zero;
        public Vector4 Amplitude = Vector4.zero;
        public Vector4 WaveLength = Vector4.zero;
        public Vector4 Speed = Vector4.zero;
        public Vector4 SteepnessOfWaves = Vector4.zero;

        private GameObject m_WavesObject = null;
        private bool m_ParmentValid = false;

        private void Awake() {
            this.m_ParmentValid = this.checkParmentValid();
        }

        private void Start() {
            if(this.m_ParmentValid) {
                this.setGerstnerWaves(this.SizePerCell, this.OriginPositon, this.WavesRenderMat, this.Size, this.Wind,
                this.Amplitude, this.WaveLength, this.Speed, this.SteepnessOfWaves);
            }
        }

        private bool checkParmentValid() {
            if (this.WavesRenderMat != null && this.Size.magnitude > 0 && this.Amplitude.magnitude > 0 && this.WaveLength.magnitude > 0 && this.Speed.magnitude > 0) {
                return true;
            }

            return false;
        }

        private void setGerstnerWaves(Vector2 pSizePerCell, Vector3 pOriginPositon, Material pWaterRenderMat, Vector2 pMeshSize, 
            Vector4 pWindDirection, Vector4 pAmplitude, Vector4 pWaveLength, Vector4 pSpeed, Vector4 pSteepnessOfWaves) {
            this.Size = pMeshSize;
            this.SizePerCell = pSizePerCell;
            this.OriginPositon = pOriginPositon;
            this.WavesRenderMat = pWaterRenderMat;
            this.Wind = pWindDirection;
            this.Amplitude = pAmplitude;
            this.WaveLength = pWaveLength;
            this.Speed = pSpeed;
            this.SteepnessOfWaves = pSteepnessOfWaves;
            
            this.m_WavesObject = Utils.getInstance().buildMesh((int)this.Size.x, (int)this.Size.y, this.OriginPositon, this.SizePerCell);
            this.m_WavesObject.GetComponent<Renderer>().material = this.WavesRenderMat;
            this.m_WavesObject.transform.SetParent(transform);

            this.setWaves(this.Wind, this.Amplitude, this.WaveLength, this.Speed, this.SteepnessOfWaves);
        }

        public void setWaves(Vector2 pWindDirection, Vector4 pAmplitude, Vector4 pWaveLength, Vector4 pSpeed, Vector4 pSteepnessOfWaves) {
            this.WavesRenderMat.SetVector("_Amplitude", pAmplitude);
            this.WavesRenderMat.SetVector("_WaveLength", pWaveLength);
            this.WavesRenderMat.SetVector("_Direction", pWindDirection);
            this.WavesRenderMat.SetVector("_Speed", pSpeed);
            this.WavesRenderMat.SetVector("_SteepnessOfWaves", pSteepnessOfWaves);
        }
    }
}
