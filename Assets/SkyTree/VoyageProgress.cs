using System;
using System.Collections.Generic;

namespace SkyTree
{
    public enum VoyagePhase { NormalWave, Intermission, Boss, Victory, Defeat, Retreat }

    // Rules are independent of scene lifetime and presentation.
    public sealed class VoyageProgress
    {
        public RoutePlan Route { get; }
        public VoyagePhase Phase { get; private set; } = VoyagePhase.NormalWave;
        public int ClearedWaves { get; private set; }
        public int WaveNumber => Math.Min(ClearedWaves + 1, Route.WaveCount);
        public int BossStarts { get; private set; }
        public IReadOnlyList<ItemReward> EarnedRewards { get; private set; } = Array.Empty<ItemReward>();

        // A future reward provider supplies finalized positive deltas; no sample amounts or payout rules here.
        public void SetEarnedRewards(IReadOnlyList<ItemReward> rewards)
        {
            var copy = new List<ItemReward>();
            if (rewards != null)
                foreach (var reward in rewards) if (reward.IsValid) copy.Add(reward);
            EarnedRewards = copy.AsReadOnly();
        }
        public bool Finished => Phase == VoyagePhase.Victory || Phase == VoyagePhase.Defeat || Phase == VoyagePhase.Retreat;
        // The destination is not a checkpoint until its boss has been defeated.
        public int ReachedPoint => Phase == VoyagePhase.Victory ? Route.Destination
            : Route.Waypoints[Math.Min(ClearedWaves / RoutePlan.WavesPerCell, Route.Distance - 1)];

        public VoyageProgress(RoutePlan route) { Route = route ?? throw new ArgumentNullException(nameof(route)); }

        public bool ClearEncounter()
        {
            if (Phase == VoyagePhase.NormalWave)
            {
                ClearedWaves++;
                Phase = VoyagePhase.Intermission;
                return true;
            }
            if (Phase != VoyagePhase.Boss) return false;
            Phase = VoyagePhase.Victory;
            return true;
        }

        public bool BeginNextEncounter()
        {
            if (Phase != VoyagePhase.Intermission) return false;
            if (ClearedWaves == Route.WaveCount) { Phase = VoyagePhase.Boss; BossStarts++; }
            else Phase = VoyagePhase.NormalWave;
            return true;
        }

        public void Fail() { if (!Finished) Phase = VoyagePhase.Defeat; }
        public void Retreat() { if (!Finished) Phase = VoyagePhase.Retreat; }
    }
}
