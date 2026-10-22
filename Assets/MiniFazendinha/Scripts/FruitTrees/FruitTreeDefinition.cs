using UnityEngine;
using MiniFazendinha.Planting;

namespace MiniFazendinha.FruitTrees
{
    [CreateAssetMenu(fileName="FruitTreeDefinition",menuName="MiniFazendinha/Fruit Tree Definition")]
    public sealed class FruitTreeDefinition : ScriptableObject
    {
        [SerializeField] string identifier;
        [SerializeField] string displayName;
        [SerializeField] GameObject modelPrefab;
        [SerializeField] InventoryItemDefinition fruit;
        [SerializeField,Min(5)] float growthSeconds=60;
        [SerializeField,Min(5)] float productionSeconds=90;
        [SerializeField,Min(1)] int yieldAmount=3;
        [SerializeField,Min(.1f)] float matureScale=1;
        [SerializeField,Min(.25f)] float spacingRadius=1.2f;
        public string Identifier=>identifier;
        public string DisplayName=>displayName;
        public GameObject ModelPrefab=>modelPrefab;
        public InventoryItemDefinition Fruit=>fruit;
        public float GrowthSeconds=>growthSeconds;
        public float ProductionSeconds=>productionSeconds;
        public int YieldAmount=>yieldAmount;
        public float MatureScale=>matureScale;
        public float SpacingRadius=>spacingRadius;
        public bool IsValid=>!string.IsNullOrWhiteSpace(identifier)&&!string.IsNullOrWhiteSpace(displayName)&&modelPrefab!=null&&fruit!=null&&growthSeconds>0&&productionSeconds>0&&yieldAmount>0;
    }
}