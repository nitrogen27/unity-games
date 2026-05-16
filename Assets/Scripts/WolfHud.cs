using UnityEngine;

namespace HelloWorldRoom
{
    public sealed class WolfHud : MonoBehaviour
    {
        [SerializeField] private Texture2D faceTexture;
        [SerializeField] private Texture2D weaponTexture;
        [SerializeField] private Texture2D keyTexture;
        [SerializeField] private int floor = 1;
        [SerializeField] private int score = 0;
        [SerializeField] private int lives = 3;
        [SerializeField] private int room = 1;
        [SerializeField] private int health = 100;
        [SerializeField] private int ammo = 8;
        [SerializeField] private string demoText = "DEMO";
        [SerializeField] private bool showHint;

        private GUIStyle labelStyle;
        private GUIStyle valueStyle;

        private void Awake()
        {
            labelStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontStyle = FontStyle.Bold
            };
            valueStyle = new GUIStyle(labelStyle)
            {
                normal = { textColor = Color.white }
            };
        }

        private void OnGUI()
        {
            DrawWeapon();
            DrawStatusBar();
            if (showHint)
            {
                DrawHint();
            }
        }

        private void DrawWeapon()
        {
            if (weaponTexture == null)
            {
                return;
            }

            float scale = Mathf.Clamp(Screen.height / 540f, 1f, 2.2f);
            float width = 180f * scale;
            float height = 180f * scale;
            Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 120f * scale - height, width, height);
            GUI.DrawTexture(rect, weaponTexture, ScaleMode.ScaleToFit, true);
        }

        private void DrawStatusBar()
        {
            float barHeight = Mathf.Clamp(Screen.height * 0.15f, 82f, 128f);
            Rect bar = new Rect(0f, Screen.height - barHeight, Screen.width, barHeight);
            bool wideLayout = Screen.width >= 700;
            int columns = wideLayout ? 7 : 5;
            float cellW = Screen.width / (float)columns;

            GUI.color = new Color(0.13f, 0.21f, 0.55f, 0.98f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);

            Color frameColor = new Color(0.55f, 0.55f, 0.60f, 1f);
            GUI.color = frameColor;
            GUI.DrawTexture(new Rect(0f, bar.y, Screen.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, Screen.height - 2f, Screen.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, bar.y, 2f, barHeight), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width - 2f, bar.y, 2f, barHeight), Texture2D.whiteTexture);

            if (wideLayout)
            {
                GUI.color = new Color(0.18f, 0.18f, 0.20f, 1f);
                GUI.DrawTexture(new Rect(3f * cellW + 2f, bar.y + 2f, cellW - 4f, barHeight - 4f), Texture2D.whiteTexture);
            }

            GUI.color = frameColor;
            for (int i = 1; i < columns; i++)
            {
                GUI.DrawTexture(new Rect(i * cellW, bar.y, 2f, barHeight), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;

            float labelY = bar.y + 9f;
            float valueY = bar.y + barHeight * 0.45f;
            float labelSize = Mathf.Clamp(barHeight * 0.20f, 14f, 24f);
            float valueSize = Mathf.Clamp(barHeight * 0.42f, 28f, 54f);
            labelStyle.fontSize = Mathf.RoundToInt(labelSize);
            valueStyle.fontSize = Mathf.RoundToInt(valueSize);

            DrawHudCell(0, cellW, labelY, valueY, "FLOOR", floor.ToString("00"));
            DrawHudCell(1, cellW, labelY, valueY, "SCORE", score.ToString("000000"));
            DrawHudCell(2, cellW, labelY, valueY, "LIVES", lives.ToString());

            if (wideLayout)
            {
                DrawHudCell(3, cellW, labelY, valueY, "ROOM", room.ToString("00"));
                DrawHudCell(4, cellW, labelY, valueY, "HEALTH", Mathf.Clamp(health, 0, 100).ToString() + "%");
                DrawHudCell(5, cellW, labelY, valueY, "AMMO", ammo.ToString());
                DrawHudCell(6, cellW, labelY, valueY, string.Empty, demoText);
            }
            else
            {
                DrawHudCell(3, cellW, labelY, valueY, "HEALTH", Mathf.Clamp(health, 0, 100).ToString() + "%");
                DrawHudCell(4, cellW, labelY, valueY, "AMMO", ammo.ToString());
            }
        }

        private void DrawHudCell(int column, float cellWidth, float labelY, float valueY, string label, string value)
        {
            Rect labelRect = new Rect(column * cellWidth, labelY, cellWidth, 24f);
            Rect valueRect = new Rect(column * cellWidth, valueY, cellWidth, 42f);
            GUI.Label(labelRect, label, labelStyle);
            GUI.Label(valueRect, value, valueStyle);
        }

        private void DrawHint()
        {
            GUIStyle hint = new GUIStyle(labelStyle)
            {
                fontSize = 14,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(1f, 0.9f, 0.58f, 0.85f) }
            };
            GUI.Label(new Rect(0f, 12f, Screen.width, 24f), "WASD + mouse, Shift run, Space jump, E doors", hint);
        }
    }
}
