using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SkyTree.CaptainPrototype
{
    public static class CaptainRigVerify
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene(CaptainRigBuilder.Root + "/CaptainRigTest.unity");
            var demo = UnityEngine.Object.FindFirstObjectByType<CaptainRigDemo>();
            string[] names = { "Breathe", "Walk", "Run" };
            var report = new System.Text.StringBuilder();
            for (int i=0;i<3;i++)
            {
                var rig = demo.characters[i];
                if(rig.GetComponentsInChildren<SpriteRenderer>(true).Length != 11) throw new Exception("Expected nine normal parts plus two attack arms");
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CaptainRigBuilder.Root + "/Animations/" + names[i] + ".anim");
                if(!clip.isLooping) throw new Exception("Clip must loop");
                foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                    if(rig.transform.Find(binding.path)==null) throw new Exception("Missing bone: "+binding.path);
                clip.SampleAnimation(rig.gameObject,0);
                var leg = rig.transform.Find("Hips/LegLeft");
                float start = leg.localEulerAngles.z;
                clip.SampleAnimation(rig.gameObject,clip.length*.25f);
                if(Mathf.Abs(Mathf.DeltaAngle(start,leg.localEulerAngles.z)) < .5f) throw new Exception("No leg motion");
                report.AppendLine(names[i]+": nine sprites, bone bindings, looping and sampled motion PASS");
            }
            Directory.CreateDirectory("Verification");
            for(int frame=0;frame<4;frame++)
            {
                for(int i=0;i<3;i++)
                {
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(CaptainRigBuilder.Root+"/Animations/"+names[i]+".anim");
                    clip.SampleAnimation(demo.characters[i].gameObject,clip.length*(frame+.5f)/4);
                }
                var camera=demo.previewCamera;
                var rt=new RenderTexture(1280,720,24);
                camera.targetTexture=rt;
                camera.Render();
                RenderTexture.active=rt;
                var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
                File.WriteAllBytes("Verification/Pose"+frame+".png",image.EncodeToPNG());
                camera.targetTexture=null; RenderTexture.active=null;
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
            }
            File.WriteAllText("Verification/results.txt",report.ToString());
            Debug.Log(report.ToString());
        }
    }
}
