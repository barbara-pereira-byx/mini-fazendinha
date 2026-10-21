using UnityEngine;

namespace MiniFazendinha.Planting
{
    [CreateAssetMenu(fileName="InventoryItem", menuName="MiniFazendinha/Inventory Item")]
    public class InventoryItemDefinition : ScriptableObject
    {
        [SerializeField] string identifier;
        [SerializeField] string displayName;
        [SerializeField] Sprite icon;

        public string Identifier => identifier;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public virtual bool IsValid => !string.IsNullOrWhiteSpace(identifier) && !string.IsNullOrWhiteSpace(displayName);
    }
}