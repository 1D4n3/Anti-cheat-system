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
        [SerializeField]
        private bool autoRegister = true;

        [SerializeField]
        private bool logLocalViolations = false;

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
            if (report.CheatType == CheatType.SpeedHack)
                return;

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
                {
                    Debug.LogWarning($"[SpeedHack] Violation: {observedSpeed:F2} > {threshold:F2} (score {_currentSuspicion}/{_config.SpeedSuspicionThreshold})");
                }

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