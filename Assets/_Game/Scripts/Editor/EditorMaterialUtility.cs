using UnityEditor;
using UnityEngine;

namespace TheWay.EditorTools
{
    /// <summary>
    /// Общие помощники редакторских сборщиков: материалы и папки.
    /// </summary>
    public static class EditorMaterialUtility
    {
        /// <summary>
        /// Возвращает URP Lit-материал по пути; если его нет — создаёт с заданным цветом.
        /// Существующий материал не перезаписывается, чтобы не терять ручные правки.
        /// </summary>
        public static Material GetOrCreateLitMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>
        /// Создаёт папку (и недостающих родителей) внутри Assets через AssetDatabase, чтобы появились .meta.
        /// </summary>
        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
