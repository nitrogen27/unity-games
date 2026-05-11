using UnityEngine;

namespace HelloWorldRoom
{
    public sealed class WolfHud : MonoBehaviour
    {
        [SerializeField] private Texture2D faceTexture;
        [SerializeField] private Texture2D weaponTexture;
        [SerializeField] private Texture2D keyTexture;
        [SerializeField] private int floor = 1;
        [SerializeField] private int score = 1200;
        [SerializeField] private int lives = 3;
        [SerializeField] private int health = 100;
        [SerializeField] private int ammo = 8;
        [SerializeField] private bool showHint;

        private GUIStyle labelStyle;
        private GUIStyle valueStyle;

        private void Awake()
        {
            labelStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.86f, 0.62f) },
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

            GUI.color = new Color(0.05f, 0.05f, 0.07f, 0.98f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);

            GUI.color = new Color(0.22f, 0.22f, 0.28f, 1f);
            GUI.DrawTexture(new Rect(0f, bar.y, Screen.width, 4f), Texture2D.whiteTexture);

            GUI.color = Color.white;

            int columns = Screen.width < 900 ? 5 : 7;
            float cellW = Screen.width / (float)columns;
            float labelY = bar.y + 9f;
            float valueY = bar.y + barHeight * 0.45f;
            float labelSize = Mathf.Clamp(barHeight * 0.16f, 12f, 18f);
            float valueSize = Mathf.Clamp(barHeight * 0.30f, 22f, 36f);
            labelStyle.fontSize = Mathf.RoundToInt(labelSize);
            valueStyle.fontSize = Mathf.RoundToInt(valueSize);

            DrawHudCell(0, cellW, labelY, valueY, "FLOOR", floor.ToString("00"));
            DrawHudCell(1, cellW, labelY, valueY, "SCORE", score.ToString("000000"));
            DrawHudCell(2, cellW, labelY, valueY, "LIVES", lives.ToString());

            Rect faceRect = new Rect(3f * cellW + (cellW - barHeight * 0.62f) * 0.5f, bar.y + barHeight * 0.19f, barHeight * 0.62f, barHeight * 0.62f);
            if (faceTexture != null && columns >= 7)
            {
                GUI.DrawTexture(faceRect, faceTexture, ScaleMode.ScaleToFit, true);
            }

            int healthColumn = columns >= 7 ? 4 : 3;
            int ammoColumn = columns >= 7 ? 5 : 4;
            DrawHudCell(healthColumn, cellW, labelY, valueY, "HEALTH", health.ToString());
            DrawHudCell(ammoColumn, cellW, labelY, valueY, "AMMO", ammo.ToString());

            if (columns >= 7)
            {
                DrawHudCell(6, cellW, labelY, valueY, "KEY", string.Empty);
                if (keyTexture != null)
                {
                    Rect keyRect = new Rect(6f * cellW + (cellW - 34f) * 0.5f, valueY + 4f, 34f, 34f);
                    GUI.DrawTexture(keyRect, keyTexture, ScaleMode.ScaleToFit, true);
                }
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
