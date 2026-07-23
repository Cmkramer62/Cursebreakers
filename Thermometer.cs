using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class Thermometer : NetworkBehaviour {

    //public GameObject[] levelsUI;
    public Animator scannerAnimator;
    public int goalTemp = 60, fakeTemp = 60, currentTemp = 60;
    public float updateSpeedMultiplier = 2f;
    public bool allowedToScan = true;
    public TextMeshProUGUI tempText;
    public AudioSource source;
    public AudioClip beep; // play same beep but at different pitches.??

    private float temt = 60;
    private Coroutine fluctuationRoutine;
    [SerializeField] private ToolController toolController;

    public override void OnNetworkSpawn() {
        toolController.defaultTemp.OnValueChanged += OnScannerChanged;
        //OnScannerChanged(0, toolController.defaultEMF.Value);

        gameObject.SetActive(false);
    }

    private void OnEnable() {
        allowedToScan = true;
    }

    // Start is called before the first frame update
    void Start() {
       // if(IsServer) InvokeRepeating("RandomFluctuation", 0, Random.Range(1f, 1.3f));  //in one second, start calling this function, every 2secs
       // move this logic to the ToolController? then remove the logic on cursed objects where they set the goal temp to a random thing.
    }

    // Update is called once per frame
    void Update() {
        if(!IsOwner) {
            return;
        }
        if(Input.GetKeyDown(KeyCode.Mouse0) && allowedToScan) {
            scannerAnimator.SetBool("ScannerView", true);
        }
        if(Input.GetKeyUp(KeyCode.Mouse0) || !allowedToScan) {
            scannerAnimator.SetBool("ScannerView", false);
        }

        //ThermometerStatusAndUIUpdate();
    }

    /*
    public void ThermometerStatusAndUIUpdate() {
        if(currentTemp < goalTemp) temt += updateSpeedMultiplier * Time.deltaTime;
        else if(currentTemp > goalTemp) temt -= updateSpeedMultiplier * Time.deltaTime;

        // Force whole number:
        currentTemp = Mathf.RoundToInt(temt);

        // Stop at target:
        currentTemp = Mathf.Clamp(currentTemp, -20, 100);
    }

    private void RandomFluctuation() {
        if(fluctuationRoutine != null) StopCoroutine(fluctuationRoutine);
        if(gameObject.activeSelf) fluctuationRoutine = StartCoroutine(FluctuationActivationTimer());

        if(!gameObject.activeSelf) ThermometerEffects(currentTemp);
    }

    private IEnumerator FluctuationActivationTimer() {
        FluctuationAmount();
        ThermometerEffects(fakeTemp);
        yield return new WaitForSeconds(Random.Range(0f, 1f));
        ThermometerEffects(currentTemp);
    }

    public void FluctuationAmount() {
        fakeTemp = currentTemp + Random.Range(-20, 20);
    }
    */
    void OnScannerChanged(int oldValue, int newValue) {
        Debug.Log(IsOwner);
        ThermometerEffects(newValue);
    }

    public void ThermometerEffects(int level) {
        tempText.text = level.ToString() + '°';

        source.pitch = .8f;
        source.pitch += level / 30f;
        if(gameObject.activeSelf) source.PlayOneShot(beep);
    }

}
