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
    private Task<IReadOnlyList<QuestEntry>>? catalogTask;
    private string? catalogError;
    private int tab;

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
            }
        }

        var loading = catalogTask is { IsCompleted: false };
        if (ImGui.Button(loading ? "Обновляю…" : "Обновить прогресс") && !loading)
        {
            catalogError = null;
            if (quests.Count == 0)
                catalogTask = Task.Run(() => HeavenswardCatalog.Load(data));
            else
                tsukimichi.RefreshNow();
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
            ImGui.TextWrapped("Для работы Heavensward Tracker установите и запустите Tsukimichi. Дождитесь, пока он загрузит данные персонажа, затем нажмите «Обновить прогресс».");
            return;
        }

        var completed = quests.Count(IsComplete);
        ImGui.TextWrapped("Heavensward · патчи 3.0–3.5");
        ImGui.Text($"Задания: {completed} / {quests.Count}");
        ImGui.ProgressBar(quests.Count == 0 ? 0 : (float)completed / quests.Count, new System.Numerics.Vector2(-1, 0));
        DrawAchievement("Основной сюжет пройден", quests.Where(q => q.Category == QuestCategory.MainScenario));
        DrawAchievement("Синие задания пройдены", quests.Where(q => q.Category == QuestCategory.Blue));
        DrawAchievement("Жёлтые задания пройдены", quests.Where(q => q.Category == QuestCategory.Yellow));
        if (ImGui.BeginTabBar("QuestCategories"))
        {
            DrawTab("Основной сюжет", QuestCategory.MainScenario, 0);
            DrawTab("Синие задания", QuestCategory.Blue, 1);
            DrawTab("Жёлтые задания", QuestCategory.Yellow, 2);
            ImGui.EndTabBar();
        }
        if (quests.Count > 0 && completed == quests.Count)
            ImGui.TextColored(new System.Numerics.Vector4(1f, .82f, .25f, 1f), "★ Достижение: «Heavensward пройден»");
    }

    private void DrawAchievement(string title, IEnumerable<QuestEntry> categoryQuests)
    {
        var list = categoryQuests.ToArray();
        if (list.Length > 0 && list.All(IsComplete))
            ImGui.TextColored(new System.Numerics.Vector4(1f, .82f, .25f, 1f), $"★ {title}");
    }

    private void DrawTab(string label, QuestCategory category, int index)
    {
        if (!ImGui.BeginTabItem(label)) return;
        tab = index;
        var entries = quests.Where(q => q.Category == category).ToArray();
        var done = entries.Count(IsComplete);
        ImGui.Text($"Выполнено: {done} / {entries.Length}");
        ImGui.ProgressBar(entries.Length == 0 ? 0 : (float)done / entries.Length, new System.Numerics.Vector2(-1, 0));
        ImGui.BeginChild($"quests-{tab}", new System.Numerics.Vector2(0, 0), true);
        foreach (var quest in entries)
        {
            var check = IsComplete(quest);
            ImGui.BeginDisabled(true);
            ImGui.Checkbox($"##{quest.Id}", ref check);
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.Text(quest.Name);
            ImGui.SameLine();
            ImGui.TextDisabled(quest.Patch);
        }
        ImGui.EndChild();
        ImGui.EndTabItem();
    }

    private bool IsComplete(QuestEntry quest) => tsukimichi.IsCompleted(quest.Id);

    public void Dispose() { }
}
