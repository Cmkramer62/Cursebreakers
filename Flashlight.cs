using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Flashlight : MonoBehaviour {

    public bool flashlightState = true;
    public GameObject flashlightObject, flashlightUI, defaultLookPoint;
    public AudioSource source;//, glowingSource;
    public AudioClip turnOnClip, turnOffClip;

    public CanvasGroup sprintBarCG;
    public Image sprintBar;
    public float sprintDuration, staminaRemaining, sprintRecoveryDec;//, lightBlastVolume = 0.7f;
    public bool isFlashing, isTired, hideBarWhenFull = false, useSprintBar = true;

    public InteractRaycast raycastScript;

    public ParticleSystem geistParticles;
    public Color stamBarUIColor;
    [SerializeField] private PlayerHandler playerHandlerScript;

    private void OnEnable() {
        //flashlightUI.SetActive(true);
    }


    public void OnDisable() {
        //flashlightUI.SetActive(false);
        if(geistParticles.isPlaying) geistParticles.Stop();
    }

    public void SwapOver() {
        flashlightState = !flashlightState;
        flashlightObject.SetActive(flashlightState);

        if(flashlightState) source.PlayOneShot(turnOnClip);
        else source.PlayOneShot(turnOffClip);
    }

    private void Update() {
        if(!playerHandlerScript.IsOwner) {
            enabled = false;
            return;
        }
        if(!isFlashing && Input.GetKeyDown(KeyCode.F) && !isTired) {
            source.PlayOneShot(turnOffClip);
        }
        if(isFlashing && Input.GetKeyUp(KeyCode.F)) {
            source.PlayOneShot(turnOnClip);
        }
        if(Input.GetKeyDown(KeyCode.F) && !isTired) {
            isFlashing = true;
        }
        if(Input.GetKeyUp(KeyCode.F) || isTired) {
            isFlashing = false;
        }

        flashlightState = isFlashing;
        flashlightObject.SetActive(isFlashing);

        if(isFlashing && !geistParticles.isPlaying) geistParticles.Play();
        else if(!isFlashing && geistParticles.isPlaying) geistParticles.Stop();
    }

    public void GeistLightUIUpdate() {
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
    }
}
