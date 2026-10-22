using System;
using MiniFazendinha.Planting;
using UnityEngine;

namespace MiniFazendinha.Animals
{
    public enum FeedingPointKind { Food, Water }

    [DisallowMultipleComponent]
    public sealed class FeedingPoint : MonoBehaviour
    {
        [SerializeField] FeedingPointKind kind;
        [SerializeField] InventoryItemDefinition acceptedItem;
        [SerializeField, Min(0)] int initialUnits = 8;
        [SerializeField, Min(1)] int capacity = 20;
        [SerializeField] Transform animalStandPoint;
        [SerializeField] Transform fillVisual;
        int units;
        bool transferring; FarmAnimalController visitor;
        public bool TryReserve(FarmAnimalController animal){if(visitor!=null&&visitor!=animal)return false;visitor=animal;return true;}
        public void Release(FarmAnimalController animal){if(visitor==animal)visitor=null;}

        public FeedingPointKind Kind => kind;
        public InventoryItemDefinition AcceptedItem => acceptedItem;
        public int Units => units;
        public int Capacity => capacity;
        public Vector3 AnimalStandPoint => animalStandPoint == null ? transform.position + transform.forward * 0.8f : animalStandPoint.position;
        public event Action Changed;

        void Awake() { units = Mathf.Clamp(initialUnits, 0, capacity); }
        void LateUpdate(){if(fillVisual==null)return;fillVisual.gameObject.SetActive(units>0);var scale=fillVisual.localScale;scale.y=.12f*units/Mathf.Max(1,capacity);fillVisual.localScale=scale;}

        public bool TryConsume(InventoryItemDefinition expected)
        {
            if (units <= 0 || expected == null || acceptedItem != expected) return false;
            units--;
            Changed?.Invoke();
            return true;
        }

        public bool TryConsume()
        {
            if (units <= 0) return false;
            units--;
            Changed?.Invoke();
            return true;
        }

        public int Refill(int amount)
        {
            if (amount <= 0) return 0;
            int added = Mathf.Min(amount, capacity - units);
            if (added <= 0) return 0;
            units += added;
            Changed?.Invoke();
            return added;
        }

        public int RefillFromInventory(FarmInventory inventory, int requested)
        {
            if (transferring || inventory == null || acceptedItem == null || requested <= 0) return 0;
            int amount = Mathf.Min(requested, Mathf.Min(capacity - units, inventory.Quantity(acceptedItem)));
            if (amount <= 0) return 0;
            transferring=true;
            try {if(!inventory.TryTransfer(acceptedItem,amount,()=>{units+=amount;return true;}))return 0;Changed?.Invoke();return amount;}
            finally {transferring=false;}
        }
    }
}
