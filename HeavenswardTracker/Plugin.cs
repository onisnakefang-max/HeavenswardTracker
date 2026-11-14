using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HeavenswardTracker.Windows;
using HeavenswardTracker.Integration;
using HeavenswardTracker.Data;

namespace HeavenswardTracker;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/hwtracker";
    private readonly WindowSystem windowSystem = new("HeavenswardTracker");
    private readonly MainWindow mainWindow;
    private readonly TsukimichiIpc tsukimichi;

    [PluginService] internal static ICommandManager Commands { get; private set; } = null!;
    [PluginService] internal static IDataManager Data { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    public string Name => "Heavensward Tracker";

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Plugin>();
        var configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        configuration.Initialize(pluginInterface);
        tsukimichi = new TsukimichiIpc(pluginInterface);
        var catalog = HeavenswardCatalog.Load(Data);
        mainWindow = new MainWindow(configuration, tsukimichi, catalog);
        windowSystem.AddWindow(mainWindow);
        Commands.AddHandler(CommandName, new CommandInfo((_, _) => mainWindow.IsOpen = true)
        {
            HelpMessage = "Открыть список заданий Heavensward",
        });
        pluginInterface.UiBuilder.Draw += windowSystem.Draw;
        pluginInterface.UiBuilder.OpenMainUi += Toggle;
        pluginInterface.UiBuilder.OpenConfigUi += Toggle;
    }

    private void Toggle() => mainWindow.IsOpen = !mainWindow.IsOpen;

    public void Dispose()
    {
        Commands.RemoveHandler(CommandName);
        tsukimichi.Dispose();
        mainWindow.Dispose();
        windowSystem.RemoveAllWindows();
    }
}
