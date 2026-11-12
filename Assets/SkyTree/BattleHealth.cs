using System;

namespace SkyTree
{
    public sealed class BattleHealth
    {
        public int Maximum { get; }
        public int Current { get; private set; }
        public bool IsAlive => Current > 0;
        public float Fraction => (float)Current / Maximum;
        public event Action Changed;

        public BattleHealth(int maximum)
        {
            if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));
            Maximum = Current = maximum;
        }

        // Returns actual damage, so overkill never produces negative HP.
        public int Damage(int amount)
        {
            if (amount <= 0 || !IsAlive) return 0;
            int applied = Math.Min(Current, amount);
            Current -= applied;
            Changed?.Invoke();
            return applied;
        }
    }
}
