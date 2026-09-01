using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchRotation : MonoBehaviour {

    private Vector3 vectOffset;
    public GameObject goFollow;
    [SerializeField] private float speed = 3.0f;

    /*
    // Start is called before the first frame update
    private IEnumerator Start() {
        while(goFollow == null) {
            yield return null;
        }
        vectOffset = transform.position - goFollow.transform.position;
    }
    */

    // Update is called once per frame
    void Update() {
        if(goFollow != null) transform.rotation = Quaternion.Slerp(transform.rotation, goFollow.transform.rotation, speed * Time.deltaTime);
    }

}
