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


    [Header("Remote Head Look")]
    public Transform visualHead;
    [SerializeField] private float maxHeadYaw = 60f;
    [SerializeField] private float headTurnSpeed = 180f;
    private float desiredHeadYaw;
    private Quaternion headInitialLocalRotation;
    private float currentHeadYaw;
    private float headYawVelocity;

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

        // Head Tracking Amount Calculation
        CalculateDesiredHeadYaw();
    }

    void LateUpdate() {
        if(playerBody == null || !allowedToLook || !playerAlive)
            return;

        currentCameraYaw = Mathf.SmoothDampAngle(currentCameraYaw, targetCameraYaw, ref yawVelocity, cameraSmoothTime);

        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, cameraSmoothTime);

        cameraParent.localRotation = Quaternion.Euler(0f, currentCameraYaw, 0f);
        transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);

        ApplyHeadRotation();
    }


    public void SetVisualHead(Transform head) {
        visualHead = head;

        if(visualHead != null) {
            headInitialLocalRotation = visualHead.localRotation;
            currentHeadYaw = 0f;
        }
    }

    private void ApplyHeadRotation() {
        if(visualHead == null)
            return;

        currentHeadYaw = Mathf.SmoothDamp(
            currentHeadYaw,
            desiredHeadYaw,
            ref headYawVelocity,
            0.08f
        );

        visualHead.localRotation =
            headInitialLocalRotation *
            Quaternion.Euler(0f, currentHeadYaw, 0f);
    }

    private void CalculateDesiredHeadYaw() {
        if(visualHead == null || playerBody == null)
            return;

        // Get the camera's horizontal viewing direction.
        Vector3 lookDirection = transform.forward;
        lookDirection.y = 0f;

        if(lookDirection.sqrMagnitude < 0.001f)
            return;

        // Calculate the camera's yaw relative to the body.
        float cameraWorldYaw =
            Quaternion.LookRotation(lookDirection).eulerAngles.y;

        desiredHeadYaw = Mathf.DeltaAngle(
            playerBody.eulerAngles.y,
            cameraWorldYaw
        );

        // Restrict the head's horizontal range.
        desiredHeadYaw = Mathf.Clamp(
            desiredHeadYaw,
            -maxHeadYaw,
            maxHeadYaw
        );

        Debug.Log($"Desired head yaw: {desiredHeadYaw:F1}");
    }

}
