using UnityEditor;
using UnityEngine;

namespace TheWay.EditorTools
{
    /// <summary>
    /// Собирает greybox-префаб Хьюстона из примитивов.
    /// Корень Houston — пустой объект (сюда позже добавим физику и контроллер),
    /// внутри Visual — тело, лапы, голова с носом и хвост.
    /// Точка опоры корня — на земле между лапами, нос смотрит по +Z.
    /// Запуск: меню The Way → Create Houston Prefab.
    /// </summary>
    public static class HoustonPrefabBuilder
    {
        private const string PrefabPath = "Assets/_Game/Prefabs/Houston.prefab";
        private const string BodyMaterialPath = "Assets/_Game/Art/Materials/Houston_Body.mat";
        private const string NoseMaterialPath = "Assets/_Game/Art/Materials/Dog_Nose.mat";

        private static readonly Color BodyColor = new Color(0.78f, 0.45f, 0.2f); // тёплый рыжий
        private static readonly Color NoseColor = new Color(0.08f, 0.07f, 0.07f);

        // Размеры в метрах. Высота в холке 0,5 м, длина от носа до крупа ≈ 0,7 м
        private const float BodyLength = 0.5f;
        private const float BodyThickness = 0.26f;
        private const float WithersHeight = 0.5f;
        private const float BodyCenterY = WithersHeight - BodyThickness / 2f;
        private const float LegThickness = 0.07f;

        [MenuItem("The Way/Create Houston Prefab")]
        public static void CreateHoustonPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
                !EditorUtility.DisplayDialog("Houston", $"Префаб {PrefabPath} уже существует. Пересоздать?", "Пересоздать", "Отмена"))
                return;

            Material bodyMat = EditorMaterialUtility.GetOrCreateLitMaterial(BodyMaterialPath, BodyColor);
            Material noseMat = EditorMaterialUtility.GetOrCreateLitMaterial(NoseMaterialPath, NoseColor);

            var root = new GameObject("Houston");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            Transform v = visual.transform;

            // Тело: капсула Unity стоит вертикально (2 м высотой, 1 м толщиной),
            // поворачиваем её на 90° по X, чтобы она легла вдоль +Z
            CreatePart("Body", PrimitiveType.Capsule, v, bodyMat,
                new Vector3(0f, BodyCenterY, 0f),
                new Vector3(90f, 0f, 0f),
                new Vector3(BodyThickness, BodyLength / 2f, BodyThickness));

            // Лапы: цилиндры от земли до середины тела (цилиндр Unity — 2 м высотой)
            float legX = 0.08f;
            float legZ = 0.17f;
            Vector3 legScale = new Vector3(LegThickness, BodyCenterY / 2f, LegThickness);
            CreatePart("Leg_FL", PrimitiveType.Cylinder, v, bodyMat, new Vector3(-legX, BodyCenterY / 2f, legZ), Vector3.zero, legScale);
            CreatePart("Leg_FR", PrimitiveType.Cylinder, v, bodyMat, new Vector3(legX, BodyCenterY / 2f, legZ), Vector3.zero, legScale);
            CreatePart("Leg_BL", PrimitiveType.Cylinder, v, bodyMat, new Vector3(-legX, BodyCenterY / 2f, -legZ), Vector3.zero, legScale);
            CreatePart("Leg_BR", PrimitiveType.Cylinder, v, bodyMat, new Vector3(legX, BodyCenterY / 2f, -legZ), Vector3.zero, legScale);

            // Голова чуть выше холки и впереди тела; морда и нос — её дочерние объекты
            Transform head = CreatePart("Head", PrimitiveType.Sphere, v, bodyMat,
                new Vector3(0f, 0.56f, 0.27f), Vector3.zero, Vector3.one * 0.2f);
            CreatePartWorld("Snout", PrimitiveType.Cube, head, bodyMat,
                new Vector3(0f, 0.53f, 0.38f), new Vector3(0.09f, 0.08f, 0.12f));
            CreatePartWorld("Nose", PrimitiveType.Sphere, head, noseMat,
                new Vector3(0f, 0.545f, 0.44f), Vector3.one * 0.045f);

            // Хвост: тонкая капсула, задрана вверх-назад под 45°
            CreatePart("Tail", PrimitiveType.Capsule, v, bodyMat,
                new Vector3(0f, 0.49f, -0.31f),
                new Vector3(-45f, 0f, 0f),
                new Vector3(0.04f, 0.11f, 0.04f));

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log($"[Houston] Префаб создан: {PrefabPath}");
        }

        /// <summary>
        /// Создаёт часть визуала. Коллайдер примитива удаляем: физика будет одна, на корне.
        /// </summary>
        private static Transform CreatePart(string name, PrimitiveType type, Transform parent, Material material,
            Vector3 localPosition, Vector3 localEuler, Vector3 localScale)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<MeshRenderer>().sharedMaterial = material;

            Transform t = part.transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            t.localRotation = Quaternion.Euler(localEuler);
            t.localScale = localScale;
            return t;
        }

        /// <summary>
        /// Как CreatePart, но позиция и размер задаются в координатах собаки,
        /// а не родителя — удобно для деталей головы, которая сама отмасштабирована.
        /// </summary>
        private static void CreatePartWorld(string name, PrimitiveType type, Transform parent, Material material,
            Vector3 position, Vector3 size)
        {
            Transform t = CreatePart(name, type, parent.root, material, position, Vector3.zero, size);
            t.SetParent(parent, true);
        }
    }
}
