using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivatorSwitch : MonoBehaviour {

    public GameObject[] objectsToAffect;

    //public bool inverse = false;

    public float intervalTime = 0f;
    public bool state = false;
    private Coroutine routine;

    public AudioSource source;
    public AudioClip clip;

    public void Activate() {
        if(routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ActivationTimer());

        state = !state;
        GetComponent<Animator>().SetBool("State", state);
        source.PlayOneShot(clip);
    }

    private IEnumerator ActivationTimer() {
        foreach(GameObject obj in objectsToAffect) {
            if(obj.GetComponent<LightFlicker>()) {
                // It's a light, inverse its state.
                obj.GetComponent<LightFlicker>().InvertLightState();
            }
            else {
                // If it wasn't any of the types, it's a generic GameObject. Inverse its state.
                obj.SetActive(!obj.gameObject.activeSelf);
            }
            yield return new WaitForSeconds(intervalTime);
        }
    }

}
