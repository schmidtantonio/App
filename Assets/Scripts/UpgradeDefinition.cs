using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeDefinition", menuName = "Instalock Empire/Upgrade Definition")]
public class UpgradeDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    public UpgradeType type;

    [Header("Economy")]
    public double baseCost = 100;
    public double costGrowth = 1.15;
    public double valuePerLevel = 1;

    [TextArea]
    public string description;

    public double GetCost(int currentLevel)
    {
        return baseCost * System.Math.Pow(costGrowth, currentLevel);
    }

    public double GetValue(int level)
    {
        return valuePerLevel * level;
    }
}
