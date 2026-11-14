namespace HeavenswardTracker.Models;

public enum QuestCategory { MainScenario, Blue, Yellow }

public sealed record QuestEntry(uint Id, string Name, string Patch, QuestCategory Category);
