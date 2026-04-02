using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Scanner : NetworkBehaviour  {

    public GameObject[] levelsUI;
    public Animator scannerAnimator;
    //public int levelEMF = 0, fakeEMF = 0;
    public bool allowedToScan = true;

    public AudioSource source;
    public AudioClip beep; // play same beep but at different pitches.??

    private Coroutine fluctuationRoutine;

    public NetworkVariable<int> scannerValue = new NetworkVariable<int>();
    public NetworkVariable<int> scannerFakeValue = new NetworkVariable<int>();

    public override void OnNetworkSpawn() {
        if(IsServer) {
            //StartCoroutine(ScannerRoutine());
            InvokeRepeating("RandomFluctuation", 0, Random.Range(1, 12));  //in one second, start calling this function, every 2secs
        }

        scannerValue.OnValueChanged += OnScannerChanged;
        //scannerFakeValue.OnValueChanged += OnScannerFakeChanged;
        OnScannerChanged(0, scannerValue.Value);
    }

    void OnEnable() {
        if(IsServer && IsSpawned) {
            StartScanner();
        }
        allowedToScan = true;
    }

    void OnDisable() {
        if(IsServer) {
            CancelInvoke(nameof(RandomFluctuation));
        }
        allowedToScan = false;
    }

    // Update is called once per frame
    void Update() {
        if(!IsOwner) {
            //enabled = false;
            return;
        }
        if(Input.GetKeyDown(KeyCode.Mouse0) && allowedToScan) {
            scannerAnimator.SetBool("ScannerView", true);
        }
        if(Input.GetKeyUp(KeyCode.Mouse0) || !allowedToScan) {
            scannerAnimator.SetBool("ScannerView", false);
        }

    }

    void OnScannerChanged(int oldValue, int newValue) {
        for(int i = 0; i < levelsUI.Length; i++) {
            levelsUI[i].SetActive(i < newValue);
        }
        if(newValue != 0) {
            source.pitch = .8f;
            source.pitch += newValue / 10f;
            source.PlayOneShot(beep);
        }
        //scannerValue.Value = newValue;
    }

    void StartScanner() {
        ScheduleNext();
    }

    void ScheduleNext() {
        float delay = Random.Range(1f, 2f);
        Invoke(nameof(RandomFluctuation), delay);
    }

    void RandomFluctuation() {
        if(!IsServer) return;

        if(Random.Range(0, 2) == 0) OnScannerChanged(scannerValue.Value, Random.Range(0, 8));
        else OnScannerChanged(scannerValue.Value, scannerValue.Value);

        ScheduleNext(); // schedule next random tick
    }

    public void ActivateEffectsEMF(int level) {
        
    }

}
