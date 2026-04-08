using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ACFSystem
{
    public static class ACFTagSetup
    {
        private static readonly string[] RequiredTags =
        {
            ACFCategoryUtility.Floor,
            ACFCategoryUtility.Wall,
            ACFCategoryUtility.Roof,
            ACFCategoryUtility.Prop,
            ACFCategoryUtility.MovableProp,
            ACFCategoryUtility.Door,
            ACFCategoryUtility.Key,
            ACFCategoryUtility.Landmark,
            ACFCategoryUtility.Ignore,
            ACFCategoryUtility.Blockout
        };

        [MenuItem("Tools/Atlas-Cathedral-Forge/Setup Tags")]
        public static void SetupTags()
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            List<string> existingTags = new List<string>();
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                existingTags.Add(tagsProp.GetArrayElementAtIndex(i).stringValue);
            }

            bool changes = false;
            foreach (string tag in RequiredTags)
            {
                if (existingTags.Contains(tag))
                {
                    continue;
                }

                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                newTag.stringValue = tag;
                existingTags.Add(tag);
                changes = true;
                Debug.Log($"Added ACF tag: {tag}");
            }

            if (changes)
            {
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log("ACF tags verified and updated.");
            }
            else
            {
                Debug.Log("ACF tags already configured.");
            }
        }
    }
}
