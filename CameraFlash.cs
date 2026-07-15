using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class CameraFlash : NetworkBehaviour {

    public GameObject lightFlash;
    public AudioSource source;
    public AudioClip flashClip;

    private bool flashOnCooldown = false;
    public float staminaRemaining = 5f, sprintDuration = 5f;
    public Image sprintBar;
    public GameObject cameraUI;

    public override void OnNetworkSpawn() {
        gameObject.SetActive(false);
    }

    public void OnEnable() {
        //cameraUI.SetActive(true);
       
    }

    private void OnDisable() {
        lightFlash.SetActive(false);
        //cameraUI.SetActive(false);
        flashOnCooldown = false;
    }

    // Update is called once per frame
    void Update() {
        if(!IsOwner) return;

        if(Input.GetKeyDown(KeyCode.F) && !flashOnCooldown) {
            TakePictureServerRpc();
            
            StartCoroutine(Cooldown());
        }
    }

    [ServerRpc]
    void TakePictureServerRpc() {
        Debug.Log("Server one");
        TakePictureClientRpc();

    }

    [ClientRpc]
    void TakePictureClientRpc() {
        Debug.Log("client one");
        StartCoroutine(FlashVisualEffects());
    }

    private IEnumerator FlashVisualEffects() {
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
        string temp = "";
        temp += "curse trig ";
        foreach(CursedObject objectee in gameObject.transform.parent.parent.parent.GetComponent<ToolController>().objectsList) {
            temp += (" - ");
            objectee.DisplayCurse(CursedObject.CursedTypes.Aura, state);
        }
        Debug.Log(temp);

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
