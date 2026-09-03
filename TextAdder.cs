using UnityEngine;
using TMPro;

public class TextAdder : MonoBehaviour {

    public float interval = 0.06f, delay = 0.0f;

    public string endWord;
    private TextMeshProUGUI text;
    private int i = 0;

    public bool useAudio = false;
    public AudioSource source;
    public AudioClip clip; // can be set by another script.

    [SerializeField] private TextAdder chainedTextScript;
    public bool startOnEnable = true, delayOnlyOnFirst = false;
    private bool done = false;

    private void OnEnable() {
        text = GetComponent<TextMeshProUGUI>();
        text.text = "";
        if(startOnEnable) StartAddingText();
    }

    public void StartAddingText() {
        text.text = "";
        i = 0;
        InvokeRepeating("AddingText", delayOnlyOnFirst && done ? 0 : delay, interval);
        done = true;
    }

    private void AddingText() {
        if(i >= endWord.Length) {
            CancelInvoke("AddingText");

            if(chainedTextScript != null && gameObject.activeInHierarchy) chainedTextScript.StartAddingText();
        }
        else {
            if(useAudio && !endWord[i].Equals(' ')) {
                source.Stop();
                source.PlayOneShot(clip);
            }
            text.text += endWord[i];
            i++;
        }
        
    }

    public void CancelText() {
        CancelInvoke("AddingText");
        text.text = "";
    }
}
