using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>Player-facing visual and camera comfort settings for the DefaultVolume profile.</summary>
public sealed class VisualSettingsController : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private Volume targetVolume;
    [SerializeField] private MouseLook playerMouseLook;
    [SerializeField] private Camera playerCamera;

    [Header("Audio mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParameter = "MainVolumeParam";
    [SerializeField] private string musicVolumeParameter = "MusicVolumeParam";
    [SerializeField] private string sfxVolumeParameter = "SFXVolumeParam";
    [SerializeField] private string ambienceVolumeParameter = "AmbienceVolumeParam";
    [SerializeField] private string dialogueVolumeParameter = "DialogueVolumeParam";

    [Header("Audio UI")]
    [Tooltip("Float percentage slider. Minimum 0, maximum 100; 0 is muted and 100 is full volume.")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeValueText;
    [Tooltip("Float percentage slider. Minimum 0, maximum 100; 0 is muted and 100 is full volume.")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeValueText;
    [Tooltip("Float percentage slider. Minimum 0, maximum 100; 0 is muted and 100 is full volume.")]
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text sfxVolumeValueText;
    [Tooltip("Float percentage slider. Minimum 0, maximum 100; 0 is muted and 100 is full volume.")]
    [SerializeField] private Slider ambienceVolumeSlider;
    [SerializeField] private TMP_Text ambienceVolumeValueText;
    [Tooltip("Float percentage slider. Minimum 0, maximum 100; 0 is muted and 100 is full volume.")]
    [SerializeField] private Slider dialogueVolumeSlider;
    [SerializeField] private TMP_Text dialogueVolumeValueText;

    [Header("Visual effects")]
    [SerializeField] private Toggle bloomToggle;
    [SerializeField] private Toggle chromaticAberrationToggle;
    [SerializeField] private Toggle filmGrainToggle;
    [SerializeField] private Toggle motionBlurToggle;
    [SerializeField] private Toggle depthOfFieldToggle;

    [Header("Brightness")]
    [Tooltip("Float slider. Minimum: -5. Maximum: 5. Controls post-exposure in exposure stops.")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TMP_Text brightnessValueText;

    [Header("Color")]
    [Tooltip("Float slider. Minimum: -100. Maximum: 100. 0 is neutral; the profile default is 41.6.")]
    [SerializeField] private Slider saturationSlider;
    [SerializeField] private TMP_Text saturationValueText;

    [Header("Camera comfort")]
    [SerializeField] private Toggle cameraShakeEnabledToggle;
    [Tooltip("Float slider. Minimum: 0. Maximum: 1. 1 preserves current maximum shake; 0 disables it.")]
    [SerializeField] private Slider cameraShakeStrengthSlider;
    [SerializeField] private TMP_Text cameraShakeStrengthValueText;

    [Header("Look controls")]
    [Tooltip("Float slider. Minimum: 0.1. Maximum: 4.0. Default: 2.0.")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private TMP_Text mouseSensitivityValueText;
    [SerializeField] private Toggle invertHorizontalLookToggle;
    [SerializeField] private Toggle invertVerticalLookToggle;
    [Tooltip("Float slider in degrees. Minimum: 65. Maximum: 85. Default reads from the player camera when available; otherwise 60.")]
    [SerializeField] private Slider fieldOfViewSlider;
    [SerializeField] private TMP_Text fieldOfViewValueText;

    [Serializable]
    private sealed class SavedSettings
    {
        public bool bloom = true, chromaticAberration = true, filmGrain = true;
        public bool motionBlur, depthOfField;
        public float brightness, saturation = 41.6f;
        public bool cameraShakeEnabled = true;
        public float cameraShakeStrength = 1f;
        public float mouseSensitivity = 2f;
        public bool invertHorizontalLook, invertVerticalLook;
        public float fieldOfView = 80f;
        public float masterVolume = 100f, musicVolume = 100f, sfxVolume = 100f;
        public float ambienceVolume = 100f, dialogueVolume = 100f;
    }

    private const string FileName = "visual_settings.json";
    private SavedSettings settings;
    private SavedSettings defaultSettings;
    private VolumeProfile runtimeProfile;
    private Bloom bloom;
    private ChromaticAberration chromaticAberration;
    private FilmGrain filmGrain;
    private MotionBlur motionBlur;
    private ColorAdjustments colorAdjustments;
    private DepthOfField depthOfField;
    private bool initialized;

    private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    private void Awake()
    {
        if (!ResolveProfile()) { enabled = false; return; }
        defaultSettings = ReadProfileDefaults();
        if (playerCamera != null) defaultSettings.fieldOfView = playerCamera.fieldOfView;
        settings = LoadSettings() ?? CopySettings(defaultSettings);
        initialized = true;
        ApplySettings();
        BindUi();
        RefreshUi();
        SaveSettings();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { ApplyCameraShakeSetting(); ApplyLookSettings(); }

    private bool ResolveProfile()
    {
        if (targetVolume == null)
        {
            foreach (Volume candidate in FindObjectsOfType<Volume>(true))
                if (candidate.sharedProfile != null && candidate.sharedProfile.name == "DefaultVolume")
                { targetVolume = candidate; break; }
        }
        if (targetVolume == null)
        {
            Debug.LogError("VisualSettingsController: assign the main Volume using the DefaultVolume profile.", this);
            return false;
        }
        runtimeProfile = targetVolume.profile;
        return runtimeProfile.TryGet(out bloom)
            && runtimeProfile.TryGet(out chromaticAberration)
            && runtimeProfile.TryGet(out filmGrain)
            && runtimeProfile.TryGet(out motionBlur)
            && runtimeProfile.TryGet(out colorAdjustments)
            && runtimeProfile.TryGet(out depthOfField);
    }

    private SavedSettings ReadProfileDefaults() => new SavedSettings
    {
        bloom = bloom.active,
        chromaticAberration = chromaticAberration.active,
        filmGrain = filmGrain.active,
        motionBlur = motionBlur.active,
        depthOfField = depthOfField.active,
        brightness = colorAdjustments.postExposure.value,
        saturation = colorAdjustments.saturation.value
    };

    private static SavedSettings CopySettings(SavedSettings source)
    {
        return JsonUtility.FromJson<SavedSettings>(JsonUtility.ToJson(source));
    }

    private SavedSettings LoadSettings()
    {
        try { return File.Exists(SavePath) ? JsonUtility.FromJson<SavedSettings>(File.ReadAllText(SavePath)) : null; }
        catch (Exception e) { Debug.LogWarning("Could not load visual settings: " + e.Message, this); return null; }
    }

    private void SaveSettings()
    {
        try { File.WriteAllText(SavePath, JsonUtility.ToJson(settings, true)); }
        catch (Exception e) { Debug.LogError("Could not save visual settings: " + e.Message, this); }
    }

    private void ApplySettings()
    {
        bloom.active = settings.bloom;
        chromaticAberration.active = settings.chromaticAberration;
        filmGrain.active = settings.filmGrain;
        motionBlur.active = settings.motionBlur;
        depthOfField.active = settings.depthOfField;
        colorAdjustments.postExposure.value = settings.brightness;
        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.saturation.value = settings.saturation;
        colorAdjustments.saturation.overrideState = true;
        ApplyAudioSettings();
        ApplyCameraShakeSetting();
        ApplyLookSettings();
    }

    private void ApplyCameraShakeSetting()
    {
        if (settings == null) return;
        float scale = settings.cameraShakeEnabled ? Mathf.Clamp01(settings.cameraShakeStrength) : 0f;
        foreach (CameraFollow cameraFollow in FindObjectsOfType<CameraFollow>(true))
            cameraFollow.SetSettingsCameraShakeScale(scale);
    }

    private void ApplyLookSettings()
    {
        if (settings == null) return;
        if (playerMouseLook != null)
        {
            playerMouseLook.mouseSensitivity = Mathf.Clamp(settings.mouseSensitivity, 0.1f, 4f);
            playerMouseLook.invertHorizontalLook = settings.invertHorizontalLook;
            playerMouseLook.invertVerticalLook = settings.invertVerticalLook;
        }
        if (playerCamera != null)
            playerCamera.fieldOfView = Mathf.Clamp(settings.fieldOfView, 65f, 85f);
    }

    private static float PercentToDecibels(float percent)
    {
        percent = Mathf.Clamp(percent, 0f, 100f);
        return percent <= 0f ? -80f : Mathf.Max(-80f, 20f * Mathf.Log10(percent / 100f));
    }

    private void ApplyAudioSettings()
    {
        if (audioMixer == null) return;
        audioMixer.SetFloat(masterVolumeParameter, PercentToDecibels(settings.masterVolume));
        audioMixer.SetFloat(musicVolumeParameter, PercentToDecibels(settings.musicVolume));
        audioMixer.SetFloat(sfxVolumeParameter, PercentToDecibels(settings.sfxVolume));
        audioMixer.SetFloat(ambienceVolumeParameter, PercentToDecibels(settings.ambienceVolume));
        audioMixer.SetFloat(dialogueVolumeParameter, PercentToDecibels(settings.dialogueVolume));
    }

    private void BindUi()
    {
        if (bloomToggle) bloomToggle.onValueChanged.AddListener(SetBloom);
        if (chromaticAberrationToggle) chromaticAberrationToggle.onValueChanged.AddListener(SetChromaticAberration);
        if (filmGrainToggle) filmGrainToggle.onValueChanged.AddListener(SetFilmGrain);
        if (motionBlurToggle) motionBlurToggle.onValueChanged.AddListener(SetMotionBlur);
        if (depthOfFieldToggle) depthOfFieldToggle.onValueChanged.AddListener(SetDepthOfField);
        if (brightnessSlider) brightnessSlider.onValueChanged.AddListener(SetBrightness);
        if (saturationSlider) saturationSlider.onValueChanged.AddListener(SetSaturation);
        if (cameraShakeEnabledToggle) cameraShakeEnabledToggle.onValueChanged.AddListener(SetCameraShakeEnabled);
        if (cameraShakeStrengthSlider) cameraShakeStrengthSlider.onValueChanged.AddListener(SetCameraShakeStrength);
        if (mouseSensitivitySlider) mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        if (invertHorizontalLookToggle) invertHorizontalLookToggle.onValueChanged.AddListener(SetInvertHorizontalLook);
        if (invertVerticalLookToggle) invertVerticalLookToggle.onValueChanged.AddListener(SetInvertVerticalLook);
        if (fieldOfViewSlider) fieldOfViewSlider.onValueChanged.AddListener(SetFieldOfView);
        if (masterVolumeSlider) masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        if (musicVolumeSlider) musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxVolumeSlider) sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
        if (ambienceVolumeSlider) ambienceVolumeSlider.onValueChanged.AddListener(SetAmbienceVolume);
        if (dialogueVolumeSlider) dialogueVolumeSlider.onValueChanged.AddListener(SetDialogueVolume);
    }

    private void RefreshUi()
    {
        SetToggle(bloomToggle, settings.bloom); SetToggle(chromaticAberrationToggle, settings.chromaticAberration);
        SetToggle(filmGrainToggle, settings.filmGrain);
        SetToggle(motionBlurToggle, settings.motionBlur); SetToggle(depthOfFieldToggle, settings.depthOfField);
        SetToggle(cameraShakeEnabledToggle, settings.cameraShakeEnabled);
        SetSlider(brightnessSlider, brightnessValueText, settings.brightness, -5f, 5f, "0.00");
        SetSlider(saturationSlider, saturationValueText, settings.saturation, -100f, 100f, "0.0");
        SetSlider(cameraShakeStrengthSlider, cameraShakeStrengthValueText, settings.cameraShakeStrength, 0f, 1f, "0.00");
        SetToggle(invertHorizontalLookToggle, settings.invertHorizontalLook);
        SetToggle(invertVerticalLookToggle, settings.invertVerticalLook);
        SetSlider(mouseSensitivitySlider, mouseSensitivityValueText, settings.mouseSensitivity, 0.1f, 4f, "0.00");
        SetSlider(fieldOfViewSlider, fieldOfViewValueText, settings.fieldOfView, 65f, 85f, "0");
        SetPercentSlider(masterVolumeSlider, masterVolumeValueText, settings.masterVolume);
        SetPercentSlider(musicVolumeSlider, musicVolumeValueText, settings.musicVolume);
        SetPercentSlider(sfxVolumeSlider, sfxVolumeValueText, settings.sfxVolume);
        SetPercentSlider(ambienceVolumeSlider, ambienceVolumeValueText, settings.ambienceVolume);
        SetPercentSlider(dialogueVolumeSlider, dialogueVolumeValueText, settings.dialogueVolume);
    }

    private static void SetToggle(Toggle control, bool value) { if (control) control.SetIsOnWithoutNotify(value); }
    private static void SetSlider(Slider slider, TMP_Text label, float value, float min, float max, string format)
    {
        if (slider) { slider.wholeNumbers = false; slider.minValue = min; slider.maxValue = max; slider.SetValueWithoutNotify(value); }
        if (label) label.text = value.ToString(format);
    }

    private static void SetPercentSlider(Slider slider, TMP_Text label, float value)
    {
        SetSlider(slider, null, value, 0f, 100f, "0.0");
        if (label) label.text = value.ToString("0.0") + "%";
    }

    private void Changed() { if (!initialized) return; ApplySettings(); RefreshUi(); SaveSettings(); }
    public void SetBloom(bool value) { settings.bloom = value; Changed(); }
    public void SetChromaticAberration(bool value) { settings.chromaticAberration = value; Changed(); }
    public void SetFilmGrain(bool value) { settings.filmGrain = value; Changed(); }
    public void SetMotionBlur(bool value) { settings.motionBlur = value; Changed(); }
    public void SetDepthOfField(bool value) { settings.depthOfField = value; Changed(); }
    public void SetBrightness(float value) { settings.brightness = value; Changed(); }
    public void SetSaturation(float value) { settings.saturation = value; Changed(); }
    public void SetCameraShakeEnabled(bool value) { settings.cameraShakeEnabled = value; Changed(); }
    public void SetCameraShakeStrength(float value) { settings.cameraShakeStrength = Mathf.Clamp01(value); Changed(); }
    public void SetMouseSensitivity(float value) { settings.mouseSensitivity = Mathf.Clamp(value, 0.1f, 4f); Changed(); }
    public void SetInvertHorizontalLook(bool value) { settings.invertHorizontalLook = value; Changed(); }
    public void SetInvertVerticalLook(bool value) { settings.invertVerticalLook = value; Changed(); }
    public void SetFieldOfView(float value) { settings.fieldOfView = Mathf.Clamp(value, 65f, 85f); Changed(); }
    public void SetMasterVolume(float value) { settings.masterVolume = Mathf.Clamp(value, 0f, 100f); Changed(); }
    public void SetMusicVolume(float value) { settings.musicVolume = Mathf.Clamp(value, 0f, 100f); Changed(); }
    public void SetSfxVolume(float value) { settings.sfxVolume = Mathf.Clamp(value, 0f, 100f); Changed(); }
    public void SetAmbienceVolume(float value) { settings.ambienceVolume = Mathf.Clamp(value, 0f, 100f); Changed(); }
    public void SetDialogueVolume(float value) { settings.dialogueVolume = Mathf.Clamp(value, 0f, 100f); Changed(); }

    /// <summary>Restores the visual profile defaults and the default full camera shake strength.</summary>
    public void ResetToDefaults()
    {
        if (!initialized || defaultSettings == null) return;
        settings = CopySettings(defaultSettings);
        Changed();
    }
}

