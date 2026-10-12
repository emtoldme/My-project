using UnityEngine;
using Reverb.Experiments.Mechanism;

namespace Waffle.Experiments.Mechanism.Presentation
{
    /// <summary>
    /// TEST_ONLY。只读共享试玩合同。没有适配器或 IsReady 为假时，按钮不可用，计数显示为破折号。
    /// 不写 HP，不推进规则时钟。没有文本框，避免抢走 WASD。
    /// </summary>
    public class M01PlaytestPanel : MonoBehaviour
    {
        public GameLoop loop;

        void Awake()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
        }

        void OnGUI()
        {
            if (loop == null || loop.Sim == null) return;
            ImguiScale.Apply();
            IMechanismPlaytestControls controls = FindOnThisObject();
            bool ready = controls != null && controls.IsReady;

            Rect box = ImguiScale.Fit(new Rect(16f, ImguiScale.Height - 214f, 560f, 198f));
            GUILayout.BeginArea(box, GUI.skin.box);
            GUILayout.Label(ready ? "TEST_ONLY 机制  " + controls.Mode : "等待机制接线");
            if (ready)
            {
                GUILayout.Label("回声 " + controls.EchoHits
                    + "    传播 " + controls.PropagationHits
                    + "    待处理 " + controls.PendingCount);
            }
            else
            {
                GUILayout.Label("回声 —    传播 —    待处理 —");
            }
            M01DerivedFx fx = GetComponent<M01DerivedFx>();
            if (fx != null && fx.CueSyncSeen)
                GUILayout.Label("回声预备 " + fx.ActiveCues + "    标记收在原目标身上，不是范围");
            else
                GUILayout.Label("回声预备：等待规则快照");
            GUILayout.Label("seed " + loop.config.seed + "    切换即同种子重开 · 固定武器对照");
            GUILayout.Label("紫红标记：0.5秒后这一个目标再受伤。数字只在打中后出现");
            GUILayout.Label("金色连线：伤害跳向另一目标，最多5米");

            bool previous = GUI.enabled;
            GUI.enabled = ready;
            GUILayout.BeginHorizontal();
            ModeButton(controls, MechanismMode.None, "无模块");
            ModeButton(controls, MechanismMode.Echo, "回声");
            ModeButton(controls, MechanismMode.Propagation, "传播");
            ModeButton(controls, MechanismMode.Combined, "组合");
            GUILayout.EndHorizontal();
            GUI.enabled = previous;
            GUILayout.EndArea();
        }

        static void ModeButton(IMechanismPlaytestControls controls, MechanismMode mode, string label)
        {
            if (GUILayout.Button(label, GUILayout.Height(28f)) && controls != null && controls.IsReady)
                controls.RestartWithMode(mode);
        }

        IMechanismPlaytestControls FindOnThisObject()
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IMechanismPlaytestControls controls) return controls;
            }
            return null;
        }
    }
}
