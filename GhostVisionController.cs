using UnityEngine;

public class GhostVisionController : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    public Transform ghost;

    [Header("Distance")]
    [SerializeField] private float maxEffectDistance = 20f;
    [SerializeField] private float minEffectDistance = 2f;

    [Header("Visibility")]
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private float ghostHeight = 2f;

    [Header("Proximity Override")]
    [SerializeField] private bool proximityOverrideEnabled = true;
    [SerializeField] private float proximityStartDistance = 5f;
    [SerializeField] private float proximityFullEffectDistance = 2f;

    [Header("Temporal Behavior")]
    [SerializeField] private float intensityRiseSpeed = 4f;
    [SerializeField] private float intensityFallSpeed = 8f;

    private float currentIntensity;

    [Header("Surges")]
    [SerializeField] private bool surgesEnabled = true;

    [SerializeField] private float surgeStrength = 0.3f;

    [SerializeField] private float surgeDuration = 0.3f;

    [SerializeField] private float minimumSurgeIntensity = 0.35f;

    [SerializeField]
    private AnimationCurve surgeCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.15f, 1f),
            new Keyframe(1f, 0f)
        );

    [Header("Automatic Surges")]
    [SerializeField] private bool automaticSurgesEnabled = true;

    [SerializeField] private float minimumSurgeInterval = 2f;

    [SerializeField] private float maximumSurgeInterval = 6f;

    private float surgeTimer;
    private float nextAutomaticSurgeTime;

    private Enemy ghostScript;

    private static readonly int GhostScreenPositionID =
        Shader.PropertyToID("_GhostScreenPosition");

    private void OnEnable() {
        GhostVisionRendererFeature.SetActive(true);

        ScheduleNextAutomaticSurge();
    }

    private void OnDisable() {
        GhostVisionRendererFeature.SetActive(false);
    }

    private void Update() {
        if(playerCamera == null || ghost == null)
            return;
        else if(ghostScript == null) {
            ghostScript = ghost.GetComponentInParent<Enemy>();
        }

        if(ghostScript == null)
            return;

        if(automaticSurgesEnabled && surgesEnabled && Time.time >= nextAutomaticSurgeTime && currentIntensity >= minimumSurgeIntensity) {
            TriggerSurge();

            ScheduleNextAutomaticSurge();
        }

        Vector3 directionToGhost = (ghost.position - playerCamera.transform.position).normalized;

        float distanceToGhost = Vector3.Distance(playerCamera.transform.position, ghost.position);

        float distanceAmount = 1f - Mathf.InverseLerp(minEffectDistance, maxEffectDistance, distanceToGhost);

        distanceAmount = Mathf.SmoothStep(0f, 1f, distanceAmount);
        // ------------------------------------------------------------
        // Proximity override
        // ------------------------------------------------------------

        float proximityAmount = 0f;

        if(proximityOverrideEnabled) {
            proximityAmount =
                1f - Mathf.InverseLerp(
                    proximityFullEffectDistance,
                    proximityStartDistance,
                    distanceToGhost
                );

            proximityAmount =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    proximityAmount
                );
        }

        Vector3 ghostFeet = ghost.position;

        Vector3 ghostChest = ghost.position + Vector3.up * (ghostHeight * 0.5f);

        Vector3 ghostHead = ghost.position + Vector3.up * ghostHeight;

        bool feetVisible = HasLineOfSightOrCloseEnough(ghostFeet);
        bool chestVisible = HasLineOfSightOrCloseEnough(ghostChest);
        bool headVisible = HasLineOfSightOrCloseEnough(ghostHead);

        int visiblePoints = 0;

        if(feetVisible)
            visiblePoints++;

        if(chestVisible)
            visiblePoints++;

        if(headVisible)
            visiblePoints++;

        float visibilityAmount =
            visiblePoints / 3f;

        float dot =
            Vector3.Dot(
                playerCamera.transform.forward,
                directionToGhost
            );

        // Convert the dot product into a 0-1 value.
        // 0.2 = barely looking toward the ghost.
        // 1.0 = looking directly at the ghost.
        float lookAmount =
            Mathf.InverseLerp(0.2f, 1f, dot);

        // Smooth the response so the effect doesn't snap.
        lookAmount =
            Mathf.SmoothStep(0f, 1f, lookAmount);

        lookAmount =
            Mathf.Max(
                lookAmount,
                proximityAmount
            );

        // Ghost is behind the camera.
        if(dot <= 0f) {
            Shader.SetGlobalVector(
                GhostScreenPositionID,
                new Vector4(
                    -10f,
                    -10f,
                    -1f,
                    0f
                )
            );
        }
        else {
            Vector3 screenPosition =
                playerCamera.WorldToViewportPoint(
                    ghost.position
                );

            Shader.SetGlobalVector(
                GhostScreenPositionID,
                new Vector4(
                    screenPosition.x,
                    screenPosition.y,
                    screenPosition.z,
                    0f
                )
            );
        }


        float targetIntensity =
            lookAmount *
            distanceAmount *
            visibilityAmount;

        float smoothingSpeed =
            targetIntensity > currentIntensity
                ? intensityRiseSpeed
                : intensityFallSpeed;

        currentIntensity =
            Mathf.MoveTowards(
                currentIntensity,
                targetIntensity,
                smoothingSpeed * Time.deltaTime
            );


        // ------------------------------------------------------------
        // Surge
        // ------------------------------------------------------------

        float surgeAmount = 0f;

        if(surgeTimer > 0f) {
            surgeTimer -= Time.deltaTime;

            float surgeProgress =
                1f -
                (surgeTimer / surgeDuration);

            float curveValue =
                surgeCurve.Evaluate(surgeProgress);

            surgeAmount =
                curveValue *
                surgeStrength;
        }

        float finalIntensity = currentIntensity + surgeAmount;

        //if the ghost is invisible, ignore all the above.
        if(ghostScript.invisible.Value) {
            finalIntensity = 0;
        }

        GhostVisionRendererFeature.SetIntensity(
            finalIntensity
        );
    }

    public void TriggerSurge() {
        if(!surgesEnabled)
            return;

        surgeTimer = surgeDuration;
    }
    private void ScheduleNextAutomaticSurge() {
        nextAutomaticSurgeTime =
            Time.time +
            Random.Range(
                minimumSurgeInterval,
                maximumSurgeInterval
            );
    }
    private bool HasLineOfSightOrCloseEnough(Vector3 target) {
        Vector3 direction =
            target - playerCamera.transform.position;

        float distance =
            direction.magnitude;

        direction.Normalize();

        return !Physics.Raycast(
            playerCamera.transform.position,
            direction,
            distance,
            obstructionMask
        );
    }
}