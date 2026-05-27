using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
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

        private const float LabelWidth = 190f;

        private GameObject _playerObject;
        private Vector3 _lastPlayerPosition;
        private float _lastPlayerRotationZ; // Для 2D пространства используем угол по оси Z
        private float _currentCalculatedSpeed = 0f;
        private float _currentAngularSpeed = 0f; // Текущая угловая скорость
        private Vector2 _scrollPosition = Vector2.zero;

        private void Awake()
        {
            if (AntiCheatManager.Instance != null)
            {
                AntiCheatManager.Instance.OnCheatDetected += OnCheatDetected;
            }
        }

        private void Start()
        {
            FindPlayer();
        }

        private void Update()
        {
            CalculateTelemetry();
        }

        private void OnDestroy()
        {
            if (AntiCheatManager.Instance != null)
            {
                AntiCheatManager.Instance.OnCheatDetected -= OnCheatDetected;
            }

            if (_backgroundTexture != null)
            {
                Destroy(_backgroundTexture);
            }
        }

        private void FindPlayer()
        {
            if (_playerObject == null)
            {
                _playerObject = GameObject.FindWithTag("Player");
                if (_playerObject != null)
                {
                    _lastPlayerPosition = _playerObject.transform.position;
                    _lastPlayerRotationZ = _playerObject.transform.eulerAngles.z;
                }
            }
        }

        private void CalculateTelemetry()
        {
            if (_playerObject == null)
            {
                FindPlayer();
                return;
            }

            if (Time.deltaTime > 0f)
            {
                // 1. Линейная скорость
                Vector3 currentPos = _playerObject.transform.position;
                float distance = Vector3.Distance(currentPos, _lastPlayerPosition);
                _currentCalculatedSpeed = distance / Time.deltaTime;
                _lastPlayerPosition = currentPos;

                // 2. Угловая скорость (изменение угла Z с учетом перегрузки через 360 градусов)
                float currentRotationZ = _playerObject.transform.eulerAngles.z;
                float deltaAngle = Mathf.DeltaAngle(_lastPlayerRotationZ, currentRotationZ);
                _currentAngularSpeed = Mathf.Abs(deltaAngle) / Time.deltaTime;
                _lastPlayerRotationZ = currentRotationZ;
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

            GUILayout.BeginArea(new Rect(15, 15, 490, Screen.height - 30));
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, _windowStyle);

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

                GUILayout.Space(12);
                GUILayout.Label("<color=#FFA500>Live Telemetry</color>", _subHeaderStyle);
                GUILayout.Space(4);

                // Вывод линейной скорости
                string speedColor = _currentCalculatedSpeed > config.MaxAllowedSpeed ? "#FF3333" : "#00FF00";
                DrawRow("  Player Movement Speed:", $"<color={speedColor}><b>{_currentCalculatedSpeed:F2} u/s</b></color>");

                // Вывод угловой скорости
                string angularColor = _currentAngularSpeed > config.MaxRotationSpeed ? "#FF3333" : "#00FF00";
                DrawRow("  Player Angular Speed:", $"<color={angularColor}><b>{_currentAngularSpeed:F1} °/s</b></color>");

                DrawSegmentedConfig(config, modules);
            }
            finally
            {
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        private void DrawSegmentedConfig(AntiCheatConfig config, IReadOnlyList<IAntiCheatModule> modules)
        {
            FieldInfo[] fields = config.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var coreGroup = new List<FieldInfo>();
            var speedGroup = new List<FieldInfo>();
            var rotationGroup = new List<FieldInfo>();
            var timeGroup = new List<FieldInfo>();

            foreach (FieldInfo field in fields)
            {
                if (field.IsPrivate && !field.IsDefined(typeof(SerializeField), inherit: true))
                    continue;

                if (field.FieldType.IsGenericType || field.FieldType.IsArray)
                    continue;

                string lowerName = field.Name.ToLower();

                if (lowerName == "enabled" || lowerName == "isenabled" || lowerName.EndsWith("enabled") && field.FieldType == typeof(bool))
                {
                    if (lowerName == "enabled" || lowerName == "isenabled")
                        continue;
                }

                if (lowerName.Contains("speed") || lowerName.Contains("move"))
                    speedGroup.Add(field);
                else if (lowerName.Contains("rotate") || lowerName.Contains("rotation") || lowerName.Contains("turn"))
                    rotationGroup.Add(field);
                else if (lowerName.Contains("time") || lowerName.Contains("sync"))
                    timeGroup.Add(field);
                else
                    coreGroup.Add(field);
            }

            DrawConfigGroup("Core & Response Settings", coreGroup, config);

            if (HasActiveModule(modules, "speed") || HasActiveModule(modules, "movement"))
            {
                DrawConfigGroup("Movement Speed Module", speedGroup, config);
            }

            if (HasActiveModule(modules, "rotate") || HasActiveModule(modules, "rotation") || HasActiveModule(modules, "turn"))
            {
                DrawConfigGroup("Rotation Module", rotationGroup, config);
            }

            if (HasActiveModule(modules, "time") || HasActiveModule(modules, "sync"))
            {
                DrawConfigGroup("Time Sync Module", timeGroup, config);
                DrawTimeSyncSection(AntiCheatManager.Instance);
            }
        }

        private void DrawConfigGroup(string title, List<FieldInfo> fields, AntiCheatConfig config)
        {
            if (fields.Count == 0) return;

            GUILayout.Space(14);
            GUILayout.Label($"<color=#FFA500>{title}</color>", _subHeaderStyle);
            GUILayout.Space(4);

            foreach (FieldInfo field in fields)
            {
                string fieldName = FormatFieldName(field.Name);
                object value = field.GetValue(config);
                string displayValue = value != null ? value.ToString() : "null";

                if (field.FieldType == typeof(bool))
                {
                    displayValue = (bool)value ? "<color=#00FF00>True</color>" : "<color=#FF5555>False</color>";
                }

                DrawRow($"  {fieldName}:", displayValue);
            }
        }

        private bool HasActiveModule(IReadOnlyList<IAntiCheatModule> modules, string keyword)
        {
            if (modules == null) return false;
            foreach (var m in modules)
            {
                if (m != null && m.GetType().Name.ToLower().Contains(keyword)) return true;
            }
            return false;
        }

        private string FormatFieldName(string name)
        {
            name = name.Replace("k__BackingField", "").Replace("<", "").Replace(">", "");
            if (name.StartsWith("_")) name = name.Substring(1);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                {
                    sb.Append(' ');
                }
                sb.Append(name[i]);
            }

            string result = sb.ToString();
            if (result.Length > 0)
                result = char.ToUpper(result[0]) + result.Substring(1);

            return result;
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
            if (manager == null) return;
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
    }
}