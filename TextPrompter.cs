using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextPrompter : MonoBehaviour {

    public TextAdder textAdderScript;
    private Coroutine routine;
    public bool waitingForResponse = false;
    [SerializeField] private int index = 0;
    public string[] currentList;
    public GameObject enterUI, textBackground;
    private int layerCurrent;

    public AudioSource source;
    public AudioClip enterClip;
    public InteractRaycast raycastScript; // This is so we have a reference to raycast's clickVolume, (LMB1).

    private InteractPrompt promptScriptCurrent;

    public void Update() {
        if(waitingForResponse && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Mouse0))) {
            if(index >= currentList.Length) {
                waitingForResponse = false;
                if(routine != null) StopCoroutine(routine);
                routine = StartCoroutine(StopPromptTimer(0));
                //textBackground.GetComponent<Animator>().Play("FadeIn 0");
                if(promptScriptCurrent != null) {
                    promptScriptCurrent.EndEffect();
                    promptScriptCurrent = null;
                }
            }
            else {
                Debug.Log("Tried to play " + currentList[index]);
                TextPromptIndefinite(currentList[index]);
                index++;
            }
            source.PlayOneShot(enterClip, raycastScript.volumeOfClick);
        }
    }

    public void QueueTextPrompt(string text, AudioClip textClip, InteractPrompt promptScript) {
        QueueTextPrompt(text, textClip);
        promptScript.EndEffect();
    }

    public void QueueTextPrompt(string[] text, AudioClip textClip, InteractPrompt promptScript) {
        QueueTextPrompt(text, textClip);
        promptScriptCurrent = promptScript;
    }

    public void QueueTextPrompt(string text, AudioClip textClip) {
        index = 0;
        textAdderScript.clip = textClip;
        TextPrompt(text);
    }

    public void QueueTextPrompt(string[] text, AudioClip textClip) {
        if(routine != null) StopCoroutine(routine);
        index = 0;
        Debug.Log("Queing list");
        currentList = new string[text.Length];
        for(int i = 0; i < text.Length; i++)
            currentList[i] = text[i];

        textBackground.SetActive(false);
        textBackground.SetActive(true);
        textAdderScript.clip = textClip;
        waitingForResponse = true;
        //QueueTextPrompt(text, textClip);
        TextPromptIndefinite(currentList[index]);
        
        index++;
    }

    public void TextPrompt(string text) {
        textBackground.SetActive(false);
        textBackground.SetActive(true);

        if(routine != null) StopCoroutine(routine);
        routine = StartCoroutine(StopPromptTimer(2));

        textAdderScript.CancelText();
        textAdderScript.endWord = text;
        textAdderScript.StartAddingText();
    }

    public void TextPromptIndefinite(string text) {
        GetComponent<PauseGame>().allowedToPause = false;
        GetComponent<PauseGame>().allowedToQuestionPause = false;

        textAdderScript.CancelText();
        textAdderScript.endWord = text;
        textAdderScript.StartAddingText();
        GameObject.Find("Player").GetComponent<PlayerMovement>().allowedToMove = false;
        Camera.main.gameObject.GetComponent<MouseLook>().allowedToLook = false;
        layerCurrent = gameObject.layer;
        gameObject.layer = 0;
        enterUI.SetActive(true);
    }

    private IEnumerator StopPromptTimer(int delay) {
        yield return new WaitForSeconds(delay);
        
        textAdderScript.CancelText();
        GetComponent<PauseGame>().allowedToPause = true;
        GetComponent<PauseGame>().allowedToQuestionPause = true;
        GameObject.Find("Player").GetComponent<PlayerMovement>().allowedToMove = true;
        Camera.main.gameObject.GetComponent<MouseLook>().allowedToLook = true;
        gameObject.layer = layerCurrent;
        enterUI.SetActive(false);
        textBackground.GetComponent<Animator>().Play("FadeIn 0");
        yield return new WaitForSeconds(1f);
        textBackground.SetActive(false);
    }
}
