using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Animations;

public class CameraEffectsManager : MonoBehaviour {
    private Camera mainCamera;

    [SerializeField] private Volume volume;
    [SerializeField] private float bloomIntensity, bloomScatter, bloomDirtIntensity, bloomThreshold;


    private ColorAdjustments colorAdjustments;
    private Bloom bloom;
    private Coroutine saturationSettingRoutine;

    private void Awake() {
        // Creates/gets a runtime instance of the Volume Profile.
        VolumeProfile profile = volume.profile;

        // Get the appropriate overrides from the Post Processing profile.
        if(!profile.TryGet(out colorAdjustments)) Debug.LogError("Color Adjustments not found on Volume Profile.");
        if(!profile.TryGet(out bloom)) Debug.LogError("Color Bloom not found on Volume Profile.");
        
        mainCamera = GetComponent<Camera>();
    }

    private void SetSaturation(float value) {
        colorAdjustments.saturation.value = value;
    }

    public void SetSaturationOverTime(float target, float duration) {
        if(saturationSettingRoutine != null) StopCoroutine(saturationSettingRoutine);
        saturationSettingRoutine = StartCoroutine(ChangeSaturationRoutine(target, duration));
    }

    private void SetBloom(float intensityValue, float scatterValue, float thresholdValue, float dirtIntensityValue) {
        bloom.intensity.value = intensityValue;
        bloom.scatter.value = scatterValue;
        bloom.threshold.value = thresholdValue;
        bloom.dirtIntensity.value = dirtIntensityValue;
    }

    // Called by jumpscare. Makes monochromatic, changes bloom.
    // float timeTillMax is how long it will take until the effects are at full intensity.
    public void AfterlifeEffects(float timeTillMax) {
        SetSaturationOverTime(-55f, timeTillMax);
        StartCoroutine(AfterlifeEffectsSequence(timeTillMax));
    }

    private IEnumerator AfterlifeEffectsSequence(float timeTillMax) {
        yield return new WaitForSeconds(timeTillMax);
        SetBloom(bloomIntensity, bloomScatter, bloomThreshold, bloomDirtIntensity);
        GetComponent<ScrollingBloomDirt>().AfterlifeBloomTexture();
        RenderSettings.fog = true;
    }

    // Called by Death when player is alive again. Reverts saturation to normal, reverts bloom.
    public void NormalEffects() {
        SetSaturationOverTime(41.6f, 1);
        SetBloom(2.22f, 0.7f, 0.65f, 2.22f); // These are the normal defaults.
        GetComponent<ScrollingBloomDirt>().NormalBloomTexture();
        RenderSettings.fog = false;
    }

    // Called by Death for angel jumpscare only. Removes camera layers except "Special", which is only used by jumpscare.
    public void JumpscareOnlyLayer() {
        HideCameraLayer("Default");
        HideCameraLayer("TransparentFX");
        HideCameraLayer("Ignore Raycast");
        HideCameraLayer("Ground");
        HideCameraLayer("Water");
        HideCameraLayer("UI");
        HideCameraLayer("Interactable");
        //Hide("Player");
        //Show("Special");
    }

    // Called by Death when player is brought back to life. Reveals all layers, as normal.
    public void NormalLayers() {
        ShowCameraLayer("Default");
        ShowCameraLayer("TransparentFX");
        ShowCameraLayer("Ignore Raycast");
        ShowCameraLayer("Ground");
        ShowCameraLayer("Water");
        ShowCameraLayer("UI");
        ShowCameraLayer("Interactable");
        ShowCameraLayer("Player");
        ShowCameraLayer("Special");
        HideCameraLayer("Afterlife");
    }

    public void AfterlifeLayers() {
        ShowCameraLayer("Default");
        ShowCameraLayer("TransparentFX");
        ShowCameraLayer("Ignore Raycast");
        ShowCameraLayer("Ground");
        ShowCameraLayer("Water");
        ShowCameraLayer("UI");
        ShowCameraLayer("Interactable");
        ShowCameraLayer("Player");
        ShowCameraLayer("Special");
        ShowCameraLayer("Afterlife");
    }

    private IEnumerator ChangeSaturationRoutine(float target, float duration) {
        float start = colorAdjustments.saturation.value;
        float elapsed = 0f;

        while(elapsed < duration) {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;
            colorAdjustments.saturation.value = Mathf.Lerp(start, target, t);

            yield return null;
        }

        colorAdjustments.saturation.value = target;
    }

    #region CAMERA LAYER HELPERS
    // Turn on the the layer.
    private void ShowCameraLayer(string layerName) {
        mainCamera.cullingMask |= 1 << LayerMask.NameToLayer(layerName);
    }

    // Turn off the layer.
    private void HideCameraLayer(string layerName) {
        mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer(layerName));
    }


    // Toggle the layer.
    private void ToggleCameraLayer(string layerName) {
        mainCamera.cullingMask ^= 1 << LayerMask.NameToLayer(layerName);
    }
    #endregion

}
