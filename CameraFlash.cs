using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraFlash : MonoBehaviour {

    public GameObject lightFlash;
    public AudioSource source;
    public AudioClip flashClip;

    private bool flashOnCooldown = false;
    public float staminaRemaining = 5f, sprintDuration = 5f;
    public Image sprintBar;
    public GameObject cameraUI;
    [SerializeField] private PlayerHandler playerHandlerScript;

    public void OnEnable() {
        //cameraUI.SetActive(true);
        if(!playerHandlerScript.IsOwner) {
            enabled = false;
            return;
        }
    }
    private void OnDisable() {
        lightFlash.SetActive(false);
        //cameraUI.SetActive(false);
        flashOnCooldown = false;
    }

    // Update is called once per frame
    void Update() {
        if(Input.GetKeyDown(KeyCode.F) && !flashOnCooldown) {
            StartCoroutine(FlashTimer());
            StartCoroutine(Cooldown());
        }

        
    }

    private IEnumerator FlashTimer() {
        source.PlayOneShot(flashClip);
        staminaRemaining = 0;

        lightFlash.SetActive(true);
        TriggerCurse(true);
        yield return new WaitForSeconds(.05f);

        lightFlash.SetActive(false);
        TriggerCurse(false);
        yield return new WaitForSeconds(.05f);

        lightFlash.SetActive(true);
        TriggerCurse(true);
        yield return new WaitForSeconds(.05f);

        lightFlash.SetActive(false);
        TriggerCurse(false);
    }

    private void TriggerCurse(bool state) {
        foreach(CursedObject objectee in gameObject.transform.parent.parent.parent.GetComponentInChildren<ToolController>().objectsList) {
            objectee.DisplayCurse(CursedObject.CursedTypes.Aura, state);
        }
    }

    private IEnumerator Cooldown() {
        flashOnCooldown = true;
        yield return new WaitForSeconds(sprintDuration);
        flashOnCooldown = false;
    }

    public void CameraUIUpdate() {
        staminaRemaining = Mathf.Clamp(staminaRemaining += 1f * Time.deltaTime, 0, sprintDuration);
        float sprintRemainingPercent = staminaRemaining / sprintDuration;
        //sprintBar.rectTransform.sizeDelta = new Vector2(sprintRemainingPercent * 175, sprintBar.rectTransform.sizeDelta.y);
    }
}
