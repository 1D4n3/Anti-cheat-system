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
                    var obj = new GameObject("[AntiCheat Manager]");
                    _instance = obj.AddComponent<AntiCheatManager>();
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

            // Ищем все модули в сцене
            var modules = GetComponentsInChildren<IAntiCheatModule>();
            foreach (var module in modules)
            {
                RegisterModule(module);
            }
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

            if (response.ShowWarning && !string.IsNullOrEmpty(response.UserMessage))
            {
                // TODO: Показать UI предупреждение
                Debug.LogWarning($"[AntiCheat WARNING] {response.UserMessage}");
            }

            if (response.DisableComponent && report.TargetObject != null)
            {
                var monoBehaviours = report.TargetObject.GetComponents<MonoBehaviour>();
                foreach (var mono in monoBehaviours)
                {
                    if (mono is not AntiCheatManager)
                    {
                        mono.enabled = false;
                    }
                }
            }

            if (response.PauseGame)
            {
                Time.timeScale = 0f;
            }

            if (response.SendToServer && !string.IsNullOrEmpty(config.ServerReportUrl))
            {
                SendReportToServer(report);
            }

            if (config.QuitGameOnDetection || response.BanPlayer)
            {
                QuitGame();
            }
        }

        /// <summary>
        /// Отправить отчёт на сервер.
        /// </summary>
        private void SendReportToServer(AntiCheatReport report)
        {
            // TODO: Реализовать отправку отчёта на сервер
            Log($"Отправка отчёта на сервер: {report}");
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
