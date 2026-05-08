using UnityEngine;

namespace Estate2D.AntiCheat.Core
{
    /// <summary>
    /// Базовый интерфейс для всех модулей системы античита.
    /// Каждый модуль должен следить за определённым типом читерства.
    /// </summary>
    public interface IAntiCheatModule
    {
        /// <summary>
        /// Уникальный идентификатор модуля.
        /// </summary>
        string ModuleId { get; }

        /// <summary>
        /// Понятное имя модуля для логирования и отчётов.
        /// </summary>
        string ModuleName { get; }

        /// <summary>
        /// Включен ли модуль в данный момент.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Инициализирует модуль с конфигурацией.
        /// </summary>
        void Initialize(AntiCheatConfig config);

        /// <summary>
        /// Вызывается при обнаружении попытки читерства.
        /// </summary>
        void OnCheatDetected(AntiCheatReport report);

        /// <summary>
        /// Очищает модуль и освобождает ресурсы.
        /// </summary>
        void Shutdown();
    }
}
