using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheWay.EditorTools
{
    /// <summary>
    /// Собирает greybox-префабы препятствий и расставляет их в сцене Sandbox.
    /// У каждого префаба корень — пустой объект с точкой опоры на земле и масштабом 1,
    /// а примитивы с коллайдерами — внутри. Так экземпляры удобно двигать и поворачивать.
    /// Запуск: меню The Way → Greybox → ...
    /// </summary>
    public static class GreyboxObstaclesBuilder
    {
        private const string PrefabsFolder = "Assets/_Game/Prefabs/Greybox";
        private const string MaterialsFolder = "Assets/_Game/Art/Materials";
        private const string SandboxScenePath = "Assets/_Game/Scenes/Sandbox.unity";
        private const string ObstaclesRootName = "Obstacles";

        // Ориентир по высотам: собака прыгает примерно на 0,7 м (уточним при настройке прыжка).
        // Бревно ниже этого, уступ — выше, а ящик + прыжок = как раз высота уступа.
        private const float LogDiameter = 0.3f;
        private const float LogLength = 3f;
        private const float LedgeHeight = 1.2f;
        private const float BoxSize = 0.6f;
        private const float RampLength = 4.5f; // по горизонтали, уклон ≈ 15°
        private const float RampWidth = 2f;
        private const float RampThickness = 0.2f;
        private const float WallHeight = 3f;

        private static Material _grey;
        private static Material _wood;
        private static Material _interactive;

        [MenuItem("The Way/Greybox/1. Create Obstacle Prefabs")]
        public static void CreateObstaclePrefabs()
        {
            EditorMaterialUtility.EnsureFolder(PrefabsFolder);
            _grey = EditorMaterialUtility.GetOrCreateLitMaterial(MaterialsFolder + "/Greybox_Grey.mat", new Color(0.62f, 0.62f, 0.64f));
            _wood = EditorMaterialUtility.GetOrCreateLitMaterial(MaterialsFolder + "/Greybox_Wood.mat", new Color(0.45f, 0.32f, 0.2f));
            // Всё, с чем можно взаимодействовать, красим одним заметным цветом
            _interactive = EditorMaterialUtility.GetOrCreateLitMaterial(MaterialsFolder + "/Greybox_Interactive.mat", new Color(0.25f, 0.55f, 0.85f));

            SavePrefab(BuildLowLog());
            SavePrefab(BuildHighLedge());
            SavePrefab(BuildBox());
            SavePrefab(BuildRamp());
            SavePrefab(BuildWall());

            AssetDatabase.SaveAssets();
            Debug.Log($"[Greybox] Префабы препятствий созданы в {PrefabsFolder}");
        }

        [MenuItem("The Way/Greybox/2. Place Obstacles In Sandbox")]
        public static void PlaceObstaclesInSandbox()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);

            // Повторный запуск пересоздаёт расстановку, а не дублирует её
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == ObstaclesRootName)
                    Object.DestroyImmediate(go);
            }

            var root = new GameObject(ObstaclesRootName);

            // Собака стартует в центре (0, 0, 0) и смотрит на +Z — препятствия впереди
            Place(root, "LowLog", new Vector3(-6f, 0f, 5f), 0f);
            Place(root, "HighLedge", new Vector3(0f, 0f, 8f), 0f);
            Place(root, "Box", new Vector3(2.5f, 0f, 4f), 0f);
            Place(root, "Ramp", new Vector3(7f, 0f, 3f), 0f);
            Place(root, "Wall", new Vector3(0f, 0f, 13f), 0f);
            Place(root, "Wall", new Vector3(-13f, 0f, 6f), 90f);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Greybox] Препятствия расставлены в Sandbox");
        }

        // --- Препятствия ---

        private static GameObject BuildLowLog()
        {
            GameObject root = new GameObject("LowLog");
            // Цилиндр Unity стоит вертикально (2 м), кладём его на бок вдоль оси X
            AddPart(root, PrimitiveType.Cylinder, _wood,
                new Vector3(0f, LogDiameter / 2f, 0f),
                Quaternion.Euler(0f, 0f, 90f),
                new Vector3(LogDiameter, LogLength / 2f, LogDiameter));
            return root;
        }

        private static GameObject BuildHighLedge()
        {
            GameObject root = new GameObject("HighLedge");
            AddPart(root, PrimitiveType.Cube, _grey,
                new Vector3(0f, LedgeHeight / 2f, 0f), Quaternion.identity,
                new Vector3(4f, LedgeHeight, 3f));
            return root;
        }

        private static GameObject BuildBox()
        {
            GameObject root = new GameObject("Box");
            // Ящик не статичный: его будет двигать Хьюстон (физику добавим в задаче головоломки)
            AddPart(root, PrimitiveType.Cube, _interactive,
                new Vector3(0f, BoxSize / 2f, 0f), Quaternion.identity,
                Vector3.one * BoxSize, isStatic: false);
            return root;
        }

        private static GameObject BuildRamp()
        {
            GameObject root = new GameObject("Ramp");

            // Наклонная плита: начинается в точке опоры на земле и поднимается вдоль +Z до высоты уступа
            float angle = Mathf.Atan2(LedgeHeight, RampLength);
            float slopeLength = Mathf.Sqrt(RampLength * RampLength + LedgeHeight * LedgeHeight);
            // Центр плиты = середина верхней поверхности, сдвинутая вниз по нормали на полтолщины
            Vector3 topMiddle = new Vector3(0f, LedgeHeight / 2f, RampLength / 2f);
            Vector3 normal = new Vector3(0f, Mathf.Cos(angle), -Mathf.Sin(angle));
            AddPart(root, PrimitiveType.Cube, _grey,
                topMiddle - normal * (RampThickness / 2f),
                Quaternion.Euler(-angle * Mathf.Rad2Deg, 0f, 0f),
                new Vector3(RampWidth, RampThickness, slopeLength));

            // Площадка наверху, чтобы с пандуса было куда зайти
            AddPart(root, PrimitiveType.Cube, _grey,
                new Vector3(0f, LedgeHeight / 2f, RampLength + 1f), Quaternion.identity,
                new Vector3(RampWidth, LedgeHeight, 2f));
            return root;
        }

        private static GameObject BuildWall()
        {
            GameObject root = new GameObject("Wall");
            AddPart(root, PrimitiveType.Cube, _grey,
                new Vector3(0f, WallHeight / 2f, 0f), Quaternion.identity,
                new Vector3(10f, WallHeight, 0.3f));
            return root;
        }

        // --- Помощники ---

        private static void AddPart(GameObject root, PrimitiveType type, Material material,
            Vector3 localPosition, Quaternion localRotation, Vector3 localScale, bool isStatic = true)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = "Mesh";
            part.GetComponent<MeshRenderer>().sharedMaterial = material;

            Transform t = part.transform;
            t.SetParent(root.transform, false);
            t.localPosition = localPosition;
            t.localRotation = localRotation;
            t.localScale = localScale;

            // Статичные объекты Unity может запекать в освещение и навигацию
            part.isStatic = isStatic;
            root.isStatic = isStatic;
        }

        private static void SavePrefab(GameObject root)
        {
            // Перезапись по тому же пути сохраняет GUID — ссылки в сценах не ломаются
            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsFolder}/{root.name}.prefab");
            Object.DestroyImmediate(root);
        }

        private static void Place(GameObject parent, string prefabName, Vector3 position, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/{prefabName}.prefab");
            if (prefab == null)
            {
                Debug.LogError($"[Greybox] Нет префаба {prefabName} — сначала запусти «1. Create Obstacle Prefabs»");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        }
    }
}
