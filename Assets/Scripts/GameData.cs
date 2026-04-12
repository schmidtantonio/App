using System;
using System.Collections.Generic;

[Serializable]
public class GameData
{
    public double coins;
    public double prestigeMultiplier;
    public int prestigeLevel;
    public long lastSaveUnixTime;

    public List<UpgradeState> upgrades = new List<UpgradeState>();

    public GameData()
    {
        coins = 0;
        prestigeMultiplier = 1.0;
        prestigeLevel = 0;
        lastSaveUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
