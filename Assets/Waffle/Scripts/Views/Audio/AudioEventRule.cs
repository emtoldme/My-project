using System;
using UnityEngine;

namespace Waffle
{
    /// <summary>
    /// 一个音效事件的播放规则（Docs/Audio/S1_Audio_Event_Table.md 第二节）。
    /// </summary>
    [Serializable]
    public class AudioEventRule
    {
        public string name;
        public AudioClip[] clips;
        [Tooltip("推荐音量（dB），在 sfxTrimDb 之后叠加")] public float volumeDb;
        [Tooltip("音高随机 ±")] public float pitchRandom;
        [Tooltip("最大并发；满了就挤掉最早开始的那个")] public int maxVoices = 1;
        [Tooltip("最短触发间隔（s，unscaled）")] public float minInterval;
        [Tooltip("0 = 2D，1 = 3D")] [Range(0f, 1f)] public float spatialBlend;
        [Tooltip("3D 最大距离（m）")] public float maxDistance = 25f;
        [Tooltip("true：按顺序轮换（受击）；false：随机抽取")] public bool sequential;

        public AudioEventRule() { }

        public AudioEventRule(string name, float db, float pitch, int voices, float interval, float spatial, float maxDist, bool sequential = false)
        {
            this.name = name;
            volumeDb = db;
            pitchRandom = pitch;
            maxVoices = voices;
            minInterval = interval;
            spatialBlend = spatial;
            maxDistance = maxDist;
            this.sequential = sequential;
        }
    }
}
