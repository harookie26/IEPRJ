using UnityEngine;

public class UISystem : MonoBehaviour
{
    [Header("UI Canvas References")]
    [SerializeField] private Canvas mainMenuCanvas;
    [SerializeField] private Canvas settingsMenuCanvas;
    
    [Header("Game State")]
    private bool isGameStarted = false;
    private bool isInMenu = true;
    
    void Start()
    {
        // Initialize UI state - show main menu, hide others
        ShowMainMenu();
    }
    
    void Update()
    {
        // Check for Q key to return to main menu when game is started
        if (isGameStarted && Input.GetKeyDown(KeyCode.Q))
        {
            ReturnToMainMenu();
        }
    }
    
    #region Main Menu Methods
    
    void ShowMainMenu()
    {
        // Show main menu canvas
        SetCanvasActive(mainMenuCanvas, true);
        
        // Hide other canvases
        SetCanvasActive(settingsMenuCanvas, false);
        
        // Pause the game while in main menu
        Time.timeScale = 0f;
        isGameStarted = false;
        isInMenu = true;
        
        // Show cursor for menu interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    public void StartGame()
    {
        // Hide main menu
        SetCanvasActive(mainMenuCanvas, false);
        
        // Hide settings menu if it's open
        SetCanvasActive(settingsMenuCanvas, false);
        
        // Resume game time
        Time.timeScale = 1f;
        isGameStarted = true;
        isInMenu = false;
        
        // Hide cursor for gameplay
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    public void ReturnToMainMenu()
    {
        // Return to main menu state
        ShowMainMenu();
    }
    
    #endregion
    
    #region Settings Menu Methods
    
    public void ShowSettingsMenu()
    {
        // Hide main menu
        SetCanvasActive(mainMenuCanvas, false);
        
        // Show settings menu
        SetCanvasActive(settingsMenuCanvas, true);
        
        // Still in menu state
        isInMenu = true;
        
        // Keep cursor visible for menu interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    public void ExitSettingsMenu()
    {
        // Hide settings menu
        SetCanvasActive(settingsMenuCanvas, false);
        
        // Show main menu
        SetCanvasActive(mainMenuCanvas, true);
        
        // Still in menu state
        isInMenu = true;
        
        // Keep cursor visible for menu interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    #endregion
    
    #region Utility Methods
    
    void SetCanvasActive(Canvas canvas, bool active)
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(active);
        }
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    #endregion
    
    #region Public Properties (for debugging or other systems)
    
    public bool IsGameStarted => isGameStarted;
    public bool IsInMenu => isInMenu;
    
    #endregion
}