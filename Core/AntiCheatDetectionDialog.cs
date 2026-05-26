using UnityEngine;
using Estate2D.AntiCheat.Core;

namespace Estate2D.AntiCheat.Utils
{
    public class AntiCheatDetectionDialog : MonoBehaviour
    {
        [SerializeField] private int fontSize = 14;

        private bool _showDialog = false;
        private string _dialogMessage = string.Empty;
        
        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _messageStyle;
        private GUIStyle _buttonStyle;
        private Texture2D _backgroundTexture;
        private Texture2D _buttonTexture;

        private void Awake()
        {
            if (AntiCheatManager.Instance != null)
            {
                AntiCheatManager.Instance.OnCheatDetected += OnCheatDetected;
            }
        }

        private void OnDestroy()
        {
            if (AntiCheatManager.Instance != null)
            {
                AntiCheatManager.Instance.OnCheatDetected -= OnCheatDetected;
            }

            if (_backgroundTexture != null) Destroy(_backgroundTexture);
            if (_buttonTexture != null) Destroy(_buttonTexture);
        }

        private void InitGUI()
        {
            if (_windowStyle != null) return;

            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.95f));
            _backgroundTexture.Apply();

            _buttonTexture = new Texture2D(1, 1);
            _buttonTexture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f, 1f));
            _buttonTexture.Apply();

            _windowStyle = new GUIStyle
            {
                normal = { background = _backgroundTexture },
                padding = new RectOffset(20, 20, 20, 20),
                margin = new RectOffset(0, 0, 0, 0)
            };

            var baseStyle = GUI.skin != null ? GUI.skin.label : new GUIStyle();

            _headerStyle = new GUIStyle(baseStyle)
            {
                fontSize = fontSize + 4,
                fontStyle = FontStyle.Bold,
                richText = true,
                alignment = TextAnchor.MiddleCenter
            };

            _messageStyle = new GUIStyle(baseStyle)
            {
                fontSize = fontSize,
                richText = true,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            _buttonStyle = new GUIStyle(GUI.skin != null ? GUI.skin.button : new GUIStyle())
            {
                normal = { background = _buttonTexture, textColor = Color.white },
                hover = { background = _buttonTexture, textColor = Color.white },
                active = { background = _buttonTexture, textColor = Color.white },
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private void OnGUI()
        {
            if (!_showDialog) return;

            InitGUI();

            float width = 500f;
            float height = 240f;
            float x = (Screen.width - width) / 2f;
            float y = (Screen.height - height) / 2f;

            GUILayout.BeginArea(new Rect(x, y, width, height));
            GUILayout.BeginVertical(_windowStyle);

            try
            {
                GUILayout.Label("<color=#FF3333><b>DETECTION NOTICE</b></color>", _headerStyle);
                GUILayout.Space(20);

                GUILayout.Label(_dialogMessage, _messageStyle);
                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Close", _buttonStyle, GUILayout.Width(140), GUILayout.Height(40)))
                {
                    _showDialog = false;
                    Time.timeScale = 1f;

                    var manager = AntiCheatManager.Instance;
                    if (manager != null && manager.Config != null && manager.Config.QuitGameOnDetection)
                    {
                        manager.QuitGame();
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#endif
                    }
                }
                
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            finally
            {
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }

        private void OnCheatDetected(AntiCheatReport report)
        {
            var manager = AntiCheatManager.Instance;
            if (manager == null || manager.Config == null) return;

            if (!manager.Config.ShowDetectionDialog) return;

            var response = manager.Config.GetResponseForCheatType(report.CheatType);
            _dialogMessage = $"<color=#FFFFFF>{response.UserMessage}</color>";
            
            _showDialog = true;

            if (manager.Config.PauseGameOnDetection)
            {
                Time.timeScale = 0f;
            }
        }
    }
}