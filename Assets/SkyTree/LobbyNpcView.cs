using UnityEngine;

namespace SkyTree
{
    // Keeps the nested NPC rig and its Animator while drawing its parts in the lobby Canvas.
    [ExecuteAlways, DefaultExecutionOrder(200)]
    public sealed class LobbyNpcView : MonoBehaviour
    {
        [SerializeField] private Transform skeleton;
        [SerializeField] private Transform[] sources;
        [SerializeField] private RectTransform[] images;

        private void OnEnable() => Sync();
        private void LateUpdate() => Sync();

        public void Sync()
        {
            if (!skeleton || sources == null || images == null) return;
            int count = Mathf.Min(sources.Length, images.Length);
            for (int i = 0; i < count; i++)
            {
                var source = sources[i];
                var image = images[i];
                if (!source || !image) continue;
                image.gameObject.SetActive(source.gameObject.activeInHierarchy);
                image.anchoredPosition = skeleton.InverseTransformPoint(source.position);
                image.localRotation = Quaternion.Inverse(skeleton.rotation) * source.rotation;
                var scale = source.lossyScale;
                var rootScale = skeleton.lossyScale;
                if (Mathf.Abs(rootScale.x) < .00001f || Mathf.Abs(rootScale.y) < .00001f) continue;
                image.localScale = new Vector3(scale.x / rootScale.x, scale.y / rootScale.y, 1);
            }
        }
    }
}
