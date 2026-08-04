using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Thermometer : NetworkBehaviour {

    //public GameObject[] levelsUI;
    public bool allowedToScan = true;
    //public AudioSource source;
    //public AudioClip beep; // play same beep but at different pitches.??

    private float temt = 60;
    private Coroutine fluctuationRoutine;
    [SerializeField] private ToolController toolController;
    [SerializeField] private Renderer rend;
    private Material tempMaterial;

    public override void OnNetworkSpawn() {
        toolController.defaultTemp.OnValueChanged += OnScannerChanged;
        tempMaterial = new Material(rend.sharedMaterial);
        rend.material = tempMaterial;
        gameObject.SetActive(false);
    }

    private void OnEnable() {
        allowedToScan = true;
    }

    void OnScannerChanged(int oldValue, int newValue) {
        ThermometerEffects(newValue);
    }

    public void ThermometerEffects(int level) {

        // Crystal Colors
        int value = level; // -20 to 60
        float t = Mathf.InverseLerp(-20f, 60f, value);
        float hue = Mathf.Lerp(240f, 360f, t);
        float hue01 = hue / 360f;
        Color emissiveColor = Color.HSVToRGB(hue01, 1f, 1f);

        float intensity = 1f;
        emissiveColor *= intensity;
        tempMaterial.SetColor("_EmissionColor", emissiveColor);

        // Audio Section
        //source.pitch = .8f;
        //source.pitch += level / 30f;
        //if(gameObject.activeSelf) source.PlayOneShot(beep);
    }

    

}
