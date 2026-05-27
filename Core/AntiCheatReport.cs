using System;
using UnityEngine;

namespace Estate2D.AntiCheat.Core
{
    [Serializable]
    public class AntiCheatReport
    {
        public string ModuleId { get; set; }
        public string ModuleName { get; set; }
        public CheatType CheatType { get; set; }
        public int SeverityLevel { get; set; }
        public GameObject TargetObject { get; set; }
        public string Message { get; set; }
        public DateTime DetectionTime { get; set; }
        public string AdditionalData { get; set; }

        public AntiCheatReport()
        {
            DetectionTime = DateTime.UtcNow;
            SeverityLevel = 5;
        }

        public override string ToString()
        {
            return $"[{CheatType}] {ModuleName}: {Message} ({SeverityLevel}/10)";
        }
    }

    public enum CheatType
    {
        SpeedHack,
        RotationHack,
        MemoryModification,
        TimeManipulation,
        Unknown
    }
}