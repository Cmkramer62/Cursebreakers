using System.IO;
using UnityEngine;
using System;

public static class SaveSystem {
    private static string path => Application.persistentDataPath + "/hideData.json";

    // SAVE
    public static void Save(int level, float masterVolume, float sfxVolume, float musicVolume, float ambientVolume, bool hubState) {
        SaveData data = new SaveData(level, masterVolume, sfxVolume, musicVolume, ambientVolume, hubState);

        string json = JsonUtility.ToJson(data, true); // true = pretty print (optional)
        File.WriteAllText(path, json);

        Debug.Log("Game Saved to: " + path);
        Debug.Log(json);
    }

    // LOAD
    public static SaveData Load() {
        if(!File.Exists(path)) {
            Debug.LogWarning("No save file found at: " + path);
            return null;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        Debug.Log("Game Loaded from: " + path);
        Debug.Log(json);

        return data;
    }
}
