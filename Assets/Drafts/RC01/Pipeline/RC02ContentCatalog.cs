using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reverb.Drafts.RC01.Pipeline
{
    public enum RC02PausePolicy
    {
        Freeze,
        Stop
    }

    [Serializable]
    public class RC02AssetRef
    {
        public string role;
        public string assetPath;
        public UnityEngine.Object asset;
    }

    [Serializable]
    public class RC02Parameter
    {
        public string key;
        public float value;
    }

    [Serializable]
    public class RC02CatalogEntry
    {
        public string id;
        public string kind;
        public string contentId;
        public string eventType;
        public string profileId;
        public RC02AssetRef[] assets = Array.Empty<RC02AssetRef>();
        public RC02Parameter[] parameters = Array.Empty<RC02Parameter>();
        public int maxInstances = 1;
        public float minInterval;
        public RC02PausePolicy pausePolicy;
        public string status;
        public string source;
        public string issues;
        public float Parameter(string key, float fallback)
        {
            foreach (var p in parameters) if (p.key == key) return p.value;
            return fallback;
        }
        public T Asset<T>(string role) where T : UnityEngine.Object
        {
            foreach (var a in assets) if (a.role == role && a.asset is T found) return found;
            return null;
        }
    }

    public class RC02ContentCatalog : ScriptableObject
    {
        public int schemaVersion = 1;
        public string packages;
        public List<RC02CatalogEntry> entries = new List<RC02CatalogEntry>();

        public bool TrySelect(string kind, string eventType, string contentId, out RC02CatalogEntry entry, out string error)
        {
            entry = null;
            error = null;
            RC02CatalogEntry exact = null;
            RC02CatalogEntry wild = null;
            int exactCount = 0;
            int wildCount = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                RC02CatalogEntry item = entries[i];
                if (item == null || item.kind != kind || item.eventType != eventType) continue;
                if (item.contentId == contentId)
                {
                    exact = item;
                    exactCount++;
                }
                else if (item.contentId == "*")
                {
                    wild = item;
                    wildCount++;
                }
            }
            if (exactCount > 1 || (exactCount == 0 && wildCount > 1))
            {
                error = "匹配冲突 " + kind + "/" + eventType + "/" + contentId;
                return false;
            }
            entry = exactCount == 1 ? exact : wild;
            return entry != null;
        }

        public RC02CatalogEntry FindId(string id)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].id == id) return entries[i];
            return null;
        }
    }
}
