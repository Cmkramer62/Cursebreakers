using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;
using System.Collections;
using System.Collections.Generic;
/*
 * UIManager is meant to be the client-side controller of all local UI.
 * No part of this is synchronized to multiplayer.
 */
public class UIManager : MonoBehaviour {
    public static UIManager Instance;

    [SerializeField] private InteractRaycast raycastScript;
    private Flashlight geistlightScript;
    private PlayerMovement movementScript;
    private ToolController toolbeltScript;

    public CanvasGroup geistlightUICanvasGroup, sprintBarCG;
    public Image geistlightUIMeterBar, slidingImage, sprintBar;
    public bool hideBarWhenFull = false, useSprintBar = true;
    public Color geistlightBarDefaultColor;

    [SerializeField] private GameObject[] toolbarUI, toolbarMarkerUI;
    private int storedHeldIndex = -1;
    [SerializeField] private Color stamBarUIColor;

    [SerializeField] private ParticleSystem spellParticleVFX;
    [SerializeField] private Material[] spellParticleMaterials;

    private void Awake() {
        Instance = this;
    }

    private void Update() {
        // Flashlight UI
        
        if(geistlightScript != null) {
            GeistLightUIUpdate();
        }

        if(movementScript != null) {
            slidingImage.gameObject.SetActive(movementScript.SlidingState());
            // update stamina bar.
            StaminaUIUpdate();
        }

        if(toolbeltScript != null) {
            if(storedHeldIndex != toolbeltScript.heldIndex.Value) {
                StartCoroutine(ToolbeltUISwap(storedHeldIndex, toolbeltScript.heldIndex.Value));
                storedHeldIndex = toolbeltScript.heldIndex.Value;
            }
        }
    }

    private IEnumerator ToolbeltUISwap(int from, int to) {
        yield return new WaitForSeconds(.35f);
        if(from != -1) {
            toolbarUI[from].transform.localScale = new Vector3(.35f, .35f, .35f);
            toolbarUI[from].GetComponent<CanvasGroup>().alpha = .4f;
            toolbarMarkerUI[from].SetActive(false);
        }

        toolbarUI[to].transform.localScale = new Vector3(.37f, .37f, .37f);
        toolbarUI[to].GetComponent<CanvasGroup>().alpha = 1f;
        toolbarMarkerUI[to].SetActive(true);

        DisplaySpellParticle(to);
    }

    private void DisplaySpellParticle(int index) {
        
        if(index == 0) return; // Ignore empty hand. This isn't a spell.

        spellParticleVFX.GetComponent<ParticleSystemRenderer>().material = spellParticleMaterials[index];
        spellParticleVFX.Play();
        // add cooldown.
        // play sound effect?
    }

    /*
     * Register Player is called by the flashlight/player manager upon Network Spawn.
     * It simply registers itself to this client-side script.
     */
    public void RegisterGeistlight(Flashlight geistlightScript) {
        Debug.Log("Registered local player to UI");

        // Example hookup
        // flashlight.flashlightEnabled.OnValueChanged += UpdateFlashlightUI;
        // We don't actualy want to only update the UI upon the value changing however. We want to update second by second based on the usage.
        // Ignore onvaluechanged line. add storage of flashlight.cs reference.
        this.geistlightScript = geistlightScript;
    }

    public void RegisterPlayer(PlayerMovement movementScript) {
        this.movementScript = movementScript;
    }

    public void RegisterToolController(ToolController toolbeltScript) {
        this.toolbeltScript = toolbeltScript;
    }

    private void UpdateFlashlightUI(bool oldValue, bool newValue) {


        // Update icon, battery bar, etc.
    }
    
    public void GeistLightUIUpdate() {
        geistlightUICanvasGroup.gameObject.SetActive(geistlightScript.gameObject.activeInHierarchy);

        if(geistlightScript.isFlashing && !geistlightScript.isTired) {
            geistlightScript.staminaRemaining -= 1 * Time.deltaTime;

            if(geistlightScript.raycastScript.curseScript != null && raycastScript.curseScript.charge <= 100f) {
                raycastScript.curseScript.charge += 40 * Time.deltaTime;
                geistlightScript.GetComponent<LookAtWithDelay>().targetObject = raycastScript.curseScript.transform;
                geistlightScript.GetComponent<LookAtWithDelay>().working = true;
            }
            if(raycastScript.curseScript != null && raycastScript.curseScript.charge >= 100f && raycastScript.curseScript.geistLight.intensity == 0) {
                raycastScript.curseScript.DisplayCurse(CursedObject.CursedTypes.Glowing, true);

            }
        }

        if(geistlightScript.isFlashing && !geistlightScript.isTired) {
            geistlightScript.staminaRemaining -= 1 * Time.deltaTime;
            if(hideBarWhenFull && useSprintBar) { geistlightUICanvasGroup.alpha += 5 * Time.deltaTime; }

        }
        else {
            geistlightScript.staminaRemaining = Mathf.Clamp(geistlightScript.staminaRemaining += geistlightScript.sprintRecoveryDec * Time.deltaTime, 0, geistlightScript.sprintDuration);
            geistlightScript.GetComponent<LookAtWithDelay>().targetObject = geistlightScript.defaultLookPoint.transform; //.working = false;
        }

        float sprintRemainingPercent = geistlightScript.staminaRemaining / geistlightScript.sprintDuration;
        if(useSprintBar) geistlightUIMeterBar.rectTransform.sizeDelta = new Vector2(sprintRemainingPercent * 175, geistlightUIMeterBar.rectTransform.sizeDelta.y);//sprintBar.transform.localScale = new Vector3(sprintRemainingPercent, 1f, 1f);

        if(geistlightScript.staminaRemaining <= 0) {
            geistlightScript.isTired = true;
            geistlightUIMeterBar.color = Color.red;
            //source.PlayOneShot(turnOnClip);

        }
        if(geistlightScript.staminaRemaining == geistlightScript.sprintDuration) {
            geistlightScript.isTired = false;
            if(hideBarWhenFull && useSprintBar) { geistlightUICanvasGroup.alpha -= 3 * Time.deltaTime; }
            if(useSprintBar) geistlightUIMeterBar.color = geistlightBarDefaultColor;
        }
        
    }

    public void StaminaUIUpdate() {
        if(movementScript.isSprinting && !movementScript.isTired && hideBarWhenFull && useSprintBar) sprintBarCG.alpha += 5 * Time.deltaTime; 
      
        else movementScript.transform.parent.GetComponent<PlayerHandler>().stamina.Value 
                = Mathf.Clamp(movementScript.transform.parent.GetComponent<PlayerHandler>().stamina.Value 
                    += movementScript.staminaRecoveryRate * Time.deltaTime, 0, movementScript.staminaDuration);

        if(useSprintBar) sprintBar.rectTransform.sizeDelta 
                = new Vector2(movementScript.transform.parent.GetComponent<PlayerHandler>().stamina.Value / movementScript.staminaDuration * 175, sprintBar.rectTransform.sizeDelta.y);

        if(movementScript.isTired) {
            sprintBar.color = Color.red;
        }
        if(movementScript.transform.parent.GetComponent<PlayerHandler>().stamina.Value == movementScript.staminaDuration) {
            if(hideBarWhenFull && useSprintBar) sprintBarCG.alpha -= 3 * Time.deltaTime;
            if(useSprintBar) {
                sprintBar.color = stamBarUIColor;
            }
        }
    }
}