using UnityEngine;

namespace SkyTree
{
    public sealed class FemaleNavigatorRigDemo : MonoBehaviour
    {
        public Animator[] characters;
        public Camera previewCamera;
        public Transform cannon;
        private float nextAttack = 1f, shotAge = 100f, playbackSpeed = 1;
        private bool paused;
        private readonly string[] names = { "Breathe", "Walk", "Run", "Attack" };
        private Vector3 cannonRest;
        private void Start()
        {
            for (int i = 0; i < characters.Length; i++)
                characters[i].GetComponent<FemaleNavigatorAnimation>().SetMovement(i == 3 ? 0 : i);
            cannonRest = cannon.localPosition;
            characters[3].GetComponent<FemaleNavigatorAnimation>().onCannonFire.AddListener(Fire);
        }
        private void Fire() => shotAge = 0;
        private void Update()
        {
            foreach (var a in characters) a.speed = paused ? 0 : playbackSpeed;
            if (paused) return;
            nextAttack -= Time.deltaTime * playbackSpeed;
            if (nextAttack <= 0) { characters[3].GetComponent<FemaleNavigatorAnimation>().Attack(); nextAttack = 2.5f; }
            shotAge += Time.deltaTime * playbackSpeed;
            cannon.localPosition = cannonRest + Vector3.left * (.13f * Mathf.Sin(Mathf.Clamp01(shotAge / .3f) * Mathf.PI));
        }
        private void LateUpdate() => previewCamera.orthographicSize = Mathf.Max(2.75f, 7.2f / previewCamera.aspect);
        private void OnGUI()
        {
            float scale = Mathf.Max(.1f, Mathf.Min(Screen.width / 1400f, Screen.height / 800f));
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale;
            var title = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0, 15, w, 40), "FEMALE NAVIGATOR / CUTOUT RIG", title);
            var label = new GUIStyle(GUI.skin.label) { fontSize = 19, alignment = TextAnchor.MiddleCenter };
            for (int i = 0; i < characters.Length; i++)
            {
                Vector3 p = previewCamera.WorldToScreenPoint(characters[i].transform.position);
                GUI.Label(new Rect(p.x / scale - 95, (Screen.height - p.y) / scale + 8, 190, 35), names[i], label);
            }
            if (GUI.Button(new Rect(w / 2 - 220, h - 65, 110, 32), paused ? "Resume" : "Pause")) paused = !paused;
            if (GUI.Button(new Rect(w / 2 - 100, h - 65, 110, 32), "Attack")) { characters[3].GetComponent<FemaleNavigatorAnimation>().Attack(); nextAttack = 2.5f; }
            GUI.Label(new Rect(w / 2 + 30, h - 64, 120, 28), "Speed " + playbackSpeed.ToString("0.00") + "x");
            playbackSpeed = GUI.HorizontalSlider(new Rect(w / 2 + 150, h - 53, 180, 20), playbackSpeed, .2f, 2f);
        }
    }
}
