using UnityEngine;
using Estate2D.AntiCheat.Core;

namespace Estate2D.AntiCheat
{
    public class RotationHackObserver : MonoBehaviour, IAntiCheatModule
    {
        [SerializeField]
        private bool autoRegister = true;

        [SerializeField]
        private bool logLocalViolations = false;

        private AntiCheatConfig _config;
        private Quaternion _lastRotation;
        private float _timer;
        private int _currentSuspicion;
        private bool _isEnabled = true;

        public string ModuleId => "rotation_hack_observer";
        public string ModuleName => "Rotation Hack Observer";

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        private void Start()
        {
            _lastRotation = transform.rotation;

            if (autoRegister && AntiCheatManager.Instance != null)
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
                _timer -= _config.RotationCheckInterval;
            }
        }

        public void Initialize(AntiCheatConfig config)
        {
            _config = config;
            _lastRotation = transform.rotation;
            _currentSuspicion = 0;
            _timer = 0f;
        }

        public void OnCheatDetected(AntiCheatReport report)
        {
            if (report.CheatType == CheatType.RotationHack)
                return;

            _currentSuspicion = 0;
        }

        public void Shutdown()
        {
        }

        private void ValidateRotation()
        {
            Quaternion currentRotation = transform.rotation;

            float deltaAngle = Quaternion.Angle(_lastRotation, currentRotation);
            float elapsed = Mathf.Max(_timer, 0.0001f);
            float observedRotationSpeed = deltaAngle / elapsed;

            if (observedRotationSpeed > _config.MaxRotationSpeed)
            {
                _currentSuspicion++;

                if (logLocalViolations)
                {
                    Debug.LogWarning($"[RotationHack] Нарушение: {observedRotationSpeed:F1}°/с > {_config.MaxRotationSpeed:F1}°/с (индекс {_currentSuspicion}/{_config.RotationSuspicionThreshold})");
                }

                if (_currentSuspicion >= _config.RotationSuspicionThreshold)
                {
                    var report = new AntiCheatReport
                    {
                        ModuleId = ModuleId,
                        ModuleName = ModuleName,
                        CheatType = CheatType.RotationHack,
                        SeverityLevel = 7,
                        TargetObject = gameObject,
                        Message = $"Обнаружено аномальное вращение: {observedRotationSpeed:F1}°/с > {_config.MaxRotationSpeed:F1}°/с",
                        AdditionalData = $"Delta угла: {deltaAngle:F2}°, рассчитанное за {elapsed:F4}с"
                    };

                    AntiCheatManager.Instance.ReportCheatDetection(report);
                }
            }
            else
            {
                _currentSuspicion = Mathf.Max(0, _currentSuspicion - 1);
            }

            _lastRotation = currentRotation;
        }
    }
}