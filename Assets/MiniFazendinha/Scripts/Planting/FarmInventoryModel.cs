using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace MiniFazendinha.Planting
{
    [Serializable]
    public sealed class InitialCropStock
    {
        public CropDefinition crop;
        [FormerlySerializedAs("seeds"), UnityEngine.Min(0)] public int quantity = 12;
    }

    [Serializable]
    public sealed class InitialItemStock
    {
        public InventoryItemDefinition item;
        [UnityEngine.Min(0)] public int quantity;
    }

    // One balance per item: planting spends crop stock; harvest replenishes that same item.
    public sealed class FarmInventoryModel
    {
        sealed class Stock { public InventoryItemDefinition item; public int quantity; }
        readonly Dictionary<string, Stock> stocks = new Dictionary<string, Stock>(StringComparer.Ordinal);
        bool transferring;
        public IEnumerable<InventoryItemDefinition> Items { get { foreach(var s in stocks.Values) yield return s.item; } }
        public int Used { get { int n=0; foreach(var s in stocks.Values)n+=s.quantity; return n; } }
        public int Capacity { get; private set; } = 2000;
        public void SetCapacity(int value) { Capacity = Math.Max(value, Used); }
        public event Action Changed;

        public FarmInventoryModel(IEnumerable<InitialCropStock> initialCrops)
            : this(initialCrops, Array.Empty<InitialItemStock>()) { }

        public FarmInventoryModel(IEnumerable<InitialCropStock> initialCrops, IEnumerable<InitialItemStock> initialItems)
        {
            if (initialCrops == null) throw new ArgumentNullException(nameof(initialCrops));
            foreach (var row in initialCrops)
            {
                if (row == null || row.crop == null || !row.crop.IsValid || row.quantity < 0)
                    throw new ArgumentException("Invalid initial crop stock.");
                AddInitial(row.crop, row.quantity);
            }
            if (initialItems == null) return;
            foreach (var row in initialItems)
            {
                if (row == null || row.item == null || !row.item.IsValid || row.quantity < 0)
                    throw new ArgumentException("Invalid initial item stock.");
                AddInitial(row.item, row.quantity);
            }
        }

        void AddInitial(InventoryItemDefinition item, int quantity)
        {
            if (stocks.ContainsKey(item.Identifier))
                throw new ArgumentException("Duplicate inventory item identifier: " + item.Identifier);
            stocks.Add(item.Identifier, new Stock { item = item, quantity = quantity });
        }

        bool Find(InventoryItemDefinition item, out Stock stock)
        {
            stock = null;
            return item != null && item.IsValid && stocks.TryGetValue(item.Identifier, out stock) && stock.item == item;
        }

        public int Quantity(InventoryItemDefinition item) => Find(item, out var stock) ? stock.quantity : 0;
        public bool CanReceive(InventoryItemDefinition item, int amount) =>
            !transferring && amount > 0 && amount <= Capacity-Used && Find(item, out var stock) && stock.quantity <= int.MaxValue - amount;

        public bool TryTransfer(InventoryItemDefinition item, int amount, Func<bool> receive)
        {
            if(transferring || amount<=0 || receive==null || !Find(item,out var stock) || stock.quantity<amount)return false;
            transferring=true;stock.quantity-=amount;
            try { if(!receive()){stock.quantity+=amount;return false;} }
            catch {stock.quantity+=amount;throw;}
            finally {transferring=false;}
            Changed?.Invoke();return true;
        }

        public bool TrySpend(InventoryItemDefinition item)
        {
            if (transferring || !Find(item, out var stock) || stock.quantity <= 0) return false;
            stock.quantity--;
            Changed?.Invoke();
            return true;
        }

        public bool TryAdd(InventoryItemDefinition item, int amount)
        {
            if (!CanReceive(item, amount)) return false;
            Find(item, out var stock);
            stock.quantity += amount;
            Changed?.Invoke();
            return true;
        }
    }
}
