using System.Collections.Generic;
using UnityEngine;

namespace ACFSystem
{
    [DisallowMultipleComponent]
    public class ACFPlayerKeyRing : MonoBehaviour
    {
        [SerializeField] private List<string> ownedKeys = new List<string>();
        [SerializeField] private bool enableLogs = true;

        public bool HasKey(string keyId)
        {
            return !string.IsNullOrWhiteSpace(keyId) && ownedKeys.Contains(keyId);
        }

        public bool AddKey(string keyId)
        {
            if (string.IsNullOrWhiteSpace(keyId) || ownedKeys.Contains(keyId))
                return false;

            ownedKeys.Add(keyId);
            Log($"Added key '{keyId}' to '{name}'.");
            return true;
        }

        public IReadOnlyList<string> GetAllKeys()
        {
            return ownedKeys;
        }

        private void Log(string message)
        {
            if (!enableLogs)
                return;

            Debug.Log($"[ACFPlayerKeyRing] {message}", this);
        }
    }
}
