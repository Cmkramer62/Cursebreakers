using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using System;

public class SaveDataHandler : MonoBehaviour {

    private SaveData saveData;
    public int level = -1, soulShards = 0, missionDataTimeSpent = -1, missionDataLivesLeft = -1, missionDataTimeSpotted = -1, missionDataLongestChase = -1, missionDataPurified = 0;
    public float masterVolume = 0f, sfxVolume = 0f, musicVolume = 0f, ambientVolume = 0f;
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeText, shardAmountText, shardAmountShadowText;
    public AudioMixer masterMixer;
    public bool hubFirstData = true;

    public static event Action OnSaveLoaded;

    // Start is called before the first frame update
    void Start() {
        Load();
    }

    public void SetMasterVolume(float value) {
        masterVolume = value;
        masterMixer.SetFloat("MasterVolumeParam", value);
        masterVolumeText.text = (((int)value) + 80).ToString() + "%";
        SaveSystem.Save(level, value, sfxVolume, musicVolume, ambientVolume, hubFirstData, soulShards, missionDataTimeSpent, missionDataLivesLeft, missionDataTimeSpotted, missionDataLongestChase, missionDataPurified);
    }

    public void SetLevel(int newLevel) {
        SaveSystem.Save(newLevel, masterVolume, sfxVolume, musicVolume, ambientVolume, hubFirstData, soulShards, missionDataTimeSpent, missionDataLivesLeft, missionDataTimeSpotted, missionDataLongestChase, missionDataPurified);
    }

    public void SetMissionData(int newLevel, int timeSpent, int livesLeft, int timeSpotted, int longestChase, int purified) {
        SaveSystem.Save(newLevel, masterVolume, sfxVolume, musicVolume, ambientVolume, hubFirstData, soulShards, timeSpent, livesLeft, timeSpotted, longestChase, purified);
    }

    public void SetHubFirst() {
        SaveSystem.Save(level, masterVolume, sfxVolume, musicVolume, ambientVolume, false, soulShards, missionDataTimeSpent, missionDataLivesLeft, missionDataTimeSpotted, missionDataLongestChase, missionDataPurified);
    }

    public void SetShards(int deltaAmount) {
        soulShards += deltaAmount;
        SaveSystem.Save(level, masterVolume, sfxVolume, musicVolume, ambientVolume, hubFirstData, soulShards, missionDataTimeSpent, missionDataLivesLeft, missionDataTimeSpotted, missionDataLongestChase, missionDataPurified);
    }

    public void Load() {
        saveData = SaveSystem.Load();
        if(saveData != null) {

            // Audio Section
            masterVolume = saveData.masterVolume;
            masterMixer.SetFloat("MasterVolumeParam", saveData.masterVolume);
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);
            masterVolumeText.text = (((int)masterVolume) + 80).ToString() + "%";
            sfxVolume = saveData.sfxVolume;
            musicVolume = saveData.musicVolume;
            ambientVolume = saveData.ambientVolume;
            hubFirstData = saveData.hubFirstMessage;

            // Mission Data Section
            level = saveData.level;
            soulShards = saveData.soulShards;
            missionDataTimeSpent = saveData.missionDataTimeSpent;
            missionDataLivesLeft = saveData.missionDataLivesLeft;
            missionDataTimeSpotted = saveData.missionDataTimeSpotted;
            missionDataLongestChase = saveData.missionDataLongestChase;
            missionDataPurified = saveData.missionDataPurified;

            // Shard Section
            shardAmountText.text = saveData.soulShards.ToString();
            shardAmountShadowText.text = saveData.soulShards.ToString();
        }
        else {
            //masterVolume = 80f;
            Debug.Log("Using default data.");
        }
        OnSaveLoaded?.Invoke(); // notify everyone listening

        Debug.Log("Finished Loading from file.");
    }


}
