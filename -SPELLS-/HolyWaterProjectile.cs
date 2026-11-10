using UnityEngine;
using Unity.Netcode;
using UnityEngine.Animations;
using System.Collections;

public class HolyWaterProjectile : MonoBehaviour {
    private bool hasHit;

    [SerializeReference] private float duration = 5f;

    // AUDIO
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip[] waterSplashClips;

    public void Awake() {
        StartCoroutine(DeathTimer());
        source.pitch = Random.Range(.9f, 1.1f);
        source.PlayOneShot(waterSplashClips[Random.Range(0, waterSplashClips.Length)]);
    }

    private void OnTriggerEnter(Collider other) {
        if(hasHit)
            return;

        Debug.Log("water touch: " + other.name);

        ProfanusClue blood = other.GetComponent<ProfanusClue>();

        if(blood == null)
            return;

        hasHit = true;

        blood.ApplyHolyWater();

        //GameObject.Destroy(gameObject);
    }

    private IEnumerator DeathTimer() {
        yield return new WaitForSeconds(duration);
        GameObject.Destroy(gameObject);
    }
}

