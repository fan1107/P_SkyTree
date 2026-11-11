using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    // Lightweight vector overlays stay sharp at every portrait resolution.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RouteMapGraphic : MaskableGraphic
    {
        public enum Shape { Grid, Point, Selection, Route }
        public Shape shape;
        public Vector2 from, to;
        public void SetRoute(Vector2 start, Vector2 end) { from = start; to = end; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            if (shape == Shape.Grid)
            {
                for (int n = 1; n < 3; n++)
                {
                    float x = r.xMin + r.width * n / 3f, y = r.yMin + r.height * n / 3f;
                    Dashed(vh, new Vector2(x, r.yMin), new Vector2(x, r.yMax), .65f);
                    Dashed(vh, new Vector2(r.xMin, y), new Vector2(r.xMax, y), .65f);
                }
            }
            else if (shape == Shape.Route)
            {
                var dir = (to - from).normalized;
                var end = to - dir * 10;
                Line(vh, from, end, 2.4f, new Color32(247, 219, 128, 255));
                Line(vh, from, end, 1.2f, color);
                var side = new Vector2(-dir.y, dir.x) * 4;
                Triangle(vh, end, end - dir * 9 + side, end - dir * 9 - side, color);
            }
            else if (shape == Shape.Selection)
            {
                Ring(vh, 11, 9.5f, new Color32(169, 118, 26, 255));
                Ring(vh, 13, 12, new Color32(255, 236, 174, 255));
            }
            else
            {
                Ring(vh, 6.7f, 0, new Color32(255, 241, 192, 255));
                Ring(vh, 5.6f, 0, new Color32(145, 107, 38, 255));
                Ring(vh, 4.5f, 0, color);
            }
        }
        void Dashed(VertexHelper vh, Vector2 a, Vector2 b, float width)
        {
            float len = Vector2.Distance(a,b); var dir=(b-a).normalized;
            for(float t=0;t<len;t+=5) Line(vh,a+dir*t,a+dir*Mathf.Min(t+3,len),width,color);
        }
        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            var d=(b-a).normalized; var n=new Vector2(-d.y,d.x)*width*.5f;
            int i=vh.currentVertCount;
            vh.AddVert(a-n,tint,Vector2.zero); vh.AddVert(a+n,tint,Vector2.zero);
            vh.AddVert(b+n,tint,Vector2.zero); vh.AddVert(b-n,tint,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
        }
        static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color tint)
        {
            int i=vh.currentVertCount;vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);vh.AddVert(c,tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);
        }
        static void Ring(VertexHelper vh,float outer,float inner,Color tint)
        {
            for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48;
                var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                int n=vh.currentVertCount;
                vh.AddVert(u*outer,tint,Vector2.zero);vh.AddVert(v*outer,tint,Vector2.zero);
                vh.AddVert(v*inner,tint,Vector2.zero);vh.AddVert(u*inner,tint,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
