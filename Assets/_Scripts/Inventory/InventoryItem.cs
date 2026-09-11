using UnityEngine;

namespace RunLight.Inventory
{
    public enum ItemType { Note, Key, Decoy, Rewind, Other }

    [System.Serializable]
    public class InventoryItem
    {
        public string   id;
        public string   displayName;
        public string   description;
        public ItemType type;
        public Sprite     icon;              // 背包格子小圖示
        public Sprite     inspectSprite;   // 備用 2D 圖（目前未使用）
        public GameObject inspectPrefab;   // 查看時顯示的 3D 模型（null → 色塊球）
        public Color      placeholderColor = Color.white;
    }
}
