using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class HiddenRoomTransition2D : MonoBehaviour
{
    [SerializeField] private GameObject classroomRoot;
    [SerializeField] private GameObject hiddenRoomRoot;
    [SerializeField] private string requiredFlag = "school_water_dispenser_room_ready";
    [SerializeField] private string playerTag = "Player";

    private readonly HashSet<Collider2D> overlappingPlayerColliders = new HashSet<Collider2D>();
    private bool hiddenRoomShown;

    private bool playerInRange => overlappingPlayerColliders.Count > 0;
    private bool CanEnter => GameManager.Instance != null
        && GameManager.Instance.HasFlag(requiredFlag);

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Start()
    {
        RefreshVisibility();
    }

    private void Update()
    {
        if (playerInRange && CanEnter && !hiddenRoomShown)
        {
            RefreshVisibility();
        }
    }

    private void OnDisable()
    {
        SetRoomVisible(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            overlappingPlayerColliders.Add(other);
            RefreshVisibility();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            overlappingPlayerColliders.Remove(other);
            RefreshVisibility();
        }
    }

    private void RefreshVisibility()
    {
        SetRoomVisible(CanEnter && playerInRange);
    }

    private void SetRoomVisible(bool showHiddenRoom)
    {
        hiddenRoomShown = showHiddenRoom;
        if (classroomRoot != null)
        {
            classroomRoot.SetActive(!showHiddenRoom);
        }
        if (hiddenRoomRoot != null)
        {
            hiddenRoomRoot.SetActive(showHiddenRoom);
        }
    }
}