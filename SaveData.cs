using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
/*
 * SaveData is the class that stores any data we wish to be in the save file.
 * It is stored in Application.PersistentDatapath.
 * 
 * This class is saved and loaded by SaveSystem.cs.
 */
public class SaveData {

    public int level;
    public float masterVolume, sfxVolume, musicVolume, ambientVolume, uiVolume;
    public bool hubFirstMessage;

    public SaveData(int level, float masterVolume, float sfxVolume, float musicVolume, float ambientVolume, bool hubFirstMessage) {

        this.level = level;
        this.masterVolume = masterVolume;
        this.sfxVolume = sfxVolume;
        this.musicVolume = musicVolume;
        this.ambientVolume = ambientVolume;
        this.hubFirstMessage = hubFirstMessage;
    }

}
