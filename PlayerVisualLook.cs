using UnityEngine;
using Unity.Netcode;

public class PlayerVisualLook : NetworkBehaviour {
    [Header("Character Rig")]
    [SerializeField] private Animator characterAnimator;

    [Header("Head Tracking")]
    [SerializeField] private float maxHeadYaw = 40f;
    [SerializeField] private float headTurnSpeed = 180f;


    [Header("Visual Body")]
    [SerializeField] private Transform skeletonRoot;


    private NetworkVariable<float> networkLookYaw = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    private NetworkVariable<float> networkLookPitch = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private Transform visualHead;
    private Quaternion headInitialLocalRotation;
    private MouseLook localMouseLook;
    private float currentHeadYaw;
    private float previousRootYaw;
    private Quaternion skeletonInitialLocalRotation;
    private Quaternion skeletonInitialWorldRotation;
    private float initialRootYaw;
    private float visualBodyYaw;
    [SerializeField] private float visualBodyTurnSpeed = 120f;

    [Header("Turn Animations")]
    [SerializeField] private float turnAnimationThreshold = 5f;
    private static readonly int TurnLeftHash = Animator.StringToHash("TurnLeft");
    private static readonly int TurnRightHash = Animator.StringToHash("TurnRight");
    private static readonly int IsTurningHash = Animator.StringToHash("IsTurning");
    private float previousVisualBodyYaw;
    private bool wasBodyTurning;

    [Header("Movement Detection")]
    [SerializeField] private float movementThreshold = 0.02f;
    private Vector3 previousRootPosition;
    public bool isVisuallyMoving;


    [SerializeField] private float maxHeadPitch = 75f;
    [SerializeField] private float headPitchSpeed = 180f;
    private float currentHeadPitch;

    public override void OnNetworkSpawn() {
        if(characterAnimator == null)
            characterAnimator = GetComponent<Animator>();

        if(characterAnimator != null &&
            characterAnimator.isHuman) {
            visualHead = characterAnimator.GetBoneTransform(
                HumanBodyBones.Head
            );
        }

        if(visualHead != null)
            headInitialLocalRotation = visualHead.localRotation;
        else
            Debug.LogWarning("PlayerVisualLook: Could not find the humanoid Head bone.", this);

        if(skeletonRoot == null) 
            Debug.LogWarning("PlayerVisualLook: Skeleton Root has not been assigned.", this);

        previousRootYaw = transform.eulerAngles.y;
        skeletonInitialLocalRotation = skeletonRoot.localRotation;
        initialRootYaw = transform.eulerAngles.y;
        visualBodyYaw = initialRootYaw;

        previousVisualBodyYaw = visualBodyYaw;

        skeletonInitialWorldRotation = skeletonRoot.rotation;

        previousRootPosition = transform.position;
        isVisuallyMoving = false;
    }

    private void Update() {
        if(!IsSpawned || !IsOwner || localMouseLook == null)
            return;

        networkLookYaw.Value = localMouseLook.LookYaw;
        networkLookPitch.Value = localMouseLook.LookPitch;
    }



    private void LateUpdate() {
        if(!IsSpawned || IsOwner || skeletonRoot == null)
            return;

        // Movement Detection
        Vector3 currentRootPosition = transform.position;
        Vector3 positionDelta = currentRootPosition - previousRootPosition;
        positionDelta.y = 0f;
        isVisuallyMoving = positionDelta.sqrMagnitude > movementThreshold * movementThreshold;
        previousRootPosition = currentRootPosition;

        float lookYaw = networkLookYaw.Value;

        // How far is the camera from the visible body's facing direction?
        float relativeYaw = Mathf.DeltaAngle(visualBodyYaw, lookYaw);

        // Turn the visible body only when the head reaches its limit.

        if(Mathf.Abs(relativeYaw) > maxHeadYaw || isVisuallyMoving) {

            float targetBodyYaw;
            if(isVisuallyMoving) {
                // While moving, face the direction the player is looking.
                targetBodyYaw = lookYaw;
            }
            else {
                // While stationary, turn only enough to keep the head within its limit.
                targetBodyYaw = lookYaw - Mathf.Sign(relativeYaw) * maxHeadYaw;
            }

            visualBodyYaw = Mathf.MoveTowardsAngle(
                visualBodyYaw,
                targetBodyYaw,
                visualBodyTurnSpeed * Time.deltaTime
            );

            float bodyYawDelta = Mathf.DeltaAngle(
                previousVisualBodyYaw,
                visualBodyYaw
            );

            if(!wasBodyTurning && Mathf.Abs(bodyYawDelta) > 0.01f) {
                if(bodyYawDelta < 0f)
                    characterAnimator.SetTrigger(TurnLeftHash);
                else
                    characterAnimator.SetTrigger(TurnRightHash);
            }

            wasBodyTurning = true;
            previousVisualBodyYaw = visualBodyYaw;
        }
        else {
            wasBodyTurning = false;
            previousVisualBodyYaw = visualBodyYaw;
        }

        bool isTurning = Mathf.Abs(
            Mathf.DeltaAngle(visualBodyYaw, lookYaw)
        ) > maxHeadYaw;

        characterAnimator.SetBool(IsTurningHash, isTurning);

        // Rotate the skeleton relative to its original world orientation,
        // independently of the networked root's changing yaw.
        float visualYawOffset = Mathf.DeltaAngle(
            initialRootYaw,
            visualBodyYaw
        );

        skeletonRoot.rotation =
            Quaternion.AngleAxis(visualYawOffset, Vector3.up)
            * skeletonInitialWorldRotation;

        // Calculate the head's yaw relative to the visible body's facing direction.
        float relativeHeadYaw = -Mathf.DeltaAngle(
            visualBodyYaw,
            networkLookYaw.Value
        );

        // Keep the head within its horizontal turning limit.
        float desiredHeadYaw = Mathf.Clamp(
            relativeHeadYaw,
            -maxHeadYaw,
            maxHeadYaw
        );

        // Smoothly rotate the head toward the desired angle.
        currentHeadYaw = Mathf.MoveTowardsAngle(
            currentHeadYaw,
            desiredHeadYaw,
            headTurnSpeed * Time.deltaTime
        );

        // Apply the yaw and pitch while preserving the head's original rig orientation.
        float desiredHeadPitch = Mathf.Clamp(
            networkLookPitch.Value,
            -maxHeadPitch,
            maxHeadPitch
        );

        currentHeadPitch = Mathf.MoveTowards(
            currentHeadPitch,
            desiredHeadPitch,
            headPitchSpeed * Time.deltaTime
        );

        visualHead.localRotation =
            headInitialLocalRotation *
            Quaternion.Euler(
                currentHeadYaw,
                0f,
                currentHeadPitch
            );
    }


    public void SetLocalMouseLook(MouseLook mouseLook) {
        if(IsOwner)
            localMouseLook = mouseLook;
    }

    public float GetNetworkLookYaw() {
        return networkLookYaw.Value;
    }


}
