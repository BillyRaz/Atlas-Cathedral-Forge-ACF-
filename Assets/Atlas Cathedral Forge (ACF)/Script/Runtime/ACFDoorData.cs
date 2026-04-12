using UnityEngine;

namespace ACFSystem
{
    [AddComponentMenu("ACF/ACF Door Data")]
    public class ACFDoorData : MonoBehaviour
    {
        public enum HingeEdge
        {
            Left,
            Right
        }

        [Header("Unlock Setup")]
        public string requiredKeyId = "";
        public ACFKeyData linkedKey;
        public bool startsLocked = false;

        [Header("Door Behavior")]
        public bool pushToOpen = true;
        public float openAngle = 90f;
        public float openSpeed = 120f;

        [Header("Hinge")]
        public HingeEdge hingeEdge = HingeEdge.Left;
        public Transform hingePivotOverride;
        public bool buildRuntimePivot = true;

        [Header("Push Detection")]
        public string playerTag = "Player";
        public float minimumPushStrength = 0.05f;
        public bool autoClose;
        public float closeSpeed = 90f;

        [Header("Interaction Settings")]
        public bool useTriggerDetection = true;
        public bool useCollisionDetection = true;
        public LayerMask pushLayers = ~0;

        [Header("Obstacle Check")]
        public bool preventOpeningIntoObstacles = true;
        public LayerMask obstacleLayers = ~0;
        public float obstaclePadding = 0.02f;
        public float obstacleInset = 0.05f;
        public bool ignoreCameraBlocker = true;
        public bool ignoreFloorWhenOpening = true;
        public bool ignoreWallsWhenOpening = true;
        public bool ignoreCorridorsWhenOpening = true;
        public string floorTagName = "Floor";
        public string[] ignoredObstacleNames = { "__ACF_CameraBlocker_MeshCollider", "Floor", "Wall", "Corridor" };

        [Header("Collider Setup")]
        public bool autoAddColliderIfMissing = true;
        public bool generatedColliderIsTrigger = true;

        [Header("Diagnostics")]
        public bool enableLogs = true;

        private Transform hingePivot;
        private Quaternion closedPivotRotation;
        private float targetSignedAngle;
        private bool isLocked;
        private float lastPushTime = float.NegativeInfinity;
        private Bounds localDoorBounds;
        private Vector3 doorBoundsCenterInPivotSpace;
        private Quaternion doorRotationInPivotSpace;
        private Collider[] doorColliders = new Collider[0];

        private void Awake()
        {
            EnsureDoorCollider();
            doorColliders = GetComponentsInChildren<Collider>(true);

            InitializeLockState();
            InitializePivot();
            CacheDoorBounds();

            Log($"Door '{name}' initialized. Colliders found: {doorColliders.Length}.");
        }

        private void Update()
        {
            if (hingePivot == null)
                return;

            if (autoClose && Mathf.Abs(targetSignedAngle) > 0.01f && Time.time - lastPushTime > 0.35f)
                targetSignedAngle = 0f;

            float speed = Mathf.Abs(targetSignedAngle) > 0.01f ? openSpeed : closeSpeed;
            Quaternion targetRotation = closedPivotRotation * Quaternion.AngleAxis(targetSignedAngle, Vector3.up);
            hingePivot.localRotation = Quaternion.RotateTowards(hingePivot.localRotation, targetRotation, speed * Time.deltaTime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!useCollisionDetection)
                return;

            if (collision != null)
                Log($"Collision enter from '{collision.transform.name}' on '{name}'. Velocity={collision.relativeVelocity.magnitude:0.##}.");

            TryPushFromCollision(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!useCollisionDetection)
                return;

            if (collision != null)
                Log($"Collision stay from '{collision.transform.name}' on '{name}'. Contacts={collision.contactCount}.");

            TryPushFromCollision(collision);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!useTriggerDetection)
                return;

            if (other != null)
                Log($"Trigger enter from '{other.transform.name}' (tag={other.tag}) on '{name}'.");

            TryPushFromTrigger(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!useTriggerDetection)
                return;

            if (other != null)
                Log($"Trigger stay from '{other.transform.name}' (tag={other.tag}) on '{name}'.");

            TryPushFromTrigger(other);
        }

        public bool IsLocked()
        {
            return isLocked;
        }

        public void TryPushFromSource(Transform source, Vector3 contactPoint, float pushStrength)
        {
            TryPushOpen(source, contactPoint, pushStrength, false);
        }

        public void TryInteractFromSource(Transform source, Vector3 contactPoint, float pushStrength)
        {
            TryPushOpen(source, contactPoint, pushStrength, true);
        }

        public bool CanUnlockFromSource(Transform source)
        {
            return HasMatchingKey(source);
        }

        public void Unlock()
        {
            if (!isLocked)
                return;

            isLocked = false;
            Log($"Unlocked '{name}'.");
        }

        private void InitializeLockState()
        {
            bool hasConfiguredKey = linkedKey != null || !string.IsNullOrWhiteSpace(requiredKeyId);
            isLocked = startsLocked && hasConfiguredKey;
        }

        private void EnsureDoorCollider()
        {
            if (!autoAddColliderIfMissing)
                return;

            Collider[] existingColliders = GetComponentsInChildren<Collider>(true);
            if (existingColliders != null && existingColliders.Length > 0)
            {
                Log($"Door '{name}' already has {existingColliders.Length} collider(s).");
                return;
            }

            Bounds bounds = CalculateLocalBounds();
            BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();
            if (boxCollider == null)
                boxCollider = gameObject.AddComponent<BoxCollider>();

            boxCollider.center = bounds.center;
            boxCollider.size = bounds.size;
            boxCollider.isTrigger = generatedColliderIsTrigger;

            Log($"Added BoxCollider to '{name}'. Center={boxCollider.center}, Size={boxCollider.size}, Trigger={boxCollider.isTrigger}.");
        }

        private void InitializePivot()
        {
            if (hingePivotOverride != null)
            {
                hingePivot = hingePivotOverride;
                closedPivotRotation = hingePivot.localRotation;
                return;
            }

            if (!buildRuntimePivot)
            {
                hingePivot = transform;
                closedPivotRotation = hingePivot.localRotation;
                return;
            }

            Transform originalParent = transform.parent;
            GameObject pivotObject = new GameObject($"{name}_Pivot");
            hingePivot = pivotObject.transform;
            hingePivot.SetParent(originalParent, false);
            hingePivot.SetPositionAndRotation(transform.position, transform.rotation);
            hingePivot.localScale = Vector3.one;

            Vector3 pivotWorldPosition = CalculateHingeWorldPosition();
            hingePivot.position = pivotWorldPosition;
            transform.SetParent(hingePivot, true);

            closedPivotRotation = hingePivot.localRotation;
            Log($"Created runtime hinge pivot for '{name}' at {pivotWorldPosition}.");
        }

        private Vector3 CalculateHingeWorldPosition()
        {
            Bounds bounds = CalculateLocalBounds();
            float hingeX = hingeEdge == HingeEdge.Left ? bounds.min.x : bounds.max.x;
            Vector3 localPivot = new Vector3(hingeX, bounds.center.y, bounds.center.z);
            return transform.TransformPoint(localPivot);
        }

        private void CacheDoorBounds()
        {
            localDoorBounds = CalculateLocalBounds();
            Vector3 currentCenterWorld = transform.TransformPoint(localDoorBounds.center);
            if (hingePivot != null)
            {
                doorBoundsCenterInPivotSpace = hingePivot.InverseTransformPoint(currentCenterWorld);
                doorRotationInPivotSpace = Quaternion.Inverse(hingePivot.rotation) * transform.rotation;
            }
            else
            {
                doorBoundsCenterInPivotSpace = localDoorBounds.center;
                doorRotationInPivotSpace = transform.localRotation;
            }
        }

        private Bounds CalculateLocalBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds combined = default;

            for (int i = 0; i < renderers.Length; i++)
            {
                Bounds worldBounds = renderers[i].bounds;
                Vector3 extents = worldBounds.extents;
                Vector3 center = worldBounds.center;

                Vector3[] corners =
                {
                    center + new Vector3(-extents.x, -extents.y, -extents.z),
                    center + new Vector3(-extents.x, -extents.y, extents.z),
                    center + new Vector3(-extents.x, extents.y, -extents.z),
                    center + new Vector3(-extents.x, extents.y, extents.z),
                    center + new Vector3(extents.x, -extents.y, -extents.z),
                    center + new Vector3(extents.x, -extents.y, extents.z),
                    center + new Vector3(extents.x, extents.y, -extents.z),
                    center + new Vector3(extents.x, extents.y, extents.z)
                };

                for (int c = 0; c < corners.Length; c++)
                {
                    Vector3 localPoint = transform.InverseTransformPoint(corners[c]);
                    if (!hasBounds)
                    {
                        combined = new Bounds(localPoint, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        combined.Encapsulate(localPoint);
                    }
                }
            }

            if (!hasBounds)
            {
                Collider ownCollider = GetComponent<Collider>();
                if (ownCollider != null)
                {
                    Vector3 localCenter = transform.InverseTransformPoint(ownCollider.bounds.center);
                    combined = new Bounds(localCenter, ownCollider.bounds.size);
                }
                else
                {
                    combined = new Bounds(Vector3.zero, Vector3.one);
                }
            }

            return combined;
        }

        private void TryPushFromCollision(Collision collision)
        {
            if (!pushToOpen || collision == null || collision.contactCount == 0)
                return;

            if (!CanBePushedBy(collision.transform))
                return;

            float pushStrength = collision.relativeVelocity.magnitude;
            if (pushStrength < minimumPushStrength && minimumPushStrength > 0f)
                pushStrength = minimumPushStrength;

            Transform source = collision.transform;
            Vector3 contactPoint = collision.GetContact(0).point;

            Log($"Push from collision: source={source.name}, strength={pushStrength:0.##}, contact={contactPoint}.");
            TryPushOpen(source, contactPoint, pushStrength, false);
        }

        private void TryPushFromTrigger(Collider other)
        {
            if (!pushToOpen || other == null)
                return;

            Transform source = other.transform;
            if (!CanBePushedBy(source))
                return;

            Vector3 contactPoint = other.ClosestPoint(transform.position);
            float pushStrength = minimumPushStrength > 0f ? minimumPushStrength : 0.1f;

            Log($"Push from trigger: source={source.name}, contact={contactPoint}.");
            TryPushOpen(source, contactPoint, pushStrength, false);
        }

        private bool CanBePushedBy(Transform source)
        {
            if (source == null)
                return false;

            int sourceLayerMask = 1 << source.gameObject.layer;
            if ((pushLayers.value & sourceLayerMask) == 0)
            {
                Log($"Source '{source.name}' rejected because layer '{LayerMask.LayerToName(source.gameObject.layer)}' is not in Push Layers.");
                return false;
            }

            if (HasDefinedTag(source.gameObject, playerTag))
            {
                Log($"Source '{source.name}' recognized by player tag.");
                return true;
            }

            if (source.GetComponentInParent<CharacterController>() != null)
            {
                Log($"Source '{source.name}' recognized by CharacterController.");
                return true;
            }

            if (source.GetComponentInParent<Rigidbody>() != null)
            {
                Log($"Source '{source.name}' recognized by Rigidbody.");
                return true;
            }

            if (source.GetComponentInParent<ACFPlayerKeyRing>() != null)
            {
                Log($"Source '{source.name}' recognized by ACFPlayerKeyRing.");
                return true;
            }

            Log($"Source '{source.name}' NOT recognized as pushable.");
            return false;
        }

        private void TryPushOpen(Transform source, Vector3 contactPoint, float pushStrength, bool allowUnlock)
        {
            if (source == null || hingePivot == null)
                return;

            if (allowUnlock && TryUnlockFromSource(source))
                Log($"'{name}' unlocked by '{source.name}'.");

            if (IsLocked())
            {
                Log($"'{name}' did not open because it is locked.");
                return;
            }

            float signedOpenDirection = DetermineOpenDirection(source.position, contactPoint);
            float desiredAngle = signedOpenDirection * Mathf.Abs(openAngle);

            if (preventOpeningIntoObstacles && IsBlocked(desiredAngle, source))
            {
                float alternateAngle = -desiredAngle;
                if (!Mathf.Approximately(alternateAngle, desiredAngle) && !IsBlocked(alternateAngle, source))
                {
                    Log($"'{name}' switched swing direction from {desiredAngle:0.##} to {alternateAngle:0.##} degrees because the preferred side was blocked.");
                    desiredAngle = alternateAngle;
                }
                else
                {
                    Log($"'{name}' is blocked and cannot open toward {desiredAngle:0.##} degrees.");
                    return;
                }
            }

            if (Mathf.Abs(targetSignedAngle - desiredAngle) < 0.01f && Time.time - lastPushTime < 0.1f)
            {
                lastPushTime = Time.time;
                return;
            }

            targetSignedAngle = desiredAngle;
            lastPushTime = Time.time;
            Log($"'{name}' opened from push by '{source.name}'. Push={pushStrength:0.##}, TargetAngle={desiredAngle:0.##}.");
        }

        private float DetermineOpenDirection(Vector3 sourcePosition, Vector3 contactPoint)
        {
            Vector3 toSource = sourcePosition - hingePivot.position;
            if (toSource.sqrMagnitude < 0.0001f)
                toSource = contactPoint - hingePivot.position;

            float side = Mathf.Sign(Vector3.Dot(transform.forward, toSource.normalized));
            if (Mathf.Approximately(side, 0f))
                side = 1f;

            return side > 0f ? 1f : -1f;
        }

        private bool TryUnlockFromSource(Transform source)
        {
            if (!isLocked)
                return false;

            if (HasMatchingKey(source))
            {
                Unlock();
                return true;
            }

            return false;
        }

        private bool HasMatchingKey(Transform source)
        {
            if (source == null)
                return false;

            if (linkedKey != null)
            {
                ACFKeyData sourceKey = source.GetComponentInParent<ACFKeyData>();
                if (sourceKey != null && sourceKey.keyId == linkedKey.keyId)
                    return true;
            }

            if (!string.IsNullOrWhiteSpace(requiredKeyId))
            {
                ACFKeyData sourceKey = source.GetComponentInParent<ACFKeyData>();
                if (sourceKey != null && sourceKey.keyId == requiredKeyId)
                    return true;

                ACFPlayerKeyRing keyRing = source.GetComponentInParent<ACFPlayerKeyRing>();
                if (keyRing != null && keyRing.HasKey(requiredKeyId))
                    return true;
            }

            return false;
        }

        private bool IsBlocked(float desiredAngle, Transform pushingSource)
        {
            Quaternion targetPivotRotation = closedPivotRotation * Quaternion.AngleAxis(desiredAngle, Vector3.up);
            Quaternion targetWorldRotation = hingePivot.parent != null
                ? hingePivot.parent.rotation * targetPivotRotation
                : targetPivotRotation;

            Vector3 worldCenter = hingePivot.position + (targetWorldRotation * doorBoundsCenterInPivotSpace);
            Quaternion worldOrientation = targetWorldRotation * doorRotationInPivotSpace;
            Vector3 halfExtents = GetObstacleCheckHalfExtents();

            Collider[] overlaps = Physics.OverlapBox(worldCenter, halfExtents, worldOrientation, obstacleLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider overlap = overlaps[i];
                if (overlap == null || overlap.transform.IsChildOf(transform) || IsOwnCollider(overlap))
                    continue;

                if (IsPushingSource(overlap.transform, pushingSource))
                    continue;

                if (ShouldIgnoreObstacle(overlap))
                {
                    Log($"'{name}' ignoring obstacle '{overlap.name}' because it is marked ignorable.");
                    continue;
                }

                Log($"'{name}' blocked by '{overlap.name}'.");
                return true;
            }

            return false;
        }

        private static bool IsPushingSource(Transform obstacleTransform, Transform pushingSource)
        {
            if (obstacleTransform == null || pushingSource == null)
                return false;

            return obstacleTransform == pushingSource
                || obstacleTransform.IsChildOf(pushingSource)
                || pushingSource.IsChildOf(obstacleTransform);
        }

        private bool ShouldIgnoreObstacle(Collider obstacle)
        {
            if (obstacle == null || ignoredObstacleNames == null)
                return false;

            for (int i = 0; i < ignoredObstacleNames.Length; i++)
            {
                string ignoredName = ignoredObstacleNames[i];
                if (string.IsNullOrWhiteSpace(ignoredName))
                    continue;

                if (obstacle.name.IndexOf(ignoredName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                Transform parent = obstacle.transform.parent;
                if (parent != null && parent.name.IndexOf(ignoredName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            if (ignoreFloorWhenOpening &&
                (IsTaggedAsFloorByACFData(obstacle.transform) ||
                 obstacle.name.IndexOf("Floor", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 HasParentNamedFloor(obstacle.transform) ||
                 HasDefinedTag(obstacle.gameObject, floorTagName)))
                return true;

            if (ignoreWallsWhenOpening &&
                (IsTaggedAsWallByACFData(obstacle.transform) ||
                 obstacle.name.IndexOf("Wall", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 HasParentNamed(obstacle.transform, "Wall") ||
                 HasDefinedTag(obstacle.gameObject, ACFCategoryUtility.Wall)))
                return true;

            if (ignoreCorridorsWhenOpening &&
                (IsTaggedAsCorridorByACFData(obstacle.transform) ||
                 obstacle.name.IndexOf("Corridor", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 HasParentNamed(obstacle.transform, "Corridor")))
                return true;

            if (ignoreCameraBlocker && obstacle.name.IndexOf("CameraBlocker", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
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

        private static bool IsTaggedAsFloorByACFData(Transform source)
        {
            if (source == null)
                return false;

            ACFObjectData objectData = source.GetComponentInParent<ACFObjectData>();
            return objectData != null && objectData.category == ACFObjectData.ObjectCategory.Floor;
        }

        private static bool IsTaggedAsWallByACFData(Transform source)
        {
            if (source == null)
                return false;

            ACFObjectData objectData = source.GetComponentInParent<ACFObjectData>();
            return objectData != null && objectData.category == ACFObjectData.ObjectCategory.Wall;
        }

        private static bool IsTaggedAsCorridorByACFData(Transform source)
        {
            if (source == null)
                return false;

            ACFObjectData objectData = source.GetComponentInParent<ACFObjectData>();
            return objectData != null &&
                   objectData.category == ACFObjectData.ObjectCategory.Floor &&
                   string.Equals(objectData.customCategory, "CorridorFloor", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasParentNamedFloor(Transform source)
        {
            return HasParentNamed(source, "Floor");
        }

        private static bool HasParentNamed(Transform source, string value)
        {
            if (source == null || string.IsNullOrWhiteSpace(value))
                return false;

            Transform parent = source.parent;
            return parent != null &&
                   parent.name.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private Vector3 GetWorldHalfExtents(Vector3 localExtents)
        {
            Vector3 scale = transform.lossyScale;
            return new Vector3(
                Mathf.Abs(localExtents.x * scale.x),
                Mathf.Abs(localExtents.y * scale.y),
                Mathf.Abs(localExtents.z * scale.z));
        }

        private Vector3 GetObstacleCheckHalfExtents()
        {
            Vector3 halfExtents = GetWorldHalfExtents(localDoorBounds.extents);
            float inset = Mathf.Max(0f, obstacleInset);

            halfExtents.x = Mathf.Max(0.01f, halfExtents.x - inset);
            halfExtents.y = Mathf.Max(0.01f, halfExtents.y - inset);
            halfExtents.z = Mathf.Max(0.01f, halfExtents.z - inset);

            if (obstaclePadding > 0f)
                halfExtents += Vector3.one * obstaclePadding;

            return halfExtents;
        }

        private bool IsOwnCollider(Collider overlap)
        {
            for (int i = 0; i < doorColliders.Length; i++)
            {
                if (doorColliders[i] == overlap)
                    return true;
            }

            return false;
        }

        private void Log(string message)
        {
            if (!enableLogs)
                return;

            Debug.Log($"[ACFDoorData] {message}", this);
        }
    }
}
