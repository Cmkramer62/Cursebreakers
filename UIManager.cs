using UnityEngine;
using UnityEngine.UI;

/*
 * UIManager is meant to be the client-side controller of all local UI.
 * No part of this is synchronized to multiplayer.
 */
public class UIManager : MonoBehaviour {
    public static UIManager Instance;

    [SerializeField] private InteractRaycast raycastScript;
    private Flashlight geistlightScript;
    private PlayerMovement movementScript;

    public CanvasGroup geistlightUICanvasGroup;
    public Image geistlightUIMeterBar, slidingImage;
    public bool hideBarWhenFull = false, useSprintBar = true;
    public Color geistlightBarDefaultColor;

    private void Awake() {
        Instance = this;
    }

    private void Update() {
        // Flashlight UI
        
        if(geistlightScript != null) {
            GeistLightUIUpdate();
        }

        if(movementScript != null) {
            // update stamina bar.
            slidingImage.gameObject.SetActive(movementScript.SlidingState());
        }
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

}