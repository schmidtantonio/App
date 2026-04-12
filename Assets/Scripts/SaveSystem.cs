using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "instalock_save.json");

    public static void Save(GameData data)
    {
        try
        {
            data.lastSaveUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Save failed: {ex.Message}");
        }
    }

    public static GameData Load()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                return new GameData();
            }

            string json = File.ReadAllText(SavePath);
            GameData data = JsonUtility.FromJson<GameData>(json);

            if (data == null)
            {
                return new GameData();
            }

            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Load failed: {ex.Message}");
            return new GameData();
        }
    }

    public static void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Delete save failed: {ex.Message}");
        }
    }
}
