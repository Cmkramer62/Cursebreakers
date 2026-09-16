using UnityEngine;

public class AudioManager : MonoBehaviour {
    private static AudioManager _instance;
    public static AudioManager Instance {
        get {
            // If someone calls Instance before Awake() runs,
            // try to grab the existing one in the scene automatically.
            if(_instance == null)
                _instance = FindObjectOfType<AudioManager>();

            return _instance;
        }
    }

    [Header("Audio Setup")]
    public AudioSource source;
    public AudioClip hoverClip, confirmClip, denyClip;

    private void Awake() {
        // Standard singleton safety
        if(_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    public static void PlayHover() => Instance.source.PlayOneShot(Instance.hoverClip);
    public static void PlayConfirm() => Instance.source.PlayOneShot(Instance.confirmClip);
    public static void PlayDeny() => Instance.source.PlayOneShot(Instance.denyClip);
}
