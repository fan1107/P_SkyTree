using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SkyTree
{
    public static class FemaleNavigatorRigBuilder
    {
        public const string Root = "Assets/Tests/FemaleNavigatorRig";
        public const string ArtRoot = "Assets/ArtResources/Coworker/FemaleNavigator";
        public const string Prefab = "Assets/Prefabs/Coworker/FemaleNavigator.prefab";
        public static readonly string[] States = { "Breathe", "Walk", "Run", "Attack" };
        static readonly string[] Parts = { "Head", "Torso", "Pelvis", "Ponytail", "ArmLeft", "ArmRight", "Collar", "Scarf", "LegLeft", "LegRight", "AttackArmLeft", "AttackArmRight" };
        static readonly Vector2[] Pivots = {
            new Vector2(.58f,.04f), new Vector2(.5f,.13f), new Vector2(.5f,.90f), new Vector2(.86f,.76f),
            new Vector2(.67f,.93f), new Vector2(.34f,.93f), new Vector2(.5f,.72f), new Vector2(.09f,.64f),
            new Vector2(.51f,.94f), new Vector2(.47f,.94f), new Vector2(.16f,.84f), new Vector2(.16f,.84f) };
        const string H = "Hips", C = "Hips/Chest";
        static Material material;

        [MenuItem("Tools/Female Navigator/Build Rig And Preview")]
        public static void Build() => EditorApplication.delayCall += BuildNow;

        public static void BuildNow()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            Directory.CreateDirectory(Root + "/Animations");
            Directory.CreateDirectory(ArtRoot + "/Parts");
            Slice();
            material = AssetDatabase.LoadAssetAtPath<Material>("Assets/ArtResources/Coworker/CaptainSprite.mat");
            if (!material) throw new InvalidOperationException("Captain sprite material missing.");
            var clips = new AnimationClip[4];
            for (int i = 0; i < 4; i++) clips[i] = SaveClip(MakeClip(i));
            var controller = Controller(clips);
            var rig = MakeRig();
            rig.AddComponent<Animator>().runtimeAnimatorController = controller;
            rig.GetComponent<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
            rig.AddComponent<FemaleNavigatorAnimation>();
            PrefabUtility.SaveAsPrefabAsset(rig, Prefab);
            UnityEngine.Object.DestroyImmediate(rig);
            string scenePath = Root + "/FemaleNavigatorRigTest.unity";
            var existing = SceneManager.GetSceneByPath(scenePath);
            if (existing.IsValid() && existing.isLoaded)
            {
                if (existing.isDirty) throw new InvalidOperationException("Preview has unsaved edits; save before rebuilding.");
                EditorSceneManager.CloseScene(existing, true);
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var camera = new GameObject("Navigator Preview Camera").AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 3.6f;
            camera.transform.position = new Vector3(.4f, 1.8f, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.065f,.09f,.13f);
            camera.cullingMask = 1 << 30;
            var light = new GameObject("Preview Light").AddComponent<Light>(); light.type = LightType.Directional;
            var demo = new GameObject("Navigator Preview Controls").AddComponent<FemaleNavigatorRigDemo>();
            demo.previewCamera = camera; demo.characters = new Animator[4];
            for (int i = 0; i < 4; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), scene);
                go.name = "Navigator_" + States[i]; go.transform.position = new Vector3((i - 1.5f) * 3.25f,0,0);
                foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
                demo.characters[i] = go.GetComponent<Animator>();
                clips[i].SampleAnimation(go, i == 3 ? .65f : 0);
            }
            demo.cannon = Cannon(demo.characters[3].transform.position + new Vector3(1.12f,1.05f,0));
            EditorSceneManager.SaveScene(scene, scenePath); AssetDatabase.SaveAssets();
            Selection.activeGameObject = demo.characters[0].gameObject;
            Debug.Log("Female Navigator: 12 parts, prefab, controller and four animation clips built.");
        }

        static void Slice()
        {
            string path = ArtRoot + "/FemaleNavigatorPartsAtlas.png";
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.isReadable = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096; importer.SaveAndReimport();
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            for (int i = 0; i < Parts.Length; i++)
            {
                int x = i % 4 * atlas.width / 4, top = i / 4 * atlas.height / 3;
                int w = atlas.width / 4, h = atlas.height / 3, y = atlas.height - top - h;
                var pixels = atlas.GetPixels(x,y,w,h);
                // Ignore disconnected flecks from adjacent atlas cells when computing the crop.
                var seen=new bool[pixels.Length]; var queue=new int[pixels.Length];
                var largest=new System.Collections.Generic.List<int>();
                for(int seed=0;seed<pixels.Length;seed++)
                {
                    if(seen[seed] || pixels[seed].a<=.05f) continue;
                    int read=0,write=1; queue[0]=seed; seen[seed]=true;
                    while(read<write)
                    {
                        int p=queue[read++],px=p%w,py=p/w;
                        for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++)
                        {
                            int nx=px+dx,ny=py+dy;
                            if(nx<0||nx>=w||ny<0||ny>=h) continue;
                            int n=ny*w+nx;
                            if(seen[n]||pixels[n].a<=.05f) continue;
                            seen[n]=true; queue[write++]=n;
                        }
                    }
                    if(write>largest.Count) { largest.Clear(); for(int k=0;k<write;k++) largest.Add(queue[k]); }
                }
                int minX=w,minY=h,maxX=-1,maxY=-1;
                var keep=new bool[pixels.Length];
                foreach(int p in largest)
                {
                    int px=p%w,py=p/w; minX=Math.Min(minX,px);minY=Math.Min(minY,py);maxX=Math.Max(maxX,px);maxY=Math.Max(maxY,py);
                    for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++) if(px+dx>=0&&px+dx<w&&py+dy>=0&&py+dy<h) keep[(py+dy)*w+px+dx]=true;
                }
                if (maxX < minX) throw new Exception("Empty part " + Parts[i]);
                var texture = new Texture2D(maxX-minX+1,maxY-minY+1,TextureFormat.RGBA32,false);
                var cropped=new Color[texture.width*texture.height];
                for(int py=0;py<texture.height;py++) for(int px=0;px<texture.width;px++) { int p=(py+minY)*w+px+minX; cropped[py*texture.width+px]=keep[p]?pixels[p]:Color.clear; }
                texture.SetPixels(cropped); texture.Apply();
                string partPath = ArtRoot + "/Parts/" + Parts[i] + ".png";
                File.WriteAllBytes(partPath, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(partPath);
                var part = (TextureImporter)AssetImporter.GetAtPath(partPath);
                part.textureType=TextureImporterType.Sprite; part.spriteImportMode=SpriteImportMode.Single;
                part.spritePixelsPerUnit=100; part.alphaIsTransparency=true; part.mipmapEnabled=false;
                part.textureCompression=TextureImporterCompression.Uncompressed;
                var settings=new TextureImporterSettings(); part.ReadTextureSettings(settings);
                settings.spriteAlignment=(int)SpriteAlignment.Custom; settings.spritePivot=Pivots[i];
                part.SetTextureSettings(settings); part.SaveAndReimport();
            }
            importer.isReadable=false; importer.SaveAndReimport();
        }

        static Transform Bone(string name, Transform parent, float x, float y)
        {
            var t=new GameObject(name).transform; t.SetParent(parent,false); t.localPosition=new Vector3(x,y,0); return t;
        }
        static void Art(string name, Transform bone, float height, int order)
        {
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot+"/Parts/"+name+".png");
            if (!sprite) throw new Exception("Missing sprite " + name);
            var art=Bone("Art",bone,0,0); var sr=art.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite=sprite; sr.sortingOrder=order; sr.sharedMaterial=material;
            art.localScale=Vector3.one*(height/sprite.bounds.size.y);
        }
        static GameObject MakeRig()
        {
            var root=new GameObject("FemaleNavigator");
            root.AddComponent<UnityEngine.Rendering.SortingGroup>();
            var hips=Bone("Hips",root.transform,0,1.22f);
            Art("LegRight",Bone("LegRight",hips,.20f,0),1.3f,1);
            Art("LegLeft",Bone("LegLeft",hips,-.22f,0),1.3f,3);
            Art("Pelvis",Bone("Pelvis",hips,0,.23f),.67f,5);
            var chest=Bone("Chest",hips,0,.34f); Art("Torso",chest,.90f,7);
            Art("ArmRight",Bone("ArmRight",chest,.31f,.57f),1.10f,6);
            Art("ArmLeft",Bone("ArmLeft",chest,-.35f,.57f),1.10f,9);
            Art("AttackArmRight",Bone("AttackArmRight",chest,.31f,.57f),.66f,6);
            Art("AttackArmLeft",Bone("AttackArmLeft",chest,-.35f,.57f),.66f,12);
            chest.Find("AttackArmRight").gameObject.SetActive(false); chest.Find("AttackArmLeft").gameObject.SetActive(false);
            var head=Bone("Head",chest,.02f,.73f); Art("Head",head,1.46f,20);
            Art("Ponytail",Bone("Ponytail",head,-.48f,.96f),1.18f,0);
            Art("Collar",Bone("Collar",chest,.03f,.77f),.34f,22);
            Art("Scarf",Bone("Scarf",chest,.24f,.75f),.50f,4);
            Bone("CannonGripNear",chest,.45f,.15f); Bone("CannonGripFar",chest,1.05f,.15f);
            return root;
        }
        static Transform Cannon(Vector3 position)
        {
            var t = new GameObject("Attack preview cannon").transform; t.position=position; t.gameObject.layer=30;
            var sr=t.gameObject.AddComponent<SpriteRenderer>(); sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtResources/Cannons/CannonStoutTransparent.png");
            sr.sharedMaterial=material; sr.sortingOrder=0;
            t.localScale=Vector3.one*(2.1f/sr.sprite.bounds.size.y); return t;
        }
        static AnimationClip SaveClip(AnimationClip clip)
        {
            string path=Root+"/Animations/"+clip.name+".anim";
            var asset=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (!asset) { AssetDatabase.CreateAsset(clip,path); return clip; }
            EditorUtility.CopySerialized(clip,asset); UnityEngine.Object.DestroyImmediate(clip); return asset;
        }
        static AnimatorController Controller(AnimationClip[] clips)
        {
            string path=Root+"/Animations/FemaleNavigator.controller";
            var ac=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (!ac) ac=AnimatorController.CreateAnimatorControllerAtPath(path);
            ac.parameters=new AnimatorControllerParameter[0]; ac.AddParameter("Speed",AnimatorControllerParameterType.Float); ac.AddParameter("Attack",AnimatorControllerParameterType.Trigger);
            var sm=ac.layers[0].stateMachine;
            foreach(var tr in sm.anyStateTransitions) sm.RemoveAnyStateTransition(tr);
            foreach(var s in sm.states) sm.RemoveState(s.state);
            var states=new AnimatorState[4];
            for(int i=0;i<4;i++) { states[i]=sm.AddState(States[i],new Vector3(250,i*80)); states[i].motion=clips[i]; states[i].writeDefaultValues=false; }
            sm.defaultState=states[0];
            for(int from=0;from<3;from++) for(int to=0;to<3;to++) if(from!=to) MovementTransition(states[from],states[to],to,false);
            var attack=sm.AddAnyStateTransition(states[3]); attack.hasExitTime=false; attack.duration=.08f; attack.canTransitionToSelf=false; attack.AddCondition(AnimatorConditionMode.If,0,"Attack");
            for(int i=0;i<3;i++) MovementTransition(states[3],states[i],i,true);
            EditorUtility.SetDirty(ac); return ac;
        }
        static void MovementTransition(AnimatorState from, AnimatorState to, int mode, bool exit)
        {
            var t=from.AddTransition(to); t.hasExitTime=exit; t.exitTime=1; t.duration=.12f;
            if(mode==0) t.AddCondition(AnimatorConditionMode.Less,.5f,"Speed");
            else if(mode==1) { t.AddCondition(AnimatorConditionMode.Greater,.499f,"Speed"); t.AddCondition(AnimatorConditionMode.Less,1.5f,"Speed"); }
            else t.AddCondition(AnimatorConditionMode.Greater,1.499f,"Speed");
        }
        static AnimationClip MakeClip(int mode)
        {
            var clip=new AnimationClip { name=States[mode], frameRate=60 };
            float d=new[]{2.4f,.85f,.52f,1.8f}[mode];
            if(mode<3)
            {
                float leg=new[]{.6f,22f,36f}[mode],arm=new[]{1.6f,22f,38f}[mode];
                Curve(clip,H,"m_LocalPosition.x",d,t=>0);
                Curve(clip,H,"m_LocalPosition.y",d,t=>1.22f+(mode==0?.012f*Mathf.Sin(t):(mode==1?.035f:.075f)*(1-Mathf.Cos(2*t))));
                Curve(clip,C,"localEulerAnglesRaw.z",d,t=>(mode==2?-6:0)+(mode==0?.5f:1.5f)*Mathf.Sin(t));
                Curve(clip,C,"m_LocalScale.y",d,t=>1+(mode==0?.015f:.006f)*Mathf.Sin(t));
                Curve(clip,H+"/LegLeft","localEulerAnglesRaw.z",d,t=>leg*Mathf.Sin(t));
                Curve(clip,H+"/LegRight","localEulerAnglesRaw.z",d,t=>-leg*Mathf.Sin(t));
                Curve(clip,C+"/ArmLeft","localEulerAnglesRaw.z",d,t=>-arm*Mathf.Sin(t));
                Curve(clip,C+"/ArmRight","localEulerAnglesRaw.z",d,t=>arm*Mathf.Sin(t));
                Curve(clip,C+"/Head","localEulerAnglesRaw.z",d,t=>(mode==2?4:0)-(mode==0?1:2)*Mathf.Sin(t+.25f));
                Curve(clip,C+"/Head/Ponytail","localEulerAnglesRaw.z",d,t=>(mode==2?-12:0)+(mode==0?3:mode==1?8:14)*Mathf.Sin(t-.7f));
                Curve(clip,C+"/Scarf","localEulerAnglesRaw.z",d,t=>(mode==2?14:0)+(mode==0?3:mode==1?8:13)*Mathf.Sin(t-.5f));
                Curve(clip,C+"/AttackArmLeft","localEulerAnglesRaw.z",d,t=>0); Curve(clip,C+"/AttackArmRight","localEulerAnglesRaw.z",d,t=>0);
            }
            else
            {
                Key(clip,H,"m_LocalPosition.x",0,0,.28f,.03f,.65f,.03f,.76f,-.10f,1.05f,-.02f,1.4f,0,1.8f,0);
                Key(clip,H,"m_LocalPosition.y",0,1.22f,.3f,1.16f,.65f,1.16f,.76f,1.12f,1.05f,1.16f,1.4f,1.22f,1.8f,1.22f);
                Key(clip,C,"localEulerAnglesRaw.z",0,0,.3f,-7,.65f,-7,.76f,3,1.05f,-5,1.4f,0,1.8f,0);
                Key(clip,C,"m_LocalScale.y",0,1,.65f,1,.76f,.97f,1.05f,1,1.8f,1);
                Key(clip,H+"/LegLeft","localEulerAnglesRaw.z",0,0,.3f,-10,1.1f,-10,1.4f,0,1.8f,0);
                Key(clip,H+"/LegRight","localEulerAnglesRaw.z",0,0,.3f,12,1.1f,12,1.4f,0,1.8f,0);
                Key(clip,C+"/ArmLeft","localEulerAnglesRaw.z",0,0,.22f,32,1.25f,32,1.5f,0,1.8f,0);
                Key(clip,C+"/ArmRight","localEulerAnglesRaw.z",0,0,.22f,32,1.25f,32,1.5f,0,1.8f,0);
                foreach(string side in new[]{"Left","Right"}) Key(clip,C+"/AttackArm"+side,"localEulerAnglesRaw.z",0,0,.3f,6,.65f,6,.76f,-4,1.05f,6,1.3f,0,1.8f,0);
                Key(clip,C+"/Head","localEulerAnglesRaw.z",0,0,.3f,4,.65f,4,.78f,-3,1.08f,4,1.4f,0,1.8f,0);
                Key(clip,C+"/Head/Ponytail","localEulerAnglesRaw.z",0,0,.4f,-8,.65f,-8,.82f,14,1.1f,-10,1.45f,3,1.8f,0);
                Key(clip,C+"/Scarf","localEulerAnglesRaw.z",0,0,.4f,8,.65f,8,.82f,-18,1.1f,12,1.45f,-3,1.8f,0);
                AnimationUtility.SetAnimationEvents(clip,new[]{new AnimationEvent { time=.70f,functionName="OnCannonFire" }});
            }
            foreach(string side in new[]{"Left","Right"})
            {
                Active(clip,C+"/Arm"+side,d,mode==3,false);
                Active(clip,C+"/AttackArm"+side,d,mode==3,true);
            }
            var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=mode<3; AnimationUtility.SetAnimationClipSettings(clip,settings); return clip;
        }
        static void Active(AnimationClip clip,string path,float d,bool attack,bool alternate)
        {
            float normal=alternate?0:1;
            var curve=attack?new AnimationCurve(new Keyframe(0,normal),new Keyframe(.22f,1-normal),new Keyframe(1.28f,normal),new Keyframe(d,normal)):AnimationCurve.Constant(0,d,normal);
            for(int i=0;i<curve.length;i++) { AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Constant); AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Constant); }
            clip.SetCurve(path,typeof(GameObject),"m_IsActive",curve);
        }
        static void Key(AnimationClip clip,string path,string property,params float[] pairs)
        {
            var curve=new AnimationCurve(); for(int i=0;i<pairs.Length;i+=2) curve.AddKey(new Keyframe(pairs[i],pairs[i+1]));
            clip.SetCurve(path,typeof(Transform),property,curve);
        }
        static void Curve(AnimationClip clip,string path,string property,float d,Func<float,float> sample)
        {
            var curve=new AnimationCurve();
            for(int i=0;i<=32;i++) { float t=i*Mathf.PI*2/32; float v=(sample(t+.001f)-sample(t-.001f))/.002f*(2*Mathf.PI/d); curve.AddKey(new Keyframe(i*d/32,sample(t),v,v)); }
            clip.SetCurve(path,typeof(Transform),property,curve);
        }
    }
}
