using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioController {
    
    public static void FadeToAnother(MonoBehaviour caller, AudioSource source, float timeToFade, AudioClip newClip, float newVolume) {
        //Debug.Log("FadeToAnother -> " + newClip.name);
        if(newClip != source.clip) caller.StartCoroutine(FadeToAnotherCoroutine(caller, source, timeToFade, newClip, newVolume));
    }

    private static IEnumerator FadeToAnotherCoroutine(MonoBehaviour caller, AudioSource source, float timeToFade, AudioClip newClip, float newVolume) {
        //float initialVolume = source.volume;
        float startVolume = source.volume;
        float timer = 0f;

        while(timer < (timeToFade / 2)) {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, timer / (timeToFade / 2));
            yield return null; // Wait for the next frame
        }

        source.volume = 0f; // Ensure volume is exactly 0 at the end
        source.Stop();
        source.clip = newClip;
        source.Play();
        caller.StartCoroutine(FadeInCoroutine(source, timeToFade / 2, newVolume));
    }

    public static void FadeOutAudio(MonoBehaviour caller, AudioSource source, float timeToFade) {
        //Debug.Log("FadeOutThis -> " + source.name);
        caller.StartCoroutine(FadeOutCoroutine(source, timeToFade));
    }

    private static IEnumerator FadeOutCoroutine(AudioSource audioSource, float duration) {
        float startVolume = audioSource.volume;
        float timer = 0f;

        while(timer < duration) {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null; // Wait for the next frame
        }

        audioSource.volume = 0f; // Ensure volume is exactly 0 at the end
        audioSource.Stop(); // Stop the audio source after fading out
    }
    
    private static IEnumerator FadeOutTransferCoroutine(AudioSource audioSource, float duration) {
        float startVolume = audioSource.volume;
        float timer = 0f;

        while(timer < duration) {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null; // Wait for the next frame
        }

        audioSource.volume = 0f; // Ensure volume is exactly 0 at the end
        audioSource.Stop(); // Stop the audio source after fading out
    }
    
    public static void FadeInAudio(MonoBehaviour caller, AudioSource source, float timeToFade, float endVolume) {
        //Debug.Log("FadeInThis -> " + source.name);
        caller.StartCoroutine(FadeInCoroutine(source, timeToFade, endVolume));
    }

    private static IEnumerator FadeInCoroutine(AudioSource audioSource, float duration, float endVolume) {
        //audiosource.stop // if it is already playing?
        float startVolume = audioSource.volume;
        float timer = 0f;
        if(!audioSource.isPlaying) audioSource.Play();
        while(timer < duration) {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, endVolume, timer / duration);
            yield return null; // Wait for the next frame
        }

        audioSource.volume = endVolume; // Ensure volume is exactly 0 at the end
    }
}
