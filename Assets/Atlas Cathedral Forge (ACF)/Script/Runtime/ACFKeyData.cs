using UnityEngine;

namespace ACFSystem
{
    [AddComponentMenu("ACF/ACF Key Data")]
    public class ACFKeyData : MonoBehaviour
    {
        [Header("Key Setup")]
        public string keyId = "Key_01";
        public string displayName = "Key";

        public void AutoConfigureFromObjectName()
        {
            string sourceName = gameObject != null ? gameObject.name : keyId;
            keyId = GenerateKeyId(sourceName);
            displayName = GenerateDisplayName(sourceName);
        }

        public static string GenerateKeyId(string sourceName)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                return "key";

            char[] buffer = sourceName.Trim().ToCharArray();
            for (int i = 0; i < buffer.Length; i++)
            {
                char c = buffer[i];
                buffer[i] = char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_';
            }

            string sanitized = new string(buffer);
            while (sanitized.Contains("__"))
                sanitized = sanitized.Replace("__", "_");

            sanitized = sanitized.Trim('_');
            return string.IsNullOrWhiteSpace(sanitized) ? "key" : sanitized;
        }

        public static string GenerateDisplayName(string sourceName)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                return "Key";

            string normalized = sourceName.Replace('_', ' ').Replace('-', ' ').Trim();
            return string.IsNullOrWhiteSpace(normalized) ? "Key" : normalized;
        }
    }
}
