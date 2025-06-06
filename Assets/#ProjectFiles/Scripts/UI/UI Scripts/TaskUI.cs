using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskUI : MonoBehaviour
{
    [SerializeField] TMP_Text taskNameText;
    [SerializeField] Slider slider;
    [SerializeField] TMP_Text requiredAmountText;
    [SerializeField] Image taskDone;

    public void SetUIInfo(Task task)
    {
        taskNameText.text = task.description;
        slider.value = task.accumulated_amount / task.required_amount;
        requiredAmountText.text = $"{slider.value * 100f}% completed";
        taskDone.enabled = task.done;
    }
}