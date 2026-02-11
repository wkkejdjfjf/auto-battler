using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class AbilitySlot : MonoBehaviour, IDropHandler
{
    public int index;
    public GameObject currentItem;
    private AbilityManager abilityManager;  

    private void Awake()
    {
        abilityManager = FindFirstObjectByType<AbilityManager>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        AbilityDragDrop draggedAbility = eventData.pointerDrag.GetComponent<AbilityDragDrop>();
        if (draggedAbility != null)
        {
            abilityManager.EquipAbility(draggedAbility.abilityName, index);
        }
    }

    public void RemoveItem()
    {
        if (currentItem != null)
        {
            Destroy(currentItem);
            currentItem = null;
        }
    }


    private void UpdateSlotUI(Ability ability)
    {
        // Update the slot's UI to display the assigned ability (e.g., icon, name)
        // Example: GetComponent<Image>().sprite = ability.icon;
    }
}
