using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Fluid {
    public class Utils {
        #region GetInstance
        private static Utils m_UtilsInstance = null;
        public static Utils getInstance() {
            if (m_UtilsInstance == null) {
                m_UtilsInstance = new Utils();
            }
            return m_UtilsInstance;
        }
        #endregion

        //Mersenne uniform random generater
        private ulong m_RandomA = (ulong)(Math.Pow(2, 31) - 1);
        private ulong m_RandomB = 0;
        private ulong m_RandomM = (ulong)Math.Pow(7, 5);
        private ulong m_Seed = 1;
        private ulong m_CurrentRandomNum = 0;

        /// <summary>
        /// build rectangle surface mesh according to input parameters.
        /// </summary>
        /// <param name="pWidth">width of mesh</param>
        /// <param name="pHeight">height of mesh</param>
        /// <param name="pOriginPos">origin of mesh</param>
        /// <param name="pCellSize">size of cell</param>
        /// <returns></returns>
        public GameObject buildMesh(int pWidth, int pHeight, Vector3 pOriginPos, Vector2 pCellSize) {
            //create game object.
            GameObject meshObject = new GameObject("rectangle_mesh");

            meshObject.AddComponent<MeshFilter>();
            meshObject.AddComponent<MeshRenderer>();

            //Build fog mesh.
            Mesh fogMesh = new Mesh();
            meshObject.GetComponent<MeshFilter>().mesh = fogMesh;

            //Build positions and uv for vertices.
            int verticesH = pWidth + 1;
            int verticesV = pHeight + 1;

            Vector3[] fogVertices = new Vector3[verticesH * verticesV];
            Vector2[] fogUV = new Vector2[fogVertices.Length];

            Vector2 uvScale = new Vector2(1.0f / pWidth, 1.0f / pHeight);

            for (int hIndex = 0; hIndex < verticesV; ++hIndex) {
                for (int vIndex = 0; vIndex < verticesH; ++vIndex) {
                    fogVertices[hIndex * verticesH + vIndex] = pOriginPos + (new Vector3(pCellSize.x * vIndex, 0, pCellSize.y * hIndex));
                    fogUV[hIndex * verticesH + vIndex] = Vector2.Scale(uvScale, (new Vector2(pCellSize.x * vIndex, pCellSize.y * hIndex)));
                }
            }
            fogMesh.vertices = fogVertices;
            fogMesh.uv = fogUV;

            // Build triangle indices: 3 indices into vertex array for each triangle
            var triangles = new int[pHeight * pWidth * 6];
            var tIndex = 0;
            for (int y = 0; y < pHeight; y++) {
                for (int x = 0; x < pWidth; x++) {
                    // For each grid cell output two triangles
                    triangles[tIndex++] = (y * verticesH) + x;
                    triangles[tIndex++] = ((y + 1) * verticesH) + x;
                    triangles[tIndex++] = (y * verticesH) + x + 1;

                    triangles[tIndex++] = ((y + 1) * verticesH) + x;
                    triangles[tIndex++] = ((y + 1) * verticesH) + x + 1;
                    triangles[tIndex++] = (y * verticesH) + x + 1;
                }
            }
            // And assign them to the mesh
            fogMesh.triangles = triangles;

            fogMesh.RecalculateNormals();

            return meshObject;
        }

        /// <summary>
        /// create complex array that length is pLength.
        /// </summary>
        /// <param name="pLength"></param>
        /// <returns></returns>
        public Complex[] createComplexArray(uint pLength) {
            if(pLength >= uint.MaxValue) {
                return null;
            }

            Complex[] newCaculateList = new Complex[pLength];
            for (int comIndex = 0; comIndex < pLength; ++comIndex) {
                newCaculateList[comIndex] = new Complex(0, 0);
            }

            return newCaculateList;
        }

        public Vector2[] createVector2Array(int pLength) {
            if (pLength >= uint.MaxValue) {
                return null;
            }

            Vector2[] newVector2List = new Vector2[pLength];
            for (int comIndex = 0; comIndex < pLength; ++comIndex) {
                newVector2List[comIndex] = Vector2.zero;
            }

            return newVector2List;
        }

        public float uniformRandomVariable() {
            //this.m_CurrentRandomNum = (this.m_RandomA * this.m_CurrentRandomNum + 7) % this.m_RandomM;
            //return (float)((double)this.m_CurrentRandomNum / this.m_RandomM);

            return UnityEngine.Random.Range(0f, 1.0f);
        }

        public Complex gaussianRandomVariable() {
            float x1, x2, w;
            do {
                x1 = 2.0f * uniformRandomVariable() - 1.0f;
                x2 = 2.0f * uniformRandomVariable() - 1.0f;
                w = x1 * x1 + x2 * x2;
            } while (w >= 1.0f);
            w = Mathf.Sqrt((-2.0f * Mathf.Log(w)) / w);
            return new Complex(x1 * w, x2 * w);
        }

        public T[] CreateArray<T>(int pLength) {
            if (pLength >= uint.MaxValue) {
                return null;
            }

            T[] newVector2List = new T[pLength];
            for (int comIndex = 0; comIndex < pLength; ++comIndex) {
                newVector2List[comIndex] = default(T);
            }

            return newVector2List;
        }
    }
}
