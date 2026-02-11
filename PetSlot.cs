using UnityEngine;
using UnityEngine.EventSystems;

public class PetSlot : MonoBehaviour, IDropHandler
{
    [Header("Slot Settings")]
    public int index;

    [Header("Current State")]
    public GameObject currentItem;

    private PetManager petManager;

    private void Awake()
    {
        petManager = FindFirstObjectByType<PetManager>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            Debug.LogError("pointerDrag is null");
            return;
        }
        DragDrop draggedItem = eventData.pointerDrag.GetComponent<DragDrop>();
        if (draggedItem == null)
        {
            Debug.LogError("No DragDrop component on dragged object");
            return;
        }
        if (string.IsNullOrEmpty(draggedItem.petName))
        {
            Debug.LogError("Dragged item has no pet name!");
            return;
        }
        if (petManager.EquipPet(draggedItem.petName, index))
        {
            if (draggedItem.gameObject != null)
            {
                Destroy(draggedItem.gameObject);
            }
        }
        else
        {
            Debug.LogError($"Failed to equip pet {draggedItem.petName} in slot {index}");
        }
    }

    public void RemoveItem()
    {
        if (currentItem != null)
        {
            Destroy(currentItem);
            currentItem = null;
        }
        else
        {
            
        }
    }

    public bool IsEmpty()
    {
        bool dataEmpty = petManager == null || petManager.IsSlotEmpty(index);
        bool uiEmpty = currentItem == null;
        return dataEmpty && uiEmpty;
    }

    [ContextMenu("Debug This Slot")]
    public void DebugSlot()
    {
        Debug.Log($"=== SLOT {index} DEBUG ===");
        Debug.Log($"GameObject name: {gameObject.name}");
        Debug.Log($"Current item: {(currentItem != null ? currentItem.name : "NULL")}");
        Debug.Log($"PetManager found: {petManager != null}");
        if (petManager != null)
        {
            Debug.Log($"Equipped pet: {petManager.GetEquippedPet(index)}");
            Debug.Log($"Is slot empty: {petManager.IsSlotEmpty(index)}");
        }
    }
}