using System;
using UnityEngine;

namespace MiniFazendinha.Planting
{
    [DisallowMultipleComponent]
    public sealed class FarmInventory : MonoBehaviour
    {
        [SerializeField] InitialCropStock[] initialStock = Array.Empty<InitialCropStock>();
        [SerializeField] InitialItemStock[] initialItems = Array.Empty<InitialItemStock>();
        FarmInventoryModel model;

        public FarmInventoryModel Model { get { EnsureInitialized(); return model; } }
        public event Action Changed;
        public InitialCropStock[] InitialStock => initialStock;
        public InitialItemStock[] InitialItems => initialItems;

        void Awake() { EnsureInitialized(); }
        void EnsureInitialized()
        {
            if (model != null) return;
            model = new FarmInventoryModel(initialStock, initialItems);
            model.Changed += Notify;
        }

        void Notify() { Changed?.Invoke(); }
        void OnDestroy() { if (model != null) model.Changed -= Notify; }
        public int Quantity(InventoryItemDefinition item) => Model.Quantity(item);
        public bool CanReceive(InventoryItemDefinition item, int amount) => Model.CanReceive(item, amount);
        public bool TrySpend(InventoryItemDefinition item) => Model.TrySpend(item);
        public bool TryAdd(InventoryItemDefinition item, int amount) => Model.TryAdd(item, amount);
    }
}