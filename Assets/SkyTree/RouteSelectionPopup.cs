using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    public sealed class RouteSelectionPopup : MonoBehaviour
    {
        [Serializable] public class DestinationInfo
        {
            public string title;
            public string arrivalHint;
        }
        [SerializeField] private Button[] points;
        [SerializeField] private DestinationInfo[] destinations;
        [SerializeField] private Button depart, close;
        [SerializeField] private RouteMapGraphic route;
        [SerializeField] private RectTransform selection, ship, currentLabel, destinationLabel;
        [SerializeField] private Text title, summary, arrival;
        private Action<RoutePlan> onDepart;
        private int currentPoint;
        private bool committed;
        public RoutePlan SelectedRoute { get; private set; }
        public bool CanDepart => depart.interactable;
        public int CurrentPoint => currentPoint;
        public bool IsTravelling { get; private set; }

        public void Open(Font font, int current, Action<RoutePlan> callback)
        {
            StopAllCoroutines(); IsTravelling=false;
            onDepart=callback; currentPoint=Mathf.Clamp(current,0,8); committed=false;
            foreach(var text in GetComponentsInChildren<Text>(true)) text.font=font;
            for(int i=0;i<points.Length;i++)
            {
                int index=i; points[i].onClick.RemoveAllListeners();
                points[i].onClick.AddListener(()=>SelectDestination(index));
            }
            close.onClick.RemoveAllListeners();close.onClick.AddListener(Close);
            depart.onClick.RemoveAllListeners();depart.onClick.AddListener(Depart);
            ship.anchoredPosition=Position(currentPoint)+new Vector2(0,22);
            currentLabel.anchoredPosition=Position(currentPoint)+new Vector2(0,-20);
            currentLabel.GetComponent<Text>().text="현재 위치";
            ResetSelection("목적지를 선택하세요", "노란 지점을 눌러 항로를 정하세요");
            gameObject.SetActive(true);transform.SetAsLastSibling();
        }
        private Vector2 Position(int index) => ((RectTransform)points[index].transform).anchoredPosition;
        private void ResetSelection(string heading,string message)
        {
            SelectedRoute=null; depart.interactable=false;route.gameObject.SetActive(false);
            selection.gameObject.SetActive(false);destinationLabel.gameObject.SetActive(false);
            title.text=heading;summary.text=message;arrival.text="같은 목적지를 다시 누르면 경유 항로가 바뀝니다";
        }
        public void SelectDestination(int index)
        {
            if(committed || IsTravelling || index<0 || index>=points.Length)return;
            if(index==currentPoint){ResetSelection("현재 정박 중인 곳입니다","다른 목적지를 선택하세요");return;}
            int optionIndex=SelectedRoute!=null && SelectedRoute.Destination==index
                ? (SelectedRoute.OptionIndex+1)%SelectedRoute.OptionCount : 0;
            if(!RoutePlan.TryCreate(currentPoint,index,out var plan,optionIndex))
            { ResetSelection("목적지를 선택하세요","이 지점으로 이동할 수 없습니다"); return; }
            SelectedRoute=plan; depart.interactable=true;
            var positions=new Vector2[plan.Waypoints.Count];
            for(int i=0;i<positions.Length;i++) positions[i]=Position(plan.Waypoints[i]);
            route.SetRoute(positions);route.gameObject.SetActive(true);
            selection.anchoredPosition=Position(index);selection.gameObject.SetActive(true);
            // These landmarks occupy the space above their waypoint.
            destinationLabel.anchoredPosition=Position(index)+new Vector2(0,index==2 || index==3 ? -25 : 25);
            destinationLabel.GetComponent<Text>().text=destinations[index].title;destinationLabel.gameObject.SetActive(true);
            title.text=destinations[index].title;summary.text=$"{plan.WaveCount} 웨이브 · 보스 {plan.BossCount}회";
            arrival.text=plan.OptionCount>1
                ? $"{destinations[plan.Waypoints[1]].title} 경유 · 다시 누르면 항로 변경"
                : destinations[index].arrivalHint;
        }
        public void Depart()
        {
            if(committed || IsTravelling || SelectedRoute==null || !depart.interactable)return;
            committed=true;depart.interactable=false;gameObject.SetActive(false);
            onDepart?.Invoke(SelectedRoute);
        }
        public void ShowVoyageResult(VoyageProgress progress)
        {
            SelectedRoute=null; depart.interactable=false;
            bool won=progress.Phase==VoyagePhase.Victory;
            title.text=won?"항해 완료":progress.Phase==VoyagePhase.Retreat?"항해 중단":"긴급 귀환";
            summary.text=won?$"{destinations[currentPoint].title}에 도착했습니다":$"{destinations[currentPoint].title}로 복귀했습니다";
            arrival.text=won?destinations[currentPoint].arrivalHint:$"일반 웨이브 {progress.ClearedWaves} / {progress.Route.WaveCount} 완료";
            int reached=0;
            for(int i=1;i<progress.Route.Waypoints.Count;i++)
                if(progress.Route.Waypoints[i]==currentPoint) reached=i;
            if(reached==0)return;
            var positions=new Vector2[reached+1];
            for(int i=0;i<positions.Length;i++)positions[i]=Position(progress.Route.Waypoints[i]);
            route.SetRoute(positions);route.gameObject.SetActive(true);
            selection.anchoredPosition=Position(currentPoint);selection.gameObject.SetActive(true);
            // The arrival summary names this point; leave the ship marker unobscured.
            destinationLabel.gameObject.SetActive(false);
            StartCoroutine(AnimateVoyage(positions));
        }

        private IEnumerator AnimateVoyage(Vector2[] positions)
        {
            IsTravelling=true;
            var label=currentLabel.GetComponent<Text>();label.text="항해 중";
            for(int i=1;i<positions.Length;i++)
            {
                float elapsed=0;
                while(elapsed<.65f)
                {
                    elapsed+=Time.unscaledDeltaTime;
                    Vector2 position=Vector2.Lerp(positions[i-1],positions[i],Mathf.SmoothStep(0,1,elapsed/.65f));
                    ship.anchoredPosition=position+new Vector2(0,22);
                    currentLabel.anchoredPosition=position+new Vector2(0,-20);
                    yield return null;
                }
            }
            ship.anchoredPosition=Position(currentPoint)+new Vector2(0,22);
            currentLabel.anchoredPosition=Position(currentPoint)+new Vector2(0,-20);
            label.text="현재 위치";IsTravelling=false;
        }

        public void Close() { StopAllCoroutines();IsTravelling=false;gameObject.SetActive(false); }
    }
}
