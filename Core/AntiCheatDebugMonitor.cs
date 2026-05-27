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
        [SerializeField] private float smoothSpeed = 5f;

        private GUIStyle _windowStyle;
        private GUIStyle _labelLeftStyle;
        private GUIStyle _labelRightStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private Texture2D _backgroundTexture;
        private int _detectionCount;

        private const float LabelWidth = 220f;

        private GameObject _playerObject;
        private Vector3 _lastPlayerPosition;
        private float _lastPlayerRotationZ;

        private float _currentCalculatedSpeed;
        private float _currentAngularSpeed;
        private float _displayedLinearSpeed;
        private float _displayedAngularSpeed;

        private Vector2 _scrollPosition = Vector2.zero;

        private readonly List<FieldInfo> _coreGroup = new List<FieldInfo>();
        private readonly List<FieldInfo> _speedGroup = new List<FieldInfo>();
        private readonly List<FieldInfo> _rotationGroup = new List<FieldInfo>();
        private readonly List<FieldInfo> _timeGroup = new List<FieldInfo>();
        private readonly Dictionary<FieldInfo, string> _formattedFieldNames = new Dictionary<FieldInfo, string>();

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
                Vector3 currentPos = _playerObject.transform.position;
                float distance = Vector3.Distance(currentPos, _lastPlayerPosition);
                _currentCalculatedSpeed = distance / Time.deltaTime;
                _lastPlayerPosition = currentPos;

                float currentRotationZ = _playerObject.transform.eulerAngles.z;
                float deltaAngle = Mathf.DeltaAngle(_lastPlayerRotationZ, currentRotationZ);
                _currentAngularSpeed = Mathf.Abs(deltaAngle) / Time.deltaTime;
                _lastPlayerRotationZ = currentRotationZ;

                _displayedLinearSpeed = Mathf.Lerp(_displayedLinearSpeed, _currentCalculatedSpeed, Time.deltaTime * smoothSpeed);
                _displayedAngularSpeed = Mathf.Lerp(_displayedAngularSpeed, _currentAngularSpeed, Time.deltaTime * smoothSpeed);
            }
        }

        private void InitGUI()
        {
            if (_windowStyle != null) return;

            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
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
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Overflow
            };

            _subHeaderStyle = new GUIStyle(baseStyle)
            {
                fontSize = fontSize + 1,
                fontStyle = FontStyle.Bold,
                richText = true,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow
            };

            _labelLeftStyle = new GUIStyle(baseStyle)
            {
                fontSize = fontSize,
                richText = true,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow
            };

            _labelRightStyle = new GUIStyle(baseStyle)
            {
                fontSize = fontSize,
                richText = true,
                alignment = TextAnchor.MiddleRight,
                clipping = TextClipping.Overflow
            };

            CacheConfigFields();
        }

        private void CacheConfigFields()
        {
            if (AntiCheatManager.Instance == null || AntiCheatManager.Instance.Config == null) return;

            FieldInfo[] fields = AntiCheatManager.Instance.Config.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

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

                _formattedFieldNames[field] = FormatFieldName(field.Name);

                if (lowerName.Contains("speed") || lowerName.Contains("move"))
                    _speedGroup.Add(field);
                else if (lowerName.Contains("rotate") || lowerName.Contains("rotation") || lowerName.Contains("turn"))
                    _rotationGroup.Add(field);
                else if (lowerName.Contains("time") || lowerName.Contains("sync"))
                    _timeGroup.Add(field);
                else
                    _coreGroup.Add(field);
            }
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
                GUILayout.Label("<color=#FFD700><b>Интерфейс анти-чит системы</b></color>", _headerStyle);
                GUILayout.Space(10);

                var config = manager.Config;
                if (config == null)
                {
                    GUILayout.Label("<color=red>Конфигурация отсутствует</color>", _labelLeftStyle);
                    return;
                }

                DrawRow("<color=#00FFFF>Статус системы:</color>", config.Enabled ? "<color=#00FF00><b>АКТИВНА</b></color>" : "<color=#FF0000><b>ОТКЛЮЧЕНА</b></color>");
                DrawRow("<color=#00FFFF>Выявлено нарушений:</color>", _detectionCount > 0 ? $"<color=#FF3333><b>{_detectionCount}</b></color>" : "0");

                var modules = manager.GetAllModules();
                DrawRow("<color=#00FFFF>Активных модулей:</color>", (modules != null ? modules.Count : 0).ToString());

                GUILayout.Space(12);
                GUILayout.Label("<color=#FFA500>Телеметрия игрока</color>", _subHeaderStyle);
                GUILayout.Space(4);

                string speedColor = _displayedLinearSpeed > config.MaxAllowedSpeed ? "#FF3333" : "#00FF00";
                DrawRow("  Скорость движения:", $"<color={speedColor}><b>{_displayedLinearSpeed:F2} ед/с</b></color>");

                string angularColor = _displayedAngularSpeed > config.MaxRotationSpeed ? "#FF3333" : "#00FF00";
                DrawRow("  Скорость вращения:", $"<color={angularColor}><b>{_displayedAngularSpeed:F1} °/с</b></color>");

                DrawConfigGroups(config, modules);
            }
            finally
            {
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        private void DrawConfigGroups(AntiCheatConfig config, IReadOnlyList<IAntiCheatModule> modules)
        {
            DrawConfigGroup("Общие настройки", _coreGroup, config);

            if (HasActiveModule(modules, "speed") || HasActiveModule(modules, "movement"))
            {
                DrawConfigGroup("Модуль контроля скорости", _speedGroup, config);
            }

            if (HasActiveModule(modules, "rotate") || HasActiveModule(modules, "rotation") || HasActiveModule(modules, "turn"))
            {
                DrawConfigGroup("Модуль контроля вращения", _rotationGroup, config);
            }

            if (HasActiveModule(modules, "time") || HasActiveModule(modules, "sync"))
            {
                DrawConfigGroup("Модуль синхронизации времени", _timeGroup, config);
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
                string fieldName = _formattedFieldNames.TryGetValue(field, out var cachedName) ? cachedName : field.Name;
                object value = field.GetValue(config);
                string displayValue = value != null ? value.ToString() : "null";

                if (field.FieldType == typeof(bool))
                {
                    displayValue = (bool)value ? "<color=#00FF00>Истина</color>" : "<color=#FF5555>Ложь</color>";
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
                DrawRow("  UTC устройства:", manager.LastDeviceTimeUtc.Value.ToString(timeFormat));
                DrawRow("  UTC сервера:", manager.LastServerTimeUtc.Value.ToString(timeFormat));

                string diffColor = Mathf.Abs(manager.LastTimeSyncDifferenceSeconds) > 5f ? "#FF5555" : "#FFFF00";
                DrawRow("  Разница времени:", $"<color={diffColor}>{manager.LastTimeSyncDifferenceSeconds:F2}с</color>");

                if (manager.LastTimeSyncAttemptUtc.HasValue)
                {
                    DrawRow("  Последняя проверка:", manager.LastTimeSyncAttemptUtc.Value.ToString(timeFormat));
                }
            }
            else
            {
                var statusText = "<color=#888888>еще не проверялось</color>";
                if (!string.IsNullOrEmpty(manager.LastTimeSyncError))
                {
                    statusText = $"<color=red>ошибка: {manager.LastTimeSyncError}</color>";
                }
                else if (manager.LastTimeSyncAttemptUtc.HasValue)
                {
                    statusText = $"<color=yellow>выполняется попытка: {manager.LastTimeSyncAttemptUtc.Value.ToString(timeFormat)}</color>";
                }

                DrawRow("  Статус синхронизации:", statusText);
            }
        }

        private void OnCheatDetected(AntiCheatReport report)
        {
            _detectionCount++;

            if (showInConsole)
            {
                Debug.LogError($"[AC] Детекция #{_detectionCount}: {report?.CheatType} - {report?.Message}");
            }
        }
    }
}