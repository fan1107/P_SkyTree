using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SkyTree
{
    public static class FemaleNavigatorRigVerify
    {
        [MenuItem("Tools/Female Navigator/Verify And Capture")]
        public static void Run()
        {
            var demo=UnityEngine.Object.FindFirstObjectByType<FemaleNavigatorRigDemo>();
            if (!demo) throw new Exception("Open the navigator preview scene first.");
            var report=new StringBuilder();
            for(int i=0;i<4;i++)
            {
                var rig=demo.characters[i].gameObject;
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(FemaleNavigatorRigBuilder.Root+"/Animations/"+FemaleNavigatorRigBuilder.States[i]+".anim");
                if(rig.GetComponentsInChildren<SpriteRenderer>(true).Length!=12) throw new Exception("Expected 12 sprite parts.");
                foreach(var sr in rig.GetComponentsInChildren<SpriteRenderer>(true)) if(!sr.sprite) throw new Exception("Missing sprite");
                foreach(var b in AnimationUtility.GetCurveBindings(clip)) if(!rig.transform.Find(b.path)) throw new Exception("Missing binding "+b.path);
                if(clip.isLooping!=(i<3)) throw new Exception("Incorrect loop setting");
                clip.SampleAnimation(rig,0);
                var hips=rig.transform.Find("Hips"); float start=hips.localPosition.y;
                clip.SampleAnimation(rig,clip.length*.25f);
                if(Mathf.Abs(hips.localPosition.y-start)<.005f) throw new Exception("No hip motion");
                if(i<3)
                {
                    foreach(var b in AnimationUtility.GetCurveBindings(clip))
                    {
                        var curve=AnimationUtility.GetEditorCurve(clip,b);
                        if(Mathf.Abs(curve.Evaluate(0)-curve.Evaluate(clip.length))>.001f) throw new Exception("Loop seam "+b.path);
                    }
                }
                else
                {
                    clip.SampleAnimation(rig,.65f);
                    if(!rig.transform.Find("Hips/Chest/AttackArmLeft").gameObject.activeSelf || rig.transform.Find("Hips/Chest/ArmLeft").gameObject.activeSelf) throw new Exception("Grip swap failed");
                    clip.SampleAnimation(rig,clip.length);
                    if(rig.transform.Find("Hips/Chest/AttackArmLeft").gameObject.activeSelf || !rig.transform.Find("Hips/Chest/ArmLeft").gameObject.activeSelf) throw new Exception("Grip reset failed");
                    var events=AnimationUtility.GetAnimationEvents(clip);
                    if(events.Length!=1 || events[0].functionName!="OnCannonFire" || Mathf.Abs(events[0].time-.7f)>.001f) throw new Exception("Fire event invalid");
                }
                report.AppendLine(clip.name+": sprites, bindings, motion, loop/attack semantics PASS");
            }
            Directory.CreateDirectory("Verification/FemaleNavigator");
            for(int frame=0;frame<4;frame++)
            {
                for(int i=0;i<4;i++)
                {
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(FemaleNavigatorRigBuilder.Root+"/Animations/"+FemaleNavigatorRigBuilder.States[i]+".anim");
                    clip.SampleAnimation(demo.characters[i].gameObject,i==3?new[]{.35f,.65f,.78f,1.5f}[frame]:clip.length*(frame+.5f)/4);
                }
                Capture(demo.previewCamera,"Verification/FemaleNavigator/Pose"+frame+".png");
            }
            File.WriteAllText("Verification/FemaleNavigator/results.txt",report.ToString());
            Debug.Log(report.ToString());
        }
        public static void Capture(Camera camera,string path)
        {
            var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active;
            var rt=new RenderTexture(1600,800,24); var texture=new Texture2D(1600,800,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
                texture.ReadPixels(new Rect(0,0,1600,800),0,0); texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG());
            }
            finally { camera.targetTexture=oldTarget; RenderTexture.active=oldActive; UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(rt); }
        }
    }
}
