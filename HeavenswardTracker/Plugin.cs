using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HeavenswardTracker.Windows;
using HeavenswardTracker.Integration;

namespace HeavenswardTracker;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/hwtracker";
    private readonly WindowSystem windowSystem = new("HeavenswardTracker");
    private readonly Action openMainUi;
    private readonly Action openConfigUi;
    private readonly IDalamudPluginInterface pluginInterface;
    private MainWindow? mainWindow;
    private TsukimichiIpc? tsukimichi;

    [PluginService] internal static ICommandManager Commands { get; private set; } = null!;
    [PluginService] internal static IDataManager Data { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    public string Name => "Heavensward Tracker";

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        pluginInterface.Create<Plugin>();
        openMainUi = () => OpenTracker();
        openConfigUi = () => OpenTracker();
        Commands.AddHandler(CommandName, new CommandInfo((_, _) => OpenTracker())
        {
            HelpMessage = "Открыть список заданий Heavensward",
        });
        pluginInterface.UiBuilder.Draw += windowSystem.Draw;
        pluginInterface.UiBuilder.OpenMainUi += openMainUi;
        pluginInterface.UiBuilder.OpenConfigUi += openConfigUi;
    }

    private void OpenTracker()
    {
        if (mainWindow == null)
        {
            var ipc = new TsukimichiIpc(pluginInterface);
            tsukimichi = ipc;
            mainWindow = new MainWindow(ipc, Data);
            windowSystem.AddWindow(mainWindow);
        }

        mainWindow.IsOpen = true;
    }

    public void Dispose()
    {
        Commands.RemoveHandler(CommandName);
        pluginInterface.UiBuilder.OpenMainUi -= openMainUi;
        pluginInterface.UiBuilder.OpenConfigUi -= openConfigUi;
        pluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        tsukimichi?.Dispose();
        mainWindow?.Dispose();
        windowSystem.RemoveAllWindows();
    }
}
