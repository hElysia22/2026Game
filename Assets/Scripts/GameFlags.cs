using System;
using System.Collections.Generic;
using UnityEngine;

public class GameFlags : MonoBehaviour
{
    public static GameFlags Instance { get; private set; }

    private readonly HashSet<string> flags = new();
    public event Action<string, bool> OnFlagChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool HasFlag(string id) => !string.IsNullOrEmpty(id) && flags.Contains(id);

    public void SetFlag(string id, bool value = true)
    {
        if (string.IsNullOrEmpty(id)) return;
        bool changed = value ? flags.Add(id) : flags.Remove(id);
        if (changed) OnFlagChanged?.Invoke(id, value);
    }

    public void ClearFlag(string id) => SetFlag(id, false);

    public List<string> GetAll() => new List<string>(flags);

    public void LoadAll(List<string> saved)
    {
        flags.Clear();
        if (saved == null) return;
        foreach (var f in saved) flags.Add(f);
    }
}

public static class FlagIds
{
    public const string BossGoblinDefeated = "boss_goblin_defeated";
    public const string Room1Cleared = "room1_cleared";
    public const string TalkedToElder = "talked_to_elder";
}