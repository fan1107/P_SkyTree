using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace SkyTree.EditorTools
{
    public static class BattleBackgroundBuilder
    {
        public static void Build()
        {
            const string path = "Assets/SkyTree/Resources/SkyTree/BattleOceanIslands.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var root = new GameObject("BattleBackground", typeof(RectTransform), typeof(RectMask2D));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(400, 720);
                var ocean = new GameObject("Scrolling ocean and islands", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                ocean.transform.SetParent(rect, false);
                ocean.rectTransform.anchorMin = Vector2.zero;
                ocean.rectTransform.anchorMax = Vector2.one;
                ocean.rectTransform.offsetMin = ocean.rectTransform.offsetMax = Vector2.zero;
                ocean.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                const string materialPath = "Assets/SkyTree/Resources/SkyTree/BattleOceanRepeat.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (!material)
                {
                    material = new Material(Shader.Find("SkyTree/Battle Ocean Repeat"));
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                ocean.material = material;
                ocean.raycastTarget = false;
                var clouds = new RectTransform[4];
                var cloudTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SkyTree/Resources/SkyTree/PassingCloud.png");
                Vector2[] positions = {new Vector2(-120, 240),new Vector2(140,-70),new Vector2(-200,-250),new Vector2(180,340)};
                for (int i = 0; i < clouds.Length; i++)
                {
                    var cloud = new GameObject("Passing cloud " + (i + 1), typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                    cloud.transform.SetParent(rect, false);
                    float width = 260 + i * 25;
                    cloud.rectTransform.sizeDelta = new Vector2(width, width * cloudTexture.height / cloudTexture.width);
                    cloud.rectTransform.anchoredPosition = positions[i];
                    cloud.texture = cloudTexture;
                    cloud.color = new Color(1,1,1,.28f + (i % 2) * .1f);
                    cloud.raycastTarget = false;
                    clouds[i] = cloud.rectTransform;
                }
                var view = root.AddComponent<BattleBackgroundView>();
                var so = new SerializedObject(view);
                so.FindProperty("ocean").objectReferenceValue = ocean;
                var list = so.FindProperty("clouds");
                list.arraySize = clouds.Length;
                for (int i = 0; i < clouds.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = clouds[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/SkyTree/Resources/SkyTree/BattleBackground.prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
