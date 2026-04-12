using UnityEngine;

namespace ACFSystem
{
    [AddComponentMenu("ACF/ACF Debug Helper")]
    public class ACFDebugHelper : MonoBehaviour
    {
        [Header("Debug Settings")]
        public bool logCollisions = true;
        public bool logTriggers = true;
        public bool logCollisionStay;
        public bool logTriggerStay;
        public Color gizmoColor = Color.cyan;

        private void OnCollisionEnter(Collision collision)
        {
            if (logCollisions && collision != null)
                Debug.Log($"[ACFDebug] {name} collided with {collision.gameObject.name}. Velocity={collision.relativeVelocity.magnitude:0.##}", this);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (logCollisionStay && collision != null)
                Debug.Log($"[ACFDebug] {name} still colliding with {collision.gameObject.name}.", this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (logTriggers && other != null)
                Debug.Log($"[ACFDebug] {name} triggered by {other.gameObject.name} (tag={other.tag}).", this);
        }

        private void OnTriggerStay(Collider other)
        {
            if (logTriggerStay && other != null)
                Debug.Log($"[ACFDebug] {name} still triggered by {other.gameObject.name} (tag={other.tag}).", this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }
    }
}
