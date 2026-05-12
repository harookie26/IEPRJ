using TMPro;
using UnityEngine;

public class BuildVersionToggle : MonoBehaviour
{
    [SerializeField] private string buildVersion = "Artist Realm Studio Sprint # v1.0";
    [SerializeField] private TextMeshProUGUI buildVersionText;

    private KeyCode toggleKeyControl1 = KeyCode.LeftControl;
    private KeyCode toggleKeyControl2 = KeyCode.RightControl;
    private KeyCode toggleKey = KeyCode.B;

    private bool isVisible = true;

    private void Awake()
    {

        ApplyVisibility();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (buildVersionText != null)
        {
            buildVersionText.text = buildVersion;
        }

    }

    private void Update()
    {
        // Toggle on/off
        if ((Input.GetKey(toggleKeyControl1) || Input.GetKey(toggleKeyControl2)) && Input.GetKeyDown(toggleKey))
            ToggleFPS();

        if (!isVisible || buildVersionText == null)
            return;
    }

    // For UI Button OnClick() hookup or other scripts. 
    public void ToggleFPS()
    {
        isVisible = !isVisible;
        ApplyVisibility();
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        if (buildVersionText != null)
            buildVersionText.gameObject.SetActive(isVisible);
    }

}
