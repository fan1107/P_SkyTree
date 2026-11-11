using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    public sealed class LobbyCurrencyHud : MonoBehaviour
    {
        [SerializeField] private Text energy;
        [SerializeField] private Text gold;
        [SerializeField] private Text gems;

        public void Display(int energyValue, int goldValue, int gemValue)
        {
            energy.text = energyValue.ToString("N0");
            gold.text = goldValue.ToString("N0");
            gems.text = gemValue.ToString("N0");
        }
    }
}
