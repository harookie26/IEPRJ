using Level.UI;
using UnityEngine;

public class DialogueSample : MonoBehaviour
{
    [SerializeField] private DialogueEntry sampleDialogue1;
    [SerializeField] private DialogueEntry sampleDialogue2;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            DialogueManager.Instance.Display(sampleDialogue1);
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            DialogueManager.Instance.Display(sampleDialogue2);
        }
    }
}
