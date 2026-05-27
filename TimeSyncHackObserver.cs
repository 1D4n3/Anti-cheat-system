using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Estate2D.AntiCheat.Core;

namespace Estate2D.AntiCheat
{
    public class TimeSyncHackObserver : MonoBehaviour, IAntiCheatModule
    {
        [SerializeField]
        private bool autoRegister = true;

        [SerializeField]
        private bool logLocalViolations = true;

        private AntiCheatConfig _config;
        private float _timer;
        private int _currentSuspicion;
        private bool _isEnabled = true;
        private bool _requestInProgress;

        public string ModuleId => "time_sync_observer";
        public string ModuleName => "Time Sync Observer";

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        private void Start()
        {
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
            if (_timer < _config.TimeSyncCheckInterval || _requestInProgress)
                return;

            _timer -= _config.TimeSyncCheckInterval;
            StartCoroutine(ValidateDeviceTime());
        }

        public void Initialize(AntiCheatConfig config)
        {
            _config = config;
            _timer = _config.TimeSyncCheckInterval;
            _currentSuspicion = 0;
            _requestInProgress = false;
        }

        public void OnCheatDetected(AntiCheatReport report)
        {
            if (report.CheatType != CheatType.TimeManipulation)
                return;

            _currentSuspicion = 0;
        }

        public void Shutdown()
        {
            StopAllCoroutines();
        }

        private IEnumerator ValidateDeviceTime()
        {
            if (string.IsNullOrEmpty(_config.TimeServerUrl))
            {
                if (logLocalViolations)
                    Debug.LogWarning("[TimeSyncHackObserver] URL-адрес сервера времени не настроен.");
                yield break;
            }

            _requestInProgress = true;
            using var request = UnityWebRequest.Get(_config.TimeServerUrl);
            request.timeout = 10;
            yield return request.SendWebRequest();
            _requestInProgress = false;

            if (logLocalViolations)
                Debug.Log($"[TimeSyncHackObserver] Запрос времени к серверу выполнен.");

            if (request.result != UnityWebRequest.Result.Success)
            {
                var error = $"Не удалось получить серверное время: {request.error}";
                AntiCheatManager.Instance.UpdateTimeSyncAttemptStatus(error);
                if (logLocalViolations)
                    Debug.LogWarning($"[TimeSyncHackObserver] {error}");
                yield break;
            }

            if (!TryParseServerTime(request.downloadHandler.text, out var serverTimeUtc))
            {
                var error = "Не удалось разобрать ответ сервера времени.";
                AntiCheatManager.Instance.UpdateTimeSyncAttemptStatus(error);
                if (logLocalViolations)
                    Debug.LogWarning($"[TimeSyncHackObserver] {error}");
                yield break;
            }

            var deviceUtc = DateTime.UtcNow;
            var diff = Math.Abs((deviceUtc - serverTimeUtc).TotalSeconds);

            AntiCheatManager.Instance.UpdateTimeSyncStatus(deviceUtc, serverTimeUtc, (float)diff);

            if (logLocalViolations)
                Debug.Log($"[TimeSyncHackObserver] Время устройства: {deviceUtc:o}, время сервера: {serverTimeUtc:o}, разница: {diff:F2}с");

            if (diff > _config.TimeSyncToleranceSeconds)
            {
                _currentSuspicion++;
                if (logLocalViolations)
                    Debug.LogWarning($"[TimeSyncHackObserver] Нарушение синхронизации времени: {diff:F2}с > {_config.TimeSyncToleranceSeconds:F2}с (индекс {_currentSuspicion}/{_config.TimeSyncSuspicionThreshold})");

                if (_currentSuspicion >= _config.TimeSyncSuspicionThreshold)
                {
                    var report = new AntiCheatReport
                    {
                        ModuleId = ModuleId,
                        ModuleName = ModuleName,
                        CheatType = CheatType.TimeManipulation,
                        SeverityLevel = 8,
                        TargetObject = gameObject,
                        Message = $"Время на устройстве отличается от серверного на {diff:F2} секунд.",
                        AdditionalData = $"Время устройства: {deviceUtc:o}, время сервера: {serverTimeUtc:o}, допустимая разница: {_config.TimeSyncToleranceSeconds:F1}с"
                    };

                    AntiCheatManager.Instance.ReportCheatDetection(report);
                }
            }
            else
            {
                _currentSuspicion = Mathf.Max(0, _currentSuspicion - 1);
            }
        }

        private bool TryParseServerTime(string responseBody, out DateTime serverTimeUtc)
        {
            serverTimeUtc = default;

            if (string.IsNullOrEmpty(responseBody))
                return false;

            try
            {
                var dateTimeKey = "\"dateTime\":\"";
                var index = responseBody.IndexOf(dateTimeKey, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    index += dateTimeKey.Length;
                    var end = responseBody.IndexOf('"', index);
                    if (end > index)
                    {
                        var dateTimeString = responseBody.Substring(index, end - index);
                        if (DateTime.TryParse(dateTimeString, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out serverTimeUtc))
                            return true;
                    }
                }

                if (DateTime.TryParse(responseBody.Trim(), null, System.Globalization.DateTimeStyles.AdjustToUniversal, out serverTimeUtc))
                    return true;
            }
            catch (Exception)
            {
            }

            return false;
        }
    }
}