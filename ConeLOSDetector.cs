using UnityEngine;
using System.Collections.Generic;

public class ConeLOSDetector : MonoBehaviour {

    private Transform cachedTransform; // Reference to the player GameObject
    private List<Transform> targetTransforms; // Reference to the monster GameObject
    public float fieldOfViewAngle = 90f; // The field of view angle of the monster
    public int viewDistance = 20; // How far away the player can be without being seen.

    public bool aTargetVisible = false, visibilityOverride = false; // Flag to indicate if the player is in sight
    public LayerMask ignoreMeLayer;

    public bool inViewDist = false, inFieldOfView = false, inLineOfSight = false, player = false;



    private void OnEnable() {
        cachedTransform = gameObject.transform;
        targetTransforms = new List<Transform>();

        if(player && targetTransforms.Count < 1) {
            targetTransforms.Add(GameObject.Find("Ghost Enemy").transform);
        }
        else {
            foreach(GameObject obj in GameObject.FindGameObjectsWithTag("Player")) {
                targetTransforms.Add(obj.transform);
            }
        }
    }


    /*
     * Update() checks once a frame if the player meets all the requirements to be considered within line of sight.
     */
    private void Update() {
        if(targetTransforms.Count > 0) {
            bool seenATarget = false;

            foreach(Transform targetTrans in targetTransforms) {
                if(SeeParticularTarget(targetTrans)) seenATarget = true;
            }

            aTargetVisible = (visibilityOverride || seenATarget);
        }
    }

    public void AddTarget(Transform target) {
        targetTransforms.Add(target);
    }

    // This is where references to all the player's transforms are stored.
    public List<Transform> PlayerTranforms() {
        return targetTransforms;
    }

    public bool SeeParticularTarget(Transform target) {
       return IsTargetInViewDistance(target) && IsTargetInFieldOfView(target) && IsTargetInLineOfSight(target);
    }

    /*
     * IsPlayerInFieldOfView() uses the angle of the Vector3 to the player to see if he is within the field of view angle.
     * Returns true if this is the case.
     */
    private bool IsTargetInFieldOfView(Transform target) {
        Vector3 directionToPlayer = cachedTransform.position - target.position;
        float angle = Vector3.Angle(cachedTransform.forward, directionToPlayer);

        if(angle >= (360 - fieldOfViewAngle) * 0.5f) {
            return true;
        }
        
        
        return false;
    }

    /*
     * IsPlayerInLineOfSight() checks whether the RayCast beam touches something else before it reaches the player.
     * If it touches the player first, then the line of sight is clear, with no obstructions, and returns true.
     */
    private bool IsTargetInLineOfSight(Transform target) {
        RaycastHit hit;

        Vector3 directionToPlayer = target.position - cachedTransform.position;

        if(Physics.Raycast(cachedTransform.position, directionToPlayer, out hit, Mathf.Infinity, ~ignoreMeLayer)) {
            if(hit.transform.name.Equals(target.name)) {
                return true;
            }
        }
        
        return false;
    }

    /*
     * IsPlayerInViewDistance() checks if the player is within a certain range of the monster.
     * Returns true if this is the case.
     */
    private bool IsTargetInViewDistance(Transform target) {
        if(Vector3.Distance(target.position, cachedTransform.position) < viewDistance) return true;
        else return false;
    }


}
