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
        public Sprite     icon;
        public Sprite     inspectSprite;
        public GameObject inspectPrefab;   // 查看時顯示的 3D 模型（null → 色塊球）
        public string     noteText;        // 非空時查看顯示紙條文字（而非 3D 模型）
        public Color      placeholderColor = Color.white;
    }
}
