using System;
using UnityEngine;

namespace SkyTree
{
    [CreateAssetMenu(menuName = "SkyTree/World Lobby Table", fileName = "WorldLobbyTable")]
    public sealed class WorldLobbyTable : ScriptableObject
    {
        public const int WorldCount = 9;
        public const int RegionsPerWorld = 9;

        [Serializable]
        public sealed class Region
        {
            [SerializeField] private string displayName;
            [SerializeField] private LobbyBackgroundView background;
            [SerializeField] private Color worldLabelColor = Color.white;
            public string DisplayName => displayName;
            public LobbyBackgroundView Background => background;
            public Color WorldLabelColor => worldLabelColor;
        }

        [Serializable]
        private sealed class World
        {
            public Region[] regions = new Region[RegionsPerWorld];
        }

        [SerializeField] private Region defaultRegion;
        [SerializeField] private World[] worlds = new World[WorldCount];

        // Both IDs are one-based, matching the world map's region numbering.
        public Region Resolve(int worldNumber, int regionNumber)
        {
            if (worldNumber < 1 || worldNumber > WorldCount || regionNumber < 1 || regionNumber > RegionsPerWorld)
                return null;
            if (worlds == null || worldNumber > worlds.Length) return defaultRegion;
            var regions = worlds[worldNumber - 1]?.regions;
            if (regions == null || regionNumber > regions.Length) return defaultRegion;
            var region = regions[regionNumber - 1];
            // 미제작 월드는 지정된 기본 지역을 공유한다. 지역별 UI/배 참조는 테이블에 넣지 않는다.
            return region != null && region.Background ? region : defaultRegion;
        }

        private void OnValidate()
        {
            if (worlds == null || worlds.Length != WorldCount) Array.Resize(ref worlds, WorldCount);
            for (int i = 0; i < worlds.Length; i++)
            {
                if (worlds[i] == null) worlds[i] = new World();
                if (worlds[i].regions == null || worlds[i].regions.Length != RegionsPerWorld)
                    Array.Resize(ref worlds[i].regions, RegionsPerWorld);
            }
        }
    }
}
