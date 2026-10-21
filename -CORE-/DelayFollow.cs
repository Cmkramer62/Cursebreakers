



using UnityEngine;

public class DelayFollow : MonoBehaviour {
    public Transform target;

    [SerializeField] private float followStrength = 8f;
    [SerializeField] private float distanceStrength = 3f;
    [SerializeField] private float backwardFollowStrength = 40f;
    [SerializeField] private float damping = 5f;
    [SerializeField] private float maxSpeed = 30f;

    private Vector3 _currentVelocity;

    public void SetNewValues(float newFollowStrength, float newDistanceStrength, float newDamping, float newMaxSpeed, float newBackStrength) {
        followStrength = newFollowStrength;
        distanceStrength = newDistanceStrength;
        damping = newDamping;
        maxSpeed = newMaxSpeed;
        backwardFollowStrength = newBackStrength;
    }
    private void LateUpdate() {
        if(target == null) {
            _currentVelocity = Vector3.zero;
            return;
        }

        float deltaTime = Time.deltaTime;
        if(deltaTime <= 0f) return;

        Vector3 offset = target.position - transform.position;

        // Express the offset in the target's local space.
        Vector3 localOffset = target.InverseTransformDirection(offset);

        // Normal follow force, with additional strength at greater distances.
        float strength = followStrength + offset.magnitude * distanceStrength;

        Vector3 acceleration = offset * strength;

        // Add extra force only when the glyph is behind the target.
        if(localOffset.z > 0f) {
            acceleration += target.forward *
                            (localOffset.z * backwardFollowStrength);
        }

        // Dampen existing velocity to reduce oscillation.
        acceleration -= _currentVelocity * damping;

        _currentVelocity += acceleration * deltaTime;

        _currentVelocity = Vector3.ClampMagnitude(
            _currentVelocity,
            maxSpeed
        );

        transform.position += _currentVelocity * deltaTime;
    }
}
