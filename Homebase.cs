using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Homebase : MonoBehaviour {

    public AudioSettings saveSystem;
    public TextMeshProUGUI resultsText;
    public GameObject firstMessage;

    // Start is called before the first frame update

    private void OnEnable() {
        AudioSettings.OnSaveLoaded += HandleSaveLoaded;
        // IMPORTANT: catch missed event
       // if(SaveSystem. != null)
       //     HandleSaveLoaded();
    }

    private void OnDisable() {
        AudioSettings.OnSaveLoaded -= HandleSaveLoaded;
    }

    private void HandleSaveLoaded() {
        Debug.Log("Save finished loading! " + saveSystem.level);

        if(saveSystem.level == -1) {
            resultsText.gameObject.GetComponent<TextAdder>().endWord = "Mission: Failed";
            resultsText.gameObject.SetActive(true);
        }
        else if(saveSystem.level == 1) {
            resultsText.gameObject.GetComponent<TextAdder>().endWord = "Mission: Success";
            resultsText.gameObject.SetActive(true);
        }
        else {
            resultsText.gameObject.GetComponent<TextAdder>().endWord = "Welcome";
            resultsText.gameObject.SetActive(true);
        }

        firstMessage.SetActive(saveSystem.hubFirstData);

        // InitializeMySystem(level);
    }

    void Start() {
        
    }

}
