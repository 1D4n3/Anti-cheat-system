using System;
using System.Collections.Generic;
using UnityEngine;

namespace Estate2D.AntiCheat.Core
{
    public class AntiCheatManager : MonoBehaviour
    {
        private static AntiCheatManager _instance;

        [SerializeField]
        private AntiCheatConfig config;

        [SerializeField]
        private bool persistBetweenScenes = true;

        private List<IAntiCheatModule> _modules = new List<IAntiCheatModule>();
        private List<AntiCheatReport> _detectionHistory = new List<AntiCheatReport>();

        public event Action<AntiCheatReport> OnCheatDetected;
        public event Action<string> OnSystemLog;

        public AntiCheatConfig Config => config;
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

        private void Initialize()
        {
            if (config == null)
            {
                config = Resources.Load<AntiCheatConfig>("AntiCheatConfig");
                if (config == null)
                {
                    return;
                }
            }

            config.Validate();
            Log($"[AntiCheat] Init. Debug: {config.DebugMode}");
        }

        public void RegisterModule(IAntiCheatModule module)
        {
            if (module == null)
                return;

            if (_modules.Contains(module))
            {
                return;
            }

            _modules.Add(module);
            module.Initialize(config);
            Log($"[AntiCheat] Registered: {module.ModuleName}");
        }

        public void UnregisterModule(IAntiCheatModule module)
        {
            if (module != null && _modules.Remove(module))
            {
                Log($"[AntiCheat] Unregistered: {module.ModuleName}");
            }
        }

        public IAntiCheatModule GetModule(string moduleId)
        {
            return _modules.Find(m => m.ModuleId == moduleId);
        }

        public void SetModuleEnabled(string moduleId, bool enabled)
        {
            var module = GetModule(moduleId);
            if (module != null)
            {
                module.IsEnabled = enabled;
                Log($"[AntiCheat] Module {module.ModuleName} state: {enabled}");
            }
        }

        public IReadOnlyList<IAntiCheatModule> GetAllModules()
        {
            return _modules.AsReadOnly();
        }

        public DateTime? LastDeviceTimeUtc { get; private set; }
        public DateTime? LastServerTimeUtc { get; private set; }
        public DateTime? LastTimeSyncAttemptUtc { get; private set; }
        public string LastTimeSyncError { get; private set; }
        public float LastTimeSyncDifferenceSeconds { get; private set; }

        public void UpdateTimeSyncStatus(DateTime deviceUtc, DateTime serverUtc, float differenceSeconds)
        {
            LastDeviceTimeUtc = deviceUtc;
            LastServerTimeUtc = serverUtc;
            LastTimeSyncDifferenceSeconds = differenceSeconds;
            LastTimeSyncAttemptUtc = DateTime.UtcNow;
            LastTimeSyncError = null;
        }

        public void UpdateTimeSyncAttemptStatus(string errorMessage)
        {
            LastTimeSyncAttemptUtc = DateTime.UtcNow;
            LastTimeSyncError = errorMessage;
        }

        public void ReportCheatDetection(AntiCheatReport report)
        {
            if (report == null)
                return;

            _detectionHistory.Add(report);

            if (config.LogDetections)
            {
                Debug.LogError($"[AntiCheat DETECTION] {report}");
            }

            foreach (var module in _modules)
            {
                if (module.IsEnabled)
                {
                    module.OnCheatDetected(report);
                }
            }

            OnCheatDetected?.Invoke(report);
            HandleCheatResponse(report);
        }

        private void HandleCheatResponse(AntiCheatReport report)
        {
            var response = config.GetResponseForCheatType(report.CheatType);

            if (response.LogToConsole)
            {
                Debug.Log($"[AntiCheat DETECTION] {response.UserMessage}");
            }

            if (!config.ShowDetectionDialog)
            {
                if (config.PauseGameOnDetection)
                {
                    Time.timeScale = 0f;
                }

                if (config.QuitGameOnDetection)
                {
                    QuitGame();
                }
            }
        }

        public void QuitGame()
        {
            Log("[AntiCheat] Quit Game");
            Application.Quit();
        }

        private void Log(string message)
        {
            if (config.DebugMode)
            {
                Debug.Log(message);
            }
            OnSystemLog?.Invoke(message);
        }

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

        public void ClearDetectionHistory()
        {
            _detectionHistory.Clear();
            Log("[AntiCheat] History Cleared");
        }

        private void Shutdown()
        {
            Log("[AntiCheat] Shutdown");

            foreach (var module in _modules)
            {
                module.Shutdown();
            }

            _modules.Clear();
        }
    }
}