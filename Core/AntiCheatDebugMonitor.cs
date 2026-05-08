using UnityEngine;
using Estate2D.AntiCheat.Core;

namespace Estate2D.AntiCheat.Utils
{
    public class AntiCheatDebugMonitor : MonoBehaviour
    {
        [SerializeField] private bool showInConsole = true;
        [SerializeField] private bool showOnScreen = true;
        [SerializeField] private int fontSize = 12;

        private GUIStyle _guiStyle;
        private int _detectionCount = 0;

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
        }

        private void InitGUI()
        {
            if (_guiStyle != null) return;

            var baseStyle = GUI.skin != null ? GUI.skin.label : new GUIStyle();

            _guiStyle = new GUIStyle(baseStyle)
            {
                fontSize = fontSize,
                richText = true,
                alignment = TextAnchor.UpperLeft
            };
        }

        private void OnGUI()
        {
            if (!showOnScreen) return;

            InitGUI();

            var manager = AntiCheatManager.Instance;
            if (manager == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 420, 320));

            try
            {
                GUILayout.Label("<color=yellow><b>AntiCheat Monitor</b></color>", _guiStyle);

                var config = manager.Config;
                if (config == null)
                {
                    GUILayout.Label("<color=red>Config is NULL</color>", _guiStyle);
                    return;
                }

                GUILayout.Label(
                    $"<color=cyan>Status:</color> {(config.Enabled ? "<color=green>ON</color>" : "<color=red>OFF</color>")}",
                    _guiStyle
                );

                GUILayout.Label(
                    $"<color=cyan>Detections:</color> {_detectionCount}",
                    _guiStyle
                );

                var modules = manager.GetAllModules();
                GUILayout.Label(
                    $"<color=cyan>Modules:</color> {(modules != null ? modules.Count : 0)}",
                    _guiStyle
                );

                GUILayout.Space(10);
                GUILayout.Label("<color=yellow>Settings:</color>", _guiStyle);

                GUILayout.Label($"  Max Speed: {config.MaxAllowedSpeed:F1} units/sec", _guiStyle);
                GUILayout.Label($"  Max Rotation: {config.MaxRotationSpeed:F1}°/sec", _guiStyle);

                GUILayout.Space(10);

                if (GUILayout.Button("Clear History", GUILayout.Width(120)))
                {
                    manager.ClearDetectionHistory();
                    _detectionCount = 0;
                }
            }
            finally
            {
                GUILayout.EndArea();
            }
        }

        private void OnCheatDetected(AntiCheatReport report)
        {
            _detectionCount++;

            if (showInConsole)
            {
                Debug.LogError(
                    $"[AntiCheat Monitor] Detection #{_detectionCount}: {report?.CheatType} - {report?.Message}"
                );
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