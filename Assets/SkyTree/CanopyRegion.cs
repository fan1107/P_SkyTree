using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
namespace SkyTree {
 // Clip the UI image to a persistent canopy region on every canvas rebuild.
 [ExecuteAlways, RequireComponent(typeof(Image))]
 public sealed class CanopyRegion : UnityEngine.UI.BaseMeshEffect {
  public Sprite source;
  [Range(0,2)] public int region;
  private static Vector2 P(float x,float y){return new Vector2(x/1136f,1-y/1376f);}
  protected override void OnEnable(){base.OnEnable();Apply();}
#if UNITY_EDITOR
  protected override void OnValidate(){base.OnValidate();Apply();}
#endif
  private void Apply(){if(source)GetComponent<Image>().sprite=source;if(graphic)graphic.SetVerticesDirty();}
  public override void ModifyMesh(VertexHelper vh){
   if(!IsActive()||!source)return;
   var all=new[]{
    new[]{P(0,0),P(1136,0),P(1136,420),P(850,420),P(745,485),P(650,600),P(565,650),P(0,650)},
    new[]{P(0,650),P(565,650),P(565,1376),P(0,1376)},
    new[]{P(565,650),P(650,600),P(745,485),P(850,420),P(1136,420),P(1136,1376),P(565,1376)}
   };
   var p=all[Mathf.Clamp(region,0,2)];var rect=((RectTransform)transform).rect;var uv=UnityEngine.Sprites.DataUtility.GetOuterUV(source);
   vh.Clear();foreach(var point in p){var v=UIVertex.simpleVert;v.color=graphic.color;v.position=new Vector3(rect.xMin+point.x*rect.width,rect.yMin+point.y*rect.height,0);v.uv0=new Vector2(Mathf.Lerp(uv.x,uv.z,point.x),Mathf.Lerp(uv.y,uv.w,point.y));vh.AddVert(v);}
   var triangles=Triangulate(p);for(int i=0;i<triangles.Length;i+=3)vh.AddTriangle(triangles[i],triangles[i+1],triangles[i+2]);
  }  private static float Cross(Vector2 a,Vector2 b,Vector2 c){return (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);}
  private static ushort[] Triangulate(Vector2[] p){
   var ids=new List<int>();float area=0;for(int i=0;i<p.Length;i++){ids.Add(i);area+=p[i].x*p[(i+1)%p.Length].y-p[(i+1)%p.Length].x*p[i].y;}if(area<0)ids.Reverse();
   var result=new List<ushort>();int guard=100;
   while(ids.Count>2 && guard-->0){bool found=false;for(int i=0;i<ids.Count;i++){
    int a=ids[(i+ids.Count-1)%ids.Count],b=ids[i],c=ids[(i+1)%ids.Count];if(Cross(p[a],p[b],p[c])<=0)continue;
    bool inside=false;foreach(int j in ids){if(j==a||j==b||j==c)continue;if(Cross(p[a],p[b],p[j])>=0&&Cross(p[b],p[c],p[j])>=0&&Cross(p[c],p[a],p[j])>=0){inside=true;break;}}
    if(inside)continue;result.Add((ushort)a);result.Add((ushort)b);result.Add((ushort)c);ids.RemoveAt(i);found=true;break;
   }if(!found)throw new System.InvalidOperationException("Invalid canopy polygon");}return result.ToArray();
  }
 }
}


