using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Fluid {
    public struct InitElement {
        public Vector2 HeightTilde0;
        public Vector2 HeightTilde0Conjugate;
    };

    public struct CaculateElement {
        public Vector2 HeightData;
        public Vector2 HeightSlopeX;
        public Vector2 HeightSlopeY;
        public Vector2 HeightDX;
        public Vector2 HeightDY;
    };

    public class FFTWavesConfig {
        public const int ComputerShaderNumthreads = 4;

        public const int FFTRevertedIndexCacheLength = 4;
        public const int InitElementLength = 16;
        public const int CaculateElementLength = 40;

        public const float Eplision = 0.0000001f;
        public const float PhillipsDamping = 0.001f;
        public const float Gravity = 9.81f;

        public const float DefaultPeriod = 200.0f;
        public const float DefaultDisplacement = -2.0f;
    }

    public class GerstnerWavesConfig {
        public const float DefaultSpeed = 1.0f;
    }

    //SinWaves(transform, new Vector2(1.0f, 1.0f), Vector3.zero, this.WaveMat,
    //    new Vector2(256, 256), new Vector4(0f, 0.25f, 0.25f, 0.25f), new Vector4(5, 5, 5, 5),
    //    new Vector4(64, 32, 16, 8), Vector4.one);

    //GerstnerWaves(transform, new Vector2(1.0f, 1.0f), Vector3.zero, this.WaveMat, new Vector2(256, 256), 
    //    new Vector4(0f, 0.25f, 0.25f, 0.25f), new Vector4(5, 5, 5, 5), new Vector4(64, 32, 16, 8), Vector4.one, new Vector4(0.75f, 0.75f, 0.75f, 0.75f));

    //FFTWaves(transform, WaveComputerShader, this.WaveMat,  new Vector2(0.1f, 0.1f), Vector3.zero, 6,
    //    new Vector2(1.0f, 0), 5.0f, 128, FFTWavesConfig.DefaultPeriod, FFTWavesConfig.DefaultDisplacement, FFTWavesConfig.PhillipsDamping,
    //    new Vector4(0.0f, 0.25f, 0.5f, 0.75f), new Vector4(2, 2, 2, 2), new Vector4(200, 100, 50, 25), Vector4.one, new Vector4(0.25f, 0.25f, 0.25f, 0.25f));
}
