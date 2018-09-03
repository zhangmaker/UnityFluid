using System;
using System.Collections.Generic;
using System.Text;

namespace Fluid {
    /// <summary>
    /// interface for pool
    /// </summary>
    public interface PoolTypeInterface {
        /// <summary>
        /// reset data.
        /// </summary>
        void reset();
    }

    /// <summary>
    /// definition of object pool
    /// </summary>
    /// <typeparam name="PoolType">type of pool</typeparam>
    public class ScriptObjectPool<PoolType> where PoolType : PoolTypeInterface, new() {
        #region getInsatnce
        private static ScriptObjectPool<PoolType> m_PoolInstance = null;
        public static ScriptObjectPool<PoolType> getInstance() {
            if (m_PoolInstance == null) {
                m_PoolInstance = new ScriptObjectPool<PoolType>();
                m_PoolInstance.InitPool(DefaultLength);
            }

            return m_PoolInstance;
        }
        #endregion

        private const int DefaultLength = 64;

        private PoolType[] m_Pool = null;
        private int m_Pool_Current_Index = 0;

        public void InitPool(int pInitLength) {
            this.m_Pool = new PoolType[pInitLength];
            this.m_Pool_Current_Index = pInitLength;

            for (int poolIndex = 0; poolIndex < pInitLength; ++poolIndex) {
                this.m_Pool[poolIndex] = new PoolType();
            }
        }

        public PoolType getPoolObject() {
            if (this.m_Pool_Current_Index <= 0) {
                PoolType oneType = new PoolType();
                oneType.reset();
                this.m_Pool_Current_Index = 0;

                return oneType;
            } else {
                --this.m_Pool_Current_Index;
                PoolType oneType = this.m_Pool[this.m_Pool_Current_Index];
                oneType.reset();

                return oneType;
            }
        }

        public void recycleObject(PoolType pObject) {
            if (this.m_Pool_Current_Index >= this.m_Pool.Length) {
                PoolType[] newPool = new PoolType[this.m_Pool.Length * 2];
                for (int poolIndex = 0; poolIndex < this.m_Pool.Length; ++poolIndex) {
                    newPool[poolIndex] = this.m_Pool[poolIndex];
                }

                this.m_Pool = newPool;
            }

            this.m_Pool[this.m_Pool_Current_Index] = pObject;
            ++this.m_Pool_Current_Index;
        }

        public void clearObject() {
            if (this.m_Pool_Current_Index >= this.m_Pool.Length) {
                for (int poolIndex = 0; poolIndex < this.m_Pool.Length; ++poolIndex) {
                    this.m_Pool[poolIndex].reset();
                }
            }

            this.m_Pool = null;
            this.m_Pool_Current_Index = 0;
        }
    }
}
