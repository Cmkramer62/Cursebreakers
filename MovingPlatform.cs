using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MovingPlatform : NetworkBehaviour {
    public enum MovementMode {
        Automatic,
        PlayerActivated
    }

    public enum CycleMode {
        Loop,
        PingPong
    }

    [Header("Movement")]
    [SerializeField] private MovementMode movementMode = MovementMode.Automatic;
    [SerializeField] private CycleMode cycleMode = CycleMode.PingPong;

    [Tooltip("Positions the platform will travel between, in order.")]
    [SerializeField] private Transform[] waypoints;

    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("How close the platform must be to a waypoint before considering it reached.")]
    [SerializeField] private float waypointTolerance = 0.01f;

    [Tooltip("Time to wait after reaching each waypoint.")]
    [SerializeField] private float pauseAtWaypoint = 0f;

    [Header("Player Activation")]
    [Tooltip("How many players must be standing on the platform before it activates.")]
    [SerializeField] private int requiredPlayers = 1;

    [Tooltip("If true, the platform returns to its starting position when nobody is on it.")]
    [SerializeField] private bool returnWhenEmpty = false;

    [SerializeField] private float returnSpeed = 5f;

    [Header("Player Detection")]
    [Tooltip("Extra distance used when checking whether a CharacterController is standing on the platform.")]
    [SerializeField] private float playerDetectionHeight = 0.2f;

    [Tooltip("Layers containing player CharacterControllers.")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Rotation")]
    [SerializeField] private bool rotatePlatform = false;

    [Tooltip("If enabled, the platform rotates toward the next waypoint's rotation.")]
    [SerializeField] private bool rotateWithWaypoints = false;

    [SerializeField] private float rotationSpeed = 180f;

    // --------------------------------------------------------------------
    // Runtime state
    // --------------------------------------------------------------------

    private int currentWaypointIndex = 0;
    private int direction = 1;

    private bool isPaused;
    private bool isReturning;

    private readonly List<CharacterController> riders =
        new List<CharacterController>();

    private Vector3 previousPosition;
    private Quaternion previousRotation;

    // Used to prevent the same controller from being registered repeatedly.
    private readonly HashSet<CharacterController> riderSet =
        new HashSet<CharacterController>();

    // --------------------------------------------------------------------
    // Unity
    // --------------------------------------------------------------------

    private void Awake() {
        previousPosition = transform.position;
        previousRotation = transform.rotation;
    }

    private void Start() {
        if(waypoints == null || waypoints.Length == 0) {
            Debug.LogWarning(
                $"{name}: MovingPlatform has no waypoints assigned.",
                this
            );

            enabled = false;
            return;
        }

        if(waypoints.Length == 1) {
            Debug.LogWarning(
                $"{name}: MovingPlatform only has one waypoint.",
                this
            );
        }
    }

    private void Update() {
        if(!IsServer)
            return;

        // Remove players that are no longer valid.
        CleanupRiders();

        // Determine what the platform should currently be doing.
        if(movementMode == MovementMode.PlayerActivated) {
            UpdatePlayerActivatedMovement();
        }
        else {
            UpdateAutomaticMovement();
        }

        // Move all riders by exactly the same amount the platform moved.
        ApplyPlatformDeltaToRiders();

        // Store the current transform for the next frame.
        previousPosition = transform.position;
        previousRotation = transform.rotation;
    }

    // --------------------------------------------------------------------
    // Automatic movement
    // --------------------------------------------------------------------

    private void UpdateAutomaticMovement() {
        if(isPaused)
            return;

        if(currentWaypointIndex >= waypoints.Length)
            return;

        Transform target = waypoints[currentWaypointIndex];

        MoveToward(target);

        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        if(distance <= waypointTolerance) {
            transform.position = target.position;

            if(rotateWithWaypoints) {
                transform.rotation = target.rotation;
            }

            StartCoroutine(HandleWaypointReached());
        }
    }

    // --------------------------------------------------------------------
    // Player activated movement
    // --------------------------------------------------------------------

    private void UpdatePlayerActivatedMovement() {
        bool hasEnoughPlayers = riderSet.Count >= requiredPlayers;

        if(isReturning) {
            MoveToward(waypoints[0], returnSpeed);

            if(Vector3.Distance(transform.position, waypoints[0].position)
                <= waypointTolerance) {
                transform.position = waypoints[0].position;

                isReturning = false;
                currentWaypointIndex = 0;
                direction = 1;
            }

            return;
        }

        // Nobody is activating the platform.
        if(!hasEnoughPlayers) {
            if(returnWhenEmpty && currentWaypointIndex != 0) {
                isReturning = true;
            }

            return;
        }

        if(isPaused)
            return;

        Transform target = waypoints[currentWaypointIndex];

        MoveToward(target);

        if(Vector3.Distance(transform.position, target.position)
            <= waypointTolerance) {
            transform.position = target.position;

            if(rotateWithWaypoints) {
                transform.rotation = target.rotation;
            }

            StartCoroutine(HandleWaypointReached());
        }
    }

    // --------------------------------------------------------------------
    // Movement
    // --------------------------------------------------------------------

    private void MoveToward(Transform target) {
        MoveToward(target, moveSpeed);
    }

    private void MoveToward(Transform target, float speed) {
        Vector3 nextPosition = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        transform.position = nextPosition;

        if(rotatePlatform && !rotateWithWaypoints) {
            Vector3 directionToTarget =
                target.position - transform.position;

            if(directionToTarget.sqrMagnitude > 0.0001f) {
                Quaternion targetRotation =
                    Quaternion.LookRotation(directionToTarget);

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    // --------------------------------------------------------------------
    // Waypoint handling
    // --------------------------------------------------------------------

    private IEnumerator HandleWaypointReached() {
        isPaused = true;

        if(pauseAtWaypoint > 0f) {
            yield return new WaitForSeconds(pauseAtWaypoint);
        }

        AdvanceWaypoint();

        isPaused = false;
    }

    private void AdvanceWaypoint() {
        if(waypoints.Length <= 1)
            return;

        if(cycleMode == CycleMode.Loop) {
            currentWaypointIndex++;

            if(currentWaypointIndex >= waypoints.Length) {
                currentWaypointIndex = 0;
            }

            return;
        }

        // Ping-pong.
        currentWaypointIndex += direction;

        if(currentWaypointIndex >= waypoints.Length) {
            currentWaypointIndex = waypoints.Length - 2;
            direction = -1;
        }
        else if(currentWaypointIndex < 0) {
            currentWaypointIndex = 1;
            direction = 1;
        }
    }

    // --------------------------------------------------------------------
    // Player detection
    // --------------------------------------------------------------------

    private void FixedUpdate() {
        DetectPlayersStandingOnPlatform();
    }

    private void DetectPlayersStandingOnPlatform() {
        if(waypoints == null)
            return;

        Collider platformCollider = GetComponent<Collider>();

        if(platformCollider == null)
            return;

        Bounds bounds = platformCollider.bounds;

        // Check a thin region immediately above the platform.
        Vector3 center = new Vector3(
            bounds.center.x,
            bounds.max.y + playerDetectionHeight * 0.5f,
            bounds.center.z
        );

        Vector3 halfExtents = new Vector3(
            bounds.extents.x,
            playerDetectionHeight * 0.5f,
            bounds.extents.z
        );

        Collider[] hits = Physics.OverlapBox(
            center,
            halfExtents,
            transform.rotation,
            playerLayer,
            QueryTriggerInteraction.Ignore
        );

        HashSet<CharacterController> detected =
            new HashSet<CharacterController>();

        foreach(Collider hit in hits) {
            CharacterController controller =
                hit.GetComponentInParent<CharacterController>();

            if(controller == null)
                continue;

            if(!controller.enabled)
                continue;

            detected.Add(controller);

            if(!riderSet.Contains(controller)) {
                riderSet.Add(controller);
                riders.Add(controller);
            }
        }

        // Remove controllers that are no longer standing on us.
        for(int i = riders.Count - 1; i >= 0; i--) {
            CharacterController controller = riders[i];

            if(!detected.Contains(controller)) {
                riderSet.Remove(controller);
                riders.RemoveAt(i);
            }
        }
    }

    private void CleanupRiders() {
        for(int i = riders.Count - 1; i >= 0; i--) {
            CharacterController controller = riders[i];

            if(controller == null || !controller.enabled) {
                riderSet.Remove(controller);
                riders.RemoveAt(i);
            }
        }
    }

    // --------------------------------------------------------------------
    // Carry players
    // --------------------------------------------------------------------

    private void ApplyPlatformDeltaToRiders() {
        Vector3 positionDelta =
            transform.position - previousPosition;

        Quaternion rotationDelta =
            transform.rotation * Quaternion.Inverse(previousRotation);

        foreach(CharacterController controller in riders) {
            if(controller == null || !controller.enabled)
                continue;

            Vector3 movement = positionDelta;

            // Account for platform rotation as well as translation.
            Vector3 relativePosition =
                controller.transform.position - transform.position;

            Vector3 rotatedPosition =
                rotationDelta * relativePosition;

            Vector3 rotationMovement =
                rotatedPosition - relativePosition;

            movement += rotationMovement;

            if(movement.sqrMagnitude > 0.0000001f) {
                controller.Move(movement);
            }
        }
    }

    // --------------------------------------------------------------------
    // Debug
    // --------------------------------------------------------------------

    private void OnDrawGizmosSelected() {
        if(waypoints == null)
            return;

        Gizmos.color = Color.yellow;

        for(int i = 0; i < waypoints.Length; i++) {
            if(waypoints[i] == null)
                continue;

            Gizmos.DrawWireSphere(
                waypoints[i].position,
                0.25f
            );

            if(i < waypoints.Length - 1 &&
                waypoints[i + 1] != null) {
                Gizmos.DrawLine(
                    waypoints[i].position,
                    waypoints[i + 1].position
                );
            }
        }
    }
}