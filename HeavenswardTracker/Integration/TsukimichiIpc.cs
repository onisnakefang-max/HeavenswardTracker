using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;

namespace HeavenswardTracker.Integration;

/// <summary>Reads quest completion from Tsukimichi's documented IPC API.</summary>
public sealed class TsukimichiIpc : IDisposable
{
    private const int SupportedApiVersion = 1;
    private readonly ICallGateSubscriber<int> apiVersion;
    private readonly ICallGateSubscriber<bool> isReady;
    private readonly ICallGateSubscriber<uint, string> getState;
    private readonly ICallGateSubscriber<object> statesChanged;

    public TsukimichiIpc(IDalamudPluginInterface pluginInterface)
    {
        apiVersion = pluginInterface.GetIpcSubscriber<int>("Tsukimichi.ApiVersion");
        isReady = pluginInterface.GetIpcSubscriber<bool>("Tsukimichi.IsReady");
        getState = pluginInterface.GetIpcSubscriber<uint, string>("Tsukimichi.GetState");
        statesChanged = pluginInterface.GetIpcSubscriber<object>("Tsukimichi.StatesChanged");
        statesChanged.Subscribe(OnStatesChanged);
    }

    public event Action? Changed;

    public bool Ready => Try(() => apiVersion.InvokeFunc() == SupportedApiVersion && isReady.InvokeFunc(), false);

    public bool IsCompleted(uint questRowId) =>
        Ready && Try(() => getState.InvokeFunc(questRowId) == "Completed", false);

    public void Dispose() => statesChanged.Unsubscribe(OnStatesChanged);

    private void OnStatesChanged() => Changed?.Invoke();

    private static T Try<T>(Func<T> call, T fallback)
    {
        try { return call(); }
        catch (IpcNotReadyError) { return fallback; }
    }
}
