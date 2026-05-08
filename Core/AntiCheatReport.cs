using System;
using UnityEngine;

namespace Estate2D.AntiCheat.Core
{
    /// <summary>
    /// Универсальная структура для сообщения об обнаруженном читерстве.
    /// </summary>
    [Serializable]
    public class AntiCheatReport
    {
        /// <summary>
        /// Идентификатор модуля, обнаружившего нарушение.
        /// </summary>
        public string ModuleId { get; set; }

        /// <summary>
        /// Имя модуля (человекочитаемое).
        /// </summary>
        public string ModuleName { get; set; }

        /// <summary>
        /// Тип обнаруженного нарушения.
        /// </summary>
        public CheatType CheatType { get; set; }

        /// <summary>
        /// Уровень серьезности нарушения (1-10).
        /// </summary>
        public int SeverityLevel { get; set; }

        /// <summary>
        /// Объект GameMObject, на котором обнаружено нарушение.
        /// </summary>
        public GameObject TargetObject { get; set; }

        /// <summary>
        /// Сообщение об ошибке/детали.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Время, когда было обнаружено нарушение.
        /// </summary>
        public DateTime DetectionTime { get; set; }

        /// <summary>
        /// Дополнительные данные в виде JSON или строки.
        /// </summary>
        public string AdditionalData { get; set; }

        public AntiCheatReport()
        {
            DetectionTime = DateTime.UtcNow;
            SeverityLevel = 5;
        }

        public override string ToString()
        {
            return $"[{CheatType}] {ModuleName}: {Message} (Severity: {SeverityLevel}/10)";
        }
    }

    /// <summary>
    /// Типы обнаруживаемых нарушений.
    /// </summary>
    public enum CheatType
    {
        SpeedHack,
        RotationHack,
        MemoryModification,
        PositionTeleport,
        InfiniteResources,
        ClientSideModification,
        Unknown
    }
}
