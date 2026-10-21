using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class RadiatioClueEmitter : MonoBehaviour {

    [Header("EMF Clue Prefab")]
    [SerializeField] private GameObject emfCluePrefab;

    [Header("Raycast")]
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private float raycastDistance = 10f;

    [Header("Ray Spread")]
    [SerializeField] private float raySpreadMin = 0.25f;
    [SerializeField] private float rayVariance = 0.6f;

    [Header("Spawn")]
    [SerializeField] private float surfaceOffset = 0.01f;

    [Header("EMF Clue Duration")]
    [SerializeField] private int minDuration = 80;
    [SerializeField] private int maxDuration = 95;

    [Header("Interval")]
    [SerializeField] private float minInterval = 1f;
    [SerializeField] private float maxInterval = 3f;

    [SerializeField] private int maxEMFCluesAtOnce = 1;
    [SerializeField] private bool emitImmediately = false;
    [SerializeField] private List<GameObject> spawnedEMFClues = new List<GameObject>();

    private bool initialDelayDone = false;
    public GameObject realEMFCLUE;
    [SerializeField] private float maxDistance = 50f;

    private void OnEnable() {
        StartCoroutine(EmitEMFFakeClues());
    }

    public void StartRealEMF() {
        StartCoroutine(EmitRealEMF());
    }

    // Emitting the real EMF Clue. Only one on the whole map.
    private IEnumerator EmitRealEMF() {
        yield return new WaitForSeconds(Random.Range(10, 35));
        
        RandomizeRayDirection();
        // One Ray.
        Vector3 rayOrigins = transform.position;

        int emfClueLifetime = 9999;
        if(Physics.Raycast(rayOrigins,transform.forward,out RaycastHit hit,raycastDistance,targetLayer)) {
            SpawnEMFClue(hit, emfClueLifetime, true);
        }
    }

    private IEnumerator EmitEMFFakeClues() {
        if(!initialDelayDone) {
            yield return new WaitForSeconds(Random.Range(10, 35));
            initialDelayDone = true;
        }
        // Only spawn if the amount of crystals out is less than the allowed amount.
        if(spawnedEMFClues.Count <= maxEMFCluesAtOnce) {
            RandomizeRayDirection();
            // One Ray.
            Vector3 rayOrigins = transform.position;
            
            int emfClueLifetime = Random.Range(minDuration, maxDuration);
            if(Physics.Raycast(
                rayOrigins,
                transform.forward,
                out RaycastHit hit,
                raycastDistance,
                targetLayer)) {
                SpawnEMFClue(hit, emfClueLifetime, false);
            }
            
        }

        yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        StartCoroutine(EmitEMFFakeClues());
    }

    // Called by death of individual crystal. Removes from this list.
    // Removing at 1 or 0 because they die in chronological order of spawning. Except the original, which has infinite life.
    public void RemoveLastCrystal() {
        if(realEMFCLUE != null) spawnedEMFClues.RemoveAt(1);
        else spawnedEMFClues.RemoveAt(0);
    }

    private void SpawnEMFClue(RaycastHit hit, int duration, bool realOne) {
        Vector3 spawnPosition =
            hit.point + hit.normal * surfaceOffset;

        Quaternion spawnRotation =
            Quaternion.FromToRotation(Vector3.up, hit.normal);

        var EMFclueReference = Instantiate(emfCluePrefab, spawnPosition, spawnRotation);
        EMFclueReference.GetComponent<RadiatioClue>().duration = duration;
        EMFclueReference.GetComponent<RadiatioClue>().emitterScript = this;

        if(realOne) {
            EMFclueReference.GetComponent<RadiatioClue>().emfLevel.Value = 8;
        }
        else if(realEMFCLUE == null) {
            // The realEMFClue has not spawned yet or does not exist for this game session.
            // In which case, the clues are leading to the ghostEmitter/ghost.
            float distance = Vector3.Distance(spawnPosition, transform.position);
            int level = 8 - Mathf.Clamp(Mathf.CeilToInt((distance / maxDistance) * 7), 1, 7);
            EMFclueReference.GetComponent<RadiatioClue>().emfLevel.Value = level;
        }
        else {
            // The realEMFClue has been spawned. The clues are leading to the real one.
            float distance = Vector3.Distance(spawnPosition, realEMFCLUE.transform.position);
            int level = 8 - Mathf.Clamp( Mathf.CeilToInt((distance / maxDistance) * 7), 1, 7);
            EMFclueReference.GetComponent<RadiatioClue>().emfLevel.Value = level;
        }


        EMFclueReference.GetComponent<NetworkObject>().Spawn();

        if(emitImmediately) {
            EMFclueReference.GetComponent<AlgorClue>().targetObject = this.gameObject;
            EMFclueReference.GetComponent<AlgorClue>().EmitParticle();
        }

        spawnedEMFClues.Add(EMFclueReference);
        if(realOne) realEMFCLUE = EMFclueReference;
    }

    private void RandomizeRayDirection() {
        // Random direction across the upper hemisphere,
        // then invert Y so it always points downward.
        Vector3 direction = Random.onUnitSphere;

        direction.y = -Mathf.Abs(direction.y);

        transform.rotation = Quaternion.LookRotation(direction);
    }
}
