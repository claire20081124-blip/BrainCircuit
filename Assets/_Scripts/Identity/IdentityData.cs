using System;
using System.Collections.Generic;

namespace RunLight.Identity
{
    /// <summary>
    /// 自我認同值系統的可序列化容器(供 JsonUtility 使用)。
    /// </summary>
    [Serializable]
    public class IdentityData
    {
        [Serializable]
        public struct Entry
        {
            public EmotionDimension dimension;
            public float value;
        }

        public List<Entry> entries = new();
    }
}
