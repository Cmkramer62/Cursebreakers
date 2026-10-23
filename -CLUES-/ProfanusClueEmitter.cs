using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ProfanusClueEmitter : MonoBehaviour {
    [SerializeField] private int startDelayMin = 0, startDelayMax = 0;

    [Header("Blood")]
    [SerializeField] private GameObject[] footstepPrefabs;
    private int prefabIndex = 0;

    [Header("Raycast")]
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private float raycastDistance = 10f;

    [Header("Ray Spread")]
    [SerializeField] private float raySpreadMin = 0.25f;
    [SerializeField] private float rayVariance = 0.6f;

    [Header("Spawn")]
    [SerializeField] private float surfaceOffset = 0.01f;

    [Header("Blood Duration")]
    [SerializeField] private int minDuration = 80;
    [SerializeField] private int maxDuration = 95;

    [Header("Interval")]
    [SerializeField] private float minInterval = 1f;
    [SerializeField] private float maxInterval = 3f;

    [SerializeField] private int maxFootstepCluesAtOnce = 9;

    [SerializeField] private bool emitImmediately = false;

    [SerializeField] private List<GameObject> spawnedFootsteps = new List<GameObject>();

    private bool initialDelayDone = false;

    private void OnEnable() {
        StartCoroutine(EmitBlood());
    }

    private IEnumerator EmitBlood() {
        if(!initialDelayDone) {
            yield return new WaitForSeconds(Random.Range(startDelayMin, startDelayMax));
            initialDelayDone = true;
        }
        // Only spawn if the amount of crystals out is less than the allowed amount.
        if(spawnedFootsteps.Count <= maxFootstepCluesAtOnce) {
            RandomizeRayDirection();
            // Three rays in a tight cluster.
            Vector3 rayOrigins = TransformVariance(transform.position);
            int iceCrystalsLifetime = Random.Range(minDuration, maxDuration);
            // CHANGE TO RAYCAST IN SPHERICAL, ANY DIRECTION. Throwing holy water, so it's okay if the blood is on the ceiling.
            if(Physics.Raycast(
                rayOrigins,
                transform.forward,
                out RaycastHit hit,
                raycastDistance,
                targetLayer)) {
                SpawnFootstep(hit, iceCrystalsLifetime);
            }

        }

        yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        StartCoroutine(EmitBlood());
    }

    // Applies a slight shift to the input Vector3 and outputs it.
    private Vector3 TransformVariance(Vector3 input) {
        Vector3 output = input;
        output.x += Random.Range(0 - rayVariance / 2, 0 + rayVariance / 2);
        output.y += Random.Range(0 - rayVariance / 2, 0 + rayVariance / 2);
        output.z += Random.Range(0 - rayVariance / 2, 0 + rayVariance / 2);
        return output;
    }

    // Called by death of individual crystal. Removes from this list.
    public void RemoveLastCrystal() {
        spawnedFootsteps.RemoveAt(0);
    }

    private void SpawnFootstep(RaycastHit hit, int duration) {
        // Move the footstep high above the surface to projet normally.
        Vector3 spawnPosition = hit.point + hit.normal * surfaceOffset;

        // Assumes the crystal's local +Y axis points "up"
        // from its base.
        Quaternion spawnRotation =
            Quaternion.FromToRotation(Vector3.up, hit.normal);

        var bloodReference = Instantiate(footstepPrefabs[prefabIndex], spawnPosition, spawnRotation);
        prefabIndex = (prefabIndex + 1) % footstepPrefabs.Length;

        bloodReference.GetComponent<ProfanusClue>().duration = duration;
        bloodReference.GetComponent<ProfanusClue>().emitterScript = this;
        bloodReference.GetComponent<NetworkObject>().Spawn();

        spawnedFootsteps.Add(bloodReference);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;

        Vector3[] rayOrigins =
        {
            transform.position,
            transform.position + transform.right * raySpreadMin,
            transform.position - transform.right * raySpreadMin
        };

        foreach(Vector3 origin in rayOrigins) {
            Gizmos.DrawRay(
                origin,
                transform.forward * raycastDistance
            );
        }
    }

    private void RandomizeRayDirection() {
        // Random direction across the upper hemisphere,
        // then invert Y so it always points downward.
        Vector3 direction = Random.onUnitSphere;

        direction.y = -Mathf.Abs(direction.y);

        transform.rotation = Quaternion.LookRotation(direction);
    }
}
