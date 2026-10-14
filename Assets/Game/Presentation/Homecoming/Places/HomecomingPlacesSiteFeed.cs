using System.Collections.Generic;
using UnityEngine;
using Waffle.Combat;
using Waffle.Homecoming;
using Waffle.Homecoming.Places;

namespace Reverb.Homecoming.Places
{
    /// <summary>
    /// 布局适配样例：宿主在地图Seed就绪后，用玩家位置取附近有限站点并注册到PoiModule。
    /// 世界种子必须来自ProceduralFieldStreamer.Seed；本类不引用streamer以免改其程序集依赖。
    /// </summary>
    public static class HomecomingPlacesSiteFeed
    {
        public const float FeedRadius = 120f;
        public static int RegisterNear(IPoiModule module, int worldSeed, Vector3 player)
        {
            int n = 0;
            foreach (var s in HomecomingPlacesLayout.SitesNear(worldSeed, new Vec2(player.x, player.z), FeedRadius))
                if (module.Register(s)) n++;
            return n;
        }
    }
}
