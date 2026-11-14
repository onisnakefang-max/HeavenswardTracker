using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using HeavenswardTracker.Data;
using HeavenswardTracker.Models;
using HeavenswardTracker.Integration;

namespace HeavenswardTracker.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly TsukimichiIpc tsukimichi;
    private readonly IReadOnlyList<QuestEntry> quests;
    private int tab;

    public MainWindow(Configuration configuration, TsukimichiIpc tsukimichi, IReadOnlyList<QuestEntry> quests) : base("Heavensward · Путеводитель")
    {
        this.configuration = configuration;
        this.tsukimichi = tsukimichi;
        this.quests = quests;
        Size = new System.Numerics.Vector2(720, 540);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        if (!tsukimichi.Ready)
        {
            ImGui.TextWrapped("Для работы Heavensward Tracker установите и запустите Tsukimichi. Дождитесь, пока он загрузит данные персонажа.");
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
        if (quests.Count == 0)
        {
            ImGui.Spacing();
            ImGui.TextWrapped("Каталог заданий пока не добавлен. Окно и отслеживание готовы, но для корректного прогресса нужен каталог с игровыми ID заданий и русскими названиями.");
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
