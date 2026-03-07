using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

public class PingCreator : MonoBehaviour {
    [SerializeField]
    private int rayLength = 7;
    [SerializeField]
    private LayerMask layerMaskInteract;
    [SerializeField]
    private string excludeLayerName = null;

    public GameObject pingPrefab, pingAntiPrefab;
    public PlayerMovement playerScript;

    public float distanceToTrim = 5f;
    public List<GameObject> pingPositions = new List<GameObject>();

    private AudioSource source;
    public AudioClip pingDown, pingAnti, pingRemove;
    // Raycast to nearest collider.
    // if collider belongs to a cursed object, do something?
    // make noise
    // record transform.position vector3. Add it to a list.
    // if the recorded vector3 is nearby any in the list already, delete the prior one.
    // spawn the prefab.
    private bool waitUntilNext = false;

    private void Update() {
        if(playerScript.allowedToMove && Input.GetKeyDown(KeyCode.Mouse2) && !waitUntilNext) {
            SpawnPing();
            waitUntilNext = true;
        }
        else if(Input.GetKeyUp(KeyCode.Mouse2) || !playerScript.allowedToMove) waitUntilNext = false;
    }

    public void SpawnPing() {
        RaycastHit hit;
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        int excludeMask = 1 << LayerMask.NameToLayer(excludeLayerName);
        int mask = layerMaskInteract.value & ~excludeMask;
        if(Physics.Raycast(transform.position, fwd, out hit, rayLength, mask)) {

            Vector3 hitPosition = hit.point;

             // Move 5 units back toward the ray origin
            //Vector3 hitPosition = hit.point - fwd.normalized * 1f;

            int i = 0;
            bool foundPingNearby = false;
            foreach(var pos in pingPositions) {
                if(Vector3.Distance(pos.transform.position, hitPosition) < distanceToTrim) {
                    // If ping is 'x', destroy.
                    // else, set it to the 'x' and do nothing except make a noise.
                    

                    StartCoroutine(DestroyPingTimer(pos));
                    pingPositions.RemoveAt(i);
                    foundPingNearby = true;

                    if(pos.GetComponentInChildren<Image>().sprite.name == "PingIcon") {
                        InstantiatePing(pingAntiPrefab, hitPosition, pingAnti);
                    }
                    else {
                        source = pos.GetComponent<AudioSource>();
                        source.PlayOneShot(pingRemove);
                    }

                    break;
                }
                i++;
            }
            if(!foundPingNearby) InstantiatePing(pingPrefab, hitPosition, pingDown);
            if(hit.transform.gameObject.tag == "CursedObject") {
                if(!foundPingNearby) hit.transform.GetComponentInChildren<CursedObject>().pinged = true;
                else hit.transform.GetComponentInChildren<CursedObject>().pinged = false;
            }

        }
    }

    private void InstantiatePing(GameObject prefabType, Vector3 hitPosition, AudioClip clip) {
        GameObject newPing = Instantiate(prefabType, hitPosition, Quaternion.identity);

        source = newPing.GetComponent<AudioSource>();
        source.PlayOneShot(clip);

        pingPositions.Add(newPing);
        LookAtConstraint lookAt = newPing.GetComponentInChildren<LookAtConstraint>();
        lookAt.locked = false;
        var csource = new ConstraintSource {
            sourceTransform = transform,
            weight = 1f
        };
        lookAt.AddSource(csource);
        lookAt.constraintActive = true;
        lookAt.locked = true;
    }

    private IEnumerator DestroyPingTimer(GameObject newObject) {
        newObject.GetComponentInChildren<Animator>().Play("PingAnimReverse");
        yield return new WaitForSeconds(.5f);
        GameObject.Destroy(newObject);
    }

}
