using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchRotation : MonoBehaviour {

    private Vector3 vectOffset;
    public GameObject goFollow;
    [SerializeField] private float speed = 3.0f;

    // Start is called before the first frame update
    void Start() {
        vectOffset = transform.position - goFollow.transform.position;
    }

    // Update is called once per frame
    void Update() {
        transform.rotation = Quaternion.Slerp(transform.rotation, goFollow.transform.rotation, speed * Time.deltaTime);
    }

}
