using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class InteractRaycast : NetworkBehaviour {

    [SerializeField] private int rayLength = 7, lookAtRayLength = 25;
    [SerializeField] private LayerMask layerMaskInteract;
    [SerializeField] private string excludeLayerName = null;
    public string hitLayerName = "";

    public bool allowedToRaycast = true, playerAlive = true, afterlife = false;
    public AudioSource source;
    public AudioClip clip;
    public float volumeOfClick = 0.2f;

    public Animator crosshairAnimator;
    public CursedObject curseScript;
    private Death deathScript;
    [SerializeField] private GameObject defaultCrosshair, interactCrosshair, afterlifeInteractCrosshair, channelLifeCrosshair; // cursedObjCrosshair
    public enum CrosshairType { Nothing, InteractLiving, InteractAfterlife, Channel, CursedObject}



    private void Update() {
        if(deathScript == null && GetComponent<MouseLook>().playerBody != null) {
            deathScript = GetComponent<MouseLook>().playerBody.GetComponent<Death>();
        }

        RaycastHit hit;
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        int excludeMask = 1 << LayerMask.NameToLayer(excludeLayerName);
        int mask = layerMaskInteract.value & ~excludeMask;
        CrosshairType currentRaycastedType;

        if(playerAlive && allowedToRaycast && Physics.Raycast(transform.position, fwd, out hit, rayLength, mask) && LayerMask.LayerToName(hit.transform.gameObject.layer) == hitLayerName) {
            //crosshairUI.SetActive(true);
            if(hit.transform.CompareTag("CursedObject")) curseScript = hit.transform.GetComponentInChildren<CursedObject>();

            // CROSSHAIR SECTION
            if(afterlife) {
                currentRaycastedType = CrosshairType.InteractAfterlife;
            }
            else {
                if(hit.transform.gameObject.tag == "PlayerBody") currentRaycastedType = CrosshairType.Channel;
                else currentRaycastedType = CrosshairType.InteractLiving;
            }
            

            // INPUT SECTION
            if(Input.GetKeyDown(KeyCode.E)) {
                source.PlayOneShot(clip, volumeOfClick);
                crosshairAnimator.Play("Crosshair Bump Anim");
                switch(hit.transform.gameObject.tag) {
                    case "Door":
                        if(!afterlife) hit.transform.parent.parent.gameObject.GetComponent<Door>().InteractDoor();
                        break;
                    case "Generic":
                        if(!afterlife) {
                            InteractPrompt seenPrompt = hit.transform.GetComponent<InteractPrompt>();
                            seenPrompt.InteractWithObject();
                            if(seenPrompt.list) source.PlayOneShot(seenPrompt.interactWithSound, volumeOfClick);
                        }
                        break;
                    case "CursedObject":
                        if(!afterlife) {
                            InteractPrompt seenPrompt2 = hit.transform.GetComponent<InteractPrompt>();
                            seenPrompt2.InteractWithObject();
                            if(seenPrompt2.list) source.PlayOneShot(seenPrompt2.interactWithSound, volumeOfClick);
                        }
                        break;
                    case "Takeable":
                        if(!afterlife) hit.transform.GetComponent<TakebleObject>().Take();
                        break;
                    case "HidingSpot":
                        if((!afterlife) && !hit.transform.GetComponent<HidingSpot>().hidingAnimOnCooldown) {
                            hit.transform.GetComponent<HidingSpot>().HideServerRpc(NetworkManager.Singleton.LocalClientId);
                            hit.transform.GetComponent<HidingSpot>().Hide(GetComponent<MouseLook>().playerBody.gameObject);
                        }
                        else if(afterlife) {
                            hit.transform.GetComponent<HidingSpot>().Paranormal();
                            // Draw ghost to that spot?
                            
                        }
                        break;
                    case "Candle":
                        if(!afterlife) hit.transform.GetComponent<Candle>().InteractWithCandle();
                        break;
                    case "Light":
                        if(!afterlife) hit.transform.GetComponent<LightFlicker>().InvertLightStateServerRpc();
                        else hit.transform.GetComponent<LightFlicker>().AfterlifeInvertLightStateServerRpc();
                        break;
                    case "Activator":
                        if(!afterlife) hit.transform.GetComponent<ActivatorSwitch>().Activate();
                        else if(!hit.transform.GetComponent<ActivatorSwitch>().state) hit.transform.GetComponent<ActivatorSwitch>().Activate();
                        // The above means they can only turn things off, which makes sense for TVS and distractors. But not for switches, which turn lights ON...
                        break;
                        
                    case "PlayerBody":
                        if(!afterlife) {
                            // Play noise.
                            // start animation?
                            deathScript.GetComponent<PlayerHandler>().channelParticles.Play();
                        }
                        break;
                        
                }
            }
            else if(Input.GetKey(KeyCode.E)) {
                if(hit.transform.gameObject.CompareTag("PlayerBody") && !afterlife) {
                    // crosshair, "hold E to channel a life".
                    // Start charging sequence, expending a stamina bar.
                    // unsynced float.
                    // bool state of charging.

                    // This camera script could be client or server.
                    // Have access to: this camera's player reference.
                    // looking at deadbody.
                    Debug.Log("channel trigger");
                    deathScript.channelingLife = true;
                    deathScript.playerArmsAnimator.SetBool("Channeling", true);

                    if(deathScript.channelingDone) {
                        Debug.Log("channel DONE");
                        hit.transform.gameObject.layer = 0;
                        deathScript.LoseLife(false);
                        //NetworkObject player = GetPlayer2ServerRpc(hit.transform.GetComponentInParent<PlayerDeadBody>().playerID.Value);
                        //GetPlayer2(hit.transform.GetComponentInParent<PlayerDeadBody>().playerID.Value).GetComponent<Death>().GainLifeServerRpc();
                        ulong clientsIDcode = hit.transform.GetComponentInParent<PlayerDeadBody>().playerID.Value;
                        deathScript.playerArmsAnimator.SetTrigger("ChannelDone");
                        deathScript.GetComponent<PlayerHandler>().channelParticles.Stop();

                        foreach(NetworkObject obj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList) {
                            if(obj.OwnerClientId == clientsIDcode) {
                                // This is that client's NetworkObject
                                obj.GetComponent<Death>().GainLifeServerRpc();
                            }
                        }

                        //DoSomethingToPlayerServerRpc(clientsIDcode);
                    }
                }
            }
            else if(Input.GetKeyUp(KeyCode.E) && deathScript.channelingLife) {
                deathScript.channelingLife = false; // UNOPTIMIZED?
                deathScript.playerArmsAnimator.SetBool("Channeling", false);
                Debug.Log("channel trigger off");
                deathScript.GetComponent<PlayerHandler>().channelParticles.Stop();

            }

        }
        else {
            currentRaycastedType = CrosshairType.Nothing;

            curseScript = null;
            if(deathScript != null && deathScript.channelingLife) {
                deathScript.channelingLife = false; // UNOPTIMIZED?
                deathScript.playerArmsAnimator.SetBool("Channeling", false);
                Debug.Log("channel trigger off");
                deathScript.GetComponent<PlayerHandler>().channelParticles.Stop();

            }
        }

        SetCrosshair(currentRaycastedType);

        if(allowedToRaycast && Physics.Raycast(transform.position, fwd, out hit, lookAtRayLength, mask) && hit.transform.gameObject.CompareTag("JumpscareLook") && LayerMask.LayerToName(hit.transform.gameObject.layer) == hitLayerName)  {
            JumpscareTrigger triggerScript = hit.transform.gameObject.GetComponent<JumpscareTrigger>();
            if(triggerScript.currentTrigger == JumpscareTrigger.TriggerType.Seen) triggerScript.Jumpscare();
        }
    }

    // This turns on the crosshair GameObject based on the corresponding enum parameter.
    // It will turn off all crosshairs that are not matching the passed parameter.
    private void SetCrosshair(CrosshairType currentRaycastedType) {
        defaultCrosshair.SetActive(currentRaycastedType == CrosshairType.Nothing);
        interactCrosshair.SetActive(currentRaycastedType == CrosshairType.InteractLiving);
        afterlifeInteractCrosshair.SetActive(currentRaycastedType == CrosshairType.InteractAfterlife);
        channelLifeCrosshair.SetActive(currentRaycastedType == CrosshairType.Channel);
    }

}