using UnityEngine;

namespace Estate2D.AntiCheat.Core
{
    [CreateAssetMenu(menuName = "AntiCheat/Config", fileName = "AntiCheatConfig")]
    public class AntiCheatConfig : ScriptableObject
    {
        [Header("Global & Response Settings")]
        [SerializeField]
        private bool enabled = true;
        public bool Enabled => enabled;

        [SerializeField]
        private bool showDetectionDialog = true;
        public bool ShowDetectionDialog => showDetectionDialog;

        [SerializeField]
        private bool pauseGameOnDetection = true;
        public bool PauseGameOnDetection => pauseGameOnDetection;

        [SerializeField]
        private bool quitGameOnDetection = true;
        public bool QuitGameOnDetection => quitGameOnDetection;

        [Header("Speed Hack Detection")]
        [SerializeField]
        private float maxAllowedSpeed = 10f;
        public float MaxAllowedSpeed => maxAllowedSpeed;

        [SerializeField]
        [Min(0.02f)]
        private float speedCheckInterval = 0.2f;
        public float SpeedCheckInterval => speedCheckInterval;

        [SerializeField]
        [Min(1f)]
        private float speedToleranceMultiplier = 1.1f;
        public float SpeedToleranceMultiplier => speedToleranceMultiplier;

        [SerializeField]
        [Min(1)]
        private int speedSuspicionThreshold = 3;
        public int SpeedSuspicionThreshold => speedSuspicionThreshold;

        [Header("Rotation Hack Detection")]
        [SerializeField]
        private float maxRotationSpeed = 200f;
        public float MaxRotationSpeed => maxRotationSpeed;

        [SerializeField]
        [Min(0.02f)]
        private float rotationCheckInterval = 0.1f;
        public float RotationCheckInterval => rotationCheckInterval;

        [SerializeField]
        [Min(1)]
        private int rotationSuspicionThreshold = 3;
        public int RotationSuspicionThreshold => rotationSuspicionThreshold;

        [Header("Time Sync Detection")]
        [SerializeField]
        private string timeServerUrl = "https://timeapi.io/api/Time/current/zone?timeZone=Etc/UTC";
        public string TimeServerUrl => timeServerUrl;

        [SerializeField]
        [Min(0f)]
        private float timeSyncToleranceSeconds = 5f;
        public float TimeSyncToleranceSeconds => timeSyncToleranceSeconds;

        [SerializeField]
        [Min(1f)]
        private float timeSyncCheckInterval = 60f;
        public float TimeSyncCheckInterval => timeSyncCheckInterval;

        [SerializeField]
        [Min(1)]
        private int timeSyncSuspicionThreshold = 2;
        public int TimeSyncSuspicionThreshold => timeSyncSuspicionThreshold;

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

            if (timeSyncToleranceSeconds < 0)
                Debug.LogWarning("[AntiCheat] TimeSyncToleranceSeconds должна быть >= 0");

            if (timeSyncCheckInterval < 1f)
                Debug.LogWarning("[AntiCheat] TimeSyncCheckInterval должна быть >= 1");

            if (timeSyncSuspicionThreshold < 1)
                Debug.LogWarning("[AntiCheat] TimeSyncSuspicionThreshold должна быть >= 1");
        }

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