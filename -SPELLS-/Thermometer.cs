using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Thermometer : NetworkBehaviour {

    //public GameObject[] levelsUI;
    public bool allowedToScan = true;
    public AudioSource source;
    //public AudioClip beep; // play same beep but at different pitches.??

    private Coroutine fluctuationRoutine;
    [SerializeField] private ToolController toolController;
    [SerializeField] private Renderer rend;
    [SerializeField] private Light crystalLight;
    [SerializeField] private ParticleSystem iceParticles;
    private Material tempMaterial;
    public HashSet<Collider> collidersInside = new HashSet<Collider>();

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


        // Audio Section
        //source.pitch = .8f;
        //source.pitch += level / 30f;
        //if(gameObject.activeSelf) source.PlayOneShot(beep);

        // -20 = blue, 60 = red
        // -20 = blue, 60 = red
        // -20 = blue, 60 = red
        float t = Mathf.InverseLerp(-20f, 60f, level);

        // 240° = blue, 360° = red
        float hue = Mathf.Lerp(240f, 360f, t);
        float hue01 = (hue % 360f) / 360f;

        Color temperatureColor = Color.HSVToRGB(hue01, 1f, 1f);

        // Light
        crystalLight.color = temperatureColor;

        // Crystal material emission
        float intensity = 1f;
        Color emissiveColor = temperatureColor * intensity;
        tempMaterial.SetColor("_EmissionColor", emissiveColor);

        // SFX
        float targetVolume = Mathf.InverseLerp(0f, -20f, level) * 0.65f;
        if(targetVolume != 0) {
            source.volume = targetVolume;
        }
        else {
            source.volume = 0;
        }

        // Particles
        if(level <= -15) {
            iceParticles.Play();
        }
        else {
            iceParticles.Stop();
        }
    }

    private void OnTriggerEnter(Collider other) {
        if(!IsOwner)
            return;

        if(other.CompareTag("AlgorClue")) {
            collidersInside.Add(other);
            //other.GetComponent<AlgorClue>().EmitParticle();
            other.GetComponent<AlgorClue>().targetObject = this.gameObject;
            other.GetComponent<AlgorClue>().PlayerWithAlgorEntered();
            other.GetComponent<AlgorClue>().thermometerScript = this;
            // add the algorclue to a list.
        }
    }

    private void OnTriggerExit(Collider other) {
        if(!IsOwner)
            return; 
        
        if(other.CompareTag("AlgorClue")) {
            RemoveAlgorClue(other);
        }
    }

    public void RemoveAlgorClue(Collider other) {
        collidersInside.Remove(other);
        //other.GetComponent<AlgorClue>().StopParticle();
        other.GetComponent<AlgorClue>().PlayerWithAlgorLeft();
        //remove the algorclue from the list.
    }

    private void OnDisable() {
        if(!IsOwner)
            return; 
        
        foreach(var thing in collidersInside) {
            thing.GetComponent<AlgorClue>().PlayerWithAlgorLeft();
        }
        collidersInside.Clear();
    }

    public int AmountOfHiddenIceCrystalsNearby() {
        return collidersInside.Count;
    }
}
