using UnityEngine;
using MiniFazendinha.Planting;

namespace MiniFazendinha.FruitTrees
{
    public enum FruitTreeState { Young, Growing, Mature, Producing, ReadyToHarvest }
    [DisallowMultipleComponent]
    public sealed class FruitTree : MonoBehaviour
    {
        [SerializeField] FruitTreeDefinition definition;
        [SerializeField] FarmInventory inventory;
        [SerializeField] Transform modelRoot;
        [SerializeField] GameObject readyMarker;
        double age, production;
        FruitTreeState state;
        Vector3 baseScale;
        public FruitTreeDefinition Definition=>definition;
        public FruitTreeState State=>state;
        public float GrowthProgress=>definition==null?0:Mathf.Clamp01((float)(age/definition.GrowthSeconds));
        public float ProductionProgress=>definition==null?0:Mathf.Clamp01((float)(production/definition.ProductionSeconds));
        void Awake(){if(modelRoot==null&&transform.childCount>0)modelRoot=transform.GetChild(0);if(modelRoot!=null)baseScale=modelRoot.localScale;if(inventory==null)inventory=FindAnyObjectByType<FarmInventory>();}
        void Start(){RefreshState();}
        void Update()
        {
            if(definition==null||!definition.IsValid)return;
            if(age<definition.GrowthSeconds)age+=Time.deltaTime;
            else if(state!=FruitTreeState.ReadyToHarvest)production+=Time.deltaTime;
            if(age>=definition.GrowthSeconds&&production>=definition.ProductionSeconds)state=FruitTreeState.ReadyToHarvest;
            else if(age>=definition.GrowthSeconds)state=production>0?FruitTreeState.Producing:FruitTreeState.Mature;
            else state=GrowthProgress<.48f?FruitTreeState.Young:FruitTreeState.Growing;
            float scale=Mathf.Lerp(.38f,definition.MatureScale,GrowthProgress);
            if(modelRoot!=null)modelRoot.localScale=baseScale*scale;
            if(readyMarker!=null)readyMarker.SetActive(state==FruitTreeState.ReadyToHarvest);
        }
        void RefreshState(){state=GrowthProgress<.48f?FruitTreeState.Young:FruitTreeState.Growing;}
        public bool TryHarvest()
        {
            if(state!=FruitTreeState.ReadyToHarvest||inventory==null||!inventory.TryAdd(definition.Fruit,definition.YieldAmount))return false;
            production=0;state=FruitTreeState.Producing;if(readyMarker!=null)readyMarker.SetActive(false);return true;
        }
        public void Configure(FruitTreeDefinition tree, FarmInventory stock, Transform visual, GameObject marker)
        {definition=tree;inventory=stock;modelRoot=visual;readyMarker=marker;baseScale=visual==null?Vector3.one:visual.localScale;}
    }
}