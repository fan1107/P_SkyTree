using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SkyTree.EditorTools
{
    public static class FlagSpriteBuilder
    {
        public const string Folder = "Assets/ArtResources/Ships/MainShip_Step01/AnimatedFlag";
        public const string PrefabPath = "Assets/SkyTree/Prefabs/ShipFlag.prefab";

        public static void Build()
        {
            string atlasPath=Folder+"/SeparatedFlag.png";
            AssetDatabase.ImportAsset(atlasPath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(atlasPath);
            importer.isReadable=true; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=4096; importer.SaveAndReimport();
            var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            Extract(atlas,0,atlas.width/2,"Pole");
            Extract(atlas,atlas.width/2,atlas.width-atlas.width/2,"ClothSource");
            importer.isReadable=false; importer.SaveAndReimport();
            BakeFrames();
            var clip=new AnimationClip { name="FlagFlutter",frameRate=16 };
            var keys=new ObjectReferenceKeyframe[17];
            for(int i=0;i<=16;i++)keys[i]=new ObjectReferenceKeyframe {time=i/16f,value=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/Flutter_"+(i%16).ToString("00")+".png")};
            AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding {path="Cloth",type=typeof(Image),propertyName="m_Sprite"},keys);
            var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=true;
            AnimationUtility.SetAnimationClipSettings(clip,settings);
            var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/FlagFlutter.anim");
            if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;}
            else AssetDatabase.CreateAsset(clip,Folder+"/FlagFlutter.anim");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/ShipFlagFlutter.controller");
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/ShipFlagFlutter.controller");
            foreach(var previous in controller.layers[0].stateMachine.states)controller.layers[0].stateMachine.RemoveState(previous.state);
            var state=controller.layers[0].stateMachine.AddState("Flutter");state.motion=clip;
            controller.layers[0].stateMachine.defaultState=state;
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
            var root=Rect("ShipFlag",null,new Vector2(55,96),Vector2.zero);
            // Old combined image: pole x=0..60 and fabric x=45..279, y=50..211.
            AddImage("Cloth",root,keys[0].value as Sprite,new Vector2(55,41.25f),new Vector2(5,11.5f));
            AddImage("Pole",root,AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/Pole.png"),new Vector2(12,74.5f),new Vector2(-21.5f,0));
            var animator=root.gameObject.AddComponent<Animator>();animator.runtimeAnimatorController=controller;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,PrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            AssetDatabase.SaveAssets();
            ReplaceIn("Assets/SkyTree/Prefabs/LobbyView.prefab");
        }

        private static void Extract(Texture2D source,int x,int width,string name)
        {
            var pixels=source.GetPixels(x,0,width,source.height);
            int left=width,right=0,bottom=source.height,top=0;
            for(int y=0;y<source.height;y++)for(int px=0;px<width;px++)
                if(pixels[y*width+px].a>.04f){left=Math.Min(left,px);right=Math.Max(right,px);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
            var texture=new Texture2D(right-left+1,top-bottom+1,TextureFormat.RGBA32,false);
            texture.SetPixels(source.GetPixels(x+left,bottom,texture.width,texture.height));texture.Apply();
            string path=Folder+"/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture); ImportSprite(path);
        }

        private static void ImportSprite(string path)
        {
            AssetDatabase.ImportAsset(path);
            var i=(TextureImporter)AssetImporter.GetAtPath(path);
            i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;
            i.alphaIsTransparency=true;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;
            i.npotScale=TextureImporterNPOTScale.None;i.spritePixelsPerUnit=100;i.SaveAndReimport();
        }

        private static void BakeFrames()
        {
            var preview=EditorSceneManager.NewPreviewScene();
            var cameraGO=new GameObject("Flag Bake Camera");SceneManager.MoveGameObjectToScene(cameraGO,preview);
            var camera=cameraGO.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=.65f;
            camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=Color.clear;camera.allowHDR=false;camera.allowMSAA=false;
            var target=new RenderTexture(512,384,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
            var cloth=new GameObject("Animated cloth mesh");SceneManager.MoveGameObjectToScene(cloth,preview);
            var mesh=new Mesh();cloth.AddComponent<MeshFilter>().sharedMesh=mesh;
            var material=new Material(Shader.Find("Hidden/SkyTree/FlagBake"));
            var source=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/ClothSource.png");material.mainTexture=source;
            cloth.AddComponent<MeshRenderer>().sharedMaterial=material;
            const int columns=32;
            var vertices=new Vector3[(columns+1)*2];var uv=new Vector2[vertices.Length];var colors=new Color[vertices.Length];
            var triangles=new int[columns*6];
            for(int x=0;x<=columns;x++)for(int y=0;y<2;y++)uv[x*2+y]=new Vector2((float)x/columns,y);
            for(int x=0;x<columns;x++){int a=x*2,b=x*6;triangles[b]=a;triangles[b+1]=a+1;triangles[b+2]=a+2;triangles[b+3]=a+2;triangles[b+4]=a+1;triangles[b+5]=a+3;}
            float height=1.4f*source.height/source.width;
            for(int frame=0;frame<16;frame++)
            {
                float phase=frame*2*Mathf.PI/16;
                for(int x=0;x<=columns;x++)for(int y=0;y<2;y++)
                {
                    float t=(float)x/columns,wave=phase-t*Mathf.PI*2;
                    vertices[x*2+y]=new Vector3(-.7f+1.4f*t*(.96f+.035f*Mathf.Sin(phase)),(y-.5f)*height+.065f*t*Mathf.Sin(wave),0);
                    float light=1-.12f*t*(.5f+.5f*Mathf.Cos(wave));colors[x*2+y]=new Color(light,light,light,1);
                }
                mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();
                var commands=new UnityEngine.Rendering.CommandBuffer();
                commands.SetRenderTarget(target);
                commands.ClearRenderTarget(true,true,Color.clear);
                commands.DrawMesh(mesh,Matrix4x4.identity,material,0,0);
                Graphics.ExecuteCommandBuffer(commands);
                commands.Release();
                RenderTexture.active=target;
                var image=new Texture2D(512,384,TextureFormat.RGBA32,false);
                image.ReadPixels(new UnityEngine.Rect(0,0,512,384),0,0);image.Apply();
                string path=Folder+"/Flutter_"+frame.ToString("00")+".png";
                File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);ImportSprite(path);
            }
            RenderTexture.active=null;camera.targetTexture=null;
            EditorSceneManager.ClosePreviewScene(preview);
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material);
        }

        private static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.sizeDelta=size;r.anchoredPosition=position;return r;
        }
        private static void AddImage(string name,Transform parent,Sprite sprite,Vector2 size,Vector2 position)
        {
            var image=Rect(name,parent,size,position).gameObject.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;
        }
        private static void ReplaceIn(string path)
        {
            var content=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var old=content.transform.Find("MainShip_Step01/Flag") as RectTransform;
                if(!old)throw new Exception("Lobby flag not found");
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),old.parent);
                root.name="Flag";var rect=root.GetComponent<RectTransform>();
                rect.anchorMin=old.anchorMin;rect.anchorMax=old.anchorMax;rect.pivot=old.pivot;
                rect.sizeDelta=old.sizeDelta;rect.anchoredPosition3D=old.anchoredPosition3D;
                rect.localRotation=old.localRotation;rect.localScale=old.localScale;rect.SetSiblingIndex(old.GetSiblingIndex());
                UnityEngine.Object.DestroyImmediate(old.gameObject);
                PrefabUtility.SaveAsPrefabAsset(content,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(content);}
        }
    }
}

