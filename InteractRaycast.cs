using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractRaycast : MonoBehaviour {

    [SerializeField]
    private int rayLength = 7, lookAtRayLength = 25;
    [SerializeField]
    private LayerMask layerMaskInteract;
    [SerializeField]
    private string excludeLayerName = null;
    public string hitLayerName = "";

    public GameObject crosshairUI;
    public bool allowedToRaycast = true;
    public AudioSource source;
    public AudioClip clip;
    public float volumeOfClick = 0.2f;

    public Animator crosshairAnimator;
    public CursedObject curseScript;

    private void OnEnable() {
        //crosshairUI = GameObject.Find("Default Crosshair");
    }


    private void Update() {
        RaycastHit hit;
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        int excludeMask = 1 << LayerMask.NameToLayer(excludeLayerName);
        int mask = layerMaskInteract.value & ~excludeMask;
        //int mask = 1 << LayerMask.NameToLayer(excludeLayerName) | layerMaskInteract.value;
        if(allowedToRaycast && Physics.Raycast(transform.position, fwd, out hit, rayLength, mask) && LayerMask.LayerToName(hit.transform.gameObject.layer) == hitLayerName) {
            crosshairUI.SetActive(true);
            if(hit.transform.CompareTag("CursedObject")) curseScript = hit.transform.GetComponentInChildren<CursedObject>();

            if(Input.GetKeyDown(KeyCode.E)) {
                source.PlayOneShot(clip, volumeOfClick);
                crosshairAnimator.Play("Crosshair Bump Anim");
                switch(hit.transform.gameObject.tag) {
                    case "Door":
                        hit.transform.parent.parent.gameObject.GetComponent<Door>().InteractDoor();
                        break;
                    case "Generic":
                        InteractPrompt seenPrompt = hit.transform.GetComponent<InteractPrompt>();
                        seenPrompt.InteractWithObject();
                        if(seenPrompt.list) source.PlayOneShot(seenPrompt.interactWithSound, volumeOfClick);
                        break;
                    case "CursedObject":
                        InteractPrompt seenPrompt2 = hit.transform.GetComponent<InteractPrompt>();
                        seenPrompt2.InteractWithObject();
                        if(seenPrompt2.list) source.PlayOneShot(seenPrompt2.interactWithSound, volumeOfClick);
                        break;
                    case "Takeable":
                        hit.transform.GetComponent<TakebleObject>().Take();
                        break;
                    case "HidingSpot":
                        if(!hit.transform.GetComponent<HidingSpot>().hidingAnimOnCooldown) hit.transform.GetComponent<HidingSpot>().Hide(gameObject.transform.parent.parent.gameObject);
                        break;
                    case "Candle":
                        hit.transform.GetComponent<Candle>().InteractWithCandle();
                        break;
                    case "Light":
                        hit.transform.GetComponent<LightFlicker>().InvertLightState();
                        break;
                    case "Activator":
                        hit.transform.GetComponent<ActivatorSwitch>().Activate();
                        break;
                }
            }
            

        }
        else {
            crosshairUI.SetActive(false);
            curseScript = null;
        }

        if(allowedToRaycast && Physics.Raycast(transform.position, fwd, out hit, lookAtRayLength, mask) && hit.transform.gameObject.CompareTag("JumpscareLook") && LayerMask.LayerToName(hit.transform.gameObject.layer) == hitLayerName)  {
                JumpscareTrigger triggerScript = hit.transform.gameObject.GetComponent<JumpscareTrigger>();
                if(triggerScript.currentTrigger == JumpscareTrigger.TriggerType.Seen) {
                    triggerScript.Jumpscare();
                }
            
        }
    }


}
