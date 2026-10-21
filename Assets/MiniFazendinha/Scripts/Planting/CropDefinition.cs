using System;
using UnityEngine;

namespace MiniFazendinha.Planting
{
    [Serializable]
    public sealed class CropVisualDefinition
    {
        public GameObject prefab;
        public Material materialOverride;
        public Vector3 eulerAngles;
        public Vector3 scale = Vector3.one;
    }

    [CreateAssetMenu(fileName="Crop", menuName="MiniFazendinha/Crop Definition")]
    public sealed class CropDefinition : InventoryItemDefinition
    {
        [SerializeField, Min(0.1f)] float growthSeconds;
        [SerializeField, Min(1)] int yieldAmount = 3;
        [SerializeField] Color accentColor = Color.green;
        [SerializeField] CropVisualDefinition growingVisual = new CropVisualDefinition();
        [SerializeField] CropVisualDefinition intermediateVisual = new CropVisualDefinition();
        [SerializeField] CropVisualDefinition matureVisual = new CropVisualDefinition();
        [SerializeField, Range(1,5)] int visualRows = 3;
        [SerializeField, Min(0.1f)] float visualSpacing = 1.2f;
        [SerializeField, TextArea] string visualNotes;

        public float GrowthSeconds => growthSeconds;
        public int YieldAmount => yieldAmount;
        public Color AccentColor => accentColor;
        public CropVisualDefinition GrowingVisual => growingVisual;
        public CropVisualDefinition IntermediateVisual => intermediateVisual;
        public CropVisualDefinition MatureVisual => matureVisual;
        public int VisualRows => visualRows;
        public float VisualSpacing => visualSpacing;
        public override bool IsValid => base.IsValid && growthSeconds > 0 && !float.IsNaN(growthSeconds) &&
            !float.IsInfinity(growthSeconds) && yieldAmount > 0;
    }
}