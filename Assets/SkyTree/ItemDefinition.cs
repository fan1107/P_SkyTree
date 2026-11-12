using UnityEngine;

namespace SkyTree
{
    [CreateAssetMenu(menuName = "SkyTree/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private Color frameTint = Color.white;
        public string Id => itemId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Color FrameTint => frameTint;
    }
}
