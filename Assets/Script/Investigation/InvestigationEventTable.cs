using UnityEngine;

/// <summary>
/// 调查次数阈值事件表。按你的策划案预配置：
/// 5  第一次回溯（哥哥的背影）
/// 8  封锁 Flag: lock_home_items
/// 10 动摇独白
/// 15 第二次回溯（写便条）
/// 18 封锁 Flag: lock_npc_talk
/// 20 由 NPC 状态条件实现（requiredFlags: inv_reached_20）
/// 25 第三次回溯（镜子）
/// 28 封锁 Flag: lock_early_scenes
/// 30 解锁终局大门 Flag: final_door_open
///
/// 每个阈值达成时自动设置 "inv_reached_<次数>" flag，场景物件可直接引用。
/// </summary>
[CreateAssetMenu(fileName = "InvestigationEventTable", menuName = "游戏数据/调查事件表")]
public class InvestigationEventTable : ScriptableObject
{
    [System.Serializable]
    public class ThresholdEvent
    {
        [Tooltip("仅供编辑器辨认")]
        public string editorLabel;

        [Tooltip("调查次数达到此值时触发（每个事件只触发一次）")]
        public int threshold;

        [Header("效果（都可留空）")]
        [Tooltip("额外设置的 Flag（如 lock_home_items / final_door_open）")]
        public string[] setFlags;

        [Tooltip("回溯闪回演出（无则跳过）")]
        public FlashbackSequence flashback;

        [Tooltip("闪回结束后播放的独白（此独白不重复计入调查次数）")]
        public DialogueData monologue;
    }

    public ThresholdEvent[] events;
}

/// <summary>回溯闪回的演出内容。</summary>
[System.Serializable]
public class FlashbackSequence
{
    [Tooltip("闪回画面（哥哥的背影/写便条/镜子），按顺序播放")]
    public Sprite[] images;

    [Tooltip("每张画面停留秒数")]
    public float secondsPerImage = 1.2f;

    [Tooltip("画面上叠加的一行字（可空）")]
    public string caption;

    public bool HasContent => images != null && images.Length > 0;
}
