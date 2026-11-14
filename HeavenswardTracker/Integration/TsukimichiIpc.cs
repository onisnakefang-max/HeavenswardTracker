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
    private readonly ICallGateSubscriber<uint[], string[]> getStates;
    private readonly ICallGateSubscriber<object> statesChanged;
    private uint[] questIds = Array.Empty<uint>();
    private IReadOnlyDictionary<uint, string> states = new Dictionary<uint, string>();
    private bool ready;

    public TsukimichiIpc(IDalamudPluginInterface pluginInterface)
    {
        apiVersion = pluginInterface.GetIpcSubscriber<int>("Tsukimichi.ApiVersion");
        isReady = pluginInterface.GetIpcSubscriber<bool>("Tsukimichi.IsReady");
        getStates = pluginInterface.GetIpcSubscriber<uint[], string[]>("Tsukimichi.GetStates");
        statesChanged = pluginInterface.GetIpcSubscriber<object>("Tsukimichi.StatesChanged");
        statesChanged.Subscribe(OnStatesChanged);
    }

    public event Action? Changed;

    public bool Ready => ready;

    public void SetQuestIds(IEnumerable<uint> ids)
    {
        questIds = ids.Distinct().ToArray();
        Refresh();
    }

    public bool IsCompleted(uint questRowId) =>
        ready && states.TryGetValue(questRowId, out var state) && state == "Completed";

    public void Dispose() => statesChanged.Unsubscribe(OnStatesChanged);

    private void OnStatesChanged()
    {
        Refresh();
        Changed?.Invoke();
    }

    private void Refresh()
    {
        try
        {
            if (apiVersion.InvokeFunc() != SupportedApiVersion || !isReady.InvokeFunc())
            {
                ready = false;
                states = new Dictionary<uint, string>();
                return;
            }

            var values = getStates.InvokeFunc(questIds);
            if (values.Length != questIds.Length)
            {
                ready = false;
                states = new Dictionary<uint, string>();
                return;
            }

            var snapshot = new Dictionary<uint, string>(questIds.Length);
            for (var i = 0; i < questIds.Length; i++)
                snapshot[questIds[i]] = values[i];

            states = snapshot;
            ready = true;
        }
        catch (IpcNotReadyError)
        {
            ready = false;
            states = new Dictionary<uint, string>();
        }
    }

    private static T Try<T>(Func<T> call, T fallback)
    {
        try { return call(); }
        catch (IpcNotReadyError) { return fallback; }
    }
}

