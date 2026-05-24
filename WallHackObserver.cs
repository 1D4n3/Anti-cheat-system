using System.Collections.Generic;
using UnityEngine;
using Estate2D.AntiCheat.Core;

namespace Estate2D.AntiCheat
{
    /// <summary>
    /// Наблюдатель для обнаружения попыток WallHack.
    /// Проверяет, находится ли цель в зоне прицеливания, но скрыта препятствием.
    /// </summary>
    public class WallHackObserver : MonoBehaviour, IAntiCheatModule
    {
        [SerializeField]
        private bool autoRegister = true;

        [SerializeField]
        private bool logLocalViolations = true;

        [SerializeField]
        [Tooltip("Камера игрока, используемая для вычисления направления прицеливания.")]
        private Camera playerCamera;

        [SerializeField]
        [Tooltip("Точка, из которой проверяется линия видимости.")]
        private Transform aimOrigin;

        [SerializeField]
        [Tooltip("Цели для проверки. Если не указаны, используется GameObject.FindGameObjectsWithTag(targetTag).")]
        private Transform[] trackedTargets;

        [SerializeField]
        [Tooltip("Тег для поиска целей, если trackedTargets пуст.")]
        private string targetTag = "Enemy";

        private AntiCheatConfig _config;
        private float _timer;
        private int _currentSuspicion;
        private bool _initialized;
        private bool _isEnabled = true;

        public string ModuleId => "wallhack_observer";
        public string ModuleName => "WallHack Observer";
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
            if (!_initialized || _config == null)
                return;

            _timer += Time.unscaledDeltaTime;
            if (_timer < _config.WallHackCheckInterval)
                return;

            _timer = 0f;
            CheckWallHack();
        }

        public void Initialize(AntiCheatConfig config)
        {
            _config = config;
            _timer = _config.WallHackCheckInterval;
            _currentSuspicion = 0;
            _initialized = true;

            if (playerCamera == null)
                playerCamera = Camera.main;

            if (aimOrigin == null)
                aimOrigin = transform;

            if (logLocalViolations)
                Debug.Log($"[WallHackObserver] Initialized with targetTag='{targetTag}' and aimOrigin={aimOrigin.name}");
        }

        public void OnCheatDetected(AntiCheatReport report)
        {
            if (report.CheatType != CheatType.WallHack)
                return;

            _currentSuspicion = 0;
        }

        public void Shutdown()
        {
            _initialized = false;
        }

        private void CheckWallHack()
        {
            if (playerCamera == null || aimOrigin == null)
                return;

            var mouseWorld = playerCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = aimOrigin.position.z;
            var aimDirection = (mouseWorld - aimOrigin.position).normalized;

            var targets = GetTargets();
            if (targets == null || targets.Count == 0)
                return;

            foreach (var target in targets)
            {
                if (target == null)
                    continue;

                var toTarget = target.position - aimOrigin.position;
                var distance = toTarget.magnitude;
                if (distance <= 0.001f || distance > _config.MaxWallHackCheckDistance)
                    continue;

                if (Vector2.Angle(aimDirection, toTarget.normalized) > _config.WallHackAimAngleDegrees)
                    continue;

                var hit = Physics2D.Raycast(aimOrigin.position, toTarget.normalized, distance, _config.WallHackOcclusionMask);
                if (hit.collider != null)
                {
                    _currentSuspicion++;
                    if (logLocalViolations)
                    {
                        Debug.LogWarning($"[WallHackObserver] Detected occluded aim at target '{target.name}' ({distance:F1}m) through layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}. Score {_currentSuspicion}/{_config.WallHackSuspicionThreshold}");
                    }

                    if (_currentSuspicion >= _config.WallHackSuspicionThreshold)
                    {
                        var report = new AntiCheatReport
                        {
                            ModuleId = ModuleId,
                            ModuleName = ModuleName,
                            CheatType = CheatType.WallHack,
                            SeverityLevel = 9,
                            TargetObject = gameObject,
                            Message = $"Обнаружено прицеливание через препятствие к цели {target.name}.",
                            AdditionalData = $"Distance={distance:F2}m, Obstacle={hit.collider.name}, AimAngle={Vector2.Angle(aimDirection, toTarget.normalized):F1}°"
                        };

                        AntiCheatManager.Instance.ReportCheatDetection(report);
                    }
                }
                else
                {
                    _currentSuspicion = Mathf.Max(0, _currentSuspicion - 1);
                }
            }
        }

        private List<Transform> GetTargets()
        {
            var result = new List<Transform>();
            if (trackedTargets != null && trackedTargets.Length > 0)
            {
                result.AddRange(trackedTargets);
            }
            else if (!string.IsNullOrEmpty(targetTag))
            {
                foreach (var obj in GameObject.FindGameObjectsWithTag(targetTag))
                {
                    if (obj != null)
                        result.Add(obj.transform);
                }
            }

            return result;
        }
    }
}
