using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VirtualPhenix.PokemonSnap3DS
{
    public class PS3DS_TextureSwapper : MonoBehaviour
    {
        [SerializeField] private short m_currentSet = -1;

        [System.Serializable]
        public class TextureSwapEntry
        {
            public Renderer Renderer;
            public bool ToAllMaterials;
            public int[] MaterialIndices;

            // Legacy texture replacement support.
            public Texture Texture;

            // Optional controls. Old configured entries with Texture assigned still work
            // even if UseTexture was left false, because Texture != null is enough.
            public bool UseTexture = true;

            // UV offset / tiling support.
            public bool UseUV;
            public Vector2 Offset;
            public Vector2 Scale = Vector2.one;
        }

        [System.Serializable]
        public class TextureSwapSet
        {
            public TextureSwapEntry[] Entries;
        }

        [SerializeField] private TextureSwapSet[] m_sets;
        private Dictionary<Renderer, Material[]> m_runtimeMaterialCache = new Dictionary<Renderer, Material[]>();

        private void Awake()
        {
            InitRuntimeCache();
        }

        private void OnDestroy()
        {
            if (m_runtimeMaterialCache != null)
                m_runtimeMaterialCache.Clear();
        }

        private void InitRuntimeCache()
        {
            m_runtimeMaterialCache = new Dictionary<Renderer, Material[]>();

            if (!Application.isPlaying || m_sets == null)
                return;

            for (int s = 0; s < m_sets.Length; s++)
            {
                TextureSwapSet set = m_sets[s];
                if (set == null || set.Entries == null)
                    continue;

                for (int e = 0; e < set.Entries.Length; e++)
                {
                    TextureSwapEntry entry = set.Entries[e];

                    if (entry == null || entry.Renderer == null)
                        continue;

                    if (!m_runtimeMaterialCache.ContainsKey(entry.Renderer))
                        m_runtimeMaterialCache.Add(entry.Renderer, entry.Renderer.materials);
                }
            }
        }

        public void TestEntryChange(int index)
        {
            if (m_currentSet == index) return;

            SwapSet(index);
        }

        public void SwapEntry(int idx)
        {
            if (m_currentSet == idx) return;

            SwapSet(idx);
        }

        public void SwapEntry(float idx)
        {
            if (m_currentSet == idx) return;

            SwapSet((int)idx);
        }

        public void SwapSet(float idx)
        {
            if (m_currentSet == idx) return;

            SwapSet((int)idx);
        }


        public void SwapSet(int idx)
        {
            if (idx < 0 || m_sets == null || idx >= m_sets.Length)
                return;

            m_currentSet = (short)idx;

            TextureSwapSet set = m_sets[idx];

            if (set == null || set.Entries == null)
                return;

            for (int i = 0; i < set.Entries.Length; i++)
                ApplyEntry(set.Entries[i]);
        }

        private Material[] GetMaterials(Renderer renderer)
        {
            if (renderer == null)
                return null;

            if (!Application.isPlaying)
                return renderer.sharedMaterials;

            Material[] materials;
            if (!m_runtimeMaterialCache.TryGetValue(renderer, out materials) || materials == null)
            {
                materials = renderer.materials;
                m_runtimeMaterialCache[renderer] = materials;
            }

            return materials;
        }

        private void ApplyEntry(TextureSwapEntry entry)
        {
            if (entry == null || entry.Renderer == null)
                return;

            Material[] materials = GetMaterials(entry.Renderer);

            if (materials == null || materials.Length == 0)
                return;

            if (entry.ToAllMaterials)
            {
                for (int i = 0; i < materials.Length; i++)
                    ApplyToMaterial(materials[i], entry);

                return;
            }

            if (entry.MaterialIndices == null)
                return;

            for (int i = 0; i < entry.MaterialIndices.Length; i++)
            {
                int materialIndex = entry.MaterialIndices[i];

                if (materialIndex < 0 || materialIndex >= materials.Length)
                    continue;

                ApplyToMaterial(materials[materialIndex], entry);
            }
        }

        private void ApplyToMaterial(Material material, TextureSwapEntry entry)
        {
            if (material == null || entry == null)
                return;

            if ((entry.UseTexture || entry.Texture != null) && entry.Texture != null)
                material.SetTexture("_MainTex", entry.Texture);
            
            if (entry.UseUV)
            {
                material.SetTextureOffset("_MainTex", entry.Offset);
                material.SetTextureScale("_MainTex", entry.Scale);
            }
        }
    }
}
