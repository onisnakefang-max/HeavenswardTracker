using System.Reflection;
using HeavenswardTracker.Models;
using Lumina.Excel.Sheets;

namespace HeavenswardTracker.Data;

/// <summary>Builds the catalog from the installed game client; no quest-name database is shipped.</summary>
public static class HeavenswardCatalog
{
    // ExVersion rows are ordered from A Realm Reborn (0), Heavensward (1), onward.
    private const uint HeavenswardExVersion = 1;

    public static IReadOnlyList<QuestEntry> Load(Dalamud.Plugin.Services.IDataManager data)
    {
        var result = new List<QuestEntry>();
        foreach (var quest in data.GetExcelSheet<Quest>())
        {
            if (quest.RowId == 0 || GetReferenceRowId(quest, "ExVersion") != HeavenswardExVersion)
                continue;

            var name = GetText(quest, "Name");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var category = Classify(quest);
            var patch = GetPatch(quest);
            result.Add(new QuestEntry(quest.RowId, name, patch, category));
        }

        return result.OrderBy(q => q.Patch, StringComparer.Ordinal)
            .ThenBy(q => q.Id)
            .ToArray();
    }

    private static QuestCategory Classify(Quest quest)
    {
        if (GetBoolean(quest, "IsMainScenario") || HasMeaning(quest, "MainScenario", "Main Scenario", "メインクエスト"))
            return QuestCategory.MainScenario;

        if (HasMeaning(quest, "Feature", "Unlock", "Special", "コンテンツ開放", "開放クエスト"))
            return QuestCategory.Blue;

        return QuestCategory.Yellow;
    }

    private static string GetPatch(Quest quest)
    {
        var value = GetText(quest, "AddedIn", "Patch", "PatchNumber");
        return string.IsNullOrWhiteSpace(value) ? "Heavensward" : value;
    }

    private static bool HasMeaning(Quest quest, params string[] needles)
    {
        foreach (var field in new[] { "QuestType", "JournalGenre", "JournalCategory", "JournalSection", "QuestCategory" })
        {
            var text = GetRelatedText(quest, field);
            if (needles.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        return false;
    }

    private static uint GetReferenceRowId(object instance, string propertyName)
    {
        var value = Read(instance, propertyName);
        if (value is null) return 0;
        var rowId = Read(value, "RowId");
        if (rowId is not null && uint.TryParse(rowId.ToString(), out var direct)) return direct;
        var referenced = Read(value, "Value");
        var nestedId = referenced is null ? null : Read(referenced, "RowId");
        return uint.TryParse(nestedId?.ToString(), out var result) ? result : 0;
    }

    private static bool GetBoolean(object instance, string propertyName)
    {
        var value = Read(instance, propertyName);
        return value is bool b && b;
    }

    private static string GetText(object instance, params string[] properties)
    {
        foreach (var property in properties)
        {
            var value = Read(instance, property);
            if (value is null) continue;
            var text = value.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(text) && text != "0") return text;
        }
        return string.Empty;
    }

    private static string GetRelatedText(object instance, string propertyName)
    {
        var value = Read(instance, propertyName);
        if (value is null) return string.Empty;
        var related = Read(value, "Value") ?? value;
        var name = Read(related, "Name");
        return name?.ToString() ?? related.ToString() ?? string.Empty;
    }

    private static object? Read(object instance, string propertyName)
    {
        var type = instance.GetType();
        return type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(instance)
               ?? type.GetField(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(instance);
    }
}
