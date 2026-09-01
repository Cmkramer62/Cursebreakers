using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeUpdateUI : MonoBehaviour {

    [SerializeField] private UIManager uiManagerReference;
    [SerializeField] private Animator[] heartAnimator;

    private void OnEnable() {
        for(int i = 0; i < heartAnimator.Length; i++) {
            heartAnimator[i].Play(uiManagerReference.trackedLives > i ? "HeartIconAnim" : "HeartIconGone");
        }
    }
}
