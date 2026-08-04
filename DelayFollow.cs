using UnityEngine;

public class DelayFollow : MonoBehaviour {
    public Transform target;      // The object to follow
    [SerializeField] private float smoothTime = 0.3f; // Approximate time to reach target
    [SerializeField] private float maxLagDistance = 2f;

    private Vector3 _currentVelocity;

    private void LateUpdate() {
        if(target == null) return;

        // Smoothly interpolates the position over smoothTime
        transform.position = Vector3.SmoothDamp(
            transform.position,
            target.position,
            ref _currentVelocity,
            smoothTime
        );

        // Clamp the maximum distance behind the target.
        Vector3 offset = transform.position - target.position;

        if(offset.sqrMagnitude > maxLagDistance * maxLagDistance) {
            transform.position = target.position + offset.normalized * maxLagDistance;

            // Optional: prevent the stored velocity from causing overshoot.
            _currentVelocity = Vector3.zero;
        }
    }
}
