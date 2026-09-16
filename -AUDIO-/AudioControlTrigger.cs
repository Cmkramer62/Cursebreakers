using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioControlTrigger : MonoBehaviour {
    public bool triggered = false;

    public AudioSource source;
    public AudioClip newClip; //only needed if certain type.
    public float timeToFade;
    public float newVolume; //only needed if certain type.
    public bool repeatable = false;

    public enum typeAudioTransition { Another, FadeOut, FadeIn}
    public typeAudioTransition currentType = typeAudioTransition.Another;

    private void OnTriggerEnter(Collider other) {
        if((!triggered || repeatable) && other.name.Equals("Player")) {
            triggered = true;
            if(currentType == typeAudioTransition.Another) AudioController.FadeToAnother(this, source, timeToFade, newClip, newVolume);
            else if(currentType == typeAudioTransition.FadeOut) AudioController.FadeOutAudio(this, source, timeToFade);
            else AudioController.FadeInAudio(this, source, timeToFade, newVolume);
        }
    }
}
