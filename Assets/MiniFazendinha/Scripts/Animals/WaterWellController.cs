using UnityEngine;
using MiniFazendinha.Planting;

namespace MiniFazendinha.Animals
{
    [DisallowMultipleComponent]
    public sealed class WaterWellController : MonoBehaviour
    {
        [SerializeField] InventoryItemDefinition waterItem;
        [SerializeField, Min(1)] int productionAmount = 10;
        [SerializeField, Min(1f)] float rechargeSeconds = 30f;
        [SerializeField] bool startsReady = true;

        int available;
        bool collecting;
        double readyAt;

        public InventoryItemDefinition WaterItem => waterItem;
        public int Available => available;
        public float SecondsToReady => available > 0 ? 0f : Mathf.Max(0f, (float)(readyAt - Time.timeAsDouble));
        public event System.Action Changed;

        void Awake() { available = startsReady ? productionAmount : 0; readyAt = Time.timeAsDouble + rechargeSeconds; }

        void Update()
        {
            if (available > 0 || Time.timeAsDouble < readyAt) return;
            available = productionAmount;
            Changed?.Invoke();
        }

        public bool TryCollect(FarmInventory inventory)
        {
            if (collecting || available <= 0 || inventory == null || waterItem == null || !inventory.CanReceive(waterItem, available))
                return false;
            int amount = available;
            collecting = true;
            available = 0;
            try {if (!inventory.TryAdd(waterItem, amount)){available=amount;return false;}readyAt = Time.timeAsDouble + rechargeSeconds;Changed?.Invoke();return true;}
            finally {collecting=false;}
        }
    }
}
