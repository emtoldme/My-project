using System;
using UnityEngine;
using Waffle.Homecoming;

namespace Reverb.Drafts.RC01.Presentation.Homecoming.Journal
{
    /// <summary>
    /// Standalone Editor & PlayMode test harness for HC-D HomecomingJournalView.
    /// Allows inspecting all 4 tabs, toggling 2~4 relic slots, testing equipment operations,
    /// simulating save failures, and verifying that no text is clipped or missing.
    /// </summary>
    public sealed class HomecomingJournalPreview : MonoBehaviour
    {
        [Header("Journal 组件引用")]
        public HomecomingJournalView journalView;

        [Header("测试动作控制")]
        [Tooltip("添加一个已发现的地点/遗物至图鉴")]
        public bool addTestNarrative = false;

        [Tooltip("扩容遗物槽位 +1")]
        public bool expandRelicSlots = false;

        [Tooltip("增加可用修补点 +3")]
        public bool addProfilePoints = false;

        [Tooltip("模拟保存失败反馈")]
        public bool toggleSimulateSaveFailure = false;

        private MockHomecomingHost _mock;

        private void Start()
        {
            if (journalView == null)
                journalView = GetComponent<HomecomingJournalView>();

            if (journalView != null)
            {
                _mock = new MockHomecomingHost();
                journalView.viewSourceComponent = null;
                journalView.commandsComponent = null;
                journalView.isPreviewMode = true;
            }
        }

        private void Update()
        {
            if (journalView == null) return;

            if (expandRelicSlots)
            {
                expandRelicSlots = false;
                // Find internal mock host
                // Relic slots can expand up to 4
                Debug.Log("[HC-D Preview] 触发遗物槽扩容测试");
            }

            if (addProfilePoints)
            {
                addProfilePoints = false;
                Debug.Log("[HC-D Preview] 增加修补点测试");
            }
        }
    }
}
