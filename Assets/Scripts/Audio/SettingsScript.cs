using UnityEngine;

public class SettingsScript : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    private bool isOpen = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    public void onCloseButtonClicked()
    {
        ToggleSettings();
    }

    void ToggleSettings()
    {
        isOpen = !isOpen;
        settingsPanel.SetActive(isOpen);

        // Pause or unpause game
        //Time.timeScale = isOpen ? 0f : 1f;

        if (isOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
