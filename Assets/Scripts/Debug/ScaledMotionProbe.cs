using UnityEngine;
using UnityEngine.SceneManagement;

namespace Survivor
{
    /// <summary>
    /// 只用于 Task 1 验收。方块使用缩放时间，暂停后必须停住。
    /// 不要把这段测试逻辑并进 GameManager。
    /// </summary>
    public sealed class ScaledMotionProbe : MonoBehaviour
    {
        [SerializeField] float degreesPerSecond = 120f;
        [SerializeField] bool drawOverlay = true;

        GameManager subscribed;
        float unscaledSeconds;
        GUIStyle overlayStyle;

        void OnEnable()
        {
            TrySubscribe();
        }

        void Start()
        {
            TrySubscribe();
            if (subscribed == null)
                Debug.LogError("[ScaledMotionProbe] 场景中没有可用的 GameManager。", this);
        }

        void OnDisable()
        {
            if (subscribed != null)
            {
                subscribed.StateChanged -= OnStateChanged;
                subscribed = null;
            }
        }

        void Update()
        {
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
            unscaledSeconds += Time.unscaledDeltaTime;
        }

        void OnGUI()
        {
            if (!drawOverlay)
                return;

            overlayStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            overlayStyle.normal.textColor = Color.white;

            var state = GameManager.Instance != null ? GameManager.Instance.State.ToString() : "无";
            GUI.Label(
                new Rect(24f, 24f, 900f, 80f),
                $"State {state}    timeScale {Time.timeScale:0.##}    unscaled {unscaledSeconds:0.0}s",
                overlayStyle);
        }

        [ContextMenu("调试/重载当前场景")]
        public void ReloadActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.name) || !scene.isLoaded)
            {
                Debug.LogError("[ScaledMotionProbe] 当前场景还没有保存，不能重载。", this);
                return;
            }

            SceneManager.LoadScene(scene.name);
        }

        void TrySubscribe()
        {
            var manager = GameManager.Instance;
            if (manager == null || subscribed == manager)
                return;

            if (subscribed != null)
                subscribed.StateChanged -= OnStateChanged;

            subscribed = manager;
            subscribed.StateChanged += OnStateChanged;
        }

        void OnStateChanged(GameState previous, GameState next)
        {
            Debug.Log($"[ScaledMotionProbe] 收到状态事件 {previous} -> {next}。", this);
        }
    }
}
