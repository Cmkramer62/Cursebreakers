using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Scanner : NetworkBehaviour  {

    public GameObject[] scannerLights;
    public Animator scannerAnimator;
    //public int levelEMF = 0, fakeEMF = 0;
    public bool allowedToScan = true;

    public AudioSource source;
    public AudioClip beep; // play same beep but at different pitches.??

    [SerializeField] private ToolController toolController;

    /*
     * On a tick, set the scanner's lights to be what the value of defaultEMF is in the Tool Controller.
     * 
     * Bug: I can see the lights work. But only on the host, for the friend's scanner. Host's own scanner and both scanners of the client
     * are not working.
     * 
     * Read chat's response about this.
     */

    public override void OnNetworkSpawn() {
        toolController.defaultEMF.OnValueChanged += OnScannerChanged;
        OnScannerChanged(0, toolController.defaultEMF.Value);

        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update() {
        if(!IsOwner) {
            return;
        }
        if(Input.GetKeyDown(KeyCode.Mouse0)) {
            scannerAnimator.SetBool("ScannerView", true);
        }
        if(Input.GetKeyUp(KeyCode.Mouse0)) {
            scannerAnimator.SetBool("ScannerView", false);
        }
    }

    void OnScannerChanged(int oldValue, int newValue) {
        Debug.Log(IsOwner);
         ScannerEffects(newValue);
        //scannerValue.Value = newValue;
    }

   // [ServerRpc]
    void ScannerEffects(int newValue) {
        if(gameObject.activeInHierarchy) {
            for(int i = 0; i < scannerLights.Length; i++) {
                scannerLights[i].SetActive(i < newValue);
            }
            if(newValue != 0) {
                source.pitch = .8f;
                source.pitch += newValue / 10f;
                source.PlayOneShot(beep);
            }
        }
    }

}
