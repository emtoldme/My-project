using System;
using System.Globalization;

namespace Waffle
{
    /// <summary>
    /// 调试命令解析（§2.11 最小集）。纯 C#，可单元测试；界面在 Debug/DebugConsole.cs。
    /// 支持：tracetick [on|off|n]、settime s、setdt s（0 = 关闭）、sethp n、givexp n、
    /// god [on|off]、hurt n、slowmo x、help。
    /// </summary>
    public static class DebugCommands
    {
        public const string Help =
            "tracetick [on|off|n] | settime s | setdt s(0=关) | sethp n | givexp n | god [on|off] | hurt n | slowmo x | spawn n | setcap n | spawngems n";

        /// <summary>执行一条命令，返回给控制台显示的结果文字。</summary>
        public static string Execute(RunSimulation sim, string line)
        {
            if (sim == null || string.IsNullOrWhiteSpace(line)) return "";
            string[] parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLowerInvariant();
            string arg = parts.Length > 1 ? parts[1].ToLowerInvariant() : "";
            float f;
            int n;

            switch (cmd)
            {
                case "help":
                    return Help;
                case "tracetick":
                    if (arg == "" ) { sim.TraceTicks = 1; return "追踪下 1 个 tick"; }
                    if (arg == "on") { sim.TraceTicks = -1; return "tick 追踪：开"; }
                    if (arg == "off") { sim.TraceTicks = 0; return "tick 追踪：关"; }
                    if (TryInt(arg, out n) && n > 0) { sim.TraceTicks = n; return "追踪下 " + n + " 个 tick"; }
                    break;
                case "settime":
                    if (TryFloat(arg, out f)) { if (!sim.DebugSetTime(f)) return Rejected(sim, line); return "now = " + sim.State.Now.ToString("F3", CultureInfo.InvariantCulture); }
                    break;
                case "setdt":
                    if (TryFloat(arg, out f) && f >= 0f) { sim.FixedDt = f; return f > 0f ? "固定 dt = " + arg : "固定 dt：关"; }
                    break;
                case "sethp":
                    if (TryInt(arg, out n)) { if (!sim.DebugSetHp(n)) return Rejected(sim, line); return "HP = " + sim.State.Hp; }
                    break;
                case "givexp":
                    if (TryInt(arg, out n) && n > 0) { if (!sim.DebugGiveXp(n)) return Rejected(sim, line); return "排队 +" + n + " XP（下一个 Playing tick 的 T10 结算）"; }
                    break;
                case "god":
                    sim.God = arg == "" ? !sim.God : arg == "on";
                    return "god = " + sim.God;
                case "hurt":
                    if (TryInt(arg, out n) && n > 0) { if (!sim.DebugHurt(n)) return Rejected(sim, line); return "排队伤害 " + n + "（下一个 Playing tick 的 T8 结算）"; }
                    break;
                case "slowmo":
                    if (TryFloat(arg, out f) && f > 0f) { sim.Slowmo = f; return "slowmo = " + arg; }
                    break;
                case "spawn":
                case "spawngems":
                    if (TryInt(arg, out n) && n > 0)
                    {
                        if (!sim.DebugSpawn(n, cmd == "spawngems")) return Rejected(sim, line);
                        return "已生成 " + Math.Min(n, 200) + (cmd == "spawn" ? " 个敌人（受池容量限制）" : " 份经验（满池合并）");
                    }
                    break;
                case "setcap":
                    if (TryInt(arg, out n) && n > 0)
                    {
                        if (!sim.DebugSetCap(n)) return Rejected(sim, line);
                        return "普通敌人上限 = " + sim.Combat.Config.aliveCap;
                    }
                    break;
                default:
                    return "未知命令：" + cmd + "。" + Help;
            }
            return "参数错误：" + line + "。" + Help;
        }

        /// <summary>终局后改玩法数据的命令一律不执行（A-79b），并明确告诉调用者。</summary>
        public static string Rejected(RunSimulation sim, string line)
        {
            if (sim.State.Phase.IsTerminal())
                return "已终局（" + sim.State.Phase + "），命令未执行：" + line.Trim();
            return "命令未执行：" + line.Trim();
        }

        private static bool TryFloat(string s, out float value)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryInt(string s, out int value)
        {
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }
}
