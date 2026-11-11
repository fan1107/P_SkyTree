using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree.Editor
{
    public static class RouteSelectionBuilder
    {
        const string ArtFolder="Assets/ArtResources/Atlas/RouteMap/";
        const string PrefabPath="Assets/SkyTree/Prefabs/RouteSelectionPopup.prefab";
        static readonly Color Ink=new Color32(74,62,40,255);
        static readonly Color Cream=new Color32(255,242,212,255);
        static Font font;
        static Sprite plate;
        static RectTransform Node(string name,Transform parent,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false);r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x,y);return r;
        }
        static Image Image(RectTransform r,Color color,Sprite sprite=null)
        {
            var im=r.gameObject.AddComponent<Image>();im.color=color;im.sprite=sprite;im.raycastTarget=false;
            if(sprite){im.type=UnityEngine.UI.Image.Type.Sliced;im.pixelsPerUnitMultiplier=5;}
            return im;
        }
        static Text Text(string name,Transform parent,float x,float y,float w,float h,int size,string value,Color color)
        {
            var t=Node(name,parent,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;
            t.fontStyle=FontStyle.Bold;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        static Button Button(string name,Transform parent,float x,float y,float w,float h,string label,int fontSize)
        {
            var r=Node(name,parent,x,y,w,h);var im=Image(r,Color.white,plate);im.raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;
            var colors=b.colors;colors.disabledColor=new Color(.5f,.5f,.5f,.75f);colors.pressedColor=new Color(.65f,.75f,.72f);b.colors=colors;
            Text("Label",r,0,0,w-12,h-4,fontSize,label,Cream);return b;
        }
        static RouteMapGraphic Graphic(string name,Transform parent,RouteMapGraphic.Shape shape,float w,float h,Color color)
        {
            var g=Node(name,parent,0,0,w,h).gameObject.AddComponent<RouteMapGraphic>();g.shape=shape;g.color=color;g.raycastTarget=false;return g;
        }
        static void Ref(SerializedObject so,string field,Object value)=>so.FindProperty(field).objectReferenceValue=value;

        [MenuItem("SkyTree/Build Route Selection Popup")]
        public static void Build()
        {
            foreach(var file in new[]{"CloudChart.png","TreeShipMarker.png"})
            {
                string path=ArtFolder+file;AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            plate=AssetDatabase.LoadAllAssetsAtPath("Assets/ArtResources/Atlas/Lobby/LobbyRenewal.png").OfType<Sprite>().First(s=>s.name=="MainTabCommon");
            font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",32);
            var root=Node("RouteSelectionPopup",null,0,0,400,720);
            var popup=root.gameObject.AddComponent<RouteSelectionPopup>();
            var so=new SerializedObject(popup);
            var blocker=Image(Node("Modal backdrop",root,0,0,400,720),new Color(0.05f,.09f,.1f,.48f));blocker.raycastTarget=true;
            var panel=Node("Popup panel",root,0,0,352,636);Image(panel,Color.white,plate).raycastTarget=true;
            Image(Node("Parchment",panel,0,0,339,621),new Color32(227,209,169,255));
            var map=Node("Map",panel,0,43,328,398);
            Image(map,Color.white,AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder+"CloudChart.png")).type=UnityEngine.UI.Image.Type.Simple;
            Graphic("Region dividers",map,RouteMapGraphic.Shape.Grid,328,398,new Color32(106,86,52,150));
            var line=Graphic("Selected route",map,RouteMapGraphic.Shape.Route,328,398,new Color32(155,112,28,255));Ref(so,"route",line);
            var points=so.FindProperty("points");points.arraySize=9;
            for(int i=0;i<9;i++)
            {
                var r=Node("Waypoint_"+(i+1),map,(i%3-1)*328f/3,(1-i/3)*398f/3,44,44);
                var g=r.gameObject.AddComponent<RouteMapGraphic>();g.shape=RouteMapGraphic.Shape.Point;g.color=new Color32(255,207,70,255);g.raycastTarget=true;
                var b=r.gameObject.AddComponent<Button>();b.targetGraphic=g;var colors=b.colors;colors.pressedColor=new Color(.75f,.65f,.2f);b.colors=colors;
                points.GetArrayElementAtIndex(i).objectReferenceValue=b;
            }
            var selection=Graphic("Destination ring",map,RouteMapGraphic.Shape.Selection,30,30,Color.white);Ref(so,"selection",selection.rectTransform);
            var ship=Node("Current ship",map,0,0,70,56);var shipImage=Image(ship,Color.white,AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder+"TreeShipMarker.png"));
            shipImage.type=UnityEngine.UI.Image.Type.Simple;shipImage.preserveAspect=true;Ref(so,"ship",ship);
            var current=Text("Current position",map,0,0,110,22,12,"현재 위치",Ink);Ref(so,"currentLabel",current.rectTransform);
            var destination=Text("Destination label",map,0,0,108,24,12,"",Ink);Ref(so,"destinationLabel",destination.rectTransform);
            Image(Node("Header",panel,0,280,338,55),Color.white,plate);
            Text("Title",panel,-10,280,270,42,25,"항로 선택",Cream);
            Ref(so,"close",Button("Close",panel,143,281,34,36,"×",25));
            Image(Node("Footer separator",panel,0,-161,328,1),new Color32(158,130,82,255));
            Ref(so,"title",Text("Destination title",panel,0,-181,318,28,19,"목적지를 선택하세요",Ink));
            Ref(so,"summary",Text("Battle summary",panel,0,-209,320,24,13,"노란 지점을 눌러 항로를 정하세요",Ink));
            Ref(so,"arrival",Text("Arrival hint",panel,0,-232,324,24,11,"가로 · 세로 · 대각선으로 이동할 수 있습니다",Ink));
            var depart=Button("Depart",panel,0,-272,292,43,"출항하기",23);depart.interactable=false;Ref(so,"depart",depart);
            Text("Failure hint",panel,0,-304,316,17,9,"실패 시 마지막 통과 지점으로 복귀",Ink);
            string[] names={"고대 유적","무역선 정박지","동쪽 구름 항로","서쪽 구름 항로","구름 쉼터","수정섬","남서쪽 구름 항로","시작 정박지","풍차섬"};
            var info=so.FindProperty("destinations");info.arraySize=9;
            for(int i=0;i<9;i++)
            {
                var item=info.GetArrayElementAtIndex(i);item.FindPropertyRelative("title").stringValue=names[i];
                item.FindPropertyRelative("arrivalHint").stringValue=i==1?"도착 시 무역선 이용 가능":"도착 후 이곳에 정박합니다";
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            line.gameObject.SetActive(false);selection.gameObject.SetActive(false);destination.gameObject.SetActive(false);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,PrefabPath);Object.DestroyImmediate(root.gameObject);
            foreach(var game in Object.FindObjectsByType<SkyTreeGame>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var gameSo=new SerializedObject(game);Ref(gameSo,"routePopupPrefab",prefab.GetComponent<RouteSelectionPopup>());gameSo.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
                EditorSceneManager.SaveScene(game.gameObject.scene);
            }
            string atlasPath=ArtFolder+"RouteMap.spriteatlas";
            var atlas=AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(atlasPath);
            if(!atlas){atlas=new UnityEngine.U2D.SpriteAtlas();AssetDatabase.CreateAsset(atlas,atlasPath);}
            UnityEditor.U2D.SpriteAtlasExtensions.Add(atlas,new Object[]{AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder+"CloudChart.png"),AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder+"TreeShipMarker.png")});
            var packing=UnityEditor.U2D.SpriteAtlasExtensions.GetPackingSettings(atlas);packing.enableRotation=false;packing.enableTightPacking=false;UnityEditor.U2D.SpriteAtlasExtensions.SetPackingSettings(atlas,packing);
            AssetDatabase.SaveAssets();
        }
    }
}
