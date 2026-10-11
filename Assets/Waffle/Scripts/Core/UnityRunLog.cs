using UnityEngine;

namespace Waffle
{
    /// <summary>把纯规则类的日志转到 Unity Console / Player.log。</summary>
    public sealed class UnityRunLog : IRunLog
    {
        public void Info(string message) { Debug.Log(message); }
        public void Error(string message) { Debug.LogError(message); }
    }
}
