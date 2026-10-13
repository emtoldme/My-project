using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Reverb.Drafts.RC01.Presentation;

namespace Reverb.Drafts.RC01.Pipeline.EditorTools
{
    public static class GardenWeatherTools
    {
        [MenuItem("Reverb/天气/选择天气控制器")]
        public static void Select()
        {
            var weather = Object.FindAnyObjectByType<GardenWeather>();
            if (weather == null) { Debug.LogWarning("请先打开主场景。"); return; }
            Selection.activeGameObject = weather.gameObject;
            EditorGUIUtility.PingObject(weather);
        }
        [MenuItem("Reverb/天气/晴天")]
        static void Clear() => Preset(GardenWeatherKind.Clear);
        [MenuItem("Reverb/天气/雨天")]
        static void Rain() => Preset(GardenWeatherKind.Rain);
        [MenuItem("Reverb/天气/雷雨")]
        static void Storm() => Preset(GardenWeatherKind.Storm);
        static void Preset(GardenWeatherKind kind)
        {
            var weather = Object.FindAnyObjectByType<GardenWeather>();
            if (weather == null) { Debug.LogWarning("请先打开主场景。"); return; }
            Undo.RecordObject(weather, "Change garden weather");
            weather.weather = kind;
            EditorUtility.SetDirty(weather);
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(weather.gameObject.scene);
            Selection.activeGameObject = weather.gameObject;
        }
    }
}
