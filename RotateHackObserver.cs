using UnityEngine;
using Estate2D.AntiCheat.Core;

namespace Estate2D.AntiCheat
{
    /// <summary>
    /// Наблюдатель за скоростью вращения персонажа.
    /// Обнаруживает попытки ускоренного вращения/паровращения через анализ угловой скорости.
    /// </summary>
    public class RotationHackObserver : MonoBehaviour, IAntiCheatModule
    {
        public struct RotationHackReport
        {
            public float ObservedDegPerSecond;
            public float ThresholdDegPerSecond;
            public float DeltaDegrees;
            public float ElapsedSeconds;
            public float FromDegrees;
            public float ToDegrees;
        }

        [SerializeField]
        private bool autoRegister = true;

        [SerializeField]
        private bool logLocalViolations = false;

        public event System.Action<RotationHackReport> Detected;

        private AntiCheatConfig _config;
        private float _lastZAngle;
        private float _timer;
        private int _currentSuspicion;
        private bool _isEnabled = true;

        // IAntiCheatModule
        public string ModuleId => "rotation_hack_observer";
        public string ModuleName => "Rotation Hack Observer";
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        private void Start()
        {
            _lastZAngle = transform.rotation.eulerAngles.z;

            // Автоматическая регистрация в менеджере
            if (autoRegister)
            {
                AntiCheatManager.Instance.RegisterModule(this);
            }
        }

        private void Update()
        {
            if (!_isEnabled || _config == null)
                return;

            _timer += Time.unscaledDeltaTime;

            if (_timer >= _config.RotationCheckInterval)
            {
                ValidateRotation();
                _timer = 0;
            }
        }

        public void Initialize(AntiCheatConfig config)
        {
            _config = config;
            _lastZAngle = transform.rotation.eulerAngles.z;
            _currentSuspicion = 0;
        }

        public void OnCheatDetected(AntiCheatReport report)
        {
            // Другой модуль обнаружил нарушение - можно синхронизировать состояние
            if (report.CheatType != CheatType.RotationHack)
                return;

            // Например, очистить подозрение при обнаружении других типов нарушений
            _currentSuspicion = 0;
        }

        public void Shutdown()
        {
            // Очистка ресурсов
        }

        private void ValidateRotation()
        {
            float currentZAngle = transform.rotation.eulerAngles.z;
            float deltaAngle = Mathf.Abs(Mathf.DeltaAngle(currentZAngle, _lastZAngle));
            float elapsed = Mathf.Max(_timer, 0.0001f);
            float observedRotationSpeed = deltaAngle / elapsed;

            if (observedRotationSpeed > _config.MaxRotationSpeed)
            {
                _currentSuspicion += 1;

                if (logLocalViolations)
                    Debug.LogWarning($"[RotationHack] Violation: {observedRotationSpeed:F1} > {_config.MaxRotationSpeed:F1} (score {_currentSuspicion}/{_config.RotationSuspicionThreshold})");

                if (_currentSuspicion >= _config.RotationSuspicionThreshold)
                {
                    var report = new AntiCheatReport
                    {
                        ModuleId = ModuleId,
                        ModuleName = ModuleName,
                        CheatType = CheatType.RotationHack,
                        SeverityLevel = 7,
                        TargetObject = gameObject,
                        Message = $"Обнаружено ускоренное вращение: {observedRotationSpeed:F1}°/с > {_config.MaxRotationSpeed:F1}°/с",
                        AdditionalData = $"DeltaAngle: {deltaAngle:F2}, From: {_lastZAngle:F2}°, To: {currentZAngle:F2}°"
                    };

                    var legacyReport = new RotationHackReport
                    {
                        ObservedDegPerSecond = observedRotationSpeed,
                        ThresholdDegPerSecond = _config.MaxRotationSpeed,
                        DeltaDegrees = deltaAngle,
                        ElapsedSeconds = elapsed,
                        FromDegrees = _lastZAngle,
                        ToDegrees = currentZAngle
                    };

                    Detected?.Invoke(legacyReport);
                    AntiCheatManager.Instance.ReportCheatDetection(report);
                }
            }
            else
            {
                _currentSuspicion = Mathf.Max(0, _currentSuspicion - 1);
            }

            _lastZAngle = currentZAngle;
        }
    }
}