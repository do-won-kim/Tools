
#if UNITY_EDITOR

namespace TA.Editor
{
    using UnityEditor;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using System.Collections.Generic;
    using UnityEngine.Rendering;

    [HideLabel]
    [System.Serializable]
    public class MaterialCopyOdinVer
    {
        Shader m_shader;

        [InfoBox("기준 머테리얼")]
        public Material m_materialBase;

        [InfoBox("수정할 머테리얼 리스트")]
        public List<Material> m_listMaterial = new List<Material>();

        [Button(ButtonSizes.Large)]
        public void GetAllMaterial()
        {
            if (m_materialBase == null)
            {
                return;
            }

            m_shader = m_materialBase.shader;

            m_listMaterial.Clear();

            string filterPath = "Assets/";

            string[] strMaterial = AssetDatabase.FindAssets("t:Material", null);

            string tempPath;

            float flPer = 0;
            for (int i = 0; i < strMaterial.Length; i++)
            {
                flPer = i / (float)strMaterial.Length;

                EditorUtility.DisplayProgressBar("Search Materials...", $"({i} / {strMaterial.Length})", flPer);

                tempPath = AssetDatabase.GUIDToAssetPath(strMaterial[i]);

                if (!tempPath.Contains(filterPath))
                    continue;

                Material material = AssetDatabase.LoadAssetAtPath(tempPath, typeof(Material)) as Material;

                if (material != null && m_materialBase == material)
                {
                    continue;
                }

                if (material.shader == m_shader)
                {
                    m_listMaterial.Add(material);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        [Button(ButtonSizes.Large)]
        public void ClearMaterial()
        {
            m_listMaterial.Clear();
        }

        [Button(ButtonSizes.Large)]
        public void ImportProperty()
        {
            if (m_materialBase == null)
            {
                return;
            }

            m_shader = m_materialBase.shader;

            int nCountProperty = m_shader.GetPropertyCount();
            float flPer = 0;
            for (int i = 0; i < m_listMaterial.Count; ++i)
            {
                flPer = i / m_listMaterial.Count;

                EditorUtility.DisplayProgressBar("ImportProperty...", $"({i} / {m_listMaterial.Count})", flPer);

                if (m_listMaterial[i].shader != m_shader)
                {
                    Debug.LogError("main shader : " + m_shader.name + " but " + m_listMaterial[i].name + " shader : " + m_listMaterial[i].shader);
                    continue;
                }

                for (int j = 0; j < nCountProperty; ++j)
                {
                    int id = m_shader.GetPropertyNameId(j);
                    var type = m_listMaterial[i].shader.GetPropertyType(j);

                    switch (type)
                    {
                        case ShaderPropertyType.Texture:
                            break;
                        case ShaderPropertyType.Color:
                            m_listMaterial[i].SetColor(id, m_materialBase.GetColor(id));
                            break;
                        case ShaderPropertyType.Float:
                        case ShaderPropertyType.Range:
                            m_listMaterial[i].SetFloat(id, m_materialBase.GetFloat(id));
                            break;
                        case ShaderPropertyType.Vector:
                            m_listMaterial[i].SetVector(id, m_materialBase.GetVector(id));
                            break;
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            AssetDatabase.SaveAssets();
        }
    }
}
#endif
