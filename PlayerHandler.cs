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
    public CameraFollow cameraReference;
    public ParticleSystem channelParticles;

    public override void OnNetworkSpawn() {
        if(!IsOwner) {
            rotationMatchingScriptCamhead.enabled = false;
            rotationMatchingScriptToolbelt.enabled = false;
            return;
        }
        Debug.Log("Player OnNetworkSpawn");

        ghostScript = GameObject.FindAnyObjectByType<Enemy>();
        StartCoroutine(FindCamera());
        /*
        cameraReference = FindObjectOfType<CameraFollow>();
        cameraReference.SetTarget(cameraHolder);
        cameraReference.GetComponent<PingCreator>().playerScript = playerMovementScript;
        cameraReference.transform.GetChild(3).GetComponent<HeadBob>().playerMovement = playerMovementScript;
        cameraReference.GetComponent<MouseLook>().playerBody = playerMovementScript.transform.parent;
        cameraReference.GetComponent<MouseLook>().cameraAnimator = animatorRef;

        rotationMatchingScriptCamhead.goFollow = cameraReference.gameObject;
        rotationMatchingScriptToolbelt.goFollow = cameraReference.gameObject;

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

        flashlightScript.raycastScript = cameraReference.GetComponent<InteractRaycast>();
        */
    }

    [ClientRpc]
    public void SetSpawnPositionClientRpc( Vector3 position, Quaternion rotation, ClientRpcParams clientRpcParams = default) {
        GetComponent<CharacterController>().enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        GetComponent<CharacterController>().enabled = true;
    }

    private IEnumerator FindCamera() {
        while(cameraReference == null) {
            cameraReference = FindObjectOfType<CameraFollow>();
            yield return null;
        }

        cameraReference.SetTarget(cameraHolder);
        cameraReference.GetComponent<PingCreator>().playerScript = playerMovementScript;
        cameraReference.transform.GetChild(3).GetComponent<HeadBob>().playerMovement = playerMovementScript;
        cameraReference.GetComponent<MouseLook>().playerBody = playerMovementScript.transform.parent;
        cameraReference.GetComponent<MouseLook>().cameraAnimator = animatorRef;

        rotationMatchingScriptCamhead.goFollow = cameraReference.gameObject;
        rotationMatchingScriptToolbelt.goFollow = cameraReference.gameObject;

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

        flashlightScript.raycastScript = cameraReference.GetComponent<InteractRaycast>();
    }
}
