using UnityEngine;
using Estate2D.AntiCheat.Core;

namespace Estate2D.AntiCheat.Utils
{
    public class AntiCheatDebugMonitor : MonoBehaviour
    {
        [SerializeField] private bool showInConsole = true;
        [SerializeField] private bool showOnScreen = true;
        [SerializeField] private int fontSize = 13;

        private GUIStyle _windowStyle;
        private GUIStyle _labelLeftStyle;
        private GUIStyle _labelRightStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private Texture2D _backgroundTexture;
        private int _detectionCount = 0;

        private const float LabelWidth = 160f;

        private void Awake()
        {
            if (AntiCheatManager.Instance != null)
            {
                AntiCheatManager.Instance.OnCheatDetected += OnCheatDetected;
                AntiCheatManager.Instance.OnSystemLog += OnSystemLog;
            }
        }

        private void OnDestroy()
        {
            if (AntiCheatManager.Instance != null)
            {
                AntiCheatManager.Instance.OnCheatDetected -= OnCheatDetected;
                AntiCheatManager.Instance.OnSystemLog -= OnSystemLog;
            }

            if (_backgroundTexture != null)
            {
                Destroy(_backgroundTexture);
            }
        }

        private void InitGUI()
        {
            if (_windowStyle != null) return;

            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.85f));
            _backgroundTexture.Apply();

            _windowStyle = new GUIStyle
            {
                normal = { background = _backgroundTexture },
                padding = new RectOffset(15, 15, 15, 15),
                margin = new RectOffset(0, 0, 0, 0)
            };

            var baseStyle = GUI.skin != null ? GUI.skin.label : new GUIStyle();

            _headerStyle = new GUIStyle(baseStyle)
            {
                fontSize = fontSize + 3,
                fontStyle = FontStyle.Bold,
                richText = true,
                alignment = TextAnchor.UpperLeft
            };

            _subHeaderStyle = new GUIStyle(baseStyle)
            {
                fontSize = fontSize + 1,
                fontStyle = FontStyle.Bold,
                richText = true,
                alignment = TextAnchor.MiddleLeft
            };

            _labelLeftStyle = new GUIStyle(baseStyle)
            {
                fontSize = fontSize,
                richText = true,
                alignment = TextAnchor.MiddleLeft
            };

            _labelRightStyle = new GUIStyle(baseStyle)
            {
                fontSize = fontSize,
                richText = true,
                alignment = TextAnchor.MiddleRight
            };
        }

        private void OnGUI()
        {
            if (!showOnScreen) return;

            InitGUI();

            var manager = AntiCheatManager.Instance;
            if (manager == null) return;

            GUILayout.BeginArea(new Rect(15, 15, 480, Screen.height - 30));
            GUILayout.BeginVertical(_windowStyle);

            try
            {
                GUILayout.Label("<color=#FFD700><b>AntiCheat Monitor</b></color>", _headerStyle);
                GUILayout.Space(10);

                var config = manager.Config;
                if (config == null)
                {
                    GUILayout.Label("<color=red>Config is NULL</color>", _labelLeftStyle);
                    return;
                }

                DrawRow("<color=#00FFFF>Status:</color>", config.Enabled ? "<color=#00FF00><b>ACTIVE</b></color>" : "<color=#FF0000><b>DISABLED</b></color>");
                DrawRow("<color=#00FFFF>Total Detections:</color>", _detectionCount > 0 ? $"<color=#FF3333><b>{_detectionCount}</b></color>" : "0");
                
                var modules = manager.GetAllModules();
                DrawRow("<color=#00FFFF>Active Modules:</color>", (modules != null ? modules.Count : 0).ToString());

                if (modules != null && modules.Count > 0)
                {
                    foreach (var module in modules)
                    {
                        if (module == null) continue;

                        GUILayout.Space(12);
                        string moduleName = module.GetType().Name.Replace("Module", "");
                        GUILayout.Label($"<color=#FFA500>{moduleName} Module</color>", _subHeaderStyle);
                        GUILayout.Space(4);

                        string typeName = module.GetType().Name.ToLower();

                        if (typeName.Contains("speed") || typeName.Contains("movement"))
                        {
                            DrawRow("  Max Allowed Speed:", $"{config.MaxAllowedSpeed:F1} u/s");
                        }
                        else if (typeName.Contains("rotation") || typeName.Contains("turn"))
                        {
                            DrawRow("  Max Rotation Speed:", $"{config.MaxRotationSpeed:F1}°/s");
                        }
                        else if (typeName.Contains("time") || typeName.Contains("sync"))
                        {
                            DrawTimeSyncSection(manager);
                        }
                        else
                        {
                            DrawRow("  Status:", "<color=#888888>Running...</color>");
                        }
                    }
                }
                else
                {
                    GUILayout.Space(10);
                    GUILayout.Label("<color=yellow>No modules loaded.</color>", _labelLeftStyle);
                }
            }
            finally
            {
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }

        private void DrawRow(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelLeftStyle, GUILayout.Width(LabelWidth));
            GUILayout.Label(value, _labelRightStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawTimeSyncSection(AntiCheatManager manager)
        {
            const string timeFormat = "yyyy-MM-dd HH:mm:ss";

            if (manager.LastDeviceTimeUtc.HasValue && manager.LastServerTimeUtc.HasValue)
            {
                DrawRow("  Device UTC:", manager.LastDeviceTimeUtc.Value.ToString(timeFormat));
                DrawRow("  Server UTC:", manager.LastServerTimeUtc.Value.ToString(timeFormat));
                
                string diffColor = Mathf.Abs((float)manager.LastTimeSyncDifferenceSeconds) > 5f ? "#FF5555" : "#FFFF00";
                DrawRow("  Time Diff:", $"<color={diffColor}>{manager.LastTimeSyncDifferenceSeconds:F2}s</color>");

                if (manager.LastTimeSyncAttemptUtc.HasValue)
                {
                    DrawRow("  Last Sync Attempt:", manager.LastTimeSyncAttemptUtc.Value.ToString(timeFormat));
                }
            }
            else
            {
                var statusText = "<color=#888888>not checked yet</color>";
                if (!string.IsNullOrEmpty(manager.LastTimeSyncError))
                {
                    statusText = $"<color=red>failed: {manager.LastTimeSyncError}</color>";
                }
                else if (manager.LastTimeSyncAttemptUtc.HasValue)
                {
                    statusText = $"<color=yellow>attempted: {manager.LastTimeSyncAttemptUtc.Value.ToString(timeFormat)}</color>";
                }

                DrawRow("  Sync Status:", statusText);
            }
        }

        private void OnCheatDetected(AntiCheatReport report)
        {
            _detectionCount++;

            if (showInConsole)
            {
                Debug.LogError($"[AntiCheat Monitor] Detection #{_detectionCount}: {report?.CheatType} - {report?.Message}");
            }
        }

        private void OnSystemLog(string message)
        {
            if (showInConsole)
            {
                Debug.Log($"[AntiCheat Monitor] {message}");
            }
        }
    }
}