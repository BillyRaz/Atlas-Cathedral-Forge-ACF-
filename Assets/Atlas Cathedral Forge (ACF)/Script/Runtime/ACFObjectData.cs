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
            Landmark,
            Ignore,
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

        [Header("Transform Presets")]
        public bool snapToGrid = false;
        public float gridSize = 1f;

        [Header("Diagnostic Info")]
        [TextArea(3, 5)]
        public string notes = "";

        private void Awake()
        {
            if (isBlockout && finalPrefab != null)
            {
                // Flag for replacement in final stage
                gameObject.tag = ACFCategoryUtility.Blockout;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw category color indicator
            Gizmos.color = GetCategoryColor();
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);

            // Draw pivot point
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.1f);
        }

        private void OnDrawGizmos()
        {
            if (isBlockout)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Gizmos.DrawCube(transform.position, transform.lossyScale);
            }
        }

        private Color GetCategoryColor()
        {
            return ACFCategoryUtility.GetCategoryColor(ACFCategoryUtility.GetCategoryName(category, customCategory));
        }

        public void SnapToGrid()
        {
            if (!snapToGrid || gridSize <= 0f)
            {
                return;
            }

            Vector3 pos = transform.position;
            pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
            pos.y = Mathf.Round(pos.y / gridSize) * gridSize;
            pos.z = Mathf.Round(pos.z / gridSize) * gridSize;
            transform.position = pos;
        }
    }
}
