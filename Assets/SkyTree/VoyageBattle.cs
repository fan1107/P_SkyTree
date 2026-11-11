namespace SkyTree
{
    // First route tuning. A new voyage always starts with a provisioned hull.
    internal sealed class VoyageBattle
    {
        public const int HullCapacity = 100;
        public const int Reward = 80;
        public const int Waves = 3;
        public int Wave { get; private set; } = 1;
        public int Hull { get; private set; } = HullCapacity;
        public int EnemyCapacity => Wave == Waves ? 54 : 30;
        public int Enemy { get; private set; } = 30;
        public bool Finished { get; private set; }
        public float ShotAge { get; private set; } = 1;
        public float RepairCooldown { get; private set; }
        private float nextShot = .6f;
        private float nextHit = 2.1f;

        public void Tick(float dt)
        {
            if (Finished) return;
            RepairCooldown = UnityEngine.Mathf.Max(0, RepairCooldown - dt);
            ShotAge += dt;
            nextShot -= dt;
            nextHit -= dt;
            if (nextShot <= 0)
            {
                ShotAge = 0;
                nextShot = 1.15f;
                Enemy -= 8;
                if (Enemy <= 0)
                {
                    if (Wave == Waves) { Finished = true; return; }
                    Wave++;
                    Enemy = EnemyCapacity;
                    nextHit = 1.8f;
                }
            }
            if (nextHit <= 0)
            {
                Hull = UnityEngine.Mathf.Max(0, Hull - (Wave == Waves ? 14 : 10));
                nextHit = 2.1f;
                Finished = Hull == 0;
            }
        }

        public void Repair()
        {
            if (Finished || Hull >= HullCapacity || RepairCooldown > 0) return;
            Hull = UnityEngine.Mathf.Min(HullCapacity, Hull + 25);
            RepairCooldown = 5;
        }
    }
}
