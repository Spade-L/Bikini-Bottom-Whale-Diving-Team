using System.Collections.Generic;
using UnityEngine;

// 调用 RequireComponent
[RequireComponent(typeof(BoxCollider2D))]
public class HiddenRoomTransition2D : MonoBehaviour
{
// 保存 classroomRoot 引用
    [SerializeField] private GameObject classroomRoot;
    [SerializeField] private GameObject hiddenRoomRoot;
// 保存 requiredFlag 数据
    [SerializeField] private string requiredFlag = "school_water_dispenser_room_ready";
    [SerializeField] private string playerTag = "Player";

// 配置 隐藏空间 BGM 分组
    [Header("隐藏空间 BGM")]
    [Tooltip("神秘、压抑、低频嗡鸣、未知空间")]
    [SerializeField] private AudioClip hiddenRoomBgm;
    [Range(0f, 1f)] [SerializeField] private float hiddenRoomBgmVolume = 0.85f;
    [Min(0f)] [SerializeField] private float hiddenRoomBgmFade = 0.6f;

// 保存 overlappingPlayerColliders 数据
    private readonly HashSet<Collider2D> overlappingPlayerColliders = new HashSet<Collider2D>();
    private bool hiddenRoomShown;
// 记录隐藏空间 BGM 是否占用中
    private bool hiddenRoomBgmActive;

// 记录 playerInRange 状态
    private bool playerInRange => overlappingPlayerColliders.Count > 0;
    private bool CanEnter => GameManager.Instance != null
// 调用 HasFlag
        && GameManager.Instance.HasFlag(requiredFlag);

// 定义 Awake 方法
    private void Awake()
    {
// 获取组件引用
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

// 定义 Start 方法
    private void Start()
    {
// 执行 RefreshVisibility
        RefreshVisibility();
    }

// 定义 Update 方法
    private void Update()
    {
// 判断当前条件
        if (playerInRange && CanEnter && !hiddenRoomShown)
        {
// 执行 RefreshVisibility
            RefreshVisibility();
        }
    }

// 定义 OnDisable 方法
    private void OnDisable()
    {
// 执行 SetRoomVisible
        SetRoomVisible(false);
    }

// 定义 OnTriggerEnter2D 方法
    private void OnTriggerEnter2D(Collider2D other)
    {
// 判断当前条件
        if (other.CompareTag(playerTag))
        {
// 调用 Add
            overlappingPlayerColliders.Add(other);
            RefreshVisibility();
        }
    }

// 定义 OnTriggerExit2D 方法
    private void OnTriggerExit2D(Collider2D other)
    {
// 判断当前条件
        if (other.CompareTag(playerTag))
        {
// 调用 Remove
            overlappingPlayerColliders.Remove(other);
            RefreshVisibility();
        }
    }

// 定义 RefreshVisibility 方法
    private void RefreshVisibility()
    {
// 执行 SetRoomVisible
        SetRoomVisible(CanEnter && playerInRange);
    }

// 定义 SetRoomVisible 方法
    private void SetRoomVisible(bool showHiddenRoom)
    {
// 更新当前状态
        hiddenRoomShown = showHiddenRoom;
        if (classroomRoot != null)
        {
// 切换显示状态
            classroomRoot.SetActive(!showHiddenRoom);
        }
// 判断当前条件
        if (hiddenRoomRoot != null)
        {
// 切换显示状态
            hiddenRoomRoot.SetActive(showHiddenRoom);
        }
        RefreshHiddenRoomBgm(showHiddenRoom);
    }

// 切换隐藏空间专用 BGM
    private void RefreshHiddenRoomBgm(bool showHiddenRoom)
    {
// 判断当前条件
        if (showHiddenRoom && !hiddenRoomBgmActive && hiddenRoomBgm != null && MusicManager.Instance != null)
        {
// 暂停普通 BGM 并循环播放隐藏空间音乐
            MusicManager.Instance.PlayInterruptingBgm(this, hiddenRoomBgm, hiddenRoomBgmVolume, hiddenRoomBgmFade);
            hiddenRoomBgmActive = true;
        }
// 处理其他分支
        else if (!showHiddenRoom && hiddenRoomBgmActive)
        {
// 离开隐藏空间后从原进度恢复普通 BGM
            MusicManager.Instance?.StopInterruptingBgm(this, hiddenRoomBgmFade);
            hiddenRoomBgmActive = false;
        }
    }
}