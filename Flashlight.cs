using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Flashlight : NetworkBehaviour {

    public NetworkVariable<bool> flashlightEnabled = new NetworkVariable<bool>(false);
    public GameObject flashlightObject, flashlightUI, defaultLookPoint;
    public AudioSource source;//, glowingSource;
    public AudioClip turnOnClip, turnOffClip;

    public float sprintDuration, staminaRemaining, sprintRecoveryDec;//, lightBlastVolume = 0.7f;
    public bool isTired, isFlashing = false;

    public InteractRaycast raycastScript;

    public ParticleSystem geistParticles;

    [SerializeField] private PlayerHandler playerHandlerScript;

    public override void OnNetworkSpawn() {

        //flashlightUI.SetActive(true);

        // Subscribe to changes
        flashlightEnabled.OnValueChanged += OnFlashlightChanged;

        // Apply initial state
        ApplyFlashlightState(flashlightEnabled.Value);
        
        
        gameObject.SetActive(false);

        if(!IsOwner) return;
        // Pass this self to the client-side UI Manager so it can handle the UI of this.
        UIManager.Instance.RegisterGeistlight(this);
    }



    public void OnDisable() {
        if(!IsOwner) return;

        //flashlightUI.SetActive(false);
        if(geistParticles.isPlaying) geistParticles.Stop();
    }

    public void OnFlashlightChanged(bool oldValue, bool newValue) {
        ApplyFlashlightState(newValue);
    }

    public void ApplyFlashlightState(bool newState) {
        flashlightEnabled.Value = newState;
        isFlashing = flashlightEnabled.Value;
        flashlightObject.SetActive(newState);

        if(flashlightEnabled.Value) {
            //source.PlayOneShot(turnOnClip);
            geistParticles.Play();
            playerHandlerScript.GetComponent<ToolController>().GeistlightAnimation(true);
            // In future, play animation on both the arms and the masked upper body.
        }
        else {
            //source.PlayOneShot(turnOffClip);
            geistParticles.Stop();
            playerHandlerScript.GetComponent<ToolController>().GeistlightAnimation(false);
            // In future, play animation on both the arms and the masked upper body.
        }
    }

    [ServerRpc]
    private void TurnFlashLightOnServerRpc() {
        Debug.Log("Activate Flashlight.");
        flashlightEnabled.Value = true;
    }

    [ServerRpc]
    private void TurnFlashLightOffServerRpc() {
        Debug.Log("Deactivate Flashlight.");
        flashlightEnabled.Value = false;
    }

    private void Update() {
        if(!IsOwner) {
            return;
        }

        if(Input.GetKeyDown(KeyCode.F) && !isTired && !flashlightEnabled.Value) {
            TurnFlashLightOnServerRpc();
        }
        else if((Input.GetKeyUp(KeyCode.F) && flashlightEnabled.Value) || (isTired && flashlightEnabled.Value)) { // || isTired && flashlightEnabled.Value
            TurnFlashLightOffServerRpc();
        }
    }

   
}
