using UnityEngine;

// 调用 CreateAssetMenu
[CreateAssetMenu(fileName = "InvestigationEventTable", menuName = "游戏数据/调查事件表")]
public class InvestigationEventTable : ScriptableObject
{
// 更新当前逻辑
    [System.Serializable]
    public class ThresholdEvent
    {
// 说明当前配置
        [Tooltip("仅供编辑器辨认")]
        public string editorLabel;

// 说明当前配置
        [Tooltip("调查次数达到此值时触发（每个事件只触发一次）")]
        public int threshold;

// 配置 效果（都可留空） 分组
        [Header("效果（都可留空）")]
        [Tooltip("额外设置的 Flag（如 lock_home_items / final_door_open）")]
// 保存 setFlags 数据
        public string[] setFlags;

// 说明当前配置
        [Tooltip("回溯闪回演出（无则跳过）")]
        public FlashbackSequence flashback;

// 说明当前配置
        [Tooltip("闪回结束后播放的独白（此独白不重复计入调查次数）")]
        public DialogueData monologue;
    }

// 保存 events 数据
    public ThresholdEvent[] events;
}

/// <summary>回溯闪回的演出内容。</summary>
[System.Serializable]
public class FlashbackSequence
{
// 说明当前配置
    [Tooltip("闪回画面（哥哥的背影/写便条/镜子），按顺序播放")]
    public Sprite[] images;

// 说明当前配置
    [Tooltip("每张画面停留秒数")]
    public float secondsPerImage = 1.2f;

// 说明当前配置
    [Tooltip("画面上叠加的一行字（可空）")]
    public string caption;

// 记录 HasContent 状态
    public bool HasContent => images != null && images.Length > 0;
}
