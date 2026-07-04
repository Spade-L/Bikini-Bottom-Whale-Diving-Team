using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景出入口门。满足条件（通常 requiredFlags: scene_cleared_xxx 或 final_door_open）
/// 时可通行，否则播放“门是关着的”台词。
/// 也用于封锁回头路：returnDoor 勾选 + lockedByFlag 填 lock_early_scenes，
/// 28 次调查后播放“后路被封了”。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class SceneDoor : MonoBehaviour
{
    [Header("目标场景")]
    [SerializeField] private string targetSceneName;

    [Header("开启条件（如 requiredFlags: scene_cleared_home）")]
    [SerializeField] private StoryCondition openCondition = new StoryCondition();

    [Header("台词")]
    [Tooltip("条件不满足时（如“门锁着，好像还缺少什么线索”）")]
    [SerializeField] private DialogueData lockedDialogue;
    [Tooltip("进门前播放的对话（可空，播完才切场景）")]
    [SerializeField] private DialogueData enterDialogue;

    [Header("交互 UI")]
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange;
    private bool isTransitioning;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInRange || isTransitioning || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (DialogueUIManager.Instance == null || !DialogueUIManager.Instance.CanOpenDialogue)
        {
            return;
        }

        if (!openCondition.IsMet())
        {
            if (lockedDialogue != null)
            {
                DialogueUIManager.Instance.StartDialogue(lockedDialogue);
            }
            return;
        }

        if (enterDialogue != null)
        {
            isTransitioning = true;
            DialogueUIManager.Instance.StartDialogue(enterDialogue, LoadTargetScene);
        }
        else
        {
            LoadTargetScene();
        }
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning($"[SceneDoor] {name} 未设置目标场景名");
            isTransitioning = false;
            return;
        }

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeOutThen(() => SceneManager.LoadScene(targetSceneName));
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;

            if (interactionUI != null)
            {
                interactionUI.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;

            if (interactionUI != null)
            {
                interactionUI.SetActive(false);
            }
        }
    }
}
