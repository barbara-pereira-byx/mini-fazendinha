using UnityEngine;
using MiniFazendinha.Planting;
namespace MiniFazendinha.FruitTrees
{
    public enum FruitTreeState { Young,Growing,Mature,Producing,ReadyToHarvest }
    [DisallowMultipleComponent]
    public sealed class FruitTree:MonoBehaviour
    {
        [SerializeField] FruitTreeDefinition definition;
        [SerializeField] FarmInventory inventory;
        [SerializeField] Transform modelRoot;
        [SerializeField] GameObject readyMarker;
        double age,production;
        int availableFruit;
        bool harvesting;
        FruitTreeState state;
        Vector3 baseScale;
        public FruitTreeDefinition Definition=>definition;
        public FruitTreeState State=>state;
        public int AvailableFruit=>availableFruit;
        public float GrowthProgress=>definition==null?0:Mathf.Clamp01((float)(age/definition.GrowthSeconds));
        public float ProductionProgress=>definition==null?0:Mathf.Clamp01((float)(production/definition.ProductionSeconds));
        public float SecondsRemaining=>definition==null?0:availableFruit>0?0:(float)(age<definition.GrowthSeconds?definition.GrowthSeconds-age:definition.ProductionSeconds-production);
        void Awake(){if(modelRoot==null&&transform.childCount>0)modelRoot=transform.GetChild(0);if(modelRoot!=null)baseScale=modelRoot.localScale;if(inventory==null)inventory=FindAnyObjectByType<FarmInventory>();}
        void Start()=>RefreshVisual();
        void Update()=>Advance(Time.timeScale>0?Time.unscaledDeltaTime:0);
        void Advance(float dt)
        {
            if(definition==null||!definition.IsValid||dt<0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            double remaining=dt;
            if(age<definition.GrowthSeconds){double growth=System.Math.Min(remaining,definition.GrowthSeconds-age);age+=growth;remaining-=growth;}
            if(age<definition.GrowthSeconds)state=GrowthProgress<.48f?FruitTreeState.Young:FruitTreeState.Growing;
            else
            {
                if(availableFruit==0){production=System.Math.Min(definition.ProductionSeconds,production+remaining);if(production>=definition.ProductionSeconds)availableFruit=definition.YieldAmount;}
                state=availableFruit>0?FruitTreeState.ReadyToHarvest:production>0?FruitTreeState.Producing:FruitTreeState.Mature;
            }
            RefreshVisual();
        }
        void RefreshVisual(){if(definition==null)return;if(modelRoot!=null)modelRoot.localScale=baseScale*Mathf.Lerp(.38f,definition.MatureScale,GrowthProgress);if(readyMarker!=null)readyMarker.SetActive(availableFruit>0);}
        public bool TryHarvest()
        {
            if(harvesting||availableFruit<=0||inventory==null||!inventory.CanReceive(definition.Fruit,availableFruit))return false;
            harvesting=true;int amount=availableFruit;availableFruit=0;production=0;state=FruitTreeState.Producing;
            try{if(!inventory.TryAdd(definition.Fruit,amount)){availableFruit=amount;production=definition.ProductionSeconds;state=FruitTreeState.ReadyToHarvest;return false;}RefreshVisual();return true;}
            finally{harvesting=false;}
        }
        public void Configure(FruitTreeDefinition tree,FarmInventory stock,Transform visual,GameObject marker)
        {definition=tree;inventory=stock;modelRoot=visual;readyMarker=marker;baseScale=visual==null?Vector3.one:visual.localScale;age=0;production=0;availableFruit=0;state=FruitTreeState.Young;RefreshVisual();}
    }
}
