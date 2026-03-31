using UnityEngine;

namespace ACFSystem
{
    [AddComponentMenu("ACF/ACF Object Data")]
    public class ACFObjectData : MonoBehaviour
    {
        public enum ObjectCategory
        {
            Floor,
            Wall,
            Prop,
            MovableProp,
            Key,
            Door,
            Roof,
            Custom
        }

        [Header("ACF Categorization")]
        public ObjectCategory category = ObjectCategory.Prop;
        public string customCategory = "";

        [Header("Blockout Settings")]
        public PrimitiveType blockoutType = PrimitiveType.Cube;
        public bool isBlockout = false;

        [Header("Replacement Settings")]
        public GameObject finalPrefab;
        public bool preserveTransform = true;

        [Header("Diagnostic Info")]
        [TextArea(3, 5)]
        public string notes = "";

        private void Awake()
        {
            if (isBlockout && finalPrefab != null)
            {
                // Flag for replacement in final stage
                gameObject.tag = "Blockout";
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw category color indicator
            Gizmos.color = GetCategoryColor();
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }

        private Color GetCategoryColor()
        {
            switch (category)
            {
                case ObjectCategory.Floor: return Color.green;
                case ObjectCategory.Wall: return Color.blue;
                case ObjectCategory.Prop: return Color.yellow;
                case ObjectCategory.MovableProp: return Color.cyan;
                case ObjectCategory.Key: return Color.magenta;
                case ObjectCategory.Door: return Color.red;
                case ObjectCategory.Roof: return Color.gray;
                default: return Color.white;
            }
        }
    }
}