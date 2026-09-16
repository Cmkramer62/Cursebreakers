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

    public int level, soulShards, missionDataTimeSpent, missionDataLivesLeft, missionDataTimeSpotted, missionDataLongestChase, missionDataPurified;
    public float masterVolume, sfxVolume, musicVolume, ambientVolume, uiVolume;
    public bool hubFirstMessage;

    public SaveData(int level, float masterVolume, float sfxVolume, float musicVolume, float ambientVolume, bool hubFirstMessage, int soulShards,
        int missionDataTimeSpent, int missionDataLivesLeft, int missionDataTimeSpotted, int missionDataLongestChase, int missionDataPurified) {

        this.level = level;
        this.masterVolume = masterVolume;
        this.sfxVolume = sfxVolume;
        this.musicVolume = musicVolume;
        this.ambientVolume = ambientVolume;
        this.hubFirstMessage = hubFirstMessage;
        this.soulShards = soulShards;

        // Mission data section
        this.missionDataTimeSpent = missionDataTimeSpent;
        this.missionDataLivesLeft = missionDataLivesLeft;
        this.missionDataTimeSpotted = missionDataTimeSpotted;
        this.missionDataLongestChase = missionDataLongestChase;
        this.missionDataPurified = missionDataPurified;
    }

}
