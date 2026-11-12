using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace SkyTree.Editor
{
    // Creates authored assets only. Never saves the open scene or rebuilds existing prefabs.
    public static class VoyageResultBuilder
    {
        public const string PopupPath = "Assets/UIPrefabs/Popups/VoyageResultPopup.prefab";
        public const string SlotPath = "Assets/UIPrefabs/Common/SrcItemSlot.prefab";
        const string Art = "Assets/ArtResources/Atlas/Popup/";
        const string Icons = "Assets/Prefabs/UI/Icon/";
        const string MapArt = "Assets/ArtResources/Atlas/RouteMap/";
        static readonly Color Ink = new Color32(93, 57, 17, 255);
        static Font font;
        static Sprite panel, button;

        static RectTransform Node(string name, Transform parent, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.gameObject.layer = 5; r.SetParent(parent, false);
            r.sizeDelta = new Vector2(w, h); r.anchoredPosition = new Vector2(x, y); return r;
        }
        static Image Picture(RectTransform r, Sprite sprite, Color tint, bool sliced = false)
        {
            var im = r.gameObject.AddComponent<Image>(); im.sprite = sprite; im.color = tint; im.raycastTarget = false;
            im.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            if (sliced) im.pixelsPerUnitMultiplier = 12;
            return im;
        }
        static Text Label(string name, Transform parent, float x, float y, float w, float h, int size, string value, Color color)
        {
            var t = Node(name, parent, x, y, w, h).gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.text = value; t.color = color; t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
            return t;
        }
        static RouteMapGraphic Graphic(string name, Transform parent, RouteMapGraphic.Shape shape, float w, float h, Color color)
        {
            var g = Node(name, parent, 0, 0, w, h).gameObject.AddComponent<RouteMapGraphic>();
            g.shape = shape; g.color = color; g.raycastTarget = false; return g;
        }
        static void Ref(SerializedObject so, string field, Object value) => so.FindProperty(field).objectReferenceValue = value;

        static void Import(string path, int maxSize, Vector4 border)
        {
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.npotScale = TextureImporterNPOTScale.None;
            imp.maxTextureSize = maxSize; imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.spriteBorder = border; imp.spritePixelsPerUnit = 100; imp.SaveAndReimport();
        }

        // Standardize generated icon files to the user's requested 512px delivery size, preserving alpha.
        static void NormalizeIcon(string path)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(path));
            if (source.width == 512 && source.height == 512) { Object.DestroyImmediate(source); return; }
            var result = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            var pixels = new Color[512 * 512];
            for (int y = 0; y < 512; y++) for (int x = 0; x < 512; x++)
                pixels[y * 512 + x] = source.GetPixelBilinear((x + .5f) / 512, (y + .5f) / 512);
            result.SetPixels(pixels); result.Apply(); File.WriteAllBytes(path, result.EncodeToPNG());
            Object.DestroyImmediate(source); Object.DestroyImmediate(result);
        }

        [MenuItem("SkyTree/Build Voyage Result Assets (Create Only)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before creating prefab assets.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath) || AssetDatabase.LoadAssetAtPath<GameObject>(SlotPath))
                throw new InvalidOperationException("Result assets already exist. Edit the authored prefabs directly.");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Import(Art + "VoyageSuccessCrest.png", 2048, Vector4.zero);
            Import(Art + "ParchmentPanel.png", 2048, new Vector4(125, 125, 125, 125));
            panel = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "ParchmentPanel.png");
            button = AssetDatabase.LoadAllAssetsAtPath("Assets/ArtResources/Atlas/Lobby/LobbyRenewal.png").OfType<Sprite>().First(x => x.name == "MainTabCommon");
            string[] ids = { "Gold", "Wood", "Gem" }, names = { "골드", "목재", "젬" };
            for (int i = 0; i < ids.Length; i++)
            {
                string path = Icons + "Icon" + ids[i] + ".png";
                NormalizeIcon(path); Import(path, 512, Vector4.zero);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var icon = Node("Icon" + ids[i], null, 0, 0, 512, 512);
                Picture(icon, sprite, Color.white).preserveAspect = true;
                PrefabUtility.SaveAsPrefabAsset(icon.gameObject, Icons + "Icon" + ids[i] + ".prefab");
                Object.DestroyImmediate(icon.gameObject);
                var item = ScriptableObject.CreateInstance<ItemDefinition>();
                var itemSo = new SerializedObject(item);
                itemSo.FindProperty("itemId").stringValue = ids[i].ToLowerInvariant();
                itemSo.FindProperty("displayName").stringValue = names[i]; Ref(itemSo, "icon", sprite);
                itemSo.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(item, "Assets/SkyTree/Data/Items/" + ids[i] + ".asset");
            }
            BuildSlot(); BuildPopup();
            var atlas = new SpriteAtlas();
            AssetDatabase.CreateAsset(atlas, Art + "Popup.spriteatlas");
            atlas.Add(new Object[] { AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "VoyageSuccessCrest.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "ParchmentPanel.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>(Icons + "IconGold.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>(Icons + "IconWood.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>(Icons + "IconGem.png") });
            var packing = atlas.GetPackingSettings(); packing.enableRotation = false; packing.enableTightPacking = false;
            atlas.SetPackingSettings(packing);
            var platform = atlas.GetPlatformSettings("DefaultTexturePlatform"); platform.maxTextureSize = 4096;
            atlas.SetPlatformSettings(platform); AssetDatabase.SaveAssetIfDirty(atlas);
        }

        static void BuildSlot()
        {
            var root = Node("SrcItemSlot", null, 0, 0, 96, 96);
            try
            {
                var slot = root.gameObject.AddComponent<UIItemSlot>(); var so = new SerializedObject(slot);
                var layout = root.gameObject.AddComponent<LayoutElement>(); layout.preferredWidth = layout.preferredHeight = 96;
                var bg = Picture(Node("Grade background", root, 0, 0, 96, 96), panel, Color.white, true); Ref(so, "gradeBackground", bg);
                var icon = Picture(Node("Icon", root, 0, 9, 75, 75), null, Color.white); icon.preserveAspect = true; Ref(so, "itemIcon", icon);
                Picture(Node("Count strip", root, 0, -30, 85, 22), null, new Color32(255, 235, 192, 205));
                var count = Label("Count", root, 0, -30, 82, 23, 18, "", Ink);
                count.resizeTextForBestFit = true; count.resizeTextMinSize = 9; count.resizeTextMaxSize = 18; Ref(so, "countText", count);
                var click = Picture(Node("Click area", root, 0, 0, 96, 96), null, Color.clear); click.raycastTarget = true;
                var buttonComponent = click.gameObject.AddComponent<Button>(); buttonComponent.targetGraphic = click;
                buttonComponent.transition = Selectable.Transition.None; Ref(so, "clickArea", buttonComponent);
                so.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root.gameObject, SlotPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        static void BuildPopup()
        {
            var root = Node("VoyageResultPopup", null, 0, 0, 400, 720);
            try
            {
                var popup = root.gameObject.AddComponent<VoyageResultPopup>(); var so = new SerializedObject(popup);
                Picture(Node("Modal blocker", root, 0, 0, 400, 720), null, new Color(.015f, .04f, .06f, .7f)).raycastTarget = true;
                var body = Node("Panel", root, 0, -7, 352, 638);
                Picture(body, panel, Color.white, true).raycastTarget = true;
                var mapRect = Node("Arrival map", body, 0, 54, 328, 324);
                var map = mapRect.gameObject.AddComponent<VoyageResultMapView>(); var mapSo = new SerializedObject(map);
                Picture(mapRect, AssetDatabase.LoadAssetAtPath<Sprite>(MapArt + "CloudChartLandmarks.png"), Color.white);
                Graphic("Grid", mapRect, RouteMapGraphic.Shape.Grid, 328, 324, new Color32(122, 94, 47, 130));
                Ref(mapSo, "route", Graphic("Travelled route", mapRect, RouteMapGraphic.Shape.Route, 328, 324, new Color32(255, 180, 10, 255)));
                var points = mapSo.FindProperty("points"); points.arraySize = 9;
                for (int i = 0; i < 9; i++)
                {
                    var point = Graphic("Waypoint_" + (i + 1), mapRect, RouteMapGraphic.Shape.Point, 18, 18, new Color32(255, 209, 77, 255));
                    point.rectTransform.anchoredPosition = new Vector2((i % 3 - 1) * 328f / 3, (1 - i / 3) * 108);
                    points.GetArrayElementAtIndex(i).objectReferenceValue = point.rectTransform;
                }
                Ref(mapSo, "destinationRing", Graphic("Destination ring", mapRect, RouteMapGraphic.Shape.Selection, 30, 30, Color.white).rectTransform);
                var ship = Node("Ship marker", mapRect, 0, 0, 64, 53);
                Picture(ship, AssetDatabase.LoadAssetAtPath<Sprite>(MapArt + "TreeShipMarker.png"), Color.white).preserveAspect = true;
                Ref(mapSo, "ship", ship); Ref(mapSo, "positionLabel", Label("Position label", mapRect, 0, 0, 84, 20, 12, "도착", Ink));
                mapSo.ApplyModifiedPropertiesWithoutUndo(); Ref(so, "map", map);
                Picture(Node("Rewards separator", body, 0, -114, 328, 1), null, new Color32(156, 109, 42, 255));
                Label("Rewards heading", body, 0, -135, 200, 28, 21, "습득 아이템", Ink);
                var scrollRoot = Node("Reward list", body, 0, -203, 318, 100);
                var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
                var viewport = Node("Viewport", scrollRoot, 0, 0, 318, 100);
                Picture(viewport, null, Color.clear).raycastTarget = true; viewport.gameObject.AddComponent<RectMask2D>();
                var content = Node("Content", viewport, 0, 0, 318, 100);
                content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
                content.pivot = new Vector2(.5f, 1); content.sizeDelta = new Vector2(0, 100);
                var grid = content.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(96, 96);
                grid.spacing = new Vector2(12, 8); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3; grid.childAlignment = TextAnchor.UpperCenter; grid.padding = new RectOffset(0, 0, 2, 2);
                var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scroll.viewport = viewport; scroll.content = content;
                Ref(so, "rewardScroll", scroll); Ref(so, "rewardContent", content);
                Ref(so, "itemSlotPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(SlotPath).GetComponent<UIItemSlot>());
                Ref(so, "emptyRewards", Label("Empty rewards", scrollRoot, 0, 0, 300, 40, 15, "습득한 아이템이 없습니다", Ink).gameObject);
                var buttonImage = Picture(Node("Confirm", body, 0, -284, 296, 48), button, Color.white, true);
                buttonImage.pixelsPerUnitMultiplier = 5; buttonImage.raycastTarget = true;
                var confirm = buttonImage.gameObject.AddComponent<Button>(); confirm.targetGraphic = buttonImage;
                Label("Label", buttonImage.transform, 0, 1, 270, 40, 27, "확인", new Color32(255, 239, 196, 255));
                Ref(so, "confirm", confirm);
                var crest = Node("Golden compass crest", body, 0, 263, 372, 124);
                Picture(crest, AssetDatabase.LoadAssetAtPath<Sprite>(Art + "VoyageSuccessCrest.png"), Color.white).preserveAspect = true;
                var title = Label("Success title", crest, 0, -28, 234, 40, 29, "항해 성공", new Color32(255, 222, 133, 255));
                var shadow = title.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color32(64, 32, 9, 255); shadow.effectDistance = new Vector2(1.2f, -1.5f);
                so.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root.gameObject, PopupPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }
    }
}
