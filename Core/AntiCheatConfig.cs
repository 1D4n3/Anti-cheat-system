using UnityEngine;

namespace Estate2D.AntiCheat.Core
{
    /// <summary>
    /// Конфигурация системы античита. Используется как ScriptableObject для удобной редакции в Unity Editor.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiCheat/Config", fileName = "AntiCheatConfig")]
    public class AntiCheatConfig : ScriptableObject
    {
        [Header("Global Settings")]
        [SerializeField]
        private bool enabled = true;
        public bool Enabled => enabled;

        [SerializeField]
        private bool debugMode = false;
        public bool DebugMode => debugMode;

        [SerializeField]
        private bool logDetections = true;
        public bool LogDetections => logDetections;

        [Header("Speed Hack Detection")]
        [SerializeField]
        [Tooltip("Максимально допустимая скорость (единицы/сек)")]
        private float maxAllowedSpeed = 10f;
        public float MaxAllowedSpeed => maxAllowedSpeed;

        [SerializeField]
        [Tooltip("Как часто проверять скорость (сек)")]
        [Min(0.02f)]
        private float speedCheckInterval = 0.2f;
        public float SpeedCheckInterval => speedCheckInterval;

        [SerializeField]
        [Tooltip("Допуск для скачков (1.1 = +10%)")]
        [Min(1f)]
        private float speedToleranceMultiplier = 1.1f;
        public float SpeedToleranceMultiplier => speedToleranceMultiplier;

        [SerializeField]
        [Tooltip("Количество нарушений для срабатывания")]
        [Min(1)]
        private int speedSuspicionThreshold = 3;
        public int SpeedSuspicionThreshold => speedSuspicionThreshold;

        [Header("Rotation Hack Detection")]
        [SerializeField]
        [Tooltip("Максимально допустимая скорость вращения (гр/сек)")]
        private float maxRotationSpeed = 200f;
        public float MaxRotationSpeed => maxRotationSpeed;

        [SerializeField]
        [Tooltip("Как часто проверять вращение (сек)")]
        [Min(0.02f)]
        private float rotationCheckInterval = 0.1f;
        public float RotationCheckInterval => rotationCheckInterval;

        [SerializeField]
        [Tooltip("Количество нарушений для срабатывания")]
        [Min(1)]
        private int rotationSuspicionThreshold = 3;
        public int RotationSuspicionThreshold => rotationSuspicionThreshold;

        [Header("Teleport Detection")]
        [SerializeField]
        [Tooltip("Максимальное допустимое расстояние прыжка за один кадр (м)")]
        [Min(0.1f)]
        private float maxAllowedTeleportDistance = 50f;
        public float MaxAllowedTeleportDistance => maxAllowedTeleportDistance;

        [SerializeField]
        [Tooltip("Количество подозрений для срабатывания")]
        [Min(1)]
        private int teleportSuspicionThreshold = 2;
        public int TeleportSuspicionThreshold => teleportSuspicionThreshold;

        [Header("Wall Hack Detection")]
        [SerializeField]
        [Tooltip("Слои, считающиеся препятствиями для обзора")]
        private LayerMask wallHackOcclusionMask = ~0;
        public LayerMask WallHackOcclusionMask => wallHackOcclusionMask;

        [SerializeField]
        [Tooltip("Максимальный угол прицеливания к цели для проверки (градусы)")]
        [Min(1f)]
        private float wallHackAimAngleDegrees = 12f;
        public float WallHackAimAngleDegrees => wallHackAimAngleDegrees;

        [SerializeField]
        [Tooltip("Максимальная дистанция проверки возможного прицела через стену (м)")]
        [Min(1f)]
        private float maxWallHackCheckDistance = 20f;
        public float MaxWallHackCheckDistance => maxWallHackCheckDistance;

        [SerializeField]
        [Tooltip("Интервал проверки WallHack (сек)")]
        [Min(0.02f)]
        private float wallHackCheckInterval = 0.2f;
        public float WallHackCheckInterval => wallHackCheckInterval;

        [SerializeField]
        [Tooltip("Количество подозрений для срабатывания WallHack")]
        [Min(1)]
        private int wallHackSuspicionThreshold = 3;
        public int WallHackSuspicionThreshold => wallHackSuspicionThreshold;

        [Header("Time Sync Detection")]
        [SerializeField]
        [Tooltip("URL сервера времени для сверки устройства")]
        private string timeServerUrl = "https://timeapi.io/api/Time/current/zone?timeZone=Etc/UTC";
        public string TimeServerUrl => timeServerUrl;

        [SerializeField]
        [Tooltip("Максимальное допустимое расхождение времени в секундах")]
        [Min(0f)]
        private float timeSyncToleranceSeconds = 5f;
        public float TimeSyncToleranceSeconds => timeSyncToleranceSeconds;

        [SerializeField]
        [Tooltip("Интервал проверки времени (сек)")]
        [Min(1f)]
        private float timeSyncCheckInterval = 60f;
        public float TimeSyncCheckInterval => timeSyncCheckInterval;

        [SerializeField]
        [Tooltip("Количество последовательных ошибок синхронизации для срабатывания")]
        [Min(1)]
        private int timeSyncSuspicionThreshold = 2;
        public int TimeSyncSuspicionThreshold => timeSyncSuspicionThreshold;

        [Header("Response Settings")]
        [SerializeField]
        [Tooltip("Показывать ли диалог при обнаружении читерства")]
        private bool showDetectionDialog = true;
        public bool ShowDetectionDialog => showDetectionDialog;

        [SerializeField]
        [Tooltip("Остановить ли игру при обнаружении читерства")]
        private bool pauseGameOnDetection = true;
        public bool PauseGameOnDetection => pauseGameOnDetection;

        [SerializeField]
        [Tooltip("Закрыть ли игру при обнаружении читерства (с диалогом)")]
        private bool quitGameOnDetection = true;
        public bool QuitGameOnDetection => quitGameOnDetection;

        /// <summary>
        /// Валидирует конфигурацию и логирует предупреждения.
        /// </summary>
        public void Validate()
        {
            if (maxAllowedSpeed <= 0)
                Debug.LogWarning("[AntiCheat] MaxAllowedSpeed должна быть > 0");

            if (speedCheckInterval < 0.02f)
                Debug.LogWarning("[AntiCheat] SpeedCheckInterval слишком мал (<0.02)");

            if (maxRotationSpeed <= 0)
                Debug.LogWarning("[AntiCheat] MaxRotationSpeed должна быть > 0");

            if (speedSuspicionThreshold < 1)
                Debug.LogWarning("[AntiCheat] SpeedSuspicionThreshold должна быть >= 1");

            if (maxAllowedTeleportDistance <= 0)
                Debug.LogWarning("[AntiCheat] MaxAllowedTeleportDistance должна быть > 0");

            if (teleportSuspicionThreshold < 1)
                Debug.LogWarning("[AntiCheat] TeleportSuspicionThreshold должна быть >= 1");

            if (wallHackAimAngleDegrees <= 0)
                Debug.LogWarning("[AntiCheat] WallHackAimAngleDegrees должна быть > 0");

            if (maxWallHackCheckDistance <= 0)
                Debug.LogWarning("[AntiCheat] MaxWallHackCheckDistance должна быть > 0");

            if (wallHackCheckInterval < 0.02f)
                Debug.LogWarning("[AntiCheat] WallHackCheckInterval слишком мал (<0.02)");

            if (wallHackSuspicionThreshold < 1)
                Debug.LogWarning("[AntiCheat] WallHackSuspicionThreshold должна быть >= 1");

            if (timeSyncToleranceSeconds < 0)
                Debug.LogWarning("[AntiCheat] TimeSyncToleranceSeconds должна быть >= 0");

            if (timeSyncCheckInterval < 1f)
                Debug.LogWarning("[AntiCheat] TimeSyncCheckInterval должна быть >= 1");

            if (timeSyncSuspicionThreshold < 1)
                Debug.LogWarning("[AntiCheat] TimeSyncSuspicionThreshold должна быть >= 1");
        }

        /// <summary>
        /// Получить ответ на обнаружение в зависимости от типа нарушения.
        /// </summary>
        public AntiCheatResponse GetResponseForCheatType(CheatType cheatType)
        {
            var response = new AntiCheatResponse();

            switch (cheatType)
            {
                case CheatType.SpeedHack:
                    response.UserMessage = "Обнаружено ускорение движения.";
                    break;
                case CheatType.RotationHack:
                    response.UserMessage = "Обнаружено аномальное вращение.";
                    break;
                case CheatType.TimeManipulation:
                    response.UserMessage = "Время устройства не совпадает с серверным временем.";
                    break;
                case CheatType.PositionTeleport:
                    response.UserMessage = "Обнаружена попытка телепортации.";
                    break;
                case CheatType.WallHack:
                    response.UserMessage = "Обнаружена попытка прицеливания через препятствия.";
                    break;
                case CheatType.MemoryModification:
                    response.UserMessage = "Обнаружено изменение памяти.";
                    break;
                default:
                    response.UserMessage = "Обнаружено подозрительное поведение.";
                    break;
            }

            return response;
        }
    }
}
