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
        private ResponseType defaultResponseType = ResponseType.Warning;
        public ResponseType DefaultResponseType => defaultResponseType;

        [SerializeField]
        private bool quitGameOnDetection = false;
        public bool QuitGameOnDetection => quitGameOnDetection;

        [SerializeField]
        private bool sendReportsToServer = false;
        public bool SendReportsToServer => sendReportsToServer;

        [SerializeField]
        private string serverReportUrl = "";
        public string ServerReportUrl => serverReportUrl;

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
        }

        /// <summary>
        /// Получить ответ на обнаружение в зависимости от типа нарушения.
        /// </summary>
        public AntiCheatResponse GetResponseForCheatType(CheatType cheatType)
        {
            var response = new AntiCheatResponse { Type = defaultResponseType };

            switch (cheatType)
            {
                case CheatType.SpeedHack:
                    response.UserMessage = "Обнаружено ускорение движения. Это может привести к блокировке.";
                    break;
                case CheatType.RotationHack:
                    response.UserMessage = "Обнаружено аномальное вращение. Это может привести к блокировке.";
                    break;
                case CheatType.TimeManipulation:
                    response.UserMessage = "Время устройства не совпадает с серверным временем. Это может указывать на попытку обмана.";
                    break;
                case CheatType.PositionTeleport:
                    response.UserMessage = "Обнаружена попытка телепортации. Аккаунт может быть заблокирован.";
                    response.BanPlayer = true;
                    break;
                case CheatType.MemoryModification:
                    response.UserMessage = "Обнаружено изменение памяти. Ваш аккаунт может быть заблокирован.";
                    response.BanPlayer = true;
                    break;
                default:
                    response.UserMessage = "Обнаружено подозрительное поведение.";
                    break;
            }

            return response;
        }
    }
}
