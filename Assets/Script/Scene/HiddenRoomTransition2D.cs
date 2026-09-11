using System.Collections.Generic;
using UnityEngine;

// 使用 当前脚本 所需功能
[RequireComponent(typeof(BoxCollider2D))]
public class HiddenRoomTransition2D : MonoBehaviour
{
// 保存 classroomRoot 引用
    [SerializeField] private GameObject classroomRoot;
    [SerializeField] private GameObject hiddenRoomRoot;
// 同步 HiddenRoomTransition2D 的相关数据
    [SerializeField] private string requiredFlag = "school_water_dispenser_room_ready";
    [SerializeField] private string playerTag = "Player";

// 配置 隐藏空间 BGM 分组
    [Header("隐藏空间 BGM")]
    [Tooltip("神秘、压抑、低频嗡鸣、未知空间")]
    [SerializeField] private AudioClip hiddenRoomBgm;
    [Range(0f, 1f)] [SerializeField] private float hiddenRoomBgmVolume = 0.85f;
    [Min(0f)] [SerializeField] private float hiddenRoomBgmFade = 0.6f;

// 同步 HiddenRoomTransition2D 的相关数据（HiddenRoomTransition2D 后续步骤）
    private readonly HashSet<Collider2D> overlappingPlayerColliders = new HashSet<Collider2D>();
    private bool hiddenRoomShown;
// 记录隐藏空间 BGM 是否占用中
    private bool hiddenRoomBgmActive;

// 记录 HiddenRoomTransition2D 的当前状态
    private bool playerInRange => overlappingPlayerColliders.Count > 0;
    private bool CanEnter => GameManager.Instance != null
// 使用 HiddenRoomTransition2D 所需功能
        && GameManager.Instance.HasFlag(requiredFlag);

// 初始化组件引用和运行状态
    private void Awake()
    {
// 获取 Awake 的组件引用
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

// 读取初始依赖并同步首帧状态
    private void Start()
    {
// 推进 Start 中的必要步骤
        RefreshVisibility();
    }

// 每帧检查输入与状态变化
    private void Update()
    {
// 检查 Update 的前置条件
        if (playerInRange && CanEnter && !hiddenRoomShown)
        {
// 推进 Update 中的必要步骤
            RefreshVisibility();
        }
    }

// 禁用时取消订阅并清理临时状态
    private void OnDisable()
    {
// 推进 OnDisable 中的必要步骤
        SetRoomVisible(false);
    }

// 玩家进入范围后登记可交互状态
    private void OnTriggerEnter2D(Collider2D other)
    {
// 检查 OnTriggerEnter2D 的前置条件
        if (other.CompareTag(playerTag))
        {
// 使用 OnTriggerEnter2D 所需功能
            overlappingPlayerColliders.Add(other);
            RefreshVisibility();
        }
    }

// 玩家离开范围后移除可交互状态
    private void OnTriggerExit2D(Collider2D other)
    {
// 检查 OnTriggerExit2D 的前置条件
        if (other.CompareTag(playerTag))
        {
// 使用 OnTriggerExit2D 所需功能
            overlappingPlayerColliders.Remove(other);
            RefreshVisibility();
        }
    }

// 刷新 RefreshVisibility 对应状态
    private void RefreshVisibility()
    {
// 推进 RefreshVisibility 中的必要步骤
        SetRoomVisible(CanEnter && playerInRange);
    }

// 设置 SetRoomVisible 的目标状态
    private void SetRoomVisible(bool showHiddenRoom)
    {
// 同步 SetRoomVisible 的状态
        hiddenRoomShown = showHiddenRoom;
        if (classroomRoot != null)
        {
// 切换 SetRoomVisible 的显示状态
            classroomRoot.SetActive(!showHiddenRoom);
        }
// 检查 SetRoomVisible 的前置条件
        if (hiddenRoomRoot != null)
        {
// 切换 SetRoomVisible 的显示状态（SetRoomVisible 后续步骤）
            hiddenRoomRoot.SetActive(showHiddenRoom);
        }
        RefreshHiddenRoomBgm(showHiddenRoom);
    }

// 刷新 RefreshHiddenRoomBgm 对应状态
    private void RefreshHiddenRoomBgm(bool showHiddenRoom)
    {
// 检查 RefreshHiddenRoomBgm 的前置条件
        if (showHiddenRoom && !hiddenRoomBgmActive && hiddenRoomBgm != null && MusicManager.Instance != null)
        {
// 暂停普通 BGM 并循环播放隐藏空间音乐
            MusicManager.Instance.PlayInterruptingBgm(this, hiddenRoomBgm, hiddenRoomBgmVolume, hiddenRoomBgmFade);
            hiddenRoomBgmActive = true;
        }
// 处理 RefreshHiddenRoomBgm 的备用分支
        else if (!showHiddenRoom && hiddenRoomBgmActive)
        {
// 离开隐藏空间后从原进度恢复普通 BGM
            MusicManager.Instance?.StopInterruptingBgm(this, hiddenRoomBgmFade);
            hiddenRoomBgmActive = false;
        }
    }
}
