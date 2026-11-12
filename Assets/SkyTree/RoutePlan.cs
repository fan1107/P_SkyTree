using System;
using System.Collections.Generic;

namespace SkyTree
{
    public sealed class RoutePlan
    {
        public const int WavesPerCell = 2;
        public int Source { get; }
        public int Destination { get; }
        // Includes the source and every crossed cell, in travel order.
        public IReadOnlyList<int> Waypoints { get; }
        public int Distance => Waypoints.Count - 1;
        public int OptionIndex { get; }
        public int OptionCount { get; }
        public int WaveCount => Distance * WavesPerCell;
        public int BossCount => 1;
        private RoutePlan(int source, int destination, int[] waypoints, int optionIndex, int optionCount)
        {
            Source = source;
            Destination = destination;
            Waypoints = Array.AsReadOnly(waypoints);
            OptionIndex = optionIndex;
            OptionCount = optionCount;
        }

        public static bool TryCreate(int source, int destination, out RoutePlan plan, int optionIndex = 0)
        {
            plan = null;
            if (source < 0 || source > 8 || destination < 0 || destination > 8 || source == destination || optionIndex < 0) return false;
            int dx = destination % 3 - source % 3;
            int dy = destination / 3 - source / 3;
            int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
            bool bends = dx != 0 && dy != 0 && Math.Abs(dx) != Math.Abs(dy);
            int optionCount = bends ? 2 : 1;
            optionIndex %= optionCount;

            int[] waypoints;
            if (distance == 1) waypoints = new[] { source, destination };
            else
            {
                int stepX = Math.Sign(dx), stepY = Math.Sign(dy);
                // First choice: straight then diagonal. Second: diagonal then straight.
                if (bends && optionIndex == 0)
                {
                    if (Math.Abs(dx) > Math.Abs(dy)) stepY = 0;
                    else stepX = 0;
                }
                int via = source + stepX + stepY * 3;
                waypoints = new[] { source, via, destination };
            }
            plan = new RoutePlan(source, destination, waypoints, optionIndex, optionCount);
            return true;
        }
    }
}
