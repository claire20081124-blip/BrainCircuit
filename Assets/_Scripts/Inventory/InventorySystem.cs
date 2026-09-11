using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunLight.Inventory
{
    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem Instance { get; private set; }

        private readonly List<InventoryItem> _items = new();

        public event Action OnChanged;

        public IReadOnlyList<InventoryItem> Items => _items;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Add(InventoryItem item)
        {
            _items.Add(item);
            OnChanged?.Invoke();
        }

        public bool Remove(string id)
        {
            var item = _items.Find(i => i.id == id);
            if (item == null) return false;
            _items.Remove(item);
            OnChanged?.Invoke();
            return true;
        }

        public bool Has(string id) => _items.Exists(i => i.id == id);

        public void Clear()
        {
            _items.Clear();
            OnChanged?.Invoke();
        }
    }
}
