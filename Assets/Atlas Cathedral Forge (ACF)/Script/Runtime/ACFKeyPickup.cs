using UnityEngine;
using UnityEngine.InputSystem;

namespace ACFSystem
{
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class ACFKeyPickup : MonoBehaviour
    {
        [SerializeField] private ACFKeyData keyData;
        [SerializeField] private bool destroyOnPickup = true;
        [SerializeField] private bool autoConfigureFromObjectName = true;
        [SerializeField] private bool autoAddKeyRingIfMissing = true;
        [SerializeField] private bool enableLogs = true;
        [SerializeField] private float fallbackPickupRadius = 2f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool acceptCharacterControllerCollectors = true;
        [SerializeField] private bool acceptRigidBodyCollectors = true;

        private Collider pickupCollider;
        private Transform nearbyCollectorRoot;
        private ACFPlayerKeyRing nearbyKeyRing;
        private bool isCollected;
        private bool loggedMissingPlayerContext;

        private void Reset()
        {
            EnsureKeyDataReference();
        }

        private void OnValidate()
        {
            EnsureKeyDataReference();
        }

        private void Awake()
        {
            EnsureKeyDataReference();

            bool needsAutoConfig =
                string.IsNullOrWhiteSpace(keyData.keyId) ||
                keyData.keyId == "Key_01" ||
                string.IsNullOrWhiteSpace(keyData.displayName) ||
                keyData.displayName == "Key";

            if (autoConfigureFromObjectName && needsAutoConfig)
                keyData.AutoConfigureFromObjectName();

            pickupCollider = GetComponent<Collider>();
            if (pickupCollider == null)
                pickupCollider = gameObject.AddComponent<SphereCollider>();

            pickupCollider.isTrigger = true;
        }

        private void OnEnable()
        {
            isCollected = false;
            loggedMissingPlayerContext = false;
        }

        private void Update()
        {
            RefreshNearbyCollectorFromDistance();

            if (!CanShowPrompt())
            {
                ACFKeyPickupPromptUI.Hide();
                return;
            }

            ACFKeyPickupPromptUI.Show(BuildPromptText());

            if (WasCollectPressed())
                TryCollect();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected || other == null)
                return;

            Log($"Trigger enter from '{other.name}' on key '{name}'.");
            if (!TryResolveCollector(other.transform, out Transform collectorRoot, out ACFPlayerKeyRing keyRing))
            {
                Log($"Trigger enter ignored on '{name}' because no valid collector context was found.");
                return;
            }

            AssignNearbyCollector(collectorRoot, keyRing);
        }

        private void OnTriggerExit(Collider other)
        {
            if (nearbyCollectorRoot == null || other == null)
                return;

            Transform source = other.transform;
            if (source != nearbyCollectorRoot &&
                !source.IsChildOf(nearbyCollectorRoot) &&
                !nearbyCollectorRoot.IsChildOf(source))
                return;

            Log($"Trigger exit from '{other.name}' on key '{name}'.");
            ClearNearbyCollector();
        }

        private void OnDisable()
        {
            if (!isCollected)
                ACFKeyPickupPromptUI.Hide();
        }

        private bool CanShowPrompt()
        {
            bool canShow =
                !isCollected &&
                nearbyCollectorRoot != null &&
                nearbyKeyRing != null &&
                nearbyCollectorRoot.gameObject.activeInHierarchy;

            if (!canShow && enableLogs && nearbyCollectorRoot == null && !loggedMissingPlayerContext)
            {
                Log($"Prompt hidden for '{name}' because no nearby collector context is assigned yet.");
                loggedMissingPlayerContext = true;
            }

            return canShow;
        }

        private string BuildPromptText()
        {
            string buttonName = Gamepad.current != null ? "A" : "E";
            string display = keyData != null && !string.IsNullOrWhiteSpace(keyData.displayName)
                ? keyData.displayName
                : "Key";
            return $"Press {buttonName} to collect {display}";
        }

        private bool WasCollectPressed()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                return true;

            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                return true;

            return false;
        }

        private bool TryCollect()
        {
            if (!CanShowPrompt() || keyData == null)
            {
                Log($"Collect failed on '{name}'. canShow={CanShowPrompt()} keyData={(keyData != null)}.");
                return false;
            }

            bool added = nearbyKeyRing.AddKey(keyData.keyId);
            bool alreadyOwned = nearbyKeyRing.HasKey(keyData.keyId);
            if (!added && !alreadyOwned)
            {
                Log($"Collect failed for key '{keyData.keyId}' because AddKey returned false and key ring does not report ownership.");
                return false;
            }

            isCollected = true;
            ACFKeyPickupPromptUI.Hide();
            Log($"Collected key '{keyData.keyId}' from '{name}'. added={added} alreadyOwned={alreadyOwned} destroyOnPickup={destroyOnPickup}.");

            DisableVisualsAndColliders();

            if (destroyOnPickup)
            {
                Destroy(gameObject);
                Log($"Destroy requested for collected key object '{name}'.");
            }
            else
            {
                gameObject.SetActive(false);
                Log($"Collected key object '{name}' set inactive.");
            }

            return true;
        }

        private void ClearNearbyCollector()
        {
            nearbyCollectorRoot = null;
            nearbyKeyRing = null;
            loggedMissingPlayerContext = false;
            ACFKeyPickupPromptUI.Hide();
        }

        private void EnsureKeyDataReference()
        {
            keyData = keyData != null ? keyData : GetComponent<ACFKeyData>();
            if (keyData == null)
                keyData = gameObject.AddComponent<ACFKeyData>();
        }

        private void RefreshNearbyCollectorFromDistance()
        {
            if (isCollected)
                return;

            Vector3 referencePoint = GetReferencePoint();
            float bestDistance = float.PositiveInfinity;
            ACFPlayerKeyRing bestKeyRing = null;

            ACFPlayerKeyRing[] keyRings = Object.FindObjectsByType<ACFPlayerKeyRing>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < keyRings.Length; i++)
            {
                ACFPlayerKeyRing keyRing = keyRings[i];
                if (keyRing == null || !keyRing.isActiveAndEnabled)
                    continue;

                float distance = Vector3.Distance(referencePoint, keyRing.transform.position);
                if (distance <= fallbackPickupRadius && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestKeyRing = keyRing;
                }
            }

            if (bestKeyRing != null)
            {
                if (nearbyCollectorRoot != bestKeyRing.transform)
                    Log($"Fallback proximity acquired collector '{bestKeyRing.name}' for key '{name}' at distance {bestDistance:0.##}.");

                AssignNearbyCollector(bestKeyRing.transform, bestKeyRing);
            }
            else if (nearbyCollectorRoot != null)
            {
                Log($"Fallback proximity cleared collector '{nearbyCollectorRoot.name}' for key '{name}'.");
                ClearNearbyCollector();
            }
        }

        private void AssignNearbyCollector(Transform collectorRoot, ACFPlayerKeyRing keyRing)
        {
            nearbyCollectorRoot = collectorRoot;
            nearbyKeyRing = keyRing;
            loggedMissingPlayerContext = false;
            Log($"Nearby collector assigned for key '{name}': collector='{collectorRoot?.name}', keyRingFound={(keyRing != null)}.");
        }

        private Vector3 GetReferencePoint()
        {
            return pickupCollider != null ? pickupCollider.bounds.center : transform.position;
        }

        private bool TryResolveCollector(Transform source, out Transform collectorRoot, out ACFPlayerKeyRing keyRing)
        {
            collectorRoot = null;
            keyRing = null;

            if (source == null)
                return false;

            keyRing = source.GetComponentInParent<ACFPlayerKeyRing>();
            if (keyRing != null)
            {
                collectorRoot = keyRing.transform;
                return true;
            }

            collectorRoot = ResolveCollectorRoot(source);
            if (collectorRoot == null)
                return false;

            keyRing = collectorRoot.GetComponent<ACFPlayerKeyRing>();
            if (keyRing == null && autoAddKeyRingIfMissing)
            {
                keyRing = collectorRoot.gameObject.AddComponent<ACFPlayerKeyRing>();
                Log($"Auto-added ACFPlayerKeyRing to '{collectorRoot.name}' so '{name}' can be collected.");
            }

            return keyRing != null;
        }

        private Transform ResolveCollectorRoot(Transform source)
        {
            Transform taggedRoot = FindTaggedCollector(source);
            if (taggedRoot != null)
                return taggedRoot;

            if (acceptCharacterControllerCollectors)
            {
                CharacterController controller = source.GetComponentInParent<CharacterController>();
                if (controller != null)
                    return controller.transform;
            }

            if (acceptRigidBodyCollectors)
            {
                Rigidbody body = source.GetComponentInParent<Rigidbody>();
                if (body != null)
                    return body.transform;
            }

            return null;
        }

        private Transform FindTaggedCollector(Transform source)
        {
            if (string.IsNullOrWhiteSpace(playerTag))
                return null;

            Transform current = source;
            while (current != null)
            {
                if (HasDefinedTag(current.gameObject, playerTag))
                    return current;

                current = current.parent;
            }

            return null;
        }

        private void DisableVisualsAndColliders()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }
        }

        private void Log(string message)
        {
            if (!enableLogs)
                return;

            Debug.Log($"[ACFKeyPickup] {message}", this);
        }

        private static bool HasDefinedTag(GameObject gameObject, string tagName)
        {
            if (gameObject == null || string.IsNullOrWhiteSpace(tagName))
                return false;

            try
            {
                string currentTag = gameObject.tag;
                return !string.IsNullOrWhiteSpace(currentTag) &&
                       string.Equals(currentTag, tagName, System.StringComparison.Ordinal);
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}
