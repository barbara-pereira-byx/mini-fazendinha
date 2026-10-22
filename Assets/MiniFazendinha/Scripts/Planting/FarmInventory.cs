using System;
using UnityEngine;

namespace MiniFazendinha.Planting
{
    [DisallowMultipleComponent]
    public sealed class FarmInventory : MonoBehaviour
    {
        [SerializeField] InitialCropStock[] initialStock = Array.Empty<InitialCropStock>();
        [SerializeField] InitialItemStock[] initialItems = Array.Empty<InitialItemStock>();
        [SerializeField, Min(1)] int storageCapacity = 2000;
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
            model.SetCapacity(storageCapacity);
            model.Changed += Notify;
        }

        void Notify() { Changed?.Invoke(); }
        void OnDestroy() { if (model != null) model.Changed -= Notify; }
        public int Quantity(InventoryItemDefinition item) => Model.Quantity(item);
        public int Used => Model.Used;
        public int Capacity => Model.Capacity;
        public System.Collections.Generic.IEnumerable<InventoryItemDefinition> Items => Model.Items;
        public bool TryTransfer(InventoryItemDefinition item,int amount,Func<bool> receive)=>Model.TryTransfer(item,amount,receive);
        public bool CanReceive(InventoryItemDefinition item, int amount) => Model.CanReceive(item, amount);
        public bool TrySpend(InventoryItemDefinition item) => Model.TrySpend(item);
        public bool TryAdd(InventoryItemDefinition item, int amount) => Model.TryAdd(item, amount);
    }
}
