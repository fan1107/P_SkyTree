using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree.Editor
{
    // Re-run safely after adjusting the shared art or layout.
    public static class LobbyUiRenewal
    {
        const string Folder = "Assets/SkyTree/Prefabs/";
        static Sprite[] sprites;
        static readonly Color Cream = new Color32(255, 243, 208, 255);
        static Sprite Art(string name) => sprites.First(s => s.name == name);
        static RectTransform Rect(Transform t, float x, float y, float w, float h)
        {
            var r = (RectTransform)t;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
            r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h);
            return r;
        }
        static Image Skin(Transform t, string sprite, float ppu = 5)
        {
            var im = t.GetComponent<Image>() ?? t.gameObject.AddComponent<Image>();
            im.sprite = Art(sprite); im.color = Color.white; im.type = Image.Type.Sliced;
            im.pixelsPerUnitMultiplier = ppu; im.raycastTarget = false;
            return im;
        }
        static GameObject Child(Transform parent, string name)
        {
            var old = parent.Find(name);
            if (old) return old.gameObject;
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go;
        }
        static void Style(Text t, int size, Color color)
        {
            t.fontSize=size; t.fontStyle=FontStyle.Bold; t.color=color; t.alignment=TextAnchor.MiddleCenter;
            t.raycastTarget=false; t.resizeTextForBestFit=true; t.resizeTextMinSize=10; t.resizeTextMaxSize=size;
        }
        static void Plate(Transform parent, string name, float x,float y,float w,float h)
        {
            var go=Child(parent,name); Rect(go.transform,x,y,w,h); Skin(go.transform,"MainTabCommon");
        }
        [MenuItem("SkyTree/Apply Lobby UI Renewal")]
        public static void Apply()
        {
            sprites=AssetDatabase.LoadAllAssetsAtPath("Assets/ArtResources/Atlas/Lobby/LobbyRenewal.png").OfType<Sprite>().ToArray();
            if(sprites.Length!=4) throw new System.InvalidOperationException("Expected four sliced UI sprites.");
            var common=new GameObject("MainTabCommon",typeof(RectTransform));
            Rect(common.transform,0,0,86,34); Skin(common.transform,"MainTabCommon");
            PrefabUtility.SaveAsPrefabAsset(common,Folder+"MainTabCommon.prefab"); Object.DestroyImmediate(common);
            var hud=PrefabUtility.LoadPrefabContents(Folder+"LobbyCurrencyHud.prefab");
            try
            {
                string[] names={"Energy","Gold","Gem"};
                for(int i=0;i<3;i++)
                {
                    var cell=hud.transform.Find(names[i]); Rect(cell,(i-1)*96,0,92,29); Skin(cell,"CurrencyBar",7);
                    var icon=cell.GetComponentsInChildren<Image>().First(im=>im.transform!=cell);
                    Rect(icon.transform,-33,1,22,25); icon.raycastTarget=false;
                    var value=cell.GetComponentInChildren<Text>(); Rect(value.transform,-1,0,43,25); Style(value,16,Cream);
                    var plus=Child(cell,"Add currency"); Rect(plus.transform,33,0,21,23); var im=Skin(plus.transform,"MainTabCommon",12); im.raycastTarget=true;
                    var b=plus.GetComponent<Button>()??plus.AddComponent<Button>(); b.targetGraphic=im;
                    var label=Child(plus.transform,"Plus"); Rect(label.transform,0,1,20,20);
                    var text=label.GetComponent<Text>()??label.AddComponent<Text>(); text.font=value.font;text.text="+";Style(text,16,Cream);
                }
                PrefabUtility.SaveAsPrefabAsset(hud,Folder+"LobbyCurrencyHud.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(hud); }
            var root=PrefabUtility.LoadPrefabContents(Folder+"LobbyView.prefab");
            try
            {
                Rect(root.transform.Find("LobbyCurrencyHud"),48,330,288,36);
                var badge=root.transform.Find("Captain badge"); Rect(badge,-150,310,74,74);
                var portrait=Skin(badge,"LongPortrait"); portrait.type=Image.Type.Simple; portrait.preserveAspect=true;
                Plate(badge,"Nameplate",0,-40,85,24);
                var name=badge.GetComponentInChildren<Text>(); name.transform.SetAsLastSibling(); Rect(name.transform,0,-40,78,23); Style(name,13,Cream);
                var world=root.transform.Find("WORLD   01"); Rect(world,31,302,150,20); var wt=world.GetComponent<Text>(); wt.text="WORLD 01";Style(wt,12,new Color32(57,65,54,255));
                var location=root.transform.Find("바람초원 정박지");
                var ribbon=Child(root.transform,"Location ribbon");Rect(ribbon.transform,31,264,224,46);Skin(ribbon.transform,"LocationRibbon",6); ribbon.transform.SetAsFirstSibling();
                Rect(location,31,264,180,32);Style(location.GetComponent<Text>(),19,new Color32(83,53,28,255));
                var caption=root.transform.Find("Adventure/Map caption");Rect(caption,0,-26,140,35);Skin(caption,"MainTabCommon");
                var adventureText=caption.GetComponentInChildren<Text>();Rect(adventureText.transform,0,1,125,28);Style(adventureText,21,Cream);
                var tabs=root.transform.Find("Lobby tabs"); var panel=tabs.GetComponent<Image>();if(panel) panel.enabled=false;
                foreach(var button in tabs.GetComponentsInChildren<Button>(true))
                {
                    var r=(RectTransform)button.transform; Rect(r,r.anchoredPosition.x,-4,88,80);
                    var old=button.GetComponent<Image>();old.color=Color.clear;old.raycastTarget=true;
                    var plate=button.transform.Find("Shared plate");if(plate)Object.DestroyImmediate(plate.gameObject);
                    var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"MainTabCommon.prefab"),button.transform);
                    visual.name="Shared plate";Rect(visual.transform,0,-24,88,34);visual.transform.SetAsFirstSibling();
                    button.targetGraphic=visual.GetComponent<Image>();
                    var icon=button.GetComponentsInChildren<Image>().First(im=>im.transform!=button.transform && im.transform!=visual.transform);
                    Rect(icon.transform,0,16,62,62);icon.raycastTarget=false;
                    var text=button.GetComponentInChildren<Text>();Rect(text.transform,0,-23,80,26);Style(text,14,Cream);
                    var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.95f,.8f);colors.pressedColor=new Color(.68f,.78f,.78f);button.colors=colors;
                }
                Skin(root.transform.Find("Menu details"),"MainTabCommon",4);
                Skin(root.transform.Find("Menu details/Close"),"MainTabCommon",8).raycastTarget=true;
                PrefabUtility.SaveAsPrefabAsset(root,Folder+"LobbyView.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }
    }
}
