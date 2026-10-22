using UnityEngine;
using MiniFazendinha.Planting;

namespace MiniFazendinha.Animals
{
    [CreateAssetMenu(fileName="AnimalDefinition", menuName="MiniFazendinha/Animal Definition")]
    public sealed class AnimalDefinition : ScriptableObject
    {
        [SerializeField] string identifier;
        [SerializeField] string displayName;
        [SerializeField] GameObject visualPrefab;
        [SerializeField] RuntimeAnimatorController animatorController;
        [SerializeField] InventoryItemDefinition feedItem;
        [SerializeField] InventoryItemDefinition waterItem;
        [SerializeField] InventoryItemDefinition product;
        [SerializeField, Min(1)] int productYield = 1;
        [SerializeField, Min(60)] float productionSeconds = 60f;
        [SerializeField, Min(0.1f)] float walkSpeed = 0.8f;
        [SerializeField, Min(0.2f)] float bodyRadius = 0.4f;
        [SerializeField, Min(0.1f)] float separationRadius = 0.9f;
        [SerializeField, Min(0.1f)] float visualScale = 1f;
        [SerializeField, Min(0.1f)] float idleSecondsMin = 2f;
        [SerializeField, Min(0.1f)] float idleSecondsMax = 5f;
        [SerializeField, Range(0f,1f)] float restChance = 0.12f;
        [SerializeField, Range(0f,1f)] float careChance = 0.28f;
        [SerializeField, HideInInspector, Min(1f)] float fedProductionMultiplier = 1.4f;
        [SerializeField, HideInInspector, Min(1f)] float fedForSeconds = 55f;
        [SerializeField] string idleState = "Idle";
        [SerializeField] string walkState = "Walk";
        [SerializeField] string eatingState = "Eating";

        public string Identifier => identifier;
        public string DisplayName => displayName;
        public GameObject VisualPrefab => visualPrefab;
        public RuntimeAnimatorController AnimatorController => animatorController;
        public InventoryItemDefinition FeedItem => feedItem;
        public InventoryItemDefinition WaterItem => waterItem;
        public InventoryItemDefinition Product => product;
        public int ProductYield => productYield;
        public float ProductionSeconds => Mathf.Max(60f,productionSeconds);
        public float WalkSpeed => walkSpeed;
        public float BodyRadius => bodyRadius;
        public float SeparationRadius => separationRadius;
        public float VisualScale => visualScale;
        public float IdleSecondsMin => idleSecondsMin;
        public float IdleSecondsMax => Mathf.Max(idleSecondsMin, idleSecondsMax);
        public float RestChance => restChance;
        public float CareChance => careChance;
        public float FedProductionMultiplier => fedProductionMultiplier;
        [SerializeField, Min(60f)] float nutritionDuration = 1800f;
        [SerializeField, Min(60f)] float hydrationDuration = 1800f;
        public float FedForSeconds => nutritionDuration;
        public float HydrationDuration => hydrationDuration;
        [SerializeField, Min(1)] float careInterval=30f;
        [SerializeField, Range(0,1)] float careGain=.1f;
        public float CareInterval=>careInterval;public float CareGain=>careGain;
        [SerializeField, Range(0,1)] float healthyYieldMultiplier=1f;
        [SerializeField, Range(0,1)] float intermediateYieldMultiplier=.75f;
        [SerializeField, Range(0,1)] float attentionYieldMultiplier=.5f;
        [SerializeField, Range(0,1)] float sickYieldMultiplier=.25f;
        public float HealthyYieldMultiplier=>healthyYieldMultiplier;
        public float IntermediateYieldMultiplier=>intermediateYieldMultiplier;
        public float AttentionYieldMultiplier=>attentionYieldMultiplier;
        public float SickYieldMultiplier=>sickYieldMultiplier;
        public string IdleState => idleState;
        public string WalkState => walkState;
        public string EatingState => eatingState;
        public bool IsValid => !string.IsNullOrWhiteSpace(identifier) && !string.IsNullOrWhiteSpace(displayName) &&
            visualPrefab != null && productionSeconds > 0f && walkSpeed > 0f && bodyRadius > 0f;
    }
}
