using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using System;

public class AudioSettings : MonoBehaviour {

    private SaveData saveData;
    public int level = -1;
    public float masterVolume = 0f, sfxVolume = 0f, musicVolume = 0f, ambientVolume = 0f;
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeText;
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

        // -80 / 20  0/100

        SaveSystem.Save(level, value, sfxVolume, musicVolume, ambientVolume, hubFirstData);
    }

    public void SetLevel(int newLevel) {
        SaveSystem.Save(newLevel, masterVolume, sfxVolume, musicVolume, ambientVolume, hubFirstData);
    }

    public void SetHubFirst() {
        SaveSystem.Save(level, masterVolume, sfxVolume, musicVolume, ambientVolume, false);
    }

    public void Load() {
        saveData = SaveSystem.Load();
        if(saveData != null) {
            level = saveData.level;

            masterVolume = saveData.masterVolume;
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);
            masterVolumeText.text = (((int)masterVolume) + 80).ToString() + "%";

            sfxVolume = saveData.sfxVolume;
            musicVolume = saveData.musicVolume;
            ambientVolume = saveData.ambientVolume;
            hubFirstData = saveData.hubFirstMessage;
        }
        else {
            //masterVolume = 80f;
            Debug.Log("Using default data.");
        }
        OnSaveLoaded?.Invoke(); // notify everyone listening

        Debug.Log("Finished Loading from file.");
    }


}
