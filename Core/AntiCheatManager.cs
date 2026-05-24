using System;
using System.Collections.Generic;
using UnityEngine;

namespace Estate2D.AntiCheat.Core
{
    /// <summary>
    /// Центральный менеджер системы античита.
    /// Управляет всеми модулями обнаружения и координирует ответы на обнаруженные нарушения.
    /// Работает как синглтон с опциональным DontDestroyOnLoad.
    /// </summary>
    public class AntiCheatManager : MonoBehaviour
    {
        private static AntiCheatManager _instance;

        [SerializeField]
        private AntiCheatConfig config;

        [SerializeField]
        private bool persistBetweenScenes = true;

        private List<IAntiCheatModule> _modules = new List<IAntiCheatModule>();
        private List<AntiCheatReport> _detectionHistory = new List<AntiCheatReport>();

        // Диалог
        private bool _showDetectionDialog = false;
        private string _detectionDialogMessage = "";
        private GUIStyle _dialogWindowStyle;
        private GUIStyle _dialogLabelStyle;
        private GUIStyle _dialogButtonStyle;

        /// <summary>
        /// Событие, вызываемое при обнаружении любого нарушения.
        /// </summary>
        public event Action<AntiCheatReport> OnCheatDetected;

        /// <summary>
        /// Событие для логирования системы.
        /// </summary>
        public event Action<string> OnSystemLog;

        /// <summary>
        /// Текущая конфигурация системы.
        /// </summary>
        public AntiCheatConfig Config => config;

        /// <summary>
        /// История всех обнаруженных нарушений.
        /// </summary>
        public IReadOnlyList<AntiCheatReport> DetectionHistory => _detectionHistory.AsReadOnly();

        public static AntiCheatManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<AntiCheatManager>();
                    if (_instance == null)
                    {
                        var obj = new GameObject("[AntiCheat Manager]");
                        _instance = obj.AddComponent<AntiCheatManager>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            // Синглтон паттерн
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (persistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Shutdown();
                _instance = null;
            }
        }

        private void OnGUI()
        {
            if (_showDetectionDialog && config.ShowDetectionDialog)
            {
                DrawDetectionDialog();
            }
        }

        /// <summary>
        /// Инициализирует систему античита.
        /// </summary>
        private void Initialize()
        {
            if (config == null)
            {
                Debug.LogError("[AntiCheat] Config не назначен! Загружаю конфигурацию по умолчанию.");
                config = Resources.Load<AntiCheatConfig>("AntiCheatConfig");

                if (config == null)
                {
                    Debug.LogError("[AntiCheat] Не удалось загрузить конфигурацию.");
                    return;
                }
            }

            config.Validate();

            Log($"Система античита инициализирована. Режим отладки: {config.DebugMode}");

            // Модули регистрируются автоматически сами (autoRegister) или вручную.
        }

        /// <summary>
        /// Регистрирует модуль в системе.
        /// </summary>
        public void RegisterModule(IAntiCheatModule module)
        {
            if (module == null)
                return;

            if (_modules.Contains(module))
            {
                Debug.LogWarning($"[AntiCheat] Модуль {module.ModuleName} уже зарегистрирован.");
                return;
            }

            _modules.Add(module);
            module.Initialize(config);

            Log($"Модуль {module.ModuleName} зарегистрирован.");
        }

        /// <summary>
        /// Удаляет модуль из системы.
        /// </summary>
        public void UnregisterModule(IAntiCheatModule module)
        {
            if (module != null && _modules.Remove(module))
            {
                Log($"Модуль {module.ModuleName} удалён.");
            }
        }

        /// <summary>
        /// Получить модуль по ID.
        /// </summary>
        public IAntiCheatModule GetModule(string moduleId)
        {
            return _modules.Find(m => m.ModuleId == moduleId);
        }

        /// <summary>
        /// Включить/отключить модуль по ID.
        /// </summary>
        public void SetModuleEnabled(string moduleId, bool enabled)
        {
            var module = GetModule(moduleId);
            if (module != null)
            {
                module.IsEnabled = enabled;
                Log($"Модуль {module.ModuleName} {(enabled ? "включён" : "отключён")}.");
            }
        }

        /// <summary>
        /// Получить список всех зарегистрированных модулей.
        /// </summary>
        public IReadOnlyList<IAntiCheatModule> GetAllModules()
        {
            return _modules.AsReadOnly();
        }

        /// <summary>
        /// Последнее синхронизированное время устройства.
        /// </summary>
        public DateTime? LastDeviceTimeUtc { get; private set; }

        /// <summary>
        /// Последнее синхронизированное серверное время.
        /// </summary>
        public DateTime? LastServerTimeUtc { get; private set; }

        /// <summary>
        /// Время последней попытки синхронизации, успешной или неуспешной.
        /// </summary>
        public DateTime? LastTimeSyncAttemptUtc { get; private set; }

        /// <summary>
        /// Текст ошибки последней попытки проверки времени.
        /// </summary>
        public string LastTimeSyncError { get; private set; }

        /// <summary>
        /// Последняя разница между устройством и сервером в секундах.
        /// </summary>
        public float LastTimeSyncDifferenceSeconds { get; private set; }

        /// <summary>
        /// Обновить статус синхронизации времени.
        /// </summary>
        public void UpdateTimeSyncStatus(DateTime deviceUtc, DateTime serverUtc, float differenceSeconds)
        {
            LastDeviceTimeUtc = deviceUtc;
            LastServerTimeUtc = serverUtc;
            LastTimeSyncDifferenceSeconds = differenceSeconds;
            LastTimeSyncAttemptUtc = DateTime.UtcNow;
            LastTimeSyncError = null;
        }

        /// <summary>
        /// Обновить информацию о неудачной попытке синхронизации.
        /// </summary>
        public void UpdateTimeSyncAttemptStatus(string errorMessage)
        {
            LastTimeSyncAttemptUtc = DateTime.UtcNow;
            LastTimeSyncError = errorMessage;
        }

        /// <summary>
        /// Обработать обнаруженное нарушение.
        /// Вызывается модулями при обнаружении читерства.
        /// </summary>
        public void ReportCheatDetection(AntiCheatReport report)
        {
            if (report == null)
                return;

            _detectionHistory.Add(report);

            if (config.LogDetections)
            {
                Debug.LogError($"[AntiCheat DETECTION] {report}");
            }

            // Уведомить все модули
            foreach (var module in _modules)
            {
                if (module.IsEnabled)
                {
                    module.OnCheatDetected(report);
                }
            }

            // Вызвать событие
            OnCheatDetected?.Invoke(report);

            // Обработать ответ в соответствии с конфигурацией
            HandleCheatResponse(report);
        }

        /// <summary>
        /// Обработать ответ на обнаруженное нарушение.
        /// </summary>
        private void HandleCheatResponse(AntiCheatReport report)
        {
            var response = config.GetResponseForCheatType(report.CheatType);

            if (response.LogToConsole)
            {
                Debug.Log($"[AntiCheat DETECTION] {response.UserMessage}");
            }

            if (config.PauseGameOnDetection)
            {
                Time.timeScale = 0f;
            }

            if (config.ShowDetectionDialog)
            {
                _detectionDialogMessage = response.UserMessage;
                _showDetectionDialog = true;
            }
            else if (config.QuitGameOnDetection)
            {
                QuitGame();
            }
        }

        /// <summary>
        /// Нарисовать диалог обнаружения нарушения.
        /// </summary>
        private void DrawDetectionDialog()
        {
            try
            {
                InitDialogGUI();

                // Вычислить размеры окна
                float windowWidth = 600f;
                float windowHeight = 350f;
                float windowX = (Screen.width - windowWidth) / 2f;
                float windowY = (Screen.height - windowHeight) / 2f;

                // Рисуем окно
                GUI.Window(0, new Rect(windowX, windowY, windowWidth, windowHeight), DrawDialogWindow, "");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AntiCheat] Ошибка при рисовании диалога: {ex.Message}");
                _showDetectionDialog = false;
                Time.timeScale = 1f;
                if (config.QuitGameOnDetection)
                {
                    QuitGame();
                }
            }
        }

        private void DrawDialogWindow(int windowID)
        {
            // Текст сообщения
            var messageText = $"⚠️ ОБНАРУЖЕНО НАРУШЕНИЕ\\n\\n{_detectionDialogMessage}";
            GUI.Label(new Rect(20, 20, 560, 200), messageText, _dialogLabelStyle);

            // Кнопка закрытия
            if (GUI.Button(new Rect(225, 250, 150, 50), "Закрыть", _dialogButtonStyle))
            {
                _showDetectionDialog = false;
                Time.timeScale = 1f;

                if (config.QuitGameOnDetection)
                {
                    QuitGame();
                }
            }
        }

        private void InitDialogGUI()
        {
            if (_dialogWindowStyle != null) return;

            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.15f, 0.95f));
            texture.Apply();

            _dialogWindowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = texture },
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            var labelTexture = new Texture2D(1, 1);
            labelTexture.SetPixel(0, 0, new Color(1f, 1f, 1f, 0f));
            labelTexture.Apply();

            _dialogLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                richText = true
            };
            _dialogLabelStyle.normal.textColor = new Color(1f, 0.3f, 0.3f, 1f);

            var buttonTexture = new Texture2D(1, 1);
            buttonTexture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f, 1f));
            buttonTexture.Apply();

            _dialogButtonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = buttonTexture, textColor = Color.white },
                hover = { background = buttonTexture, textColor = Color.white },
                active = { background = buttonTexture, textColor = Color.white },
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 10, 10, 10)
            };
        }

        /// <summary>
        /// Выход из игры.
        /// </summary>
        private void QuitGame()
        {
            Log("Выход из игры по причине обнаружения читерства.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Логировать сообщение.
        /// </summary>
        private void Log(string message)
        {
            if (config.DebugMode)
            {
                Debug.Log($"[AntiCheat] {message}");
            }
            OnSystemLog?.Invoke(message);
        }

        /// <summary>
        /// Получить статистику обнаружений.
        /// </summary>
        public Dictionary<CheatType, int> GetDetectionStatistics()
        {
            var stats = new Dictionary<CheatType, int>();

            foreach (var report in _detectionHistory)
            {
                if (!stats.ContainsKey(report.CheatType))
                    stats[report.CheatType] = 0;

                stats[report.CheatType]++;
            }

            return stats;
        }

        /// <summary>
        /// Очистить историю обнаружений.
        /// </summary>
        public void ClearDetectionHistory()
        {
            _detectionHistory.Clear();
            Log("История обнаружений очищена.");
        }

        /// <summary>
        /// Завершить работу системы.
        /// </summary>
        private void Shutdown()
        {
            Log("Система античита выключается.");

            foreach (var module in _modules)
            {
                module.Shutdown();
            }

            _modules.Clear();
        }
    }
}
