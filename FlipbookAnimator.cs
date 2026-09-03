using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlipbookAnimator : MonoBehaviour {

    [SerializeField] private bool looping = false, onEnable = false;
    [SerializeField, Range(0.001f, 0.2f)] private float intervalSpeed = 0.1f; // increase to make slower.
    [SerializeField, Range(0, 20)] private float delay = 0f;

    private Image imageReference;
    private SpriteRenderer spriteReference;
    public Sprite[] spriteList;
    private int spriteIndex = 0;

    private void OnEnable() {
        if(GetComponent<Image>()) imageReference = GetComponent<Image>();
        else spriteReference = GetComponent<SpriteRenderer>();

        if(onEnable) {
            InvokeRepeating("FlipbookCycle", delay, intervalSpeed);
        }

    }

    private void FlipbookCycle() {
        if(imageReference != null) imageReference.sprite = spriteList[spriteIndex];
        else spriteReference.sprite = spriteList[spriteIndex];
        spriteIndex++;
        if(spriteIndex == spriteList.Length && looping) spriteIndex = 0;
        else if(spriteIndex == spriteList.Length) CancelInvoke();
    }

    private void OnDisable() {
        spriteIndex = 0;
        CancelInvoke();
    }
}
