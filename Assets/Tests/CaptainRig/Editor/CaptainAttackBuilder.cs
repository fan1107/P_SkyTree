using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SkyTree
{
    public static class CaptainAttackBuilder
    {
        const string Root="Assets/Tests/CaptainRig";
        const string Art="Assets/ArtResources/Coworker/CaptainAttack";
        const string Prefab="Assets/Prefabs/Coworker/Captain.prefab";
        public const string ScenePath=Root+"/CannonAttackTest.unity";
        [MenuItem("Tools/Captain Rig/Add Cannon Attack")]
        public static void Build()
        {
            Slice();
            var source=AssetDatabase.LoadAssetAtPath<AnimationClip>(FemaleNavigatorRigBuilder.Root+"/Animations/Attack.anim");
            var clip=UnityEngine.Object.Instantiate(source); clip.name="Attack";
            foreach(var b in AnimationUtility.GetCurveBindings(clip))
            {
                if(b.path.Contains("Ponytail")) { AnimationUtility.SetEditorCurve(clip,b,null); continue; }
                if(b.path=="Hips" && b.propertyName=="m_LocalPosition.y")
                {
                    var curve=AnimationUtility.GetEditorCurve(clip,b); var keys=curve.keys;
                    for(int i=0;i<keys.Length;i++) keys[i].value-=.19f;
                    curve.keys=keys; AnimationUtility.SetEditorCurve(clip,b,curve);
                }
            }
            string clipPath=Root+"/Animations/Attack.anim";
            var asset=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if(asset) { EditorUtility.CopySerialized(clip,asset); UnityEngine.Object.DestroyImmediate(clip); clip=asset; }
            else AssetDatabase.CreateAsset(clip,clipPath);
            // Explicit resets make exiting Attack safe with direct Play/CrossFade callers, too.
            foreach(string state in new[]{"Breathe","Walk","Run"})
            {
                var locomotion=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Animations/"+state+".anim");
                locomotion.SetCurve("Hips",typeof(Transform),"m_LocalPosition.x",AnimationCurve.Constant(0,locomotion.length,0));
                foreach(string side in new[]{"Left","Right"})
                {
                    locomotion.SetCurve("Hips/Chest/Arm"+side,typeof(GameObject),"m_IsActive",AnimationCurve.Constant(0,locomotion.length,1));
                    locomotion.SetCurve("Hips/Chest/AttackArm"+side,typeof(GameObject),"m_IsActive",AnimationCurve.Constant(0,locomotion.length,0));
                    locomotion.SetCurve("Hips/Chest/AttackArm"+side,typeof(Transform),"localEulerAnglesRaw.z",AnimationCurve.Constant(0,locomotion.length,0));
                }
                EditorUtility.SetDirty(locomotion);
            }
            var ac=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"/Animations/Captain.controller");
            bool parameter=false; foreach(var p in ac.parameters) if(p.name=="Attack") parameter=true;
            if(!parameter) ac.AddParameter("Attack",AnimatorControllerParameterType.Trigger);
            var sm=ac.layers[0].stateMachine; AnimatorState attack=null,breathe=null;
            foreach(var s in sm.states) { if(s.state.name=="Attack") attack=s.state; if(s.state.name=="Breathe") breathe=s.state; }
            if(!attack) attack=sm.AddState("Attack",new Vector3(240,240));
            attack.motion=clip;
            foreach(var t in sm.anyStateTransitions) if(t.destinationState==attack) sm.RemoveAnyStateTransition(t);
            foreach(var t in attack.transitions) attack.RemoveTransition(t);
            var enter=sm.AddAnyStateTransition(attack); enter.hasExitTime=false; enter.duration=.08f; enter.canTransitionToSelf=false; enter.AddCondition(AnimatorConditionMode.If,0,"Attack");
            var exit=attack.AddTransition(breathe); exit.hasExitTime=true; exit.exitTime=1; exit.duration=.12f;
            EditorUtility.SetDirty(ac);
            var rig=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var chest=rig.transform.Find("Hips/Chest");
                for(int i=0;i<2;i++)
                {
                    string name="AttackArm"+(i==0?"Left":"Right");
                    var bone=chest.Find(name); if(!bone) { bone=new GameObject(name).transform; bone.SetParent(chest,false); }
                    bone.localPosition=chest.Find(i==0?"ArmLeft":"ArmRight").localPosition;
                    var visual=bone.Find("Art"); if(!visual) { visual=new GameObject("Art").transform; visual.SetParent(bone,false); }
                    var sr=visual.GetComponent<SpriteRenderer>(); if(!sr) sr=visual.gameObject.AddComponent<SpriteRenderer>();
                    sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/"+name+".png"); sr.sortingOrder=i==0?12:6;
                    sr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/ArtResources/Coworker/CaptainSprite.mat");
                    visual.localScale=Vector3.one*(.63f/sr.sprite.bounds.size.y); bone.gameObject.SetActive(false);
                }
                if(!rig.GetComponent<CaptainAttackAnimation>()) rig.AddComponent<CaptainAttackAnimation>();
                if(!rig.GetComponent<UnityEngine.Rendering.SortingGroup>()) rig.AddComponent<UnityEngine.Rendering.SortingGroup>();
                PrefabUtility.SaveAsPrefabAsset(rig,Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(rig); }
            AssetDatabase.SaveAssets(); BuildPreview();
        }
        static void Slice()
        {
            string path=Art+"/AttackArmsAtlas.png"; AssetDatabase.ImportAsset(path);
            var imp=(TextureImporter)AssetImporter.GetAtPath(path); imp.isReadable=true; imp.npotScale=TextureImporterNPOTScale.None;
            imp.textureCompression=TextureImporterCompression.Uncompressed; imp.maxTextureSize=4096; imp.SaveAndReimport();
            var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            for(int i=0;i<2;i++)
            {
                int w=atlas.width/2,h=atlas.height,x=i*w; var pixels=atlas.GetPixels(x,0,w,h);
                int minX=w,minY=h,maxX=0,maxY=0;
                for(int py=0;py<h;py++) for(int px=0;px<w;px++) if(pixels[py*w+px].a>.1f) { minX=Math.Min(minX,px);minY=Math.Min(minY,py);maxX=Math.Max(maxX,px);maxY=Math.Max(maxY,py); }
                var tex=new Texture2D(maxX-minX+1,maxY-minY+1,TextureFormat.RGBA32,false);
                tex.SetPixels(atlas.GetPixels(x+minX,minY,tex.width,tex.height)); tex.Apply();
                string target=Art+"/AttackArm"+(i==0?"Left":"Right")+".png"; File.WriteAllBytes(target,tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(target); var p=(TextureImporter)AssetImporter.GetAtPath(target);
                p.textureType=TextureImporterType.Sprite; p.spriteImportMode=SpriteImportMode.Single; p.spritePixelsPerUnit=100;
                p.alphaIsTransparency=true;p.mipmapEnabled=false;p.textureCompression=TextureImporterCompression.Uncompressed;
                var settings=new TextureImporterSettings();p.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.20f,.86f);p.SetTextureSettings(settings);p.SaveAndReimport();
            }
            imp.isReadable=false;imp.SaveAndReimport();
        }
        static void BuildPreview()
        {
            var old=SceneManager.GetSceneByPath(ScenePath);
            if(old.IsValid()&&old.isLoaded) { if(old.isDirty) throw new Exception("Save preview edits before rebuilding."); EditorSceneManager.CloseScene(old,true); }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var camera=new GameObject("Attack Preview Camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=2.7f;
            camera.transform.position=new Vector3(.4f,1.65f,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.065f,.09f,.13f);camera.cullingMask=1<<29;
            new GameObject("Preview Light").AddComponent<Light>().type=LightType.Directional;
            var demo=new GameObject("Cannon Attack Preview").AddComponent<CannonAttackDemo>();demo.previewCamera=camera;demo.cannons=new Transform[2];
            for(int i=0;i<2;i++)
            {
                string path=i==0?Prefab:FemaleNavigatorRigBuilder.Prefab;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);go.transform.position=new Vector3(i==0?-2.2f:2.2f,0,0);
                foreach(var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=29;
                if(i==0) demo.captain=go.GetComponent<CaptainAttackAnimation>(); else demo.navigator=go.GetComponent<FemaleNavigatorAnimation>();
                var cannon=new GameObject(i==0?"Long cannon":"Navigator cannon").transform;cannon.gameObject.layer=29;cannon.position=go.transform.position+new Vector3(1.12f,i==0?.91f:1.05f,0);
                var sr=cannon.gameObject.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtResources/Cannons/CannonStoutTransparent.png");sr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/ArtResources/Coworker/CaptainSprite.mat");
                cannon.localScale=Vector3.one*((i==0?1.82f:2.1f)/sr.sprite.bounds.size.y);demo.cannons[i]=cannon;
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>((i==0?Root:FemaleNavigatorRigBuilder.Root)+"/Animations/Attack.anim");clip.SampleAnimation(go,.65f);
            }
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        }
        [MenuItem("Tools/Captain Rig/Verify Cannon Attacks")]
        public static void Verify()
        {
            var demo=UnityEngine.Object.FindFirstObjectByType<CannonAttackDemo>(); if(!demo) throw new Exception("Open attack preview first.");
            var report=new System.Text.StringBuilder();
            var rigs=new[]{demo.captain.gameObject,demo.navigator.gameObject};
            for(int i=0;i<2;i++)
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>((i==0?Root:FemaleNavigatorRigBuilder.Root)+"/Animations/Attack.anim");
                if(clip.isLooping) throw new Exception("Attack must be a one-shot");
                foreach(var b in AnimationUtility.GetCurveBindings(clip)) if(!rigs[i].transform.Find(b.path)) throw new Exception("Missing attack binding "+b.path);
                if(rigs[i].GetComponentsInChildren<SpriteRenderer>(true).Length!=(i==0?11:12)) throw new Exception("Part count mismatch");
                var events=AnimationUtility.GetAnimationEvents(clip); if(events.Length!=1||events[0].functionName!="OnCannonFire") throw new Exception("Fire event missing");
                clip.SampleAnimation(rigs[i],.65f);float hold=rigs[i].transform.Find("Hips").localPosition.x;
                if(!rigs[i].transform.Find("Hips/Chest/AttackArmLeft").gameObject.activeSelf) throw new Exception("Grip missing");
                clip.SampleAnimation(rigs[i],.76f);if(rigs[i].transform.Find("Hips").localPosition.x>=hold-.05f) throw new Exception("Recoil missing");
                clip.SampleAnimation(rigs[i],1.8f);if(rigs[i].transform.Find("Hips/Chest/AttackArmLeft").gameObject.activeSelf) throw new Exception("Arm reset missing");
                report.AppendLine((i==0?"Long":"Navigator")+": part count, all bindings, hold, recoil, recovery and fire event PASS");
            }
            Directory.CreateDirectory("Verification/CannonAttack");
            for(int f=0;f<4;f++)
            {
                for(int i=0;i<2;i++) AssetDatabase.LoadAssetAtPath<AnimationClip>((i==0?Root:FemaleNavigatorRigBuilder.Root)+"/Animations/Attack.anim").SampleAnimation(rigs[i],new[]{.35f,.65f,.76f,1.6f}[f]);
                FemaleNavigatorRigVerify.Capture(demo.previewCamera,"Verification/CannonAttack/Pose"+f+".png");
            }
            File.WriteAllText("Verification/CannonAttack/results.txt",report.ToString());Debug.Log(report.ToString());
        }
    }
}
