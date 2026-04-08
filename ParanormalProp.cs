using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParanormalProp : MonoBehaviour {

    public bool initiallyParanormal = true, spinning = true, slow = false;
    public int paranormalOdds = 1;
    public float speedMin = .9f, speedMax = 1.1f;
    // 1 = 100%, 2 = 50%, 3 = 33%
    public Animator propAnimator;


    // Start is called before the first frame update
    void OnEnable() {
        if(0 == Random.Range(0, paranormalOdds)) {
            if(initiallyParanormal) {
                propAnimator.speed = Random.Range(speedMin, speedMax);
                if(!spinning && !slow) propAnimator.Play("ParanormalFloating");
                else if(!spinning && slow) propAnimator.Play("ParanormalFloatingSmall");
                else propAnimator.Play("ParanormalFloatingSpinning");
            }
            else {
                // Start timer that makes object float after x seconds.
            }
        }
        
    }

    // Update is called once per frame
    void Update() {

    }
}
