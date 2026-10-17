using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheWay.EditorTools
{
    /// <summary>
    /// Собирает тестовую сцену Sandbox: пол 40×40 м, направленный свет и камера сверху-сбоку.
    /// Запуск: меню The Way → Create Sandbox Scene.
    /// </summary>
    public static class SandboxSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Sandbox.unity";

        // Стандартный Plane в Unity — 10×10 м, поэтому масштаб 4 даёт 40×40 м
        private const float FloorSize = 40f;
        private const float UnityPlaneSize = 10f;

        [MenuItem("The Way/Create Sandbox Scene")]
        public static void CreateSandboxScene()
        {
            // Не теряем несохранённые изменения в открытой сцене
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Sandbox", $"Сцена {ScenePath} уже существует. Пересоздать?", "Пересоздать", "Отмена"))
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateFloor();
            CreateSun();
            CreateCamera();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Sandbox] Сцена создана: {ScenePath}");
        }

        private static void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            float scale = FloorSize / UnityPlaneSize;
            floor.transform.localScale = new Vector3(scale, 1f, scale);
            floor.isStatic = true;
        }

        private static void CreateSun()
        {
            var go = new GameObject("Directional Light");
            // Свет падает под углом — так у объектов видны объём и тени
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.Soft;

            RenderSettings.sun = light;
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            // Сверху-сбоку: 20 м над полом и 25 м назад от центра
            go.transform.position = new Vector3(0f, 20f, -25f);
            go.transform.LookAt(Vector3.zero);

            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;

            go.AddComponent<AudioListener>();
        }
    }
}
