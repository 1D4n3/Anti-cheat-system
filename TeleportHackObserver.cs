using UnityEngine;
using Estate2D.AntiCheat.Core;

namespace Estate2D.AntiCheat
{
    /// <summary>
    /// Наблюдатель, обнаруживающий телепортацию игрока.
    /// Сравнивает расстояние перемещения между кадрами с максимально допустимым.
    /// </summary>
    public class TeleportHackObserver : MonoBehaviour, IAntiCheatModule
    {
        [SerializeField]
        private bool autoRegister = true;

        [SerializeField]
        private bool logLocalViolations = true;

        [SerializeField]
        private Transform targetTransform;

        private AntiCheatConfig _config;
        private Vector3 _lastPosition;
        private int _currentSuspicion;
        private bool _isEnabled = true;
        private bool _initialized;

        public string ModuleId => "teleport_observer";
        public string ModuleName => "Teleport Observer";
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        private void Start()
        {
            if (autoRegister)
            {
                AntiCheatManager.Instance.RegisterModule(this);
            }
        }

        private void Update()
        {
            if (!_isEnabled || _config == null || !_initialized)
                return;

            if (targetTransform == null)
                return;

            var currentPosition = targetTransform.position;
            var distance = Vector3.Distance(_lastPosition, currentPosition);

            // Проверяем, не произошла ли телепортация
            if (distance > _config.MaxAllowedTeleportDistance)
            {
                _currentSuspicion += 1;

                if (logLocalViolations)
                    Debug.LogWarning(
                        $"[TeleportHackObserver] Suspicious jump detected: {distance:F2}m > {_config.MaxAllowedTeleportDistance:F1}m " +
                        $"(score {_currentSuspicion}/{_config.TeleportSuspicionThreshold})"
                    );

                if (_currentSuspicion >= _config.TeleportSuspicionThreshold)
                {
                    var report = new AntiCheatReport
                    {
                        ModuleId = ModuleId,
                        ModuleName = ModuleName,
                        CheatType = CheatType.PositionTeleport,
                        SeverityLevel = 7,
                        TargetObject = gameObject,
                        Message = $"Обнаружена телепортация: прыжок на {distance:F2}м.",
                        AdditionalData = $"Distance={distance:F2}m, Tolerance={_config.MaxAllowedTeleportDistance:F1}m, " +
                                        $"From={_lastPosition}, To={currentPosition}"
                    };

                    AntiCheatManager.Instance.ReportCheatDetection(report);
                }
            }
            else
            {
                // Снижаем подозрение, если движение нормальное
                _currentSuspicion = Mathf.Max(0, _currentSuspicion - 1);
            }

            _lastPosition = currentPosition;
        }

        public void Initialize(AntiCheatConfig config)
        {
            _config = config;

            // Если трансформа не указана, используем трансформу этого объекта
            if (targetTransform == null)
            {
                targetTransform = transform;
            }

            _lastPosition = targetTransform.position;
            _currentSuspicion = 0;
            _initialized = true;

            if (logLocalViolations)
                Debug.Log($"[TeleportHackObserver] Initialized on {targetTransform.gameObject.name}");
        }

        public void OnCheatDetected(AntiCheatReport report)
        {
            if (report.CheatType != CheatType.PositionTeleport)
                return;

            _currentSuspicion = 0;
        }

        public void Shutdown()
        {
            _initialized = false;
        }
    }
}
