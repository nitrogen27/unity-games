using UnityEngine;

namespace WolfE1M1
{
    public sealed class SimpleHud : MonoBehaviour
    {
        private Texture2D hudBackground;
        private GUIStyle labelStyle;

        private void Awake()
        {
            hudBackground = Resources.Load<Texture2D>("WolfE1M1/Textures/hudbg_640x80");
            if (hudBackground != null)
            {
                hudBackground.filterMode = FilterMode.Point;
            }

            labelStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                normal = { textColor = new Color32(222, 222, 222, 255) }
            };
        }

        private void OnGUI()
        {
            float scale = Mathf.Max(1f, Mathf.Floor(Screen.width / (float)WolfE1M1Constants.HudBaseWidth));
            float hudWidth = WolfE1M1Constants.HudBaseWidth * scale;
            float hudHeight = WolfE1M1Constants.HudBaseHeight * scale;
            float x = Mathf.Floor((Screen.width - hudWidth) * 0.5f);
            float y = Screen.height - hudHeight;

            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(x, y, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

            Rect hudRect = new Rect(0f, 0f, WolfE1M1Constants.HudBaseWidth, WolfE1M1Constants.HudBaseHeight);
            if (hudBackground != null)
            {
                GUI.DrawTexture(hudRect, hudBackground, ScaleMode.StretchToFill, false);
            }
            else
            {
                GUI.Box(hudRect, GUIContent.none);
            }

            GUI.Label(new Rect(24f, 16f, 160f, 48f), "FLOOR 1", labelStyle);
            GUI.Label(new Rect(240f, 16f, 160f, 48f), "ROOM 01", labelStyle);
            GUI.Label(new Rect(456f, 16f, 160f, 48f), "DEMO", labelStyle);

            GUI.matrix = previous;
        }
    }
}
