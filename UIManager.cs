using UnityEngine;
using UnityEngine.UI;

/*
 * UIManager is meant to be the client-side controller of all local UI.
 * No part of this is synchronized to multiplayer.
 */
public class UIManager : MonoBehaviour {
    public static UIManager Instance;

    [SerializeField] private InteractRaycast raycastScript;
    private Flashlight geistlightReference;

    public CanvasGroup geistlightUICanvasGroup;
    public Image geistlightUIMeterBar;
    public bool hideBarWhenFull = false, useSprintBar = true;
    public Color geistlightBarDefaultColor;


    private void Awake() {
        Instance = this;
    }

    private void Update() {
        // Flashlight UI
        
        if(geistlightReference != null) {
            GeistLightUIUpdate();
        }

    }

    /*
     * Register Player is called by the flashlight/player manager upon Network Spawn.
     * It simply registers itself to this client-side script.
     */
    public void RegisterPlayer(Flashlight geistlight) {
        Debug.Log("Registered local player to UI");

        // Example hookup
        // flashlight.flashlightEnabled.OnValueChanged += UpdateFlashlightUI;
        // We don't actualy want to only update the UI upon the value changing however. We want to update second by second based on the usage.
        // Ignore onvaluechanged line. add storage of flashlight.cs reference.
        geistlightReference = geistlight;
    }

    private void UpdateFlashlightUI(bool oldValue, bool newValue) {


        // Update icon, battery bar, etc.
    }
    
    public void GeistLightUIUpdate() {
        geistlightUICanvasGroup.gameObject.SetActive(geistlightReference.gameObject.activeInHierarchy);

        if(geistlightReference.isFlashing && !geistlightReference.isTired) {
            geistlightReference.staminaRemaining -= 1 * Time.deltaTime;

            if(geistlightReference.raycastScript.curseScript != null && raycastScript.curseScript.charge <= 100f) {
                raycastScript.curseScript.charge += 40 * Time.deltaTime;
                gameObject.GetComponent<LookAtWithDelay>().targetObject = raycastScript.curseScript.transform;
                gameObject.GetComponent<LookAtWithDelay>().working = true;
            }
            if(raycastScript.curseScript != null && raycastScript.curseScript.charge >= 100f && raycastScript.curseScript.geistLight.intensity == 0) {
                raycastScript.curseScript.DisplayCurse(CursedObject.CursedTypes.Glowing, true);

            }
        }

        if(geistlightReference.isFlashing && !geistlightReference.isTired) {
            geistlightReference.staminaRemaining -= 1 * Time.deltaTime;
            if(hideBarWhenFull && useSprintBar) { geistlightUICanvasGroup.alpha += 5 * Time.deltaTime; }

        }
        else {
            geistlightReference.staminaRemaining = Mathf.Clamp(geistlightReference.staminaRemaining += geistlightReference.sprintRecoveryDec * Time.deltaTime, 0, geistlightReference.sprintDuration);
            //gameObject.GetComponent<LookAtWithDelay>().targetObject = geistlightReference.defaultLookPoint.transform; //.working = false;
        }

        float sprintRemainingPercent = geistlightReference.staminaRemaining / geistlightReference.sprintDuration;
        if(useSprintBar) geistlightUIMeterBar.rectTransform.sizeDelta = new Vector2(sprintRemainingPercent * 175, geistlightUIMeterBar.rectTransform.sizeDelta.y);//sprintBar.transform.localScale = new Vector3(sprintRemainingPercent, 1f, 1f);

        if(geistlightReference.staminaRemaining <= 0) {
            geistlightReference.isTired = true;
            geistlightUIMeterBar.color = Color.red;
            //source.PlayOneShot(turnOnClip);

        }
        if(geistlightReference.staminaRemaining == geistlightReference.sprintDuration) {
            geistlightReference.isTired = false;
            if(hideBarWhenFull && useSprintBar) { geistlightUICanvasGroup.alpha -= 3 * Time.deltaTime; }
            if(useSprintBar) geistlightUIMeterBar.color = geistlightBarDefaultColor;
        }
        
    }

}