using System;
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

        public void Open(Font font, int current, Action<RoutePlan> callback)
        {
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
            ResetSelection("목적지를 선택하세요", "노란 지점을 눌러 항로를 정하세요");
            gameObject.SetActive(true);transform.SetAsLastSibling();
        }
        private Vector2 Position(int index) => ((RectTransform)points[index].transform).anchoredPosition;
        private void ResetSelection(string heading,string message)
        {
            SelectedRoute=null; depart.interactable=false;route.gameObject.SetActive(false);
            selection.gameObject.SetActive(false);destinationLabel.gameObject.SetActive(false);
            title.text=heading;summary.text=message;arrival.text="가로 · 세로 · 대각선으로 이동할 수 있습니다";
        }
        public void SelectDestination(int index)
        {
            if(committed || index<0 || index>=points.Length)return;
            if(index==currentPoint){ResetSelection("현재 정박 중인 곳입니다","다른 목적지를 선택하세요");return;}
            if(!RoutePlan.TryCreate(currentPoint,index,out var plan))
            { ResetSelection("직선 항로를 선택하세요","이 지점은 현재 위치에서 바로 갈 수 없습니다"); return; }
            SelectedRoute=plan; depart.interactable=true;
            route.SetRoute(Position(currentPoint),Position(index));route.gameObject.SetActive(true);
            selection.anchoredPosition=Position(index);selection.gameObject.SetActive(true);
            destinationLabel.anchoredPosition=Position(index)+new Vector2(0,25);
            destinationLabel.GetComponent<Text>().text=destinations[index].title;destinationLabel.gameObject.SetActive(true);
            title.text=destinations[index].title;summary.text=$"{plan.WaveCount} 웨이브 · 보스 {plan.BossCount}회";
            arrival.text=destinations[index].arrivalHint;
        }
        public void Depart()
        {
            if(committed || SelectedRoute==null || !depart.interactable)return;
            committed=true;depart.interactable=false;gameObject.SetActive(false);
            onDepart?.Invoke(SelectedRoute);
        }
        public void Close() { gameObject.SetActive(false); }
    }
}
