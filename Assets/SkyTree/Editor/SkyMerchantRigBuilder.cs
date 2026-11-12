using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SkyTree
{
    /// <summary>Rebuilds only the merchant's separated artwork, prefab and idle animation.</summary>
    public static class SkyMerchantRigBuilder
    {
        public const string ArtRoot = "Assets/ArtResources/NPC/SkyMerchant";
        public const string PrefabPath = "Assets/Prefabs/NPC/SkyMerchant.prefab";
        public const string AnimationRoot = "Assets/Animations/NPC/SkyMerchant";
        public const string PreviewRoot = "Verification/SkyMerchantRig";
        const float PixelScale = 3.4f / 951f;
        const float Duration = 3.2f;
        const string ChestPath = "Pose/Chest";
        static readonly string[] Names = { "Head", "Torso", "ArmCoin", "ArmLedger", "LegLeft", "LegRight", "Scarf", "Satchel", "Hat" };
        static readonly Vector2[] Pivots = {
            new Vector2(.55f,.045f), new Vector2(.5f,.43f), new Vector2(.16f,.84f),
            new Vector2(.40f,.92f), new Vector2(.56f,.96f), new Vector2(.42f,.96f),
            new Vector2(.5f,.75f), new Vector2(.48f,.95f), new Vector2(.5f,.25f)
        };

        [MenuItem("Tools/Sky Merchant/Build Breathing Prefab")]
        public static void BuildMenu() { BuildNow(); }

        public static string BuildNow()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Build in Edit mode.");
            Directory.CreateDirectory(ArtRoot + "/Parts");
            Directory.CreateDirectory(AnimationRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh();
            string partsReport = SliceParts();
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var rig = MakeRig(scene);
                var clip = MakeClip(rig);
                string clipPath = AnimationRoot + "/Idle_Breathe.anim";
                var existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (existingClip)
                {
                    EditorUtility.CopySerialized(clip, existingClip);
                    UnityEngine.Object.DestroyImmediate(clip);
                    clip = existingClip;
                }
                else AssetDatabase.CreateAsset(clip, clipPath);
                var controller = MakeController(clip);
                var animator = rig.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
                bool saved;
                PrefabUtility.SaveAsPrefabAsset(rig, PrefabPath, out saved);
                if (!saved) throw new InvalidOperationException("Merchant prefab did not save.");
                AssetDatabase.SaveAssetIfDirty(clip);
                AssetDatabase.SaveAssetIfDirty(controller);
                return "Saved " + PrefabPath + "; one looping Idle_Breathe state, 3.2 seconds. " + partsReport;
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        static string SliceParts()
        {
            string path = ArtRoot + "/SkyMerchantPartsAtlas.png";
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            try
            {
                var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                int w = atlas.width, h = atlas.height;
                var pixels = atlas.GetPixels32();
                int transparent = 0;
                foreach (var c in pixels) if (c.a == 0) transparent++;
                if (transparent < pixels.Length / 4) throw new InvalidOperationException("Expected a transparent parts atlas.");
                var seen = new bool[pixels.Length];
                var queue = new int[pixels.Length];
                var groups = new List<int>[9];
                for (int i = 0; i < 9; i++) groups[i] = new List<int>();
                // Find whole connected pieces before assigning cells: a hat tip may cross a grid boundary.
                for (int seed = 0; seed < pixels.Length; seed++)
                {
                    if (seen[seed] || pixels[seed].a <= 10) continue;
                    int read = 0, count = 1;
                    long sumX = 0, sumY = 0;
                    seen[seed] = true; queue[0] = seed;
                    while (read < count)
                    {
                        int p = queue[read++], x = p % w, y = p / w;
                        sumX += x; sumY += y;
                        for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                            int n = ny * w + nx;
                            if (seen[n] || pixels[n].a <= 10) continue;
                            seen[n] = true; queue[count++] = n;
                        }
                    }
                    if (count < 80) continue;
                    int col = Mathf.Clamp((int)(sumX * 3L / count / w), 0, 2);
                    int row = Mathf.Clamp(2 - (int)(sumY * 3L / count / h), 0, 2);
                    for (int k = 0; k < count; k++) groups[row * 3 + col].Add(queue[k]);
                }
                var report = new System.Text.StringBuilder();
                for (int i = 0; i < 9; i++)
                {
                    if (groups[i].Count < 1000) throw new InvalidOperationException("Empty or incomplete part: " + Names[i]);
                    int minX = w, minY = h, maxX = -1, maxY = -1;
                    foreach (int p in groups[i])
                    {
                        int x = p % w, y = p / w;
                        minX = Math.Min(minX,x); minY = Math.Min(minY,y);
                        maxX = Math.Max(maxX,x); maxY = Math.Max(maxY,y);
                    }
                    minX = Math.Max(0,minX-2); minY = Math.Max(0,minY-2);
                    maxX = Math.Min(w-1,maxX+2); maxY = Math.Min(h-1,maxY+2);
                    int cw = maxX-minX+1, ch = maxY-minY+1;
                    var cropped = new Color32[cw*ch];
                    for (int y = 0; y < ch; y++) Array.Copy(pixels, (minY+y)*w+minX, cropped, y*cw, cw);
                    var texture = new Texture2D(cw,ch,TextureFormat.RGBA32,false);
                    try
                    {
                        texture.SetPixels32(cropped); texture.Apply();
                        string partPath = ArtRoot + "/Parts/" + Names[i] + ".png";
                        File.WriteAllBytes(partPath,texture.EncodeToPNG());
                        AssetDatabase.ImportAsset(partPath);
                        var part = (TextureImporter)AssetImporter.GetAtPath(partPath);
                        part.textureType = TextureImporterType.Sprite;
                        part.spriteImportMode = SpriteImportMode.Single;
                        part.spritePixelsPerUnit = 100;
                        part.alphaIsTransparency = true; part.mipmapEnabled = false;
                        part.npotScale = TextureImporterNPOTScale.None;
                        part.textureCompression = TextureImporterCompression.Uncompressed;
                        var settings = new TextureImporterSettings(); part.ReadTextureSettings(settings);
                        settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = Pivots[i];
                        part.SetTextureSettings(settings); part.SaveAndReimport();
                    }
                    finally { UnityEngine.Object.DestroyImmediate(texture); }
                    report.Append(Names[i]).Append(' ').Append(cw).Append('x').Append(ch).Append("; ");
                }
                return report.ToString();
            }
            finally { importer.isReadable = false; importer.SaveAndReimport(); }
        }

        static Vector3 Point(float x, float y) { return new Vector3((x-384)*PixelScale,(991-y)*PixelScale,0); }
        static Transform Bone(string name, Transform parent, float x, float y)
        {
            var bone = new GameObject(name).transform;
            bone.SetParent(parent,false); bone.position = Point(x,y); return bone;
        }
        static Transform Part(string name, Transform parent, float x, float y, float height, int order)
        {
            var bone = Bone(name,parent,x,y);
            var art = new GameObject("Art").transform; art.SetParent(bone,false);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/Parts/" + name + ".png");
            if (!sprite) throw new InvalidOperationException("Missing " + name);
            var renderer = art.gameObject.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/ArtResources/Coworker/CaptainSprite.mat");
            if (!renderer.sharedMaterial) throw new InvalidOperationException("Shared character material is missing.");
            renderer.sortingOrder = order;
            art.localScale = Vector3.one * (height*PixelScale/sprite.bounds.size.y);
            return bone;
        }
        static GameObject MakeRig(Scene scene)
        {
            var root = new GameObject("SkyMerchant"); SceneManager.MoveGameObjectToScene(root,scene);
            root.AddComponent<SortingGroup>();
            var pose = new GameObject("Pose").transform; pose.SetParent(root.transform,false);
            Part("LegRight",pose,439,677,308,0);
            Part("LegLeft",pose,333,691,310,1);
            var chest = Bone("Chest",pose,378,638);
            Part("Torso",chest,378,638,464,3);
            Part("ArmCoin",chest,476,431,172,4);
            Part("Satchel",chest,222,652,130,6);
            Part("ArmLedger",chest,254,430,254,7);
            var head = Part("Head",chest,379,400,306,8);
            Part("Scarf",chest,377,423,149,10);
            Part("Hat",head,360,219,234,11);
            return root;
        }
        static AnimationClip MakeClip(GameObject rig)
        {
            var clip = new AnimationClip { name="Idle_Breathe", frameRate=60 };
            float chestY = rig.transform.Find(ChestPath).localPosition.y;
            var chestPosition = rig.transform.Find(ChestPath).localPosition;
            var torsoScale = rig.transform.Find(ChestPath+"/Torso/Art").localScale;
            float torsoY = torsoScale.y;
            // Unity groups vector channels; supply constants so unanimated axes retain the sprite's authored scale.
            Curve(clip,ChestPath,"m_LocalPosition.x",t=>chestPosition.x);
            Curve(clip,ChestPath,"m_LocalPosition.z",t=>chestPosition.z);
            Curve(clip,ChestPath,"m_LocalPosition.y",t=>chestY+.009f*(1-Mathf.Cos(t)));
            Curve(clip,ChestPath+"/Torso/Art","m_LocalScale.x",t=>torsoScale.x);
            Curve(clip,ChestPath+"/Torso/Art","m_LocalScale.y",t=>torsoY*(1+.006f*(1-Mathf.Cos(t))));
            Curve(clip,ChestPath+"/Torso/Art","m_LocalScale.z",t=>torsoScale.z);
            Curve(clip,ChestPath+"/Head","localEulerAnglesRaw.z",t=>-.4f*Mathf.Sin(t));
            Curve(clip,ChestPath+"/ArmCoin","localEulerAnglesRaw.z",t=>.45f*Mathf.Sin(t));
            Curve(clip,ChestPath+"/ArmLedger","localEulerAnglesRaw.z",t=>-.3f*Mathf.Sin(t));
            Curve(clip,ChestPath+"/Scarf","localEulerAnglesRaw.z",t=>.5f*(Mathf.Sin(t-.4f)-Mathf.Sin(-.4f)));
            Curve(clip,ChestPath+"/Satchel","localEulerAnglesRaw.z",t=>.25f*(Mathf.Sin(t-.6f)-Mathf.Sin(-.6f)));
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true; settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip,settings);
            return clip;
        }
        static void Curve(AnimationClip clip, string path, string property, Func<float,float> sample)
        {
            var curve = new AnimationCurve();
            for (int i = 0; i <= 48; i++)
            {
                float phase = i*Mathf.PI*2/48;
                float tangent = (sample(phase+.001f)-sample(phase-.001f))/.002f*(2*Mathf.PI/Duration);
                curve.AddKey(new Keyframe(i*Duration/48,sample(phase),tangent,tangent));
            }
            clip.SetCurve(path,typeof(Transform),property,curve);
        }
        static AnimatorController MakeController(AnimationClip clip)
        {
            string path = AnimationRoot + "/SkyMerchant.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
            foreach (var child in machine.states) machine.RemoveState(child.state);
            controller.parameters = new AnimatorControllerParameter[0];
            var state = machine.AddState("Idle_Breathe"); state.motion = clip; state.writeDefaultValues = false;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(machine);
            return controller;
        }

        public static string RenderAndVerify(bool frames)
        {
            Directory.CreateDirectory(PreviewRoot);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationRoot + "/Idle_Breathe.anim");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimationRoot + "/SkyMerchant.controller");
            if (!prefab || !clip || !clip.isLooping || controller.layers[0].stateMachine.states.Length != 1)
                throw new InvalidOperationException("Expected a prefab with a single looping idle state.");
            var scene = EditorSceneManager.NewPreviewScene();
            var oldActive = RenderTexture.active;
            RenderTexture target = null;
            Texture2D capture = null;
            try
            {
                var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                if (rig.GetComponentsInChildren<SpriteRenderer>().Length != 9) throw new Exception("Expected nine separated parts.");
                foreach (var t in rig.GetComponentsInChildren<Transform>())
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0) throw new Exception("Missing script.");
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (!rig.transform.Find(binding.path)) throw new Exception("Missing animated bone: "+binding.path);
                    var curve = AnimationUtility.GetEditorCurve(clip,binding);
                    if (Mathf.Abs(curve.Evaluate(0)-curve.Evaluate(Duration)) > .00001f) throw new Exception("Idle loop discontinuity.");
                }
                var leg = rig.transform.Find("Pose/LegLeft");
                Vector3 footStart = leg.position;
                var chest = rig.transform.Find(ChestPath);
                var torso = rig.transform.Find(ChestPath+"/Torso/Art");
                float authoredWidth = torso.localScale.x;
                clip.SampleAnimation(rig,0); float restY = chest.position.y;
                clip.SampleAnimation(rig,Duration/2);
                if (chest.position.y-restY < .01f) throw new Exception("No visible breathing motion.");
                if (Mathf.Abs(torso.localScale.x-authoredWidth) > .00001f) throw new Exception("Animation changed the torso width.");
                if ((leg.position-footStart).sqrMagnitude > .00000001f) throw new Exception("Feet slide during breathing.");
                var animator = rig.GetComponent<Animator>();
                animator.Rebind(); animator.Update(0);
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle_Breathe")) throw new Exception("Default idle did not start.");
                animator.enabled = false;
                var cameraGo = new GameObject("Merchant preview camera",typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraGo,scene);
                var camera = cameraGo.GetComponent<Camera>(); camera.scene = scene;
                camera.orthographic = true; camera.orthographicSize = 1.95f;
                camera.transform.position = new Vector3(.03f,1.7f,-10);
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.11f,.16f,.21f,1);
                camera.allowHDR = false; camera.allowMSAA = false;
                var light = new GameObject("Merchant preview light",typeof(Light));
                SceneManager.MoveGameObjectToScene(light,scene); light.GetComponent<Light>().type = LightType.Directional;
                target = new RenderTexture(640,896,24,RenderTextureFormat.ARGB32); camera.targetTexture = target;
                capture = new Texture2D(640,896,TextureFormat.RGBA32,false);
                int count = frames ? 48 : 4;
                if (frames) Directory.CreateDirectory(PreviewRoot+"/Frames");
                for (int i = 0; i < count; i++)
                {
                    clip.SampleAnimation(rig,Duration*i/count);
                    camera.Render(); RenderTexture.active = target;
                    capture.ReadPixels(new Rect(0,0,640,896),0,0); capture.Apply();
                    string file = frames ? "/Frames/frame"+i.ToString("D3")+".png" : "/Pose"+i+".png";
                    File.WriteAllBytes(PreviewRoot+file,capture.EncodeToPNG());
                }
                camera.targetTexture = null;
                string report = "PASS: nine transparent sprite parts; no missing scripts; one default Idle_Breathe state; 3.2-second seamless loop; chest motion sampled; planted feet; rendered " + count + " frames.";
                File.WriteAllText(PreviewRoot+"/results.txt",report);
                return report;
            }
            finally
            {
                RenderTexture.active = oldActive;
                if (capture) UnityEngine.Object.DestroyImmediate(capture);
                if (target) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
