using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SkyTree.CaptainPrototype
{
    public static class CaptainRigBuilder
    {
        public const string Root = "Assets/Tests/CaptainRig";
        public const string ArtRoot = "Assets/ArtResources/Coworker";
        private static readonly string[] Names = { "Head", "Torso", "Pelvis", "ArmLeft", "ArmRight", "Collar", "LegLeft", "LegRight", "Scarf" };
        private static readonly Vector2[] Pivots = {
            new Vector2(.59f,.04f), new Vector2(.5f,.12f), new Vector2(.5f,.88f),
            new Vector2(.74f,.91f), new Vector2(.30f,.91f), new Vector2(.5f,.5f),
            new Vector2(.55f,.94f), new Vector2(.45f,.94f), new Vector2(.94f,.74f) };

        [MenuItem("Tools/Captain Rig/Create Test Scene")]
        public static void Build()
        {
            // Leave the MCP callback before scene lifecycle events run.
            EditorApplication.delayCall += BuildNow;
        }

        public static void BuildNow()
        {
            string scenePath = Root + "/CaptainRigTest.unity";
            if (File.Exists(scenePath)) throw new InvalidOperationException("Test scene already exists; open it instead of overwriting.");
            Directory.CreateDirectory(Root + "/Animations");
            Slice();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root + "/Animations/Captain.controller");
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(Root + "/Animations/Captain.controller");
            if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
            var machine = controller.layers[0].stateMachine;
            foreach (var previous in machine.states) machine.RemoveState(previous.state);
            for (int i = 0; i < 3; i++)
            {
                var generated = MakeClip(i);
                string clipPath = Root + "/Animations/" + generated.name + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null) { clip = generated; AssetDatabase.CreateAsset(clip, clipPath); }
                else { EditorUtility.CopySerialized(generated, clip); UnityEngine.Object.DestroyImmediate(generated); }
                var state = machine.AddState(clip.name, new Vector3(240, i * 80));
                state.motion = clip;
                if (i == 0) machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var camera = new GameObject("Preview Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 2.65f;
            camera.transform.position = new Vector3(0, 1.8f, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.065f,.09f,.13f);
            var demo = new GameObject("Preview Controls").AddComponent<CaptainRigDemo>();
            demo.previewCamera = camera;
            demo.characters = new Animator[3];
            for (int i = 0; i < 3; i++)
            {
                var character = MakeRig();
                character.name = new[] { "Captain_Breathe", "Captain_Walk", "Captain_Run" }[i];
                character.transform.position = new Vector3((i - 1) * 2.65f, 0, 0);
                var animator = character.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (i == 0) PrefabUtility.SaveAsPrefabAsset(character, "Assets/Prefabs/Coworker/Captain.prefab");
                // Different default states without duplicating the editable source clips.
                if (i > 0)
                {
                    string path = Root + "/Animations/Preview" + i + ".controller";
                    AssetDatabase.CopyAsset(Root + "/Animations/Captain.controller", path);
                    var preview = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                    var sm = preview.layers[0].stateMachine;
                    sm.defaultState = sm.states[i].state;
                    animator.runtimeAnimatorController = preview;
                }
                demo.characters[i] = animator;
            }
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = demo.characters[1].gameObject;
        }

        private static void Slice()
        {
            string atlasPath = ArtRoot + "/CaptainPartsAtlas.png";
            AssetDatabase.ImportAsset(atlasPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(atlasPath);
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            // Normalized source rectangles, measured from the top-left of the generated atlas.
            float[,] regions = { {0,0,.34f,.33f}, {.37f,.04f,.62f,.32f}, {.66f,.07f,.98f,.30f},
                {.07f,.35f,.26f,.64f}, {.42f,.35f,.59f,.64f}, {.70f,.40f,.94f,.55f},
                {.08f,.67f,.25f,.99f}, {.42f,.67f,.59f,.99f}, {.65f,.69f,.99f,.96f} };
            for (int i = 0; i < Names.Length; i++)
            {
                int x = Mathf.FloorToInt(regions[i,0] * atlas.width), y = Mathf.FloorToInt((1-regions[i,3]) * atlas.height);
                int w = Mathf.FloorToInt((regions[i,2]-regions[i,0]) * atlas.width);
                int h = Mathf.FloorToInt((regions[i,3]-regions[i,1]) * atlas.height);
                var pixels = atlas.GetPixels(x,y,w,h);
                int minX=w,minY=h,maxX=0,maxY=0;
                for (int py=0;py<h;py++) for(int px=0;px<w;px++)
                    if (pixels[py*w+px].a > .03f) { minX=Math.Min(minX,px);minY=Math.Min(minY,py);maxX=Math.Max(maxX,px);maxY=Math.Max(maxY,py); }
                if (minX > maxX) throw new InvalidOperationException("Empty atlas part: " + Names[i]);
                var texture = new Texture2D(maxX-minX+1,maxY-minY+1,TextureFormat.RGBA32,false);
                texture.SetPixels(atlas.GetPixels(x+minX,y+minY,texture.width,texture.height));
                texture.Apply();
                string path = ArtRoot + "/" + Names[i] + ".png";
                File.WriteAllBytes(path,texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var part = (TextureImporter)AssetImporter.GetAtPath(path);
                part.textureType = TextureImporterType.Sprite;
                part.spriteImportMode = SpriteImportMode.Single;
                part.spritePixelsPerUnit = 100;
                part.alphaIsTransparency = true;
                part.mipmapEnabled = false;
                part.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings();
                part.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = Pivots[i];
                part.SetTextureSettings(settings);
                part.SaveAndReimport();
            }
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static Transform Bone(string name, Transform parent, float x, float y)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent,false);
            t.localPosition = new Vector3(x,y,0);
            return t;
        }

        private static void Art(string name, Transform bone, float height, int order)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/" + name + ".png");
            var visual = Bone("Art", bone,0,0);
            var renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            var material = AssetDatabase.LoadAssetAtPath<Material>(ArtRoot + "/CaptainSprite.mat");
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ArtRoot + "/CaptainSprite.mat");
            }
            renderer.sharedMaterial = material;
            visual.localScale = Vector3.one * (height / sprite.bounds.size.y);
        }

        private static GameObject MakeRig()
        {
            var root = new GameObject("CaptainRig");
            var hips = Bone("Hips",root.transform,0,1.03f);
            Art("LegRight",Bone("LegRight",hips,.21f,0),.99f,1);
            Art("LegLeft",Bone("LegLeft",hips,-.22f,0),.99f,3);
            var chest = Bone("Chest",hips,0,.26f);
            Art("Pelvis",Bone("Pelvis",chest,0,-.05f),.62f,5);
            Art("Torso",chest,.88f,7);
            Art("ArmRight",Bone("ArmRight",chest,.32f,.57f),.86f,6);
            Art("ArmLeft",Bone("ArmLeft",chest,-.36f,.57f),.86f,9);
            Art("Scarf",Bone("Scarf",chest,-.08f,.73f),.91f,0);
            Art("HeadNoNeck",Bone("Head",chest,0,.68f),1.40f,20);
            Art("Collar",Bone("Collar",chest,0,.73f),.30f,11);
            return root;
        }

        private static AnimationClip MakeClip(int mode)
        {
            var clip = new AnimationClip { name = new[] { "Breathe", "Walk", "Run" }[mode], frameRate = 60 };
            float duration = new[] {2.4f,.85f,.52f}[mode];
            float leg = new[] {1.2f,24f,39f}[mode], arm = new[] {2f,24f,42f}[mode];
            Curve(clip,"Hips","m_LocalPosition.y",duration,t=>1.03f + (mode==0 ? .015f*Mathf.Sin(t) : (mode==1?.045f:.11f)*(1-Mathf.Cos(2*t))));
            Curve(clip,"Hips/Chest","localEulerAnglesRaw.z",duration,t=>(mode==2?-7f:0f)+ (mode==0? .5f:2f)*Mathf.Sin(t));
            Curve(clip,"Hips/Chest","m_LocalScale.y",duration,t=>1 + (mode==0?.018f:.008f)*Mathf.Sin(t));
            Curve(clip,"Hips/LegLeft","localEulerAnglesRaw.z",duration,t=>leg*Mathf.Sin(t));
            Curve(clip,"Hips/LegRight","localEulerAnglesRaw.z",duration,t=>-leg*Mathf.Sin(t));
            Curve(clip,"Hips/Chest/ArmLeft","localEulerAnglesRaw.z",duration,t=>-arm*Mathf.Sin(t));
            Curve(clip,"Hips/Chest/ArmRight","localEulerAnglesRaw.z",duration,t=>arm*Mathf.Sin(t));
            Curve(clip,"Hips/Chest/Head","localEulerAnglesRaw.z",duration,t=> (mode==2?4:0) - (mode==0?1.2f:3f)*Mathf.Sin(t+.25f));
            Curve(clip,"Hips/Chest/Scarf","localEulerAnglesRaw.z",duration,t=>(mode==2?-18f:mode==1?-8f:0) + (mode==0?4:mode==1?10:16)*Mathf.Sin(t-.65f));
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip,settings);
            return clip;
        }

        private static void Curve(AnimationClip clip,string path,string property,float duration,Func<float,float> sample)
        {
            var curve = new AnimationCurve();
            const int count = 32;
            for (int i=0;i<=count;i++)
            {
                float phase = i * Mathf.PI * 2 / count;
                float derivative = (sample(phase+.001f)-sample(phase-.001f))/.002f * (2*Mathf.PI/duration);
                curve.AddKey(new Keyframe(i*duration/count,sample(phase),derivative,derivative));
            }
            clip.SetCurve(path,typeof(Transform),property,curve);
        }
    }
}
