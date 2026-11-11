using UnityEngine;

namespace SkyTree
{
    public sealed class ShipTreeGrowth : MonoBehaviour
    {
        [SerializeField, Range(1, 3)] private int stage = 1;
        [SerializeField] private GameObject[] stages;
        [SerializeField] private GameObject[] foliageStages;
        public int Stage => stage;

        public void SetStage(int value)
        {
            stage = Mathf.Clamp(value, 1, 3);
            ApplyStage();
        }

        private void OnEnable() => ApplyStage();

        private void OnValidate() => ApplyStage();

        private void ApplyStage()
        {
            ApplyStages(stages);
            ApplyStages(foliageStages);
        }

        private void ApplyStages(GameObject[] targets)
        {
            if (targets == null) return;
            for (int i = 0; i < targets.Length; i++)
                if (targets[i]) targets[i].SetActive(i == stage - 1);
        }
    }
}
