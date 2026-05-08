using UnityEngine;
using Estate2D.AntiCheat.Core;

namespace Estate2D.AntiCheat
{
    /// <summary>
    /// Наблюдатель за скоростью движения персонажа.
    /// Обнаруживает попытки разгона (SpeedHack) путем анализа скорости перемещения.
    /// </summary>
    public class SpeedHackObserver : MonoBehaviour, IAntiCheatModule
    {
        public struct SpeedHackReport
        {
            public float ObservedSpeed;
            public float ThresholdSpeed;
            public float Distance;
            public float ElapsedSeconds;
            public Vector2 From;
            public Vector2 To;
        }

        [SerializeField]
        private bool autoRegister = true;

        [SerializeField]
        private bool logLocalViolations = false;

        public event System.Action<SpeedHackReport> Detected;

        private AntiCheatConfig _config;
        private Vector2 _lastPosition;
        private float _timer;
        private int _currentSuspicion;
        private bool _isEnabled = true;

        // IAntiCheatModule
        public string ModuleId => "speed_hack_observer";
        public string ModuleName => "Speed Hack Observer";
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        private void Start()
        {
            _lastPosition = transform.position;

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

            if (_timer >= _config.SpeedCheckInterval)
            {
                ValidateSpeed();
                _timer = 0;
            }
        }

        public void Initialize(AntiCheatConfig config)
        {
            _config = config;
            _lastPosition = transform.position;
            _currentSuspicion = 0;
        }

        public void OnCheatDetected(AntiCheatReport report)
        {
            // Другой модуль обнаружил нарушение - можно синхронизировать состояние
            if (report.CheatType != CheatType.SpeedHack)
                return;

            // Например, очистить подозрение при обнаружении других типов нарушений
            _currentSuspicion = 0;
        }

        public void Shutdown()
        {
            // Очистка ресурсов
        }

        private void ValidateSpeed()
        {
            float elapsed = Mathf.Max(_timer, 0.0001f);

            Vector2 currentPosition = transform.position;
            float distance = Vector2.Distance(currentPosition, _lastPosition);
            float observedSpeed = distance / elapsed;
            float threshold = _config.MaxAllowedSpeed * _config.SpeedToleranceMultiplier;

            if (observedSpeed > threshold)
            {
                _currentSuspicion += 1;

                if (logLocalViolations)
                    Debug.LogWarning($"[SpeedHack] Violation: {observedSpeed:F2} > {threshold:F2} (score {_currentSuspicion}/{_config.SpeedSuspicionThreshold})");

                if (_currentSuspicion >= _config.SpeedSuspicionThreshold)
                {
                    var report = new AntiCheatReport
                    {
                        ModuleId = ModuleId,
                        ModuleName = ModuleName,
                        CheatType = CheatType.SpeedHack,
                        SeverityLevel = 6,
                        TargetObject = gameObject,
                        Message = $"Обнаружено ускорение: {observedSpeed:F2} м/с > {threshold:F2} м/с",
                        AdditionalData = $"Distance: {distance:F2}, From: {_lastPosition}, To: {currentPosition}"
                    };

                    var legacyReport = new SpeedHackReport
                    {
                        ObservedSpeed = observedSpeed,
                        ThresholdSpeed = threshold,
                        Distance = distance,
                        ElapsedSeconds = elapsed,
                        From = _lastPosition,
                        To = currentPosition
                    };

                    Detected?.Invoke(legacyReport);
                    AntiCheatManager.Instance.ReportCheatDetection(report);
                }
            }
            else
            {
                _currentSuspicion = Mathf.Max(0, _currentSuspicion - 1);
            }

            _lastPosition = currentPosition;
        }
    }
}