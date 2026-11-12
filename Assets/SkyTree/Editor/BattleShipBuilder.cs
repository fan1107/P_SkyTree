using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree.EditorTools
{
    public static class BattleShipBuilder
    {
        public static void Build()
        {
            string hullPath = "Assets/ArtResources/Ships/MainShip_Step01/HullReverseMatched.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(hullPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            var lobby = PrefabUtility.LoadPrefabContents("Assets/Prefabs/MainShip/MainShip_Step01_Lobby.prefab");
            GameObject root = null;
            try
            {
                root = Object.Instantiate(lobby);
                // 로비 표시 연출은 전투 프리팹으로 복사하지 않는다.
                foreach (var walker in root.GetComponents<LobbyCaptainWander>()) Object.DestroyImmediate(walker);
                Object.DestroyImmediate(root.GetComponent<LobbyShipView>());
                root.name = "MainShip_Step01_Adventure";
                var rect = root.GetComponent<RectTransform>();
                rect.SetParent(null, false);
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(0, -30);
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
                foreach (string name in new[] {"Flag", "Flag (1)", "Propeller", "Planter", "Captain"})
                {
                    var child = rect.Find(name);
                    if (child) Object.DestroyImmediate(child.gameObject);
                }
                var hull = rect.Find("Hull").GetComponent<Image>();
                hull.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(hullPath);
                hull.preserveAspect = true;
                hull.rectTransform.sizeDelta = new Vector2(390, 331);
                hull.rectTransform.anchoredPosition = new Vector2(0, -30);
                foreach (string name in new[] {"SkyTreeTrunk", "SkyTreeFoliage"})
                {
                    var tree = (RectTransform)rect.Find(name);
                    tree.anchoredPosition = new Vector2(75, 12);
                    tree.localScale = Vector3.one * .65f;
                }
                var cannon = (RectTransform)rect.Find("Cannon");
                cannon.anchoredPosition = new Vector2(-93, 26);
                cannon.localScale = new Vector3(-1, 1, 1);
                var captain = new GameObject("Captain Station", typeof(RectTransform)).GetComponent<RectTransform>();
                captain.SetParent(rect, false);
                captain.sizeDelta = Vector2.zero;
                captain.anchoredPosition = new Vector2(-25, -5);
                var view = root.AddComponent<BattleShipView>();
                var so = new SerializedObject(view);
                so.FindProperty("captainRig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Coworker/Captain.prefab");
                so.FindProperty("captainAnchor").objectReferenceValue = captain;
                so.FindProperty("captainPixelsPerUnit").floatValue = 20f;
                so.ApplyModifiedPropertiesWithoutUndo();
                rect.Find("Hull").SetAsFirstSibling();
                rect.Find("SkyTreeTrunk").SetAsLastSibling();
                cannon.SetAsLastSibling();
                captain.SetAsLastSibling();
                rect.Find("SkyTreeFoliage").SetAsLastSibling();
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/MainShip/MainShip_Step01_Adventure.prefab");
            }
            finally
            {
                if (root) Object.DestroyImmediate(root);
                PrefabUtility.UnloadPrefabContents(lobby);
            }
        }
    }
}
