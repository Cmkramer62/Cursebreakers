using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

// This is no longer tied to ghost visibility. Will start, then stay on permanently.
public class AlgorClueEmitter : MonoBehaviour {
    [Header("Crystal")]
    [SerializeField] private GameObject[] crystalPrefabs;
    private int prefabIndex = 0;

    [Header("Raycast")]
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private float raycastDistance = 10f;

    [Header("Ray Spread")]
    [SerializeField] private float raySpreadMin = 0.25f;
    [SerializeField] private float rayVariance = 0.6f;

    [Header("Spawn")]
    [SerializeField] private float surfaceOffset = 0.01f;

    [Header("Crystal Duration")]
    [SerializeField] private int minDuration = 80;
    [SerializeField] private int maxDuration = 95;

    [Header("Interval")]
    [SerializeField] private float minInterval = 1f;
    [SerializeField] private float maxInterval = 3f;

    [SerializeField] private int maxCrystalsAtOnce = 9;

    [SerializeField] private bool emitImmediately = false;

    [SerializeField] private List<GameObject> spawnedCrystals = new List<GameObject>();

    private bool initialDelayDone = false;

    private void OnEnable() {
        StartCoroutine(EmitCrystals());
    }

    private IEnumerator EmitCrystals() {
        if(!initialDelayDone) {
            yield return new WaitForSeconds(Random.Range(10, 35));
            initialDelayDone = true;
        }
        // Only spawn if the amount of crystals out is less than the allowed amount.
        if(spawnedCrystals.Count <= maxCrystalsAtOnce) {
            RandomizeRayDirection();
            // Three rays in a tight cluster.
            Vector3[] rayOrigins =
            {
            TransformVariance(transform.position),
            TransformVariance(transform.position + transform.right * raySpreadMin),
            TransformVariance(transform.position - transform.right * raySpreadMin)
        };
            int bufferBetweenCrystals = 0;
            int iceCrystalsLifetime = Random.Range(minDuration, maxDuration);
            foreach(Vector3 origin in rayOrigins) {
                if(Physics.Raycast(
                    origin,
                    transform.forward,
                    out RaycastHit hit,
                    raycastDistance,
                    targetLayer)) {
                    SpawnCrystal(hit, iceCrystalsLifetime + bufferBetweenCrystals);
                    bufferBetweenCrystals++;
                }
            }
        }

        yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        StartCoroutine(EmitCrystals());
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
        spawnedCrystals.RemoveAt(0);
    }

    private void SpawnCrystal(RaycastHit hit, int duration) {
        // Move the crystal slightly away from the surface
        // to prevent z-fighting/intersection.
        Vector3 spawnPosition =
            hit.point + hit.normal * surfaceOffset;

        // Assumes the crystal's local +Y axis points "up"
        // from its base.
        Quaternion spawnRotation =
            Quaternion.FromToRotation(Vector3.up, hit.normal);

        var crystalReference = Instantiate(crystalPrefabs[prefabIndex], spawnPosition, spawnRotation);
        prefabIndex = (prefabIndex + 1) % crystalPrefabs.Length;

        crystalReference.GetComponent<AlgorClue>().duration = duration;
        crystalReference.GetComponent<AlgorClue>().emitterScript = this;
        crystalReference.GetComponent<NetworkObject>().Spawn();

        if(emitImmediately) {
            crystalReference.GetComponent<AlgorClue>().targetObject = this.gameObject;
            crystalReference.GetComponent<AlgorClue>().EmitParticle();
        }

        spawnedCrystals.Add(crystalReference);
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