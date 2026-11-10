using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TakebleObject : MonoBehaviour {

    private GameObject gameManager;
    public AudioClip takeClip;
    public float volume = 0.5f;

    private AudioSource source;

    public void Take() {
        if(gameManager == null) gameManager = GameObject.Find("Game Manager");

        if(name.Contains("(") && name.Contains(")")){
            name = name.Substring(0, name.Length - 4);
        } 

        gameManager.GetComponent<Inventory>().AddItem(name);
        if(source == null) source = gameManager.GetComponent<TextPrompter>().source;
        source.PlayOneShot(takeClip, volume);

        GameObject.Destroy(gameObject);
    }

}

