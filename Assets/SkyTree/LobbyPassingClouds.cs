using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    // Created between the ocean and the lobby prefab: clouds never cover the ship or HUD.
    public sealed class LobbyPassingClouds : MonoBehaviour
    {
        private RectTransform[] clouds;
        private float[] speeds;

        public void Build()
        {
            if(clouds != null) return;
            var texture = Resources.Load<Texture2D>("SkyTree/PassingCloud");
            if(!texture) return;
            var layer = new GameObject("Clouds below ship", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            layer.SetParent(transform,false);
            layer.anchorMin=Vector2.zero; layer.anchorMax=Vector2.one;
            layer.offsetMin=layer.offsetMax=Vector2.zero;
            clouds=new RectTransform[4];
            speeds=new[] {5f,8f,6.5f,10f};
            float[] x={-130,160,-280,300}, y={140,-100,-270,270}, widths={290,350,270,320};
            for(int i=0;i<clouds.Length;i++)
            {
                var rect=new GameObject("Drifting cloud "+i,typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(layer,false);
                rect.sizeDelta=new Vector2(widths[i],widths[i]*texture.height/texture.width);
                rect.anchoredPosition=new Vector2(x[i],y[i]);
                var image=rect.gameObject.AddComponent<RawImage>();
                image.texture=texture;
                image.color=new Color(1,1,1,i%2==0?.42f:.55f);
                image.raycastTarget=false;
                clouds[i]=rect;
            }
        }

        private void OnEnable()
        {
            // Rebuild transient references if Unity reloads scripts during a preview.
            if(clouds==null && transform.Find("Clouds below ship"))
            {
                var old=transform.Find("Clouds below ship");
                old.gameObject.SetActive(false);
                Destroy(old.gameObject);
            }
            Build();
        }

        private void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if(clouds==null)return;
            for(int i=0;i<clouds.Length;i++)
            {
                var p=clouds[i].anchoredPosition;
                p.x+=speeds[i]*deltaTime;
                float edge=200+clouds[i].sizeDelta.x*.5f+8;
                if(p.x>edge)p.x=-edge+(p.x-edge)%(2*edge);
                clouds[i].anchoredPosition=p;
            }
        }
    }
}
