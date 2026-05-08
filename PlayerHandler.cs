using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerHandler : NetworkBehaviour {

    public PlayerMovement playerMovementScript;
    private ToolController toolControllerScript;
    private Enemy ghostScript;

    public NetworkVariable<int> lives = new NetworkVariable<int>(3);
    public NetworkVariable<float> stamina = new NetworkVariable<float>(40, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public Transform cameraHolder;

    [SerializeField] private SkinnedMeshRenderer[] bodySkinnedRenderers;
    [SerializeField] private MeshRenderer[] bodyMeshRenderers;
    [SerializeField] private Animator animatorRef;
    [SerializeField] private Flashlight flashlightScript;

    public override void OnNetworkSpawn() {
        if(!IsOwner) return;

        ghostScript = GameObject.FindAnyObjectByType<Enemy>();

        var cam = FindObjectOfType<CameraFollow>();
        cam.SetTarget(cameraHolder);
        cam.GetComponent<PingCreator>().playerScript = playerMovementScript;
        cam.transform.GetChild(3).GetComponent<HeadBob>().playerMovement = playerMovementScript;
        cam.GetComponent<MouseLook>().playerBody = playerMovementScript.transform.parent;
        cam.GetComponent<MouseLook>().cameraAnimator = animatorRef;

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
