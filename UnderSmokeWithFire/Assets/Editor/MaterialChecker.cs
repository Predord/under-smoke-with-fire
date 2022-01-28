using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Utils.EditorExtension
{
    public class MaterialChecker : Editor
    {
        [MenuItem("Assets/Check Material")]
        private static void CheckMaterial()
        {
            Material matToCheck = Selection.activeObject as Material;

            foreach (var renderer in FindObjectsOfType<MeshRenderer>(true))
            {
                if (renderer.sharedMaterials.Contains(matToCheck))
                    Debug.Log("Material used by " + renderer.transform.name, renderer.gameObject);
            }
        }

        [MenuItem("Assets/Check Material", true)]
        private static bool CheckMaterialValidation()
        {
            return Selection.activeObject is Material;
        }
    }
}
