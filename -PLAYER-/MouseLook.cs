using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour {

    [Range(0.1f, 4f)] public float mouseSensitivity = 2f;
    public bool invertHorizontalLook;
    public bool invertVerticalLook;
    [Range(0f, 2f)] public float cameraSmoothTime = 0.04f;

    public Transform playerBody, cameraParent;
    public bool allowedToLook = true, playerAlive = true, playerHanging = false;
    [HideInInspector] public Animator cameraAnimator;

    // Up and Down
    private float targetPitch;
    private float currentPitch;
    private float pitchVelocity;

    // Left and Right
    private float targetCameraYaw;
    private float currentCameraYaw;
    private float yawVelocity;
    [SerializeField, Min(0.01f)] private float ledgeFacingSmoothTime = 0.08f;
    private bool ledgeLookTransitionActive;
    private float targetBodyYaw;
    private float targetWorldYaw;
    private float currentBodyYaw;
    private float bodyYawVelocity;


    public float LookYaw => transform.eulerAngles.y;
    public float LookPitch => Mathf.DeltaAngle(
        0f,
        transform.localEulerAngles.x
    );
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

    /// <summary>Locks mouse look and aligns the body and camera during a ledge hang.</summary>
    public void SetLedgeHangLook(bool hanging, Vector3 facingDirection) {
        playerHanging = hanging;
        if(!hanging) {
            ledgeLookTransitionActive = false;
            return;
        }
        if(playerBody == null) return;

        facingDirection = Vector3.ProjectOnPlane(facingDirection, Vector3.up);
        if(facingDirection.sqrMagnitude < 0.001f) return;

        float facingYaw = Quaternion.LookRotation(facingDirection.normalized, Vector3.up).eulerAngles.y;
        targetBodyYaw = facingYaw;
        targetWorldYaw = facingYaw;
        currentBodyYaw = playerBody.eulerAngles.y;
        bodyYawVelocity = 0f;

        targetCameraYaw = GetCameraLocalYawForWorldYaw(targetWorldYaw);
        currentCameraYaw = cameraParent != null ? cameraParent.localEulerAngles.y : targetCameraYaw;
        yawVelocity = 0f;
        targetPitch = 0f;
        currentPitch = Mathf.DeltaAngle(0f, transform.localEulerAngles.x);
        pitchVelocity = 0f;
        ledgeLookTransitionActive = true;
        desiredHeadYaw = 0f;
        currentHeadYaw = 0f;
        headYawVelocity = 0f;
    }

    // Update is called once per frame
    void Update() {
        if(playerBody == null || !allowedToLook || !playerAlive || playerHanging)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * (invertHorizontalLook ? -1f : 1f);
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertVerticalLook ? -1f : 1f);

        playerBody.Rotate(Vector3.up * mouseX);

        targetCameraYaw += mouseX;
        targetPitch = Mathf.Clamp(targetPitch - mouseY, -90f, 90f);

        // Head Tracking Amount Calculation
        CalculateDesiredHeadYaw();
    }

    void LateUpdate() {
        if(playerBody == null || !allowedToLook || !playerAlive)
            return;

        if(playerHanging) {
            if(ledgeLookTransitionActive) UpdateLedgeLookTransition();
            return;
        }

        currentCameraYaw = Mathf.SmoothDampAngle(currentCameraYaw, targetCameraYaw, ref yawVelocity, cameraSmoothTime);

        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, cameraSmoothTime);

        cameraParent.localRotation = Quaternion.Euler(0f, currentCameraYaw, 0f);
        transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    private void UpdateLedgeLookTransition() {
        float smoothTime = Mathf.Max(0.01f, ledgeFacingSmoothTime);
        currentBodyYaw = Mathf.SmoothDampAngle(currentBodyYaw, targetBodyYaw, ref bodyYawVelocity, smoothTime);
        // Recalculate the local camera yaw as its parent turns, keeping the camera aimed
        // toward the same world-space direction without doubling the body's rotation.
        targetCameraYaw = GetCameraLocalYawForWorldYaw(targetWorldYaw);
        currentCameraYaw = Mathf.SmoothDampAngle(currentCameraYaw, targetCameraYaw, ref yawVelocity, smoothTime);
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, smoothTime);

        playerBody.rotation = Quaternion.Euler(0f, currentBodyYaw, 0f);
        if(cameraParent != null) cameraParent.localRotation = Quaternion.Euler(0f, currentCameraYaw, 0f);
        transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);

        if(Mathf.Abs(Mathf.DeltaAngle(currentBodyYaw, targetBodyYaw)) < 0.1f
            && Mathf.Abs(Mathf.DeltaAngle(currentCameraYaw, targetCameraYaw)) < 0.1f
            && Mathf.Abs(Mathf.DeltaAngle(currentPitch, targetPitch)) < 0.1f) {
            currentBodyYaw = targetBodyYaw;
            targetCameraYaw = GetCameraLocalYawForWorldYaw(targetWorldYaw);
            currentCameraYaw = targetCameraYaw;
            currentPitch = targetPitch;
            playerBody.rotation = Quaternion.Euler(0f, targetBodyYaw, 0f);
            if(cameraParent != null) cameraParent.localRotation = Quaternion.Euler(0f, targetCameraYaw, 0f);
            transform.localRotation = Quaternion.Euler(targetPitch, 0f, 0f);
            ledgeLookTransitionActive = false;
        }
    }

    private float GetCameraLocalYawForWorldYaw(float worldYaw) {
        if(cameraParent == null || cameraParent.parent == null) return worldYaw;

        Vector3 parentForward = Vector3.ProjectOnPlane(cameraParent.parent.forward, Vector3.up);
        if(parentForward.sqrMagnitude < 0.001f) return worldYaw;

        float parentYaw = Quaternion.LookRotation(parentForward, Vector3.up).eulerAngles.y;
        return Mathf.DeltaAngle(parentYaw, worldYaw);
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

