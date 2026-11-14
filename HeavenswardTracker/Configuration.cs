using Dalamud.Configuration;
using Dalamud.Plugin;

namespace HeavenswardTracker;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool ShowCompleted { get; set; } = true;
    public void Initialize(IDalamudPluginInterface pluginInterface) => PluginInterface = pluginInterface;
    [NonSerialized] private IDalamudPluginInterface? PluginInterface;
    public void Save() => PluginInterface?.SavePluginConfig(this);
}
