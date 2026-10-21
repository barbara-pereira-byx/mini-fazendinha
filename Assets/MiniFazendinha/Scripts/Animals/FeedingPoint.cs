using System;
using UnityEngine;

namespace MiniFazendinha.Animals
{
    public enum FeedingPointKind { Food, Water }

    [DisallowMultipleComponent]
    public sealed class FeedingPoint : MonoBehaviour
    {
        [SerializeField] FeedingPointKind kind;
        [SerializeField, Min(0)] int initialUnits = 8;
        [SerializeField, Min(1)] int capacity = 16;
        [SerializeField] Transform animalStandPoint;
        int units;
        public FeedingPointKind Kind => kind;
        public int Units => units;
        public int Capacity => capacity;
        public Vector3 AnimalStandPoint => animalStandPoint == null ? transform.position + transform.forward * 0.8f : animalStandPoint.position;
        public event Action Changed;

        void Awake() { units = Mathf.Clamp(initialUnits, 0, capacity); }
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
    }
}