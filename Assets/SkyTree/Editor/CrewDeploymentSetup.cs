using UnityEditor;
using UnityEngine;

namespace SkyTree.EditorTools
{
    public static class CrewDeploymentSetup
    {
        [MenuItem("Tools/SkyTree/Configure Two Crew Deployment")]
        public static void Apply()
        {
            var navigator = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Coworker/FemaleNavigator.prefab");
            string battlePath = "Assets/Prefabs/MainShip/MainShip_Step01_Adventure.prefab";
            var battle = PrefabUtility.LoadPrefabContents(battlePath);
            try
            {
                var captain = (RectTransform)battle.transform.Find("Captain Station");
                captain.anchoredPosition = new Vector2(-130, 60);
                var station = battle.transform.Find("Navigator Station") as RectTransform;
                if (!station)
                {
                    station = new GameObject("Navigator Station", typeof(RectTransform)).GetComponent<RectTransform>();
                    station.SetParent(battle.transform, false); station.sizeDelta = Vector2.zero;
                }
                station.anchoredPosition = new Vector2(90, -40);
                captain.SetAsLastSibling(); station.SetAsLastSibling();
                battle.transform.Find("SkyTreeFoliage").SetAsLastSibling();
                var so = new SerializedObject(battle.GetComponent<BattleShipView>());
                so.FindProperty("navigatorRig").objectReferenceValue = navigator;
                so.FindProperty("navigatorAnchor").objectReferenceValue = station;
                so.FindProperty("captainPixelsPerUnit").floatValue = 18;
                so.FindProperty("navigatorPixelsPerUnit").floatValue = 15.5f;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(battle, battlePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(battle); }

            string lobbyPath = "Assets/Prefabs/MainShip/MainShip_Step01_Lobby.prefab";
            var lobby = PrefabUtility.LoadPrefabContents(lobbyPath);
            try
            {
                var captain = lobby.GetComponent<LobbyCaptainWander>();
                var all = lobby.GetComponents<LobbyCaptainWander>();
                var companion = all.Length > 1 ? all[1] : lobby.AddComponent<LobbyCaptainWander>();
                EditorUtility.CopySerialized(captain, companion);
                var so = new SerializedObject(companion);
                so.FindProperty("rigPrefab").objectReferenceValue = navigator;
                so.FindProperty("staticCaptain").objectReferenceValue = null;
                so.FindProperty("actorName").stringValue = "Wandering Navigator";
                so.FindProperty("pixelsPerUnit").floatValue = 18.5f;
                so.FindProperty("routeOffset").intValue = 4;
                so.FindProperty("initialWait").floatValue = 2.7f;
                so.FindProperty("pace").floatValue = .8f;
                so.FindProperty("canopyFrontEdge").floatValue = -98;
                SetPoints(so.FindProperty("deckBoundary"), new[]{new Vector2(-43,-49),new Vector2(-13,-28),new Vector2(68,-63),new Vector2(73,-100),new Vector2(53,-123),new Vector2(-28,-87)});
                SetPoints(so.FindProperty("stops"), new[]{new Vector2(8,-99),new Vector2(18,-104),new Vector2(30,-110),new Vector2(42,-111),new Vector2(54,-108),new Vector2(48,-103),new Vector2(36,-105),new Vector2(20,-102)});
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(lobby, lobbyPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            AssetDatabase.SaveAssets();
        }
        static void SetPoints(SerializedProperty p, Vector2[] points)
        {
            p.arraySize = points.Length;
            for (int i=0;i<points.Length;i++) p.GetArrayElementAtIndex(i).vector2Value = points[i];
        }
    }
}
