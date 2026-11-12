using System;
using UnityEngine;

namespace SkyTree
{
    public sealed class VoyageBattleController : MonoBehaviour
    {
        public const int EnemiesPerWave = 3;
        [SerializeField] private float intermissionDuration = 2f;
        private BattleCannonSequence battle;
        private BattleShipView ship;
        private float delay;
        private int savedPoint;
        private bool notified;
        public VoyageProgress Progress { get; private set; }
        public event Action Changed;
        public event Action<int> CheckpointReached;
        public event Action<VoyageProgress> Completed;

        public void Initialize(RoutePlan route, BattleCannonSequence sequence, BattleShipView player)
        {
            Progress = new VoyageProgress(route);
            battle = sequence; ship = player; savedPoint = route.Source;
        }

        private void Update()
        {
            if (Progress == null || notified) return;
            if (!Progress.Finished && !ship.Health.IsAlive)
            {
                Progress.Fail(); delay = 1.2f; Changed?.Invoke();
            }
            if (Progress.Finished)
            {
                delay -= Time.deltaTime;
                if (delay <= 0) NotifyCompletion();
                return;
            }
            if (Progress.Phase == VoyagePhase.Intermission)
            {
                delay -= Time.deltaTime;
                if (delay > 0) return;
                Progress.BeginNextEncounter();
                bool boss = Progress.Phase == VoyagePhase.Boss;
                battle.StartEncounter(boss ? 1 : EnemiesPerWave, boss);
                Changed?.Invoke();
                return;
            }
            if (battle.Swarm.AliveCount != 0 || !Progress.ClearEncounter()) return;
            if (Progress.ReachedPoint != savedPoint && !Progress.Finished)
            {
                savedPoint = Progress.ReachedPoint;
                CheckpointReached?.Invoke(savedPoint);
            }
            delay = Progress.Finished ? 1.2f : intermissionDuration;
            Changed?.Invoke();
        }

        public void Retreat()
        {
            if (Progress == null || notified) return;
            Progress.Retreat();
            NotifyCompletion();
        }

        private void NotifyCompletion()
        {
            if (notified) return;
            notified = true;
            Completed?.Invoke(Progress);
        }
    }
}
