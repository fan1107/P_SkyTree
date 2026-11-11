using UnityEngine;

namespace SkyTree
{
    public sealed class ShipTreeGrowth : MonoBehaviour
    {
        [SerializeField, Range(1, 3)] private int stage = 1;
        [SerializeField] private GameObject[] stages;
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
            if (stages == null) return;
            for (int i = 0; i < stages.Length; i++)
                if (stages[i]) stages[i].SetActive(i == stage - 1);
        }
    }
}
