using UnityEngine;

namespace SkyTree
{
    /// <summary>Locates the mother's beak in its fixed spread-wing attack image.</summary>
    [DisallowMultipleComponent]
    public sealed class SkybirdHeadbuttProfile : MonoBehaviour
    {
        [SerializeField] private string displayName = "어미 스카이버드";
        [SerializeField] private Vector2 headContactUV = new Vector2(.10f, .35f);
        [SerializeField] private float forwardAngle = -145f;

        public string DisplayName => displayName;
        public Vector2 HeadContactUV => headContactUV;
        public float ForwardAngle => forwardAngle;

        public Vector2 ContactOffset(RectTransform rect, float angle)
        {
            Vector2 point = Vector2.Scale(headContactUV - rect.pivot, rect.rect.size);
            return Quaternion.Euler(0, 0, angle) * point;
        }
    }
}
