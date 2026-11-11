using System;

namespace SkyTree
{
    public sealed class RoutePlan
    {
        public int Source { get; }
        public int Destination { get; }
        public int Distance { get; }
        public int WaveCount => Distance * 3;
        public int BossCount => 1;
        private RoutePlan(int source, int destination, int distance)
        { Source = source; Destination = destination; Distance = distance; }

        public static bool TryCreate(int source, int destination, out RoutePlan plan)
        {
            plan = null;
            if (source < 0 || source > 8 || destination < 0 || destination > 8 || source == destination) return false;
            int dx = Math.Abs(destination % 3 - source % 3);
            int dy = Math.Abs(destination / 3 - source / 3);
            if (dx != 0 && dy != 0 && dx != dy) return false;
            plan = new RoutePlan(source, destination, Math.Max(dx, dy));
            return true;
        }
    }
}
