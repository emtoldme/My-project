using UnityEngine;

namespace Waffle
{
    /// <summary>
    /// IMGUI 统一缩放：按 1280x720 参考分辨率等比缩放，保证小 Game 视图或高 DPI 下面板不被截断。
    /// 在 OnGUI 开头调用 Apply()，之后用 Width/Height 代替 Screen.width/height 布局。
    /// </summary>
    public static class ImguiScale
    {
        public const float RefWidth = 1280f;
        public const float RefHeight = 720f;

        public static float Scale { get; private set; } = 1f;
        public static float Width => Screen.width / Scale;
        public static float Height => Screen.height / Scale;

        public static void Apply()
        {
            float s = Mathf.Min(Screen.width / RefWidth, Screen.height / RefHeight);
            Scale = Mathf.Clamp(s, 0.5f, 3f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1f));
        }

        /// <summary>把矩形限制在可见区域内（逻辑坐标）。</summary>
        public static Rect Fit(Rect r)
        {
            r.width = Mathf.Min(r.width, Width - 8f);
            r.height = Mathf.Min(r.height, Height - 8f);
            r.x = Mathf.Clamp(r.x, 4f, Mathf.Max(4f, Width - r.width - 4f));
            r.y = Mathf.Clamp(r.y, 4f, Mathf.Max(4f, Height - r.height - 4f));
            return r;
        }
    }
}
