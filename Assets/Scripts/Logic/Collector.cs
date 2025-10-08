using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class Collector : MonoBehaviour
{
    [Tooltip("How many items left to collect (or specifically, how many times the \"CollectOne\" method needs to be called) before the UnityEvent is triggered.")]
    public int leftToCollect = 3;
    [Tooltip("The event that will trigger when 'Left To Collect' reaches zero.")]
    public UnityEvent collectionTrigger;
    [Tooltip("A in-game text object that will display the number left to collect.")]
    public TMP_Text displayText;
    [Tooltip("The format for how 'Display Text' will show the number of items left to collect. \"{0}\" is a placeholder for the number left, but you can add any text you want before or after it.")]
    [TextAreaAttribute]
    public string displayFormat = "{0}";
    [Tooltip("The text that will display on 'Display Text' when the number of items left to collect is lower than or equal to zero.")]
    [TextAreaAttribute]
    public string completeText = "";

    [Space]
    [Header("Debug")]
    [Tooltip("Whether or not this script prints information to the debug console.")]
    public bool consoleLogCollector = false;

    void Start()
    {
        if (displayText != null)
        {
            displayText.text = string.Format(displayFormat, leftToCollect);
        }
    }

    public void SetNumberToCollect(int amount)
    {
        leftToCollect = amount;
        CheckIfCollected();
    }

    public void AdjustNumberToCollect(int amount)
    {
        leftToCollect += amount;
        CheckIfCollected();
    }

    public void CollectOne()
    {
        leftToCollect--;
        CheckIfCollected();
    }

    private void CheckIfCollected()
    {
        if (leftToCollect <= 0)
        {
            collectionTrigger.Invoke();
            if (displayText != null)
            {
                displayText.text = completeText;
            }
            Destroy(this);
        }
        else if (displayText != null)
        {
            displayText.text = string.Format(displayFormat, leftToCollect);
        }

        if (consoleLogCollector) Debug.Log("Collection Event, " + leftToCollect + " remaining: " + gameObject.name);
    }
}
