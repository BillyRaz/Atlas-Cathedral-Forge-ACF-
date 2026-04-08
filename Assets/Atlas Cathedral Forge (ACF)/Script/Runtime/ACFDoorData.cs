using UnityEngine;

namespace ACFSystem
{
    [AddComponentMenu("ACF/ACF Door Data")]
    public class ACFDoorData : MonoBehaviour
    {
        [Header("Unlock Setup")]
        public string requiredKeyId = "Key_01";
        public ACFKeyData linkedKey;
        public bool startsLocked = true;

        [Header("Door Behavior")]
        public bool pushToOpen = true;
        public float openAngle = 90f;
        public float openSpeed = 2f;
    }
}
