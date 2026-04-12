using System;

[Serializable]
public class UpgradeState
{
    public string id;
    public int level;

    public UpgradeState(string id, int level = 0)
    {
        this.id = id;
        this.level = level;
    }
}
