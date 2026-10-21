using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class StellaeClueEmitter : MonoBehaviour {
    [Header("Footsteps")]
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

    [Header("Footstep Duration")]
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
        StartCoroutine(EmitFootsteps());
    }

    private IEnumerator EmitFootsteps() {
        if(!initialDelayDone) {
            yield return new WaitForSeconds(Random.Range(10, 35));
            initialDelayDone = true;
        }
        // Only spawn if the amount of crystals out is less than the allowed amount.
        if(spawnedFootsteps.Count <= maxFootstepCluesAtOnce) {
            RandomizeRayDirection();
            // Three rays in a tight cluster.
            Vector3 rayOrigins = TransformVariance(transform.position);
            int iceCrystalsLifetime = Random.Range(minDuration, maxDuration);

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
        StartCoroutine(EmitFootsteps());
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
        Vector3 spawnPosition =  hit.point + hit.normal * surfaceOffset;

        // Assumes the crystal's local +Y axis points "up"
        // from its base.
        Quaternion spawnRotation =
            Quaternion.FromToRotation(Vector3.up, hit.normal);

        var crystalReference = Instantiate(footstepPrefabs[0], spawnPosition, spawnRotation);
        //prefabIndex = Mathf.Clamp(prefabIndex + 1, 0, 3);

        crystalReference.GetComponent<StellaeClue>().duration = duration;
        crystalReference.GetComponent<StellaeClue>().emitterScript = this;
        crystalReference.GetComponent<NetworkObject>().Spawn();

        if(emitImmediately) {
            crystalReference.GetComponent<AlgorClue>().targetObject = this.gameObject;
            crystalReference.GetComponent<AlgorClue>().EmitParticle();
        }

        spawnedFootsteps.Add(crystalReference);
        // Set the crystal code's AlgorClueEmitter to this. (only if server), on death it calls RemoveLastCrystal().

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
