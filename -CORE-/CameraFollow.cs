using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class CameraFollow : MonoBehaviour {
    [Serializable]
    public class ImpulseProfile {
        [Tooltip("Camera-local position offset in metres at full strength.")]
        public Vector3 position = Vector3.zero;
        [Tooltip("Camera-local rotation offset in degrees at full strength.")]
        public Vector3 rotation = Vector3.zero;
        [Min(0.05f)] public float duration = 0.25f;
        [Min(0f)] public float frequency = 18f;
        [Range(0f, 1f)] public float decay = 0.85f;
    }

    private class ActiveImpulse {
        public Vector3 position;
        public Vector3 rotation;
        public float duration;
        public float frequency;
        public float decay;
        public float elapsed;
        public float phase;
    }

    public Transform target;
    public Transform rotationTarget;
    public Vector3 rotationOffset;

    [Header("Camera impact profiles")]
    [SerializeField] private ImpulseProfile jumpImpulse = new ImpulseProfile {
        position = new Vector3(0f, 0.025f, 0f), rotation = new Vector3(-1.2f, 0f, 0f), duration = 0.18f, frequency = 20f, decay = 0.9f
    };
    [SerializeField] private ImpulseProfile landingImpulse = new ImpulseProfile {
        position = new Vector3(0f, -0.055f, 0f), rotation = new Vector3(2.4f, 0f, 0.35f), duration = 0.32f, frequency = 16f, decay = 0.82f
    };
    [SerializeField] private ImpulseProfile heavyLandingImpulse = new ImpulseProfile {
        position = new Vector3(0f, -0.055f, 0f), rotation = new Vector3(2.4f, 0f, 0.35f), duration = 0.32f, frequency = 16f, decay = 0.82f
    };
    [SerializeField] private ImpulseProfile wallGrabFrontImpulse = new ImpulseProfile {
        position = new Vector3(0f, -0.055f, 0f), rotation = new Vector3(2.4f, 0f, 0.35f), duration = 0.32f, frequency = 16f, decay = 0.82f
    };
    [SerializeField] private ImpulseProfile wallGrabLeftImpulse = new ImpulseProfile {
        position = new Vector3(0f, -0.055f, 0f), rotation = new Vector3(2.4f, 0f, 0.35f), duration = 0.32f, frequency = 16f, decay = 0.82f
    };
    [SerializeField] private ImpulseProfile wallGrabRightImpulse = new ImpulseProfile {
        position = new Vector3(0f, -0.055f, 0f), rotation = new Vector3(2.4f, 0f, 0.35f), duration = 0.32f, frequency = 16f, decay = 0.82f
    };

    [SerializeField, Min(0f)] private float landingSpeedForFullStrength = 14f;
    [SerializeField, Range(0f, 1f)] private float minimumLandingStrength = 0.18f;
    [SerializeField, Min(0f)] private float cameraShakeScale = 1f;

    [Header("Earthquake")]
    [SerializeField, Min(0f)] private float earthquakePosition = 0.045f;
    [SerializeField, Min(0f)] private float earthquakeRotation = 1.1f;
    [SerializeField, Min(0f)] private float earthquakeFrequency = 9f;
    [SerializeField, Min(0.01f)] private float earthquakeFadeSpeed = 2f;

    private readonly List<ActiveImpulse> activeImpulses = new List<ActiveImpulse>();
    private float earthquakeIntensity;
    private float targetEarthquakeIntensity;
    private float noiseSeed;

    private void Awake() {
        noiseSeed = UnityEngine.Random.Range(0f, 1000f);
    }

    public void SetTarget(Transform newTarget) {
        target = newTarget;
    }

    /// <summary>Adds a camera-local impulse. Repeated events stack and decay independently.</summary>
    public void AddImpulse(ImpulseProfile profile, float strength = 1f) {
        if(profile == null || strength <= 0f || cameraShakeScale <= 0f) return;

        activeImpulses.Add(new ActiveImpulse {
            position = profile.position * strength * cameraShakeScale,
            rotation = profile.rotation * strength * cameraShakeScale,
            duration = Mathf.Max(0.05f, profile.duration),
            frequency = Mathf.Max(0f, profile.frequency),
            decay = Mathf.Clamp01(profile.decay),
            phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f)
        });
    }

    public void PlayJumpImpulse() {
        AddImpulse(jumpImpulse);
    }

    public void PlayWallGrabLeftImpulse() {
        AddImpulse(wallGrabLeftImpulse);
    }

    public void PlayWallGrabRightImpulse() {
        AddImpulse(wallGrabRightImpulse);
    }

    public void PlayWallGrabFrontImpulse() {
        AddImpulse(wallGrabFrontImpulse);
    }

    /// <summary>Scales the landing response from a soft touch-down to a hard fall.</summary>
    public void PlayLandingImpulse(float impactSpeed) {
        Debug.Log("Normal landing.");
        if(impactSpeed <= 0f) return;
        float normalizedSpeed = landingSpeedForFullStrength > 0f
            ? Mathf.Clamp01(impactSpeed / landingSpeedForFullStrength)
            : 1f;
        float strength = Mathf.Lerp(minimumLandingStrength, 1f, normalizedSpeed);
        Debug.Log("Landing Speed: " + impactSpeed + " strength: " + strength);
        AddImpulse(landingImpulse, strength);
    }

    public void PlayGreatLandingImpulse() {
        Debug.Log("Great landing.");
        AddImpulse(heavyLandingImpulse, 1);
    }

    /// <summary>Sets ongoing earthquake strength. Pass zero to fade the tremor out.</summary>
    public void SetEarthquakeIntensity(float intensity) {
        targetEarthquakeIntensity = Mathf.Max(0f, intensity);
    }

    public void StopEarthquake() {
        targetEarthquakeIntensity = 0f;
    }

    private void LateUpdate() {
        if(target != null) transform.position = target.position;
        if(rotationTarget != null) transform.rotation = rotationTarget.rotation * Quaternion.Euler(rotationOffset);

        // MouseLook writes the camera's local rotation during LateUpdate. This component runs
        // afterward and treats that result as the base pose, so camera effects never accumulate.
        Quaternion baseLocalRotation = transform.localRotation;
        Quaternion baseWorldRotation = transform.rotation;

        Vector3 localPositionOffset = Vector3.zero;
        Vector3 localRotationOffset = Vector3.zero;
        float deltaTime = Time.deltaTime;

        for(int i = activeImpulses.Count - 1; i >= 0; i--) {
            ActiveImpulse impulse = activeImpulses[i];
            impulse.elapsed += deltaTime;
            float progress = Mathf.Clamp01(impulse.elapsed / impulse.duration);
            float envelope = Mathf.Pow(1f - progress, 1f + impulse.decay * 5f);
            float wave = Mathf.Sin(impulse.phase + impulse.elapsed * impulse.frequency * Mathf.PI * 2f);
            localPositionOffset += impulse.position * (envelope * wave);
            localRotationOffset += impulse.rotation * (envelope * wave);

            if(impulse.elapsed >= impulse.duration) activeImpulses.RemoveAt(i);
        }

        earthquakeIntensity = Mathf.MoveTowards(
            earthquakeIntensity,
            targetEarthquakeIntensity,
            earthquakeFadeSpeed * deltaTime
        );
        if(earthquakeIntensity > 0.001f && cameraShakeScale > 0f) {
            float time = Time.time * earthquakeFrequency;
            localPositionOffset += new Vector3(
                (Mathf.PerlinNoise(noiseSeed, time) - 0.5f) * 2f,
                (Mathf.PerlinNoise(noiseSeed + 23.4f, time) - 0.5f) * 2f,
                0f
            ) * earthquakePosition * earthquakeIntensity * cameraShakeScale;
            localRotationOffset += new Vector3(
                (Mathf.PerlinNoise(noiseSeed + 57.1f, time) - 0.5f) * 2f,
                (Mathf.PerlinNoise(noiseSeed + 81.6f, time) - 0.5f) * 2f,
                (Mathf.PerlinNoise(noiseSeed + 109.2f, time) - 0.5f) * 2f
            ) * earthquakeRotation * earthquakeIntensity * cameraShakeScale;
        }

        // Apply after the follow pose so camera shake never accumulates or fights camera tracking.
        transform.position += transform.right * localPositionOffset.x
            + transform.up * localPositionOffset.y
            + transform.forward * localPositionOffset.z;
        if(rotationTarget != null) {
            transform.rotation = baseWorldRotation * Quaternion.Euler(localRotationOffset);
        }
        else {
            transform.localRotation = baseLocalRotation * Quaternion.Euler(localRotationOffset);
        }
    }
}
