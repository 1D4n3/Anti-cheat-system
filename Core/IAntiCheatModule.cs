using UnityEngine;

namespace Estate2D.AntiCheat.Core
{
    public interface IAntiCheatModule
    {
        string ModuleId { get; }

        string ModuleName { get; }

        bool IsEnabled { get; set; }

        void Initialize(AntiCheatConfig config);

        void OnCheatDetected(AntiCheatReport report);

        void Shutdown();
    }
}
