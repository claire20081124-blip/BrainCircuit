using System;
using System.Collections.Generic;

namespace RunLight.Flags
{
    /// <summary>
    /// 旗標系統的可序列化容器。
    /// Unity 的 JsonUtility 不支援 Dictionary,因此改用 List 儲存鍵值對。
    /// </summary>
    [Serializable]
    public class FlagData
    {
        [Serializable] public struct BoolEntry { public string key; public bool value; }
        [Serializable] public struct IntEntry { public string key; public int value; }
        [Serializable] public struct FloatEntry { public string key; public float value; }
        [Serializable] public struct StringEntry { public string key; public string value; }

        public List<BoolEntry> bools = new();
        public List<IntEntry> ints = new();
        public List<FloatEntry> floats = new();
        public List<StringEntry> strings = new();
    }
}
