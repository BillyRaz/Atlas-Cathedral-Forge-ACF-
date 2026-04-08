using System;
using System.Collections.Generic;
using UnityEngine;

namespace ACFSystem
{
    public static class ACFCategoryUtility
    {
        public const string Floor = "Floor";
        public const string Wall = "Wall";
        public const string Roof = "Roof";
        public const string Prop = "Prop";
        public const string MovableProp = "MovableProp";
        public const string Door = "Door";
        public const string Key = "Key";
        public const string Landmark = "Landmark";
        public const string Ignore = "Ignore";
        public const string Blockout = "Blockout";

        public static readonly string[] StructuralCategories = { Floor, Wall, Roof };
        public static readonly string[] ObjectCategories = { Prop, MovableProp, Door, Key };
        public static readonly string[] SpecialCategories = { Landmark, Ignore };
        public static readonly string[] AllCategories = { Floor, Wall, Roof, Prop, MovableProp, Door, Key, Landmark, Ignore };
        public static readonly string[] WallKeywords = { "wall", "column", "pillar", "post", "beam", "railing", "fence", "barrier", "divider", "screen", "partition" };
        public static readonly string[] FloorKeywords = { "floor", "ground", "terrain", "base", "platform", "stage", "tile", "pavement" };
        public static readonly string[] RoofKeywords = { "roof", "ceiling", "canopy", "overhang", "dome", "arch" };
        public static readonly string[] PropKeywords = { "prop", "object", "item", "decoration", "furniture", "chair", "table", "crate", "barrel", "plant", "tree", "rock", "stone" };
        public static readonly string[] MovableKeywords = { "movable", "move", "push", "pull", "slide", "rolling", "wheel", "cart" };
        public static readonly string[] DoorKeywords = { "door", "gate", "entrance", "exit", "portal", "opening" };
        public static readonly string[] KeyKeywords = { "key", "keycard", "token", "artifact", "crystal", "gem", "orb", "collectible" };
        public static readonly string[] LandmarkKeywords = { "landmark", "statue", "monument", "tower", "obelisk", "fountain", "shrine", "altar", "waypoint", "spawn" };
        public static readonly string[] IgnoreKeywords = { "ignore", "debug", "editoronly", "temp", "temporary", "helper" };

        private static readonly Dictionary<string, Color> CategoryColors = new Dictionary<string, Color>(StringComparer.Ordinal)
        {
            { Floor, new Color(0.5f, 0.5f, 0.5f) },
            { Wall, new Color(0.6f, 0.4f, 0.3f) },
            { Roof, new Color(0.4f, 0.3f, 0.2f) },
            { Prop, new Color(0.3f, 0.5f, 0.3f) },
            { MovableProp, new Color(0.2f, 0.6f, 0.2f) },
            { Door, new Color(0.5f, 0.3f, 0.2f) },
            { Key, new Color(0.8f, 0.7f, 0.2f) },
            { Landmark, new Color(0.2f, 0.3f, 0.8f) },
            { Ignore, new Color(0.3f, 0.3f, 0.3f) }
        };

        public static bool IsStandardCategory(string category)
        {
            return Array.IndexOf(AllCategories, category) >= 0;
        }

        public static Color GetCategoryColor(string category)
        {
            if (!string.IsNullOrEmpty(category) && CategoryColors.TryGetValue(category, out Color color))
            {
                return color;
            }

            return Color.white;
        }

        public static string GetCategoryName(ACFObjectData.ObjectCategory category, string customCategory = "")
        {
            switch (category)
            {
                case ACFObjectData.ObjectCategory.Floor: return Floor;
                case ACFObjectData.ObjectCategory.Wall: return Wall;
                case ACFObjectData.ObjectCategory.Roof: return Roof;
                case ACFObjectData.ObjectCategory.Prop: return Prop;
                case ACFObjectData.ObjectCategory.MovableProp: return MovableProp;
                case ACFObjectData.ObjectCategory.Door: return Door;
                case ACFObjectData.ObjectCategory.Key: return Key;
                case ACFObjectData.ObjectCategory.Landmark: return Landmark;
                case ACFObjectData.ObjectCategory.Ignore: return Ignore;
                case ACFObjectData.ObjectCategory.Custom: return customCategory;
                default: return string.Empty;
            }
        }

        public static bool TryInferCategory(GameObject gameObject, out string category)
        {
            category = string.Empty;

            if (gameObject == null)
            {
                return false;
            }

            if (IsStandardCategory(gameObject.tag))
            {
                category = gameObject.tag;
                return true;
            }

            if (TryInferCategoryFromName(gameObject.name, out category))
            {
                return true;
            }

            ACFObjectData objectData = gameObject.GetComponent<ACFObjectData>();
            if (objectData != null)
            {
                string objectDataCategory = GetCategoryName(objectData.category, objectData.customCategory);
                if (IsStandardCategory(objectDataCategory))
                {
                    category = objectDataCategory;
                    return true;
                }
            }

            if (gameObject.GetComponent<Renderer>() != null) category = Prop;

            return !string.IsNullOrEmpty(category);
        }

        public static bool TryInferCategoryFromName(string objectName, out string category)
        {
            string name = objectName.ToLowerInvariant();

            if (ContainsAny(name, WallKeywords)) category = Wall;
            else if (ContainsAny(name, FloorKeywords)) category = Floor;
            else if (ContainsAny(name, RoofKeywords)) category = Roof;
            else if (ContainsAny(name, MovableKeywords)) category = MovableProp;
            else if (ContainsAny(name, DoorKeywords)) category = Door;
            else if (ContainsAny(name, KeyKeywords)) category = Key;
            else if (ContainsAny(name, LandmarkKeywords)) category = Landmark;
            else if (ContainsAny(name, IgnoreKeywords)) category = Ignore;
            else if (ContainsAny(name, PropKeywords)) category = Prop;
            else category = string.Empty;

            return !string.IsNullOrEmpty(category);
        }

        public static bool ContainsAny(string source, string[] keywords)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (source.Contains(keywords[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
