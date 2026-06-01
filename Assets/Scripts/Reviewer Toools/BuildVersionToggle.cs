using TMPro;
using UnityEngine;

public class BuildVersionToggle : MonoBehaviour
{
    [SerializeField] private string buildVersion = "Artist Realm Studio Sprint 2 v2.1";
    [SerializeField] private TextMeshProUGUI buildVersionText;

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
        if (!isVisible || buildVersionText == null)
            return;
    }

    // For UI Button OnClick() hookup or other scripts. 
    public void ToggleBuildVersion()
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
