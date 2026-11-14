using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using HeavenswardTracker.Data;
using HeavenswardTracker.Models;
using HeavenswardTracker.Integration;
using Dalamud.Plugin.Services;

namespace HeavenswardTracker.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private readonly TsukimichiIpc tsukimichi;
    private readonly IDataManager data;
    private IReadOnlyList<QuestEntry> quests = Array.Empty<QuestEntry>();
    private QuestEntry[] mainScenarioQuests = Array.Empty<QuestEntry>();
    private QuestEntry[] blueQuests = Array.Empty<QuestEntry>();
    private QuestEntry[] yellowQuests = Array.Empty<QuestEntry>();
    private int completedCount;
    private int mainScenarioCompleted;
    private int blueCompleted;
    private int yellowCompleted;
    private Task<IReadOnlyList<QuestEntry>>? catalogTask;
    private string? catalogError;

    public MainWindow(TsukimichiIpc tsukimichi, IDataManager data) : base("Heavensward · Путеводитель")
    {
        this.tsukimichi = tsukimichi;
        this.data = data;
        Size = new System.Numerics.Vector2(720, 540);
        SizeCondition = ImGuiCond.FirstUseEver;
        IsOpen = false;
    }

    public override void Draw()
    {
        if (catalogTask is { IsCompleted: true } completedTask)
        {
            catalogTask = null;
            if (completedTask.IsFaulted || completedTask.IsCanceled)
                catalogError = "Не удалось обновить список заданий из игровых данных.";
            else
            {
                quests = completedTask.Result;
                tsukimichi.SetQuestIds(quests.Select(quest => quest.Id));
                CacheQuestGroups();
            }
        }

        var loading = catalogTask is { IsCompleted: false };
        if (ImGui.Button(loading ? "Обновляю…" : "Обновить прогресс") && !loading)
        {
            catalogError = null;
            if (quests.Count == 0)
                catalogTask = Task.Run(() => HeavenswardCatalog.Load(data));
            else
            {
                tsukimichi.RefreshNow();
                UpdateProgressCounts();
            }
        }

        if (loading)
        {
            ImGui.TextWrapped("Список заданий загружается в фоне. Игра остаётся доступной.");
            return;
        }

        if (catalogError != null)
        {
            ImGui.TextWrapped(catalogError);
            return;
        }

        if (quests.Count == 0)
        {
            ImGui.TextWrapped("Нажмите «Обновить прогресс», чтобы загрузить список заданий и статусы персонажа.");
            return;
        }

        if (!tsukimichi.Ready)
        {
            ImGui.TextWrapped("Tsukimichi ещё загружает персонажа. Когда он будет готов, нажмите «Обновить прогресс».");
            return;
        }

        ImGui.Text($"Задания: {completedCount} / {quests.Count}");
        ImGui.ProgressBar(quests.Count == 0 ? 0 : (float)completedCount / quests.Count, new System.Numerics.Vector2(-1, 0));
        DrawAchievement("Основной сюжет пройден", mainScenarioQuests, mainScenarioCompleted);
        DrawAchievement("Синие задания пройдены", blueQuests, blueCompleted);
        DrawAchievement("Жёлтые задания пройдены", yellowQuests, yellowCompleted);
        if (ImGui.BeginTabBar("QuestCategories"))
        {
            DrawTab("Основной сюжет", mainScenarioQuests, mainScenarioCompleted, 0);
            DrawTab("Синие задания", blueQuests, blueCompleted, 1);
            DrawTab("Жёлтые задания", yellowQuests, yellowCompleted, 2);
            ImGui.EndTabBar();
        }
        if (completedCount == quests.Count)
            ImGui.TextColored(new System.Numerics.Vector4(1f, .82f, .25f, 1f), "★ Достижение: «Heavensward пройден»");
    }

    private void DrawAchievement(string title, QuestEntry[] categoryQuests, int completed)
    {
        if (categoryQuests.Length > 0 && completed == categoryQuests.Length)
            ImGui.TextColored(new System.Numerics.Vector4(1f, .82f, .25f, 1f), $"★ {title}");
    }

    private void DrawTab(string label, QuestEntry[] entries, int done, int index)
    {
        if (!ImGui.BeginTabItem(label)) return;
        ImGui.Text($"Выполнено: {done} / {entries.Length}");
        ImGui.ProgressBar(entries.Length == 0 ? 0 : (float)done / entries.Length, new System.Numerics.Vector2(-1, 0));
        ImGui.BeginChild($"quests-{index}", new System.Numerics.Vector2(0, 0), true);
        foreach (var quest in entries)
        {
            var check = IsComplete(quest);
            ImGui.BeginDisabled(true);
            ImGui.Checkbox($"##{quest.Id}", ref check);
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.Text(quest.Name);
        }
        ImGui.EndChild();
        ImGui.EndTabItem();
    }

    private bool IsComplete(QuestEntry quest) => tsukimichi.IsCompleted(quest.Id);

    private void CacheQuestGroups()
    {
        mainScenarioQuests = quests.Where(q => q.Category == QuestCategory.MainScenario).ToArray();
        blueQuests = quests.Where(q => q.Category == QuestCategory.Blue).ToArray();
        yellowQuests = quests.Where(q => q.Category == QuestCategory.Yellow).ToArray();
        UpdateProgressCounts();
    }

    private void UpdateProgressCounts()
    {
        completedCount = quests.Count(IsComplete);
        mainScenarioCompleted = mainScenarioQuests.Count(IsComplete);
        blueCompleted = blueQuests.Count(IsComplete);
        yellowCompleted = yellowQuests.Count(IsComplete);
    }

    public void Dispose() { }
}
