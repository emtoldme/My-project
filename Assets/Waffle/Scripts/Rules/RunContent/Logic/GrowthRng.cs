namespace Waffle.RunContent
{
    /// <summary>跨 Mono/.NET 一致的确定性 RNG（SplitMix64）。只在出牌时推进；读视图不推进。</summary>
    public sealed class GrowthRng
    {
        private ulong state;
        public GrowthRng(int seed) { state = (ulong)(uint)seed * 0x9E3779B97F4A7C15UL + 0x1234567UL; }
        public ulong NextULong()
        {
            ulong z = (state += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
        /// <summary>[0, max)。max&lt;=0 返回 0。</summary>
        public int Next(int max) => max <= 0 ? 0 : (int)(NextULong() % (ulong)max);
    }
}
