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

    public CanvasGroup sprintBarCG;
    public Image sprintBar;
    public float sprintDuration, staminaRemaining, sprintRecoveryDec;//, lightBlastVolume = 0.7f;
    public bool isTired, hideBarWhenFull = false, useSprintBar = true;

    public InteractRaycast raycastScript;

    public ParticleSystem geistParticles;
    public Color stamBarUIColor;

    [SerializeField] private PlayerHandler playerHandlerScript;

    public override void OnNetworkSpawn() {
        //flashlightUI.SetActive(true);

        // Subscribe to changes
        flashlightEnabled.OnValueChanged += OnFlashlightChanged;

        // Apply initial state
        ApplyFlashlightState(flashlightEnabled.Value);

        gameObject.SetActive(false);
    }

    public void OnDisable() {
        //flashlightUI.SetActive(false);
        if(geistParticles.isPlaying) geistParticles.Stop();
    }

    private void OnFlashlightChanged(bool oldValue, bool newValue) {
        ApplyFlashlightState(newValue);
    }

    public void ApplyFlashlightState(bool newState) {
        flashlightEnabled.Value = newState;
        flashlightObject.SetActive(newState);

        if(flashlightEnabled.Value) {
            source.PlayOneShot(turnOnClip);
            geistParticles.Play();
        }
        else {
            source.PlayOneShot(turnOffClip);
            geistParticles.Stop();
        }
    }

    [ServerRpc]
    private void ToggleFlashlightServerRpc() {
        Debug.Log("Activate Flashlight.");
        flashlightEnabled.Value = !flashlightEnabled.Value;
    }

    private void Update() {

        
        if(!IsOwner) {
            return;
        }

        Debug.Log("I AM THE OWNER");

        if(Input.GetKeyDown(KeyCode.F)) {
            Debug.Log("Activate Flashlight2.");

            ToggleFlashlightServerRpc();
        }
    }

    public void GeistLightUIUpdate() {
        /*
        if(isFlashing && !isTired) {
            staminaRemaining -= 1 * Time.deltaTime;

            if(raycastScript.curseScript != null && raycastScript.curseScript.charge <= 100f) {
                raycastScript.curseScript.charge += 40 * Time.deltaTime;
                gameObject.GetComponent<LookAtWithDelay>().targetObject = raycastScript.curseScript.transform;
                gameObject.GetComponent<LookAtWithDelay>().working = true;
            }
            if(raycastScript.curseScript != null && raycastScript.curseScript.charge >= 100f && raycastScript.curseScript.geistLight.intensity == 0) {
                raycastScript.curseScript.DisplayCurse(CursedObject.CursedTypes.Glowing, true);

            }
        }

        if(isFlashing && !isTired) {
            staminaRemaining -= 1 * Time.deltaTime;
            if(hideBarWhenFull && useSprintBar) { sprintBarCG.alpha += 5 * Time.deltaTime; }

        }
        else {
            staminaRemaining = Mathf.Clamp(staminaRemaining += sprintRecoveryDec * Time.deltaTime, 0, sprintDuration);
            gameObject.GetComponent<LookAtWithDelay>().targetObject = defaultLookPoint.transform; //.working = false;
        }

        float sprintRemainingPercent = staminaRemaining / sprintDuration;
        //if(useSprintBar) sprintBar.rectTransform.sizeDelta = new Vector2(sprintRemainingPercent * 175, sprintBar.rectTransform.sizeDelta.y);//sprintBar.transform.localScale = new Vector3(sprintRemainingPercent, 1f, 1f);

        if(staminaRemaining <= 0) {
            isTired = true;
            //sprintBar.color = Color.red;
            source.PlayOneShot(turnOnClip);

        }
        if(staminaRemaining == sprintDuration) {
            isTired = false;
            //if(hideBarWhenFull && useSprintBar) { sprintBarCG.alpha -= 3 * Time.deltaTime; }
            //if(useSprintBar) sprintBar.color = stamBarUIColor;
        }
        */
    }
}
