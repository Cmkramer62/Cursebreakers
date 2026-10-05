using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour {

    [Range(0.1f, 4f)] public float mouseSensitivity = 2f;
    [Range(0f, 2f)] public float cameraSmoothTime = 0.04f;

    public Transform playerBody, cameraParent;
    public bool allowedToLook = true, playerAlive = true;
    [HideInInspector] public Animator cameraAnimator;

    // Up and Down
    private float targetPitch;
    private float currentPitch;
    private float pitchVelocity;

    // Left and Right
    private float targetCameraYaw;
    private float currentCameraYaw;
    private float yawVelocity;

    void Start() {
        if(cameraParent != null) {
            targetCameraYaw = currentCameraYaw = cameraParent.localEulerAngles.y;
        }
    }

    // Update is called once per frame
    void Update() {
        if(playerBody == null || !allowedToLook || !playerAlive)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        playerBody.Rotate(Vector3.up * mouseX);

        targetCameraYaw += mouseX;
        targetPitch = Mathf.Clamp(targetPitch - mouseY, -90f, 90f);
    }

    void LateUpdate() {
        if(playerBody == null || !allowedToLook || !playerAlive)
            return;

        currentCameraYaw = Mathf.SmoothDampAngle(currentCameraYaw, targetCameraYaw, ref yawVelocity, cameraSmoothTime);

        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, cameraSmoothTime);

        cameraParent.localRotation = Quaternion.Euler(0f, currentCameraYaw, 0f);
        transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }
}
