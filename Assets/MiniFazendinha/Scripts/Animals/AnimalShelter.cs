using System.Collections.Generic;
using UnityEngine;
using MiniFazendinha.Planting;

namespace MiniFazendinha.Animals
{
    [DisallowMultipleComponent]
    public sealed class AnimalShelter : MonoBehaviour
    {
        [SerializeField] string shelterId = "barn-main";
        [SerializeField] Collider roamingArea;
        [SerializeField] Vector3 gatePoint = new Vector3(9f, 0.03f, -3.5f);
        [SerializeField] Vector3 pathPoint = new Vector3(8f, 0.03f, 3f);
        [SerializeField] Vector3 entrancePoint = new Vector3(7f, 0.03f, 5.45f);
        [SerializeField, Min(0.2f)] float boundaryInset = 0.85f;
        [SerializeField] GameObject productReadyMarker;
        readonly List<FarmAnimalController> residents = new List<FarmAnimalController>();
        public string ShelterId => shelterId;
        public Vector3 GatePoint => gatePoint;
        public Vector3 PathPoint => pathPoint;
        public Vector3 EntrancePoint => entrancePoint;
        public Bounds RoamingBounds => roamingArea == null ? new Bounds(transform.position, new Vector3(8f, 0.1f, 14f)) : roamingArea.bounds;
        public IReadOnlyList<FarmAnimalController> Residents => residents;
        public int ResidentCount => residents.Count;
        public int ProductsAvailable { get { int total=0;foreach(var a in residents)if(a!=null)total+=a.ProductsAvailable;return total; } }

        void LateUpdate() { if(productReadyMarker!=null) productReadyMarker.SetActive(ProductsAvailable>0); }
        public int ResidentIndex(FarmAnimalController animal) => residents.IndexOf(animal);

        public void Register(FarmAnimalController animal)
        {
            if (animal != null && !residents.Contains(animal)) residents.Add(animal);
        }

        public void RecollectAll()
        {
            foreach (var animal in residents)
                if (animal != null && !animal.IsInsideShelter) animal.ReturnToShelter(this);
        }

        public void ReleaseAll()
        {
            foreach (var animal in residents)
                if (animal != null && animal.IsInsideShelter) animal.ReleaseFromShelter(this);
        }

        public Vector3 ReleasePoint(int index)
        {
            Vector3 inside = (transform.position - gatePoint).normalized;
            if (inside.sqrMagnitude < 0.01f) inside = Vector3.right;
            float lane = (index % 5) * 0.55f;
            float row = (index / 5) * 0.6f;
            Vector3 point = gatePoint + inside * (1.1f + lane) + Vector3.forward * row;
            var bounds = RoamingBounds;
            point.x = Mathf.Clamp(point.x, bounds.min.x + boundaryInset, bounds.max.x - boundaryInset);
            point.z = Mathf.Clamp(point.z, bounds.min.z + boundaryInset, bounds.max.z - boundaryInset);
            point.y = bounds.min.y + 0.03f;
            return point;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.9f);
            Gizmos.DrawSphere(gatePoint, 0.18f);
            Gizmos.color = new Color(0.2f, 0.85f, 0.5f, 0.9f);
            Gizmos.DrawSphere(entrancePoint, 0.18f);
            if (roamingArea != null)
            {
                Gizmos.color = new Color(0.2f, 0.7f, 0.3f, 0.4f);
                Gizmos.DrawWireCube(roamingArea.bounds.center, roamingArea.bounds.size);
            }
        }
    }
}
