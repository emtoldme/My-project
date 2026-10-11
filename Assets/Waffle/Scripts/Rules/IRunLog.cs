namespace Waffle
{
    /// <summary>
    /// 日志出口。纯规则类不依赖 UnityEngine，所以通过这个接口写日志；
    /// Unity 里由 GameLoop 转成 Debug.Log，测试里收集到列表中。
    /// </summary>
    public interface IRunLog
    {
        void Info(string message);
        void Error(string message);
    }

    /// <summary>什么都不做的日志（默认值，避免到处判空）。</summary>
    public sealed class NullRunLog : IRunLog
    {
        public void Info(string message) { }
        public void Error(string message) { }
    }
}
