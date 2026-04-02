using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadBob : MonoBehaviour {

    public float sprintingIntensity = 8.8f, walkingIntensity = 6f, idleIntensity = .6f;
    public float sprintDepth = .4f, walkingDepth = .1f, idleDepth = .025f;

    [SerializeField]
    private GroundChecker groundChecker;

    [HideInInspector] public PlayerMovement playerMovement;

    private bool stepped = false;
    private bool isIdle = false;
    public bool playSounds = true;

    private Vector3 objectOrigin;
    public Transform objectParent;
    private float idleCounter;
    private float normalCounter;
    private Vector3 objectBobPosition;

    [SerializeField] private PlayerHandler playerHandlerScript;

    private void Start() {
        if(playerHandlerScript != null && !playerHandlerScript.IsOwner) {
            enabled = false;
            return;
        }
        objectOrigin = objectParent.localPosition;
    }


    private void HeadBobCall(float p_z, float p_x_intensity, float p_y_intesity) {//Headbob for Camera
        objectBobPosition = objectOrigin + new Vector3(Mathf.Cos(p_z) * p_x_intensity, Mathf.Sin(p_z * 2) * p_y_intesity, 0);
        if(0 >= (Mathf.Sin(p_z * 2) * p_y_intesity) && !stepped) {
            stepped = true;
            if(playSounds && playerMovement.allowedToMove && groundChecker.isGrounded && !isIdle) {
                groundChecker.PlaySound();
            }

        }
        else if(0 < (Mathf.Sin(p_z * 2) * p_y_intesity)) {
            stepped = false;
        }

    }

    private void Update() {
        if(playerMovement!= null) {
            objectParent.localPosition = Vector3.Lerp(objectParent.localPosition, objectBobPosition, Time.deltaTime * 8f);
            if(playerMovement.allowedToMove && playerMovement.isSprinting) { //&& playerMovement.allowedToMove) {
                HeadBobCall(normalCounter, sprintDepth, sprintDepth);
                normalCounter += (Time.deltaTime * sprintingIntensity);
                isIdle = false;
            }
            else if(playerMovement.allowedToMove && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)) && !playerMovement.isSprinting) {// && playerMovement.allowedToMove) {
                HeadBobCall(idleCounter, walkingDepth, walkingDepth);
                idleCounter += (Time.deltaTime * walkingIntensity);
                isIdle = false;
            }
            else if(playerMovement.allowedToMove) {
                isIdle = true;
                HeadBobCall(idleCounter, idleDepth, idleDepth);
                idleCounter += (Time.deltaTime * idleIntensity);
            }
            else {
                isIdle = true;
                HeadBobCall(idleCounter, idleDepth / 2, idleDepth / 2);
                idleCounter += (Time.deltaTime * (idleIntensity / 2));
            }
        }
    }

}
