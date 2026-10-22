using System;
using UnityEngine;
using MiniFazendinha.Planting;
namespace MiniFazendinha.Animals
{
    public enum AnimalHealthState { Healthy, Attention, Sick }
    [DisallowMultipleComponent, RequireComponent(typeof(FarmAnimalNavigation))]
    public sealed class FarmAnimalController : MonoBehaviour
    {
        [SerializeField] AnimalDefinition definition;
        [SerializeField] string animalId,displayName;
        [SerializeField] AnimalShelter home;
        [SerializeField] FarmInventory inventory;
        [SerializeField] Animator animator;
        [SerializeField] Transform visualRoot;
        [SerializeField] GameObject productReadyMarker;
        [SerializeField] FeedingPoint[] feedingPoints=Array.Empty<FeedingPoint>();
        [SerializeField,Range(0,1)] float happiness=.7f;
        [SerializeField] bool sick;
        FarmAnimalNavigation navigation;
        FarmAnimalState state=FarmAnimalState.Idle;
        FeedingPoint careGoal;
        double idleUntil,fedUntil,hydratedUntil,productionProgress,careReadyAt,blockedSince,yieldRetryAt; FarmAnimalController yieldFor;
        int productsAvailable,routeIndex; double fractionalYield; bool collecting;
        [NonSerialized] Vector3[] route;
        bool confined=true,returning,started,pendingReturn;
        string lastClip;
        public AnimalDefinition Definition=>definition;
        public string AnimalId=>animalId;
        public bool Rename(string value){value=value?.Trim();if(string.IsNullOrEmpty(value)||value.Length>24)return false;displayName=value;return true;}
        // Extension point for a future resource-backed treatment service. No random sickness or death.
        public bool Treat(){if(!sick)return false;sick=false;return true;}
        public string DisplayName=>string.IsNullOrWhiteSpace(displayName)?definition==null?"Animal":definition.DisplayName:displayName;
        public AnimalShelter Home=>home;
        public FarmAnimalState State=>state;
        public bool IsInsideShelter=>home!=null&&home.Contains(transform.position);
        public bool IsConfined=>confined;
        public bool IsReturningHome=>pendingReturn||route!=null&&route.Length>0&&returning;
        public bool IsExitingHome=>route!=null&&route.Length>0&&!returning;
        public int ProductsAvailable=>productsAvailable;
        public float Happiness=>happiness;
        public float ProductionEfficiency=>Health==AnimalHealthState.Sick?definition.SickYieldMultiplier:Health==AnimalHealthState.Attention?definition.AttentionYieldMultiplier:happiness>=.7f&&FeedingLevel>=.4f&&HydrationLevel>=.4f?definition.HealthyYieldMultiplier:definition.IntermediateYieldMultiplier;
        public AnimalHealthState Health=>sick?AnimalHealthState.Sick:FeedingLevel<=.2f||HydrationLevel<=.2f?AnimalHealthState.Attention:AnimalHealthState.Healthy;
        public string HealthLabel=>Health==AnimalHealthState.Sick?"Doente":Health==AnimalHealthState.Attention?"Atenção: alimento / água":"Saudável";
        public float ProductionProgress=>definition==null?0:Mathf.Clamp01((float)(productionProgress/definition.ProductionSeconds));
        public float SecondsToNextProduct=>definition==null?0:Mathf.Max(0,(float)(definition.ProductionSeconds-productionProgress));
        public float FeedingLevel=>Mathf.Clamp01((float)((fedUntil-Time.timeAsDouble)/(definition==null?1:definition.FedForSeconds)));
        public float HydrationLevel=>Mathf.Clamp01((float)((hydratedUntil-Time.timeAsDouble)/(definition==null?1:definition.HydrationDuration)));
        public string StateLabel=>route!=null&&route.Length>0?(returning?"Voltando ao curral":"Saindo pelo portão"):state==FarmAnimalState.Wander?"Passeando":state==FarmAnimalState.Eating?(careGoal!=null&&careGoal.Kind==FeedingPointKind.Water?"Bebendo":"Comendo"):"Descansando";
        void OnEnable(){FarmAnimalRegistry.Register(this);if(started&&home!=null)home.Register(this);}
        void OnDisable(){if(careGoal!=null)careGoal.Release(this);FarmAnimalRegistry.Unregister(this);if(home!=null){home.Unregister(this);home.ReleasePassage(this);}}
        void Awake(){navigation=GetComponent<FarmAnimalNavigation>();if(animator==null)animator=GetComponentInChildren<Animator>(true);}
        void Start()
        {
            if(definition==null||!definition.IsValid||home==null||!home.Register(this))
            {Debug.LogError("Animal sem definição / curral compatível ou capacidade disponível.",this);enabled=false;return;}
            productionProgress=0;fractionalYield=0;productsAvailable=0;route=null;confined=true;navigation.Configure(home.FarmBounds,.35f,definition.BodyRadius);navigation.Confined=true;
            if(inventory==null)inventory=FindAnyObjectByType<FarmInventory>();
            if(animator!=null){if(definition.AnimatorController!=null)animator.runtimeAnimatorController=definition.AnimatorController;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;}
            fedUntil=Time.timeAsDouble+definition.FedForSeconds;hydratedUntil=Time.timeAsDouble+definition.HydrationDuration;
            idleUntil=Time.timeAsDouble+UnityEngine.Random.Range(.3f,2f);started=true;ChangeState(FarmAnimalState.Idle);
        }
        void Update()
        {
            if(!started)return;
            float targetHappiness=Mathf.Clamp01((FeedingLevel+HydrationLevel)*.4f+(sick?0:.2f));happiness=Mathf.MoveTowards(happiness,targetHappiness,Time.deltaTime/600f);Produce(Time.timeScale>0?Time.unscaledDeltaTime:0);UpdateMarker();
            if(yieldFor!=null&&(yieldFor.IsReturningHome||yieldFor.IsExitingHome))
            {
                // A moving neighbour can invalidate a yielding route too; retry it rather than deadlock.
                if(!navigation.HasDestination&&Vector3.Distance(transform.position,navigation.Destination)>.12f&&Time.timeAsDouble>=yieldRetryAt)
                {var arriving=yieldFor;yieldFor=null;MakeRoomFor(arriving);yieldFor=arriving;yieldRetryAt=Time.timeAsDouble+1;}
                navigation.Advance(definition.WalkSpeed,Time.deltaTime,definition.SeparationRadius,FarmAnimalRegistry.All);UpdateAnimation();return;
            }
            if(pendingReturn){ReturnToShelter(home);UpdateAnimation();return;}
            if(route!=null&&route.Length>0){AdvanceRoute();UpdateAnimation();return;}
            if(state==FarmAnimalState.Eating||state==FarmAnimalState.Resting)
            {if(Time.timeAsDouble>=idleUntil){if(careGoal!=null)careGoal.Release(this);careGoal=null;ChangeState(FarmAnimalState.Idle);idleUntil=Time.timeAsDouble+.5f;}UpdateAnimation();return;}
            if(state==FarmAnimalState.Wander)
            {
                if(navigation.Advance(definition.WalkSpeed,Time.deltaTime,definition.SeparationRadius,FarmAnimalRegistry.All))
                {if(careGoal!=null)BeginEating();else{idleUntil=Time.timeAsDouble+UnityEngine.Random.Range(definition.IdleSecondsMin,definition.IdleSecondsMax);ChangeState(FarmAnimalState.Idle);}}
                else if(!navigation.HasDestination){if(careGoal!=null)careGoal.Release(this);careGoal=null;idleUntil=Time.timeAsDouble+1;ChangeState(FarmAnimalState.Idle);}
            }
            else if(Time.timeAsDouble>=idleUntil)
            {
                if((FeedingLevel<.4f||HydrationLevel<.4f)&&TryChooseCarePoint()){}
                else if(navigation.ChooseWanderPoint())ChangeState(FarmAnimalState.Wander);
                else idleUntil=Time.timeAsDouble+2;
            }
            UpdateAnimation();
        }
        bool TryChooseCarePoint()
        {
            foreach(var p in feedingPoints)
            {
                if(p==null||p.Units==0)continue;
                if(p.Kind==FeedingPointKind.Food?(FeedingLevel>.4f||p.AcceptedItem!=definition.FeedItem):(HydrationLevel>.4f||p.AcceptedItem!=definition.WaterItem))continue;
                if(!p.TryReserve(this))continue;
                if(navigation.TrySetDestination(p.AnimalStandPoint)){careGoal=p;ChangeState(FarmAnimalState.Wander);return true;}p.Release(this);
            }
            return false;
        }
        void BeginEating()
        {
            if(careGoal!=null&&careGoal.TryConsume(careGoal.Kind==FeedingPointKind.Food?definition.FeedItem:definition.WaterItem))
            {if(careGoal.Kind==FeedingPointKind.Food)fedUntil=Time.timeAsDouble+definition.FedForSeconds;else hydratedUntil=Time.timeAsDouble+definition.HydrationDuration;idleUntil=Time.timeAsDouble+3;ChangeState(FarmAnimalState.Eating);}
            else{if(careGoal!=null)careGoal.Release(this);careGoal=null;idleUntil=Time.timeAsDouble+1;ChangeState(FarmAnimalState.Idle);}
        }
        public bool Care()
        {if(Time.timeAsDouble<careReadyAt)return false;happiness=Mathf.Clamp01(happiness+definition.CareGain);careReadyAt=Time.timeAsDouble+definition.CareInterval;return true;}
        public bool Feed()
        {if(definition==null||inventory==null||FeedingLevel>.9f||!inventory.TrySpend(definition.FeedItem))return false;fedUntil=Time.timeAsDouble+definition.FedForSeconds;return true;}
        void Produce(float dt)
        {
            if(definition.Product==null||definition.ProductYield<=0||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            productionProgress+=dt;
            double period=definition.ProductionSeconds;
            while(productionProgress+1e-7>=period)
            {
                productionProgress=Math.Max(0,productionProgress-period);
                fractionalYield+=definition.ProductYield*Mathf.Clamp01(ProductionEfficiency);
                int produced=(int)Math.Floor(fractionalYield+1e-7);fractionalYield-=produced;
                productsAvailable=Mathf.Min(productsAvailable+produced,99);
            }
        }        public int CollectProducts()
        {if(collecting||productsAvailable<=0||inventory==null||definition.Product==null)return 0;collecting=true;int n=productsAvailable;productsAvailable=0;try{if(!inventory.TryAdd(definition.Product,n)){productsAvailable=n;return 0;}UpdateMarker();return n;}finally{collecting=false;}}        public void ReturnToShelter(AnimalShelter shelter)
        {
            if(shelter==null||shelter!=home||!shelter.Accepts(this)||(!shelter.GateOpen&&!IsInsideShelter))return;
            yieldFor=null;
            if(home.Contains(transform.position,definition.BodyRadius+.05f))
            {pendingReturn=false;returning=false;home.ReleasePassage(this);if(careGoal!=null)careGoal.Release(this);careGoal=null;route=null;confined=true;navigation.Confined=true;navigation.ClearDestination();idleUntil=Time.timeAsDouble+.5;ChangeState(FarmAnimalState.Idle);return;}
            if(careGoal!=null)careGoal.Release(this);careGoal=null;
            confined=false;navigation.Confined=false;returning=true;
            if(!home.TryPassage(this)){pendingReturn=true;route=null;navigation.ClearDestination();return;}
            pendingReturn=false;
            route=home.Contains(transform.position)?new[]{home.EntrancePoint}:new[]{home.PathPoint,home.GatePoint,home.EntrancePoint};
            routeIndex=0;blockedSince=Time.timeAsDouble;navigation.SetDestination(route[0]);ChangeState(FarmAnimalState.ReturningHome);
        }
        public void ReleaseFromShelter(AnimalShelter shelter)
        {
            if(shelter==null||shelter!=home||!shelter.GateOpen||!confined||route!=null)return;
            if(careGoal!=null)careGoal.Release(this);careGoal=null;if(!home.TryPassage(this))return;pendingReturn=false;returning=false;navigation.Confined=false;
            route=new[]{home.GatePoint,home.PathPoint,home.DeparturePoint(this)};routeIndex=0;blockedSince=Time.timeAsDouble;navigation.SetDestination(route[0]);ChangeState(FarmAnimalState.Wander);
        }
        public bool MakeRoomFor(FarmAnimalController arriving)
        {
            if(arriving==null||arriving==this||IsInsideShelter&&arriving.Home!=home||route!=null&&route.Length>0||yieldFor!=null&&(yieldFor.IsReturningHome||yieldFor.IsExitingHome))return false;
            var candidates=new System.Collections.Generic.List<Vector3>();
            if(IsInsideShelter)
            {
                var bounds=home.RoamingBounds;float inset=definition.BodyRadius+.16f;
                for(float x=bounds.min.x+inset;x<=bounds.max.x-inset;x+=.25f)for(float z=bounds.min.z+inset;z<=bounds.max.z-inset;z+=.25f)candidates.Add(new Vector3(x,transform.position.y,z));
            }
            else
            {
                for(float radius=1;radius<=3;radius+=.5f)for(int i=0;i<16;i++){float angle=i*Mathf.PI/8;candidates.Add(transform.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius);}
            }
            candidates.Sort((a,b)=>Vector3.Distance(b,home.GatePoint).CompareTo(Vector3.Distance(a,home.GatePoint)));
            foreach(var point in candidates)
            {
                float clearance=definition.BodyRadius+arriving.Definition.BodyRadius+.15f;
                if(Vector3.Distance(point,arriving.GetComponent<FarmAnimalNavigation>().Destination)<clearance||Vector3.Distance(point,transform.position)<.25f||Vector3.Distance(point,arriving.Home.EntrancePoint)<clearance||Vector3.Distance(point,arriving.Home.GatePoint)<clearance||Vector3.Distance(point,arriving.Home.PathPoint)<clearance)continue;
                if(!navigation.TrySetDestination(point))continue;
                if(careGoal!=null)careGoal.Release(this);careGoal=null;yieldFor=arriving;ChangeState(FarmAnimalState.Wander);return true;
            }
            return false;
        }        void AdvanceRoute()
        {
            if(navigation.Advance(definition.WalkSpeed,Time.deltaTime,definition.SeparationRadius,FarmAnimalRegistry.All))
            {
                routeIndex++;blockedSince=Time.timeAsDouble;
                if(routeIndex<route.Length){navigation.SetDestination(route[routeIndex]);return;}
                home.ReleasePassage(this);confined=returning;navigation.Confined=confined;route=null;idleUntil=Time.timeAsDouble+1;ChangeState(FarmAnimalState.Idle);return;
            }
            // Retry a blocked route without ignoring fences or relocating the animal.
            if(!navigation.HasDestination||Time.timeAsDouble-blockedSince>5)
            {home.ClearEntranceFor(this);navigation.SetDestination(route[routeIndex]);blockedSince=Time.timeAsDouble;}
        }
        public void Assign(AnimalDefinition species,string id,string name,AnimalShelter shelter,FarmInventory farmInventory,Animator anim,Transform visual,GameObject readyMarker,FeedingPoint[] carePoints)
        {definition=species;animalId=id;displayName=name;home=shelter;inventory=farmInventory;animator=anim;visualRoot=visual;productReadyMarker=readyMarker;feedingPoints=carePoints??Array.Empty<FeedingPoint>();}
        void ChangeState(FarmAnimalState next){state=next;UpdateAnimation();}
        void UpdateAnimation()
        {
            if(animator==null||definition==null||animator.runtimeAnimatorController==null)return;
            bool moving=navigation!=null&&navigation.CurrentSpeed>.03f;
            string clip=moving?definition.WalkState:state==FarmAnimalState.Eating?definition.EatingState:definition.IdleState;
            if(lastClip==clip)return;
            if(animator.HasState(0,Animator.StringToHash(clip))){animator.CrossFadeInFixedTime(clip,.25f);lastClip=clip;}
        }
        void UpdateMarker(){if(productReadyMarker!=null)productReadyMarker.SetActive(productsAvailable>0);}
    }
}
