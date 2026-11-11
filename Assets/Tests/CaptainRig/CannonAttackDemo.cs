using UnityEngine;

namespace SkyTree
{
    public sealed class CannonAttackDemo : MonoBehaviour
    {
        public CaptainAttackAnimation captain;
        public FemaleNavigatorAnimation navigator;
        public Transform[] cannons;
        public Camera previewCamera;
        private Vector3[] rest;
        private float[] ages = {100,100};
        private float next=1, speed=1;
        private bool paused;
        private void Start()
        {
            rest=new[]{cannons[0].localPosition,cannons[1].localPosition};
            captain.onCannonFire.AddListener(()=>ages[0]=0);
            navigator.onCannonFire.AddListener(()=>ages[1]=0);
        }
        public void AttackBoth() { captain.Attack(); navigator.Attack(); next=2.5f; }
        private void Update()
        {
            captain.GetComponent<Animator>().speed=paused?0:speed; navigator.GetComponent<Animator>().speed=paused?0:speed;
            if(paused) return;
            float dt=Time.deltaTime*speed; next-=dt;
            if(next<=0) AttackBoth();
            for(int i=0;i<2;i++) { ages[i]+=dt; cannons[i].localPosition=rest[i]+Vector3.left*(.13f*Mathf.Sin(Mathf.Clamp01(ages[i]/.3f)*Mathf.PI)); }
        }
        private void LateUpdate() => previewCamera.orthographicSize=Mathf.Max(2.7f,4.8f/previewCamera.aspect);
        private void OnGUI()
        {
            float s=Mathf.Max(.1f,Mathf.Min(Screen.width/1200f,Screen.height/720f)); GUI.matrix=Matrix4x4.Scale(Vector3.one*s);
            float w=Screen.width/s,h=Screen.height/s;
            var style=new GUIStyle(GUI.skin.label){fontSize=23,alignment=TextAnchor.MiddleCenter};
            GUI.Label(new Rect(0,20,w,40),"LONG + NAVIGATOR / CANNON ATTACK",style);
            GUI.Label(new Rect(0,58,w,32),"Grip  /  Fire  /  Recoil  /  Recover",style);
            if(GUI.Button(new Rect(w/2-210,h-65,120,34),"Attack both")) AttackBoth();
            if(GUI.Button(new Rect(w/2-80,h-65,100,34),paused?"Resume":"Pause")) paused=!paused;
            GUI.Label(new Rect(w/2+40,h-65,110,30),"Speed "+speed.ToString("0.00"));
            speed=GUI.HorizontalSlider(new Rect(w/2+150,h-53,170,20),speed,.2f,2);
        }
    }
}
