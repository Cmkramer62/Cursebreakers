using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {
    public Transform target;

    public void SetTarget(Transform newTarget) {
        target = newTarget;
    }

    void LateUpdate() {
        if(target == null) return;

        transform.position = target.position + new Vector3(0, 0, 0);
        //transform.LookAt(target);
    }
}