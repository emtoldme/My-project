using System;
using Waffle.Combat;
using Waffle.GameplayNext;

namespace Waffle.Finale
{
    /// <summary>
    /// AI-C：终局首领数值与时序配置。
    /// </summary>
    [Serializable]
    public sealed class FinaleConfig
    {
        // 首领基础属性
        public string BossArchetypeId = "boss_harvester";
        public float BossMaxHp = 800f;
        public float BossSpeed = 2.4f;
        public float BossRadius = 1.6f;
        public int BossContactDamage = 18;
        public int BossGemValue = 50;

        // 生成与出生时序 (9:00 开始发出预警，3秒后生成)
        public float SpawnWarningStartTime = 540f; // 9:00
        public float SpawnWarningDuration = 3.0f;
        public float FirstAttackDelay = 2.0f;

        // 招式 1：三连落波 (Triple Falling Pulse)
        public float PulseRadius = 2.0f;
        public int PulseDamage = 14;
        public float PulseWarning = 1.0f;
        public float PulseWarningEnraged = 0.8f;
        public float PulseActiveDuration = 0.3f;
        public float PulseSubInterval = 0.7f;
        public float PulseSubIntervalEnraged = 0.5f;
        public float WaveCooldown = 3.5f;
        public float WaveCooldownEnraged = 2.4f;

        // 招式 2：扩散声环 (Expanding Sonic Ring)
        public float RingRadius = 2.5f;
        public float RingInnerRadius = 0.5f;
        public float RingExpansionSpeed = 6.0f;
        public int RingDamage = 16;
        public float RingWarning = 1.4f; // 提前给出至少约 1.2 秒预警
        public float RingWarningEnraged = 1.2f;
        public float RingActiveDuration = 2.0f;
        public float RingCooldown = 4.0f;
        public float RingCooldownEnraged = 2.8f;
    }
}
