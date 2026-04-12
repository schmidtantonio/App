using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Upgrade Definitions")]
    [SerializeField] private List<UpgradeDefinition> upgradeDefinitions = new List<UpgradeDefinition>();

    [Header("Gameplay Tuning")]
    [SerializeField] private double baseMatchReward = 5;
    [SerializeField] private double matchDurationSeconds = 3.0;
    [SerializeField] private double clickBurstBase = 1.0;
    [SerializeField] private double prestigeRequirementBase = 5000.0;

    [Header("Autosave")]
    [SerializeField] private float autosaveInterval = 10f;

    private GameData data;
    private Dictionary<string, UpgradeDefinition> upgradeMap = new Dictionary<string, UpgradeDefinition>();
    private Dictionary<string, UpgradeState> upgradeStateMap = new Dictionary<string, UpgradeState>();

    private float autosaveTimer;
    private bool matchRunning;
    private float currentMatchTimer;

    public event Action OnDataChanged;
    public event Action<float> OnMatchProgressChanged;
    public event Action<bool> OnMatchStateChanged;

    public double Coins => data.coins;
    public int PrestigeLevel => data.prestigeLevel;
    public double PrestigeMultiplier => data.prestigeMultiplier;
    public bool MatchRunning => matchRunning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildUpgradeLookup();
        LoadGame();
        EnsureUpgradeStatesExist();
        ApplyOfflineIncome();
        NotifyDataChanged();
    }

    private void Update()
    {
        TickPassiveIncome();
        TickMatch();
        TickAutosave();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
    }

    private void BuildUpgradeLookup()
    {
        upgradeMap.Clear();

        foreach (var def in upgradeDefinitions)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.id))
            {
                continue;
            }

            if (!upgradeMap.ContainsKey(def.id))
            {
                upgradeMap.Add(def.id, def);
            }
            else
            {
                Debug.LogWarning($"Duplicate UpgradeDefinition id found: {def.id}");
            }
        }
    }

    private void LoadGame()
    {
        data = SaveSystem.Load();
        RebuildUpgradeStateMap();
    }

    private void SaveGame()
    {
        SaveSystem.Save(data);
    }

    private void RebuildUpgradeStateMap()
    {
        upgradeStateMap.Clear();

        foreach (var state in data.upgrades)
        {
            if (state != null && !string.IsNullOrWhiteSpace(state.id))
            {
                upgradeStateMap[state.id] = state;
            }
        }
    }

    private void EnsureUpgradeStatesExist()
    {
        foreach (var def in upgradeDefinitions)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.id))
            {
                continue;
            }

            if (!upgradeStateMap.ContainsKey(def.id))
            {
                var state = new UpgradeState(def.id, 0);
                data.upgrades.Add(state);
                upgradeStateMap[def.id] = state;
            }
        }
    }

    private void ApplyOfflineIncome()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long last = data.lastSaveUnixTime;
        long elapsed = Math.Max(0, now - last);

        if (elapsed <= 0)
        {
            return;
        }

        double passivePerSecond = GetPassiveIncomePerSecond();
        double offlineReward = passivePerSecond * elapsed;

        data.coins += offlineReward;
    }

    private void TickPassiveIncome()
    {
        double passivePerSecond = GetPassiveIncomePerSecond();
        data.coins += passivePerSecond * Time.deltaTime;
        NotifyDataChanged();
    }

    private void TickMatch()
    {
        if (!matchRunning)
        {
            return;
        }

        currentMatchTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(currentMatchTimer / (float)matchDurationSeconds);
        OnMatchProgressChanged?.Invoke(progress);

        if (currentMatchTimer >= matchDurationSeconds)
        {
            FinishMatch();
        }
    }

    private void TickAutosave()
    {
        autosaveTimer += Time.deltaTime;

        if (autosaveTimer >= autosaveInterval)
        {
            autosaveTimer = 0f;
            SaveGame();
        }
    }

    public void StartMatch()
    {
        if (matchRunning)
        {
            return;
        }

        matchRunning = true;
        currentMatchTimer = 0f;
        OnMatchStateChanged?.Invoke(true);
        OnMatchProgressChanged?.Invoke(0f);
    }

    private void FinishMatch()
    {
        matchRunning = false;
        currentMatchTimer = 0f;

        double reward = GetMatchReward();
        data.coins += reward;

        OnMatchStateChanged?.Invoke(false);
        OnMatchProgressChanged?.Invoke(0f);
        NotifyDataChanged();
    }

    public void ClickBurst()
    {
        double reward = GetClickBurstReward();
        data.coins += reward;
        NotifyDataChanged();
    }

    public bool CanAffordUpgrade(string upgradeId)
    {
        var def = GetUpgradeDefinition(upgradeId);
        if (def == null)
        {
            return false;
        }

        int level = GetUpgradeLevel(upgradeId);
        double cost = def.GetCost(level);
        return data.coins >= cost;
    }

    public bool BuyUpgrade(string upgradeId)
    {
        var def = GetUpgradeDefinition(upgradeId);
        var state = GetUpgradeState(upgradeId);

        if (def == null || state == null)
        {
            return false;
        }

        double cost = def.GetCost(state.level);

        if (data.coins < cost)
        {
            return false;
        }

        data.coins -= cost;
        state.level++;
        NotifyDataChanged();
        return true;
    }

    public void PrestigeReset()
    {
        double requirement = GetPrestigeRequirement();

        if (data.coins < requirement)
        {
            return;
        }

        data.prestigeLevel++;
        data.prestigeMultiplier = 1.0 + (data.prestigeLevel * 0.25);

        data.coins = 0;

        foreach (var state in data.upgrades)
        {
            if (state != null)
            {
                state.level = 0;
            }
        }

        matchRunning = false;
        currentMatchTimer = 0f;

        NotifyDataChanged();
    }

    public double GetPrestigeRequirement()
    {
        return prestigeRequirementBase * Math.Pow(2.0, data.prestigeLevel);
    }

    public int GetUpgradeLevel(string upgradeId)
    {
        var state = GetUpgradeState(upgradeId);
        return state?.level ?? 0;
    }

    public UpgradeDefinition GetUpgradeDefinition(string upgradeId)
    {
        if (upgradeMap.TryGetValue(upgradeId, out var def))
        {
            return def;
        }

        return null;
    }

    public UpgradeState GetUpgradeState(string upgradeId)
    {
        if (upgradeStateMap.TryGetValue(upgradeId, out var state))
        {
            return state;
        }

        return null;
    }

    public IReadOnlyList<UpgradeDefinition> GetAllUpgradeDefinitions()
    {
        return upgradeDefinitions;
    }

    public double GetUpgradeCost(string upgradeId)
    {
        var def = GetUpgradeDefinition(upgradeId);
        var state = GetUpgradeState(upgradeId);

        if (def == null || state == null)
        {
            return 0;
        }

        return def.GetCost(state.level);
    }

    public double GetPassiveIncomePerSecond()
    {
        double streamValue = SumUpgradeValues(UpgradeType.Stream);
        double gearBonus = SumUpgradeValues(UpgradeType.Gear) * 0.2;
        double total = (streamValue + gearBonus) * data.prestigeMultiplier;
        return total;
    }

    public double GetMatchReward()
    {
        double skillBonus = SumUpgradeValues(UpgradeType.Skill) * 2.0;
        double gearBonus = SumUpgradeValues(UpgradeType.Gear) * 1.5;
        double reward = (baseMatchReward + skillBonus + gearBonus) * data.prestigeMultiplier;
        return reward;
    }

    public double GetClickBurstReward()
    {
        double skillBonus = SumUpgradeValues(UpgradeType.Skill) * 0.5;
        double reward = (clickBurstBase + skillBonus) * data.prestigeMultiplier;
        return reward;
    }

    private double SumUpgradeValues(UpgradeType type)
    {
        double total = 0;

        foreach (var def in upgradeDefinitions)
        {
            if (def == null || def.type != type)
            {
                continue;
            }

            int level = GetUpgradeLevel(def.id);
            total += def.GetValue(level);
        }

        return total;
    }

    private void NotifyDataChanged()
    {
        OnDataChanged?.Invoke();
    }

    public static string FormatNumber(double value)
    {
        if (value >= 1_000_000_000)
            return (value / 1_000_000_000d).ToString("0.##") + "B";
        if (value >= 1_000_000)
            return (value / 1_000_000d).ToString("0.##") + "M";
        if (value >= 1_000)
            return (value / 1_000d).ToString("0.##") + "K";

        return value.ToString("0");
    }
}
