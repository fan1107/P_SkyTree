using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    // 그림과 배치는 배경 프리팹에 저장하며, 이 컴포넌트는 구름 이동만 담당한다.
    public sealed class LobbyPassingClouds : MonoBehaviour
    {
        [SerializeField] private RectTransform[] clouds;
        [SerializeField] private float[] speeds;
        [SerializeField] private float halfWidth = 200;

        private void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if(clouds==null || speeds==null)return;
            for(int i=0;i<clouds.Length && i<speeds.Length;i++)
            {
                if(!clouds[i])continue;
                var p=clouds[i].anchoredPosition;
                p.x+=speeds[i]*deltaTime;
                float edge=halfWidth+clouds[i].sizeDelta.x*.5f+8;
                if(p.x>edge)p.x=-edge+(p.x-edge)%(2*edge);
                clouds[i].anchoredPosition=p;
            }
        }
    }
}
