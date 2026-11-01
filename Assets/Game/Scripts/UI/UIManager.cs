
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text fuelText;
    [SerializeField] private TMP_Text speedText;

    [Header("Fuel Bar")]
    [SerializeField] private Image fuelFill;

    [Header("Menus")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Game Over Text")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text finalBestText;

    private GameManager gm;

    private void Awake()
    {
        // Automatically find SpeedText if not assigned
        if (speedText == null)
        {
            GameObject speedObject =
                GameObject.Find("SpeedText");

            if (speedObject != null)
            {
                speedText =
                    speedObject.GetComponent<TMP_Text>();
            }
        }

        if (speedText == null)
        {
            Debug.LogError(
                "SpeedText not found in GameCanvas!"
            );
        }

        if (GetComponent<MenuChrome>() == null)
            gameObject.AddComponent<MenuChrome>();
    }

    private void Start()
    {
        gm = GameManager.Instance;

        if (gm == null)
        {
            Debug.LogError("GameManager not found!");
        }
    }

    private void Update()
    {
        if (gm == null)
            return;

        UpdateHUD();
        UpdateMenus();
    }

    private void UpdateHUD()
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + gm.Score;
        }

        if (highScoreText != null)
        {
            highScoreText.text = "BEST: " + gm.HighScore;
        }

        if (livesText != null)
        {
            livesText.text = "LIVES: " + gm.Lives;
        }

        if (fuelText != null)
        {
            fuelText.text =
                "FUEL: " + Mathf.CeilToInt(gm.Fuel) + "%";
        }

        // Live speed update
        if (speedText != null)
        {
            speedText.text =
                "SPEED: " + Mathf.RoundToInt(gm.CurrentSpeed);
        }

        if (fuelFill != null)
        {
            fuelFill.fillAmount =
                Mathf.Clamp01(gm.Fuel / gm.MaxFuel);
        }
    }

    private void UpdateMenus()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(
                gm.State == GameManager.GameState.Menu
            );
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(
                gm.State == GameManager.GameState.Paused
            );
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(
                gm.State == GameManager.GameState.GameOver
            );
        }

        if (gm.IsGameOver)
        {
            if (finalScoreText != null)
            {
                finalScoreText.text =
                    "SCORE: " + gm.Score;
            }

            if (finalBestText != null)
            {
                finalBestText.text =
                    "BEST: " + gm.HighScore;
            }
        }
    }

    // Main Menu Buttons

    public void StartButton()
    {
        PlayClick();
        gm.StartGame();
    }

    public void ExitButton()
    {
        PlayClick();
        gm.QuitGame();
    }

    // Pause Menu Buttons

    public void ResumeButton()
    {
        PlayClick();
        gm.ResumeGame();
    }

    public void RestartButton()
    {
        PlayClick();
        gm.RestartGame();
    }

    public void MainMenuButton()
    {
        PlayClick();
        gm.ReturnToMenu();
    }

    private static void PlayClick()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
    }
}
