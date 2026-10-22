using System.Collections.Generic;
using UnityEngine;
using MiniFazendinha.Planting;

namespace MiniFazendinha.Animals
{
    [DisallowMultipleComponent]
    public sealed class AnimalShelter : MonoBehaviour
    {
        [SerializeField] string shelterId="barn-main";
        [SerializeField] string displayName="Curral";
        [SerializeField] AnimalDefinition permittedSpecies;
        [SerializeField, Min(1)] int capacity=6;
        [SerializeField] FeedingPoint food, water;
        [SerializeField] Collider roamingArea;
        [SerializeField] Vector3 gatePoint, pathPoint, entrancePoint;
        [SerializeField] Bounds farmArea=new Bounds(new Vector3(10,0,-2),new Vector3(16,.1f,30));
        [SerializeField, Min(.2f)] float boundaryInset=.35f;
        [SerializeField] GameObject productReadyMarker;
        [SerializeField] bool gateOpen=true;
        [SerializeField] Transform gateVisual;
        bool releasing; FarmAnimalController passage;
        static readonly HashSet<AnimalShelter> activeShelters=new HashSet<AnimalShelter>();
        void OnEnable()=>activeShelters.Add(this);
        void OnDisable(){activeShelters.Remove(this);passage=null;}
        public bool TryPassage(FarmAnimalController a)
        {
            if(!gateOpen||passage!=null&&passage!=a)return false;
            // Opposite pens share a narrow approach. Reserve that corridor as one passage.
            foreach(var shelter in activeShelters)
                if(shelter!=this&&shelter.passage!=null&&Vector3.Distance(pathPoint,shelter.pathPoint)<a.Definition.BodyRadius+shelter.passage.Definition.BodyRadius+.3f)return false;
            passage=a;return true;
        }
        public void ReleasePassage(FarmAnimalController a){if(passage==a)passage=null;}
        public bool GateOpen=>gateOpen;
        public bool Rename(string value){value=value?.Trim();if(string.IsNullOrEmpty(value)||value.Length>24)return false;displayName=value;return true;}
        public void SetGate(bool value)
        {
            if(!value){if(passage!=null)return;foreach(var a in residents)if(a!=null&&Vector3.Distance(a.transform.position,gatePoint)<a.Definition.BodyRadius+.35f)return;}
            gateOpen=value;
            if(gateVisual!=null)
            {
                gateVisual.localRotation=Quaternion.identity;
                foreach(var renderer in gateVisual.GetComponentsInChildren<Renderer>(true))renderer.enabled=!value;
                foreach(var collider in gateVisual.GetComponentsInChildren<Collider>(true))collider.enabled=!value;
            }
            if(!value)releasing=false;
        }        readonly List<FarmAnimalController> residents=new List<FarmAnimalController>();
        public string ShelterId=>shelterId;
        public string DisplayName=>displayName;
        public AnimalDefinition PermittedSpecies=>permittedSpecies;
        public int Capacity=>capacity;
        public FeedingPoint Food=>food;
        public FeedingPoint Water=>water;
        public Vector3 GatePoint=>gatePoint;
        public Vector3 PathPoint=>pathPoint;
        public Vector3 EntrancePoint=>entrancePoint;
        public Vector3 DeparturePoint(FarmAnimalController a)=>pathPoint+new Vector3(0,0,(ResidentIndex(a)%2==0?1:-1)*1.85f);
        public Bounds FarmBounds=>farmArea;
        public Bounds RoamingBounds=>roamingArea==null?new Bounds(transform.position,new Vector3(4,.1f,4)):roamingArea.bounds;
        public IReadOnlyList<FarmAnimalController> Residents=>residents;
        public int ResidentCount=>residents.Count;
        public int ProductsAvailable {get{int n=0;foreach(var a in residents)if(a!=null)n+=a.ProductsAvailable;return n;}}
        void Start(){SetGate(gateOpen);}
        void LateUpdate(){if(productReadyMarker!=null)productReadyMarker.SetActive(ProductsAvailable>0);}
        public int ResidentIndex(FarmAnimalController animal)=>residents.IndexOf(animal);
        public bool Accepts(FarmAnimalController animal)=>animal!=null&&animal.Definition==permittedSpecies&&(residents.Contains(animal)||residents.Count<capacity);
        public bool Register(FarmAnimalController animal)
        {
            if(!Accepts(animal)||animal.Home!=this)return false;
            if(!residents.Contains(animal))residents.Add(animal);
            return true;
        }
        public void Unregister(FarmAnimalController animal)=>residents.Remove(animal);
        public void ClearEntranceFor(FarmAnimalController arriving)
        {
            foreach(var resident in FarmAnimalRegistry.All)
            {
                if(resident==null||resident==arriving)continue;
                float clearance=resident.Definition.BodyRadius+arriving.Definition.BodyRadius+.5f;
                var target=arriving.GetComponent<FarmAnimalNavigation>().Destination;
                if(Vector3.Distance(resident.transform.position,target)<clearance||Vector3.Distance(resident.transform.position,entrancePoint)<clearance||Vector3.Distance(resident.transform.position,gatePoint)<clearance||Vector3.Distance(resident.transform.position,pathPoint)<clearance)resident.MakeRoomFor(arriving);
            }
        }
        public void RecollectAll(){if(!gateOpen)return;releasing=false;foreach(var a in residents)if(a!=null)a.ReturnToShelter(this);}
        public void ReleaseAll(){if(gateOpen)releasing=true;}
        void Update(){if(!releasing)return;foreach(var a in residents)if(a!=null&&a.IsExitingHome)return;FarmAnimalController candidate=null;float distance=float.PositiveInfinity;foreach(var a in residents)if(a!=null&&a.IsConfined){float d=Vector3.Distance(a.transform.position,gatePoint);if(d<distance){distance=d;candidate=a;}}if(candidate!=null){candidate.ReleaseFromShelter(this);return;}releasing=false;}
        public Vector3 ReleasePoint(int index)=>pathPoint;
        public bool Contains(Vector3 p,float inset=0)
        {var b=RoamingBounds;return p.x>=b.min.x+inset&&p.x<=b.max.x-inset&&p.z>=b.min.z+inset&&p.z<=b.max.z-inset;}
    }
}
