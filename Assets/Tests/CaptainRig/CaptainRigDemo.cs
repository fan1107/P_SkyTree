using UnityEngine;

namespace SkyTree.CaptainPrototype
{
    public sealed class CaptainRigDemo : MonoBehaviour
    {
        public Animator[] characters;
        public Camera previewCamera;
        private float playbackSpeed = 1f;
        private bool paused;
        private string mode = "Comparison";
        private readonly string[] states = { "Breathe", "Walk", "Run" };

        private void LateUpdate()
        {
            if (previewCamera) previewCamera.orthographicSize = Mathf.Max(2.65f, 4.65f / previewCamera.aspect);
        }

        private void OnGUI()
        {
            float scale = Mathf.Max(0.1f, Mathf.Min(Screen.height / 720f, Screen.width / 1000f));
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            var title = new GUIStyle(GUI.skin.label) { fontSize = 25, alignment = TextAnchor.MiddleCenter };
            var label = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0, 18, width, 36), "CAPTAIN / UNITY CUTOUT RIG", title);
            GUI.Label(new Rect(0, 56, width, 26), "9 separate sprites  /  Transform bones  /  Editable animation clips", label);
            for (int i = 0; i < characters.Length; i++)
            {
                Vector3 screen = previewCamera.WorldToScreenPoint(characters[i].transform.position);
                GUI.Label(new Rect(screen.x / scale - 100, (Screen.height-screen.y)/scale+8, 200, 30),
                    mode == "Comparison" ? states[i] : mode, label);
            }
            float x = width * 0.5f - 290;
            if (GUI.Button(new Rect(x, height-110, 110, 32), "Compare")) SetMode("Comparison");
            for (int i = 0; i < states.Length; i++)
                if (GUI.Button(new Rect(x + 118 * (i + 1), height-110, 110, 32), states[i])) SetMode(states[i]);
            if (GUI.Button(new Rect(x + 472, height-110, 110, 32), paused ? "Resume" : "Pause")) paused = !paused;
            GUI.Label(new Rect(x, height-63, 140, 25), "Speed: " + playbackSpeed.ToString("0.00") + "x");
            playbackSpeed = GUI.HorizontalSlider(new Rect(x + 145, height-56, 300, 20), playbackSpeed, 0.2f, 2f);
            foreach (var animator in characters) animator.speed = paused ? 0 : playbackSpeed;
        }

        private void SetMode(string value)
        {
            mode = value;
            for (int i = 0; i < characters.Length; i++)
                characters[i].CrossFadeInFixedTime(mode == "Comparison" ? states[i] : mode, 0.18f);
        }
    }
}
