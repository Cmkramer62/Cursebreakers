using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerHandler : NetworkBehaviour {

    public PlayerMovement playerMovementScript;
    private ToolController toolControllerScript;
    private Enemy ghostScript;

    public NetworkVariable<float> stamina = new NetworkVariable<float>(40, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public Transform cameraHolder;

    [SerializeField] private SkinnedMeshRenderer[] bodySkinnedRenderers;
    [SerializeField] private MeshRenderer[] bodyMeshRenderers;
    [SerializeField] private Animator animatorRef;
    [SerializeField] private Flashlight flashlightScript;

    // Both the toolbelt and the main camera script need to match the camera's rotation. Tools for the obv.
    // Camera for the reason of mimicing the position of the cam's sphere child.
    [SerializeField] private MatchRotation rotationMatchingScriptCamhead, rotationMatchingScriptToolbelt;

    public override void OnNetworkSpawn() {
        if(!IsOwner) {
            rotationMatchingScriptCamhead.enabled = false;
            rotationMatchingScriptToolbelt.enabled = false;
            return;
        }

        ghostScript = GameObject.FindAnyObjectByType<Enemy>();

        var cam = FindObjectOfType<CameraFollow>();
        cam.SetTarget(cameraHolder);
        cam.GetComponent<PingCreator>().playerScript = playerMovementScript;
        cam.transform.GetChild(3).GetComponent<HeadBob>().playerMovement = playerMovementScript;
        cam.GetComponent<MouseLook>().playerBody = playerMovementScript.transform.parent;
        cam.GetComponent<MouseLook>().cameraAnimator = animatorRef;

        rotationMatchingScriptCamhead.goFollow = cam.gameObject;
        rotationMatchingScriptToolbelt.goFollow = cam.gameObject;

        if(ghostScript != null) {
            playerMovementScript.enemyVisionScript = ghostScript.GetComponent<ConeLOSDetector>();
            playerMovementScript.enemyVisionScript.AddTarget(playerMovementScript.transform);
            ghostScript.GetComponent<GhostRandomizer>().deathScript = GetComponent<Death>();
        }
        Cursor.lockState = CursorLockMode.Locked;

        foreach(SkinnedMeshRenderer bodyRenderer in bodySkinnedRenderers) {
            bodyRenderer.enabled = false;
        }
        foreach(MeshRenderer meshRenderObj in bodyMeshRenderers) {
            meshRenderObj.enabled = false;
        }

        flashlightScript.raycastScript = cam.GetComponent<InteractRaycast>();
    }
}
