using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;

namespace HeavenswardTracker.Integration;

/// <summary>Reads completion in one explicit refresh request.</summary>
public sealed class TsukimichiIpc : IDisposable
{
    private const int SupportedApiVersion = 1;
    private readonly ICallGateSubscriber<int> apiVersion;
    private readonly ICallGateSubscriber<bool> isReady;
    private readonly ICallGateSubscriber<uint[], string[]> getStates;
    private uint[] questIds = Array.Empty<uint>();
    private IReadOnlyDictionary<uint, string> states = new Dictionary<uint, string>();
    private bool ready;

    public TsukimichiIpc(IDalamudPluginInterface pluginInterface)
    {
        apiVersion = pluginInterface.GetIpcSubscriber<int>("Tsukimichi.ApiVersion");
        isReady = pluginInterface.GetIpcSubscriber<bool>("Tsukimichi.IsReady");
        getStates = pluginInterface.GetIpcSubscriber<uint[], string[]>("Tsukimichi.GetStates");
    }

    public bool Ready => ready;

    public void SetQuestIds(IEnumerable<uint> ids)
    {
        questIds = ids.Distinct().ToArray();
        RefreshNow();
    }

    public void RefreshNow()
    {
        try
        {
            if (apiVersion.InvokeFunc() != SupportedApiVersion || !isReady.InvokeFunc())
            {
                Clear();
                return;
            }

            var values = getStates.InvokeFunc(questIds);
            if (values.Length != questIds.Length)
            {
                Clear();
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
            Clear();
        }
    }

    public bool IsCompleted(uint questRowId) =>
        ready && states.TryGetValue(questRowId, out var state) && state == "Completed";

    public void Dispose() { }

    private void Clear()
    {
        ready = false;
        states = new Dictionary<uint, string>();
    }
}
