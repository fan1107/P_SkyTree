using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    public static class SkybirdAnimationBuilder
    {
        public const string PrefabPath = "Assets/Prefabs/Enemy/Skybird.prefab";
        public const string AnimationRoot = "Assets/Animations/Enemy/Skybird";
        public const string OriginalPath = "Assets/ArtResources/Enemy/SkybirdTopDown.png";
        public const string UpPath = "Assets/ArtResources/Enemy/SkybirdTopDown/Idle_01_WingsUp.png";
        public const string DownPath = "Assets/ArtResources/Enemy/SkybirdTopDown/Idle_03_WingsDown.png";
        public const string VerificationRoot = "Verification/SkybirdAnimation";
        public const float FrameRate = 8f;

        [MenuItem("Tools/Skybird/Build Wing Animation Prefab")]
        public static void BuildMenu() { Debug.Log(BuildNow()); }

        public static string BuildNow()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Build in Edit mode.");
            var up = ImportFrame(UpPath);
            var spread = AssetDatabase.LoadAssetAtPath<Texture2D>(OriginalPath);
            var down = ImportFrame(DownPath);
            if (!spread) throw new InvalidOperationException("Missing original SkybirdTopDown.");
            Directory.CreateDirectory(AnimationRoot);
            AssetDatabase.Refresh();

            // Object-reference curves include one full frame after their final key.
            var idle = MakeClip("Idle_Flap", new[] { up, spread, down, spread });
            var attack = MakeClip("Attack_Glide", new[] { spread });
            var controller = MakeController(idle, attack);
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var image = root.GetComponent<RawImage>();
                if (!image) throw new InvalidOperationException("Skybird must retain its RawImage root.");
                image.texture = spread;
                image.uvRect = new Rect(0, 0, 1, 1);
                image.raycastTarget = false;
                image.rectTransform.sizeDelta = new Vector2(46, 46);
                var animator = root.GetComponent<Animator>();
                if (!animator) animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (!root.GetComponent<SkybirdAnimation>()) root.AddComponent<SkybirdAnimation>();
                bool saved;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out saved);
                if (!saved) throw new InvalidOperationException("Could not save Skybird prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssetIfDirty(idle);
            AssetDatabase.SaveAssetIfDirty(attack);
            AssetDatabase.SaveAssetIfDirty(controller);
            return "Saved " + PrefabPath + "; Idle: up/spread/down/spread at 8 fps, Attack: original spread wings.";
        }

        private static Texture2D ImportFrame(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Missing wing frame", path);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static AnimationClip MakeClip(string name, Texture2D[] frames)
        {
            var clip = new AnimationClip { name = name, frameRate = FrameRate };
            var keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / FrameRate, value = frames[i] };
            var binding = EditorCurveBinding.PPtrCurve("", typeof(RawImage), "m_Texture");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            string path = AnimationRoot + "/" + name + ".anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing)
            {
                EditorUtility.CopySerialized(clip, existing);
                UnityEngine.Object.DestroyImmediate(clip);
                return existing;
            }
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static AnimatorController MakeController(AnimationClip idle, AnimationClip attack)
        {
            string path = AnimationRoot + "/Skybird.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
            foreach (var child in machine.states) machine.RemoveState(child.state);
            controller.parameters = new AnimatorControllerParameter[0];
            controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
            var idleState = machine.AddState("Idle_Flap", new Vector3(250, 0));
            idleState.motion = idle; idleState.writeDefaultValues = false;
            var attackState = machine.AddState("Attack_Glide", new Vector3(500, 0));
            attackState.motion = attack; attackState.writeDefaultValues = false;
            machine.defaultState = idleState;
            Transition(idleState, attackState, AnimatorConditionMode.If);
            Transition(attackState, idleState, AnimatorConditionMode.IfNot);
            EditorUtility.SetDirty(machine);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode condition)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0;
            transition.hasFixedDuration = true;
            transition.AddCondition(condition, 0, "IsAttacking");
        }

        public static string VerifyAssets()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationRoot + "/Idle_Flap.anim");
            var attack = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationRoot + "/Attack_Glide.anim");
            var original = AssetDatabase.LoadAssetAtPath<Texture2D>(OriginalPath);
            var up = AssetDatabase.LoadAssetAtPath<Texture2D>(UpPath);
            var down = AssetDatabase.LoadAssetAtPath<Texture2D>(DownPath);
            if (!prefab || !idle || !attack || !original || !up || !down) throw new Exception("Missing asset.");
            if (!idle.isLooping || !Mathf.Approximately(idle.length, .5f)) throw new Exception("Invalid idle timing.");
            if (prefab.GetComponent<RawImage>().texture != original) throw new Exception("Prefab must show original spread wings.");
            if (!prefab.GetComponent<SkybirdAnimation>()) throw new Exception("Missing animation bridge.");
            if (prefab.GetComponent<RectTransform>().sizeDelta != new Vector2(46,46)) throw new Exception("Distorted frame aspect.");
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab) != 0) throw new Exception("Missing script.");
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var image = instance.GetComponent<RawImage>();
                Texture[] expected = { up, original, down, original };
                for (int i = 0; i < expected.Length; i++)
                {
                    idle.SampleAnimation(instance, i / FrameRate + .001f);
                    if (image.texture != expected[i]) throw new Exception("Wrong idle frame " + i);
                    if (instance.transform.localScale != Vector3.one) throw new Exception("Idle squashes the artwork.");
                }
                for (int i = 0; i < 5; i++)
                {
                    attack.SampleAnimation(instance, i * .03f);
                    if (image.texture != original) throw new Exception("Attack does not hold original wings.");
                }
                var animator = instance.GetComponent<Animator>();
                animator.Rebind(); animator.Update(0);
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle_Flap")) throw new Exception("Invalid initial state.");
                animator.SetBool("IsAttacking", true); animator.Update(.02f); animator.Update(.02f);
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack_Glide") || image.texture != original)
                    throw new Exception("Idle to Attack transition failed.");
                animator.SetBool("IsAttacking", false); animator.Update(.02f); animator.Update(.02f);
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle_Flap")) throw new Exception("Attack to Idle transition failed.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            string report = "PASS: three distinct idle textures; 0.5-second up/spread/down/spread loop; original attack texture; both immediate Animator transitions; square prefab; no scale animation or missing scripts.";
            Directory.CreateDirectory(VerificationRoot);
            File.WriteAllText(VerificationRoot + "/AssetResults.txt", report);
            return report;
        }

        public static string RenderPreview()
        {
            Directory.CreateDirectory(VerificationRoot);
            var scene = EditorSceneManager.NewPreviewScene();
            var previousTarget = RenderTexture.active;
            RenderTexture target = null;
            Texture2D capture = null;
            try
            {
                var cameraObject = new GameObject("Skybird preview camera", typeof(Camera));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = scene;
                camera.orthographic = true;
                camera.orthographicSize = 300;
                camera.transform.position = new Vector3(0, 0, -1000);
                camera.farClipPlane = 2000;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.075f, .105f, .16f, 1);
                camera.allowHDR = false; camera.allowMSAA = false;
                var canvasObject = new GameObject("Wing poses", typeof(RectTransform), typeof(Canvas));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1536, 600);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationRoot + "/Idle_Flap.anim");
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                string[] labels = { "01 / WINGS UP", "02 / SPREAD + ATTACK", "03 / WINGS DOWN" };
                for (int i = 0; i < 3; i++)
                {
                    var bird = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    bird.transform.SetParent(canvas.transform, false);
                    bird.GetComponent<Animator>().enabled = false;
                    var rect = bird.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(480,480);
                    rect.anchoredPosition = new Vector2((i-1)*512, 15);
                    idle.SampleAnimation(bird, i/FrameRate + .001f);
                    var labelObject = new GameObject(labels[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                    labelObject.transform.SetParent(canvas.transform, false);
                    var label = labelObject.GetComponent<Text>();
                    label.font = font; label.fontSize = 20;
                    label.alignment = TextAnchor.MiddleCenter;
                    label.color = new Color(.83f,.9f,1,1);
                    label.text = labels[i];
                    label.rectTransform.sizeDelta = new Vector2(500,50);
                    label.rectTransform.anchoredPosition = new Vector2((i-1)*512,-260);
                }
                Canvas.ForceUpdateCanvases();
                target = new RenderTexture(1536,600,24,RenderTextureFormat.ARGB32);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                capture = new Texture2D(1536,600,TextureFormat.RGBA32,false);
                capture.ReadPixels(new Rect(0,0,1536,600),0,0); capture.Apply();
                string path = VerificationRoot + "/ThreeFrames.png";
                File.WriteAllBytes(path,capture.EncodeToPNG());
                camera.targetTexture = null;
                return Path.GetFullPath(path);
            }
            finally
            {
                RenderTexture.active = previousTarget;
                if (capture) UnityEngine.Object.DestroyImmediate(capture);
                if (target) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
