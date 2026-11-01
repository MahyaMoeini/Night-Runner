
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private static bool startImmediatelyAfterReload = false;

    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }

    [Header("Game Settings")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float fuelConsumption = 2f;
    [SerializeField] private float fuelPickupAmount = 25f;

    [Header("Difficulty Settings")]
    [SerializeField] private float initialSpeed = 12f;
    [SerializeField] private float maximumSpeed = 24f;
    [SerializeField] private float minimumSpeed = 8f;
    [SerializeField] private float difficultyStepSeconds = 15f;
    [SerializeField] private float speedIncreasePerStep = 2f;

    [Header("Collision Settings")]
    [SerializeField] private float collisionSpeedReduction = 1.5f;

    public int Lives { get; private set; }
    public int MaxLives => startingLives;

    public float Fuel { get; private set; }
    public float MaxFuel => maxFuel;

    public float FuelConsumptionRate => fuelConsumption;
    public float FuelPickupAmount => fuelPickupAmount;

    public float MinimumSpeed => minimumSpeed;
    public float MaximumSpeed => maximumSpeed;

    public int Score { get; private set; }
    public int HighScore { get; private set; }

    public float CurrentSpeed { get; private set; }
    public int DifficultyLevel { get; private set; }

    public GameState State { get; private set; }

    public bool IsPlaying => State == GameState.Playing;
    public bool IsGameOver => State == GameState.GameOver;

    private float difficultyTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Lives = startingLives;
        Fuel = maxFuel;
        Score = 0;

        HighScore = PlayerPrefs.GetInt("HighScore", 0);

        ResetDifficulty();

        State = startImmediatelyAfterReload
            ? GameState.Playing
            : GameState.Menu;

        startImmediatelyAfterReload = false;

        Time.timeScale = IsPlaying ? 1f : 0f;
    }

    private void Update()
    {
        if (!IsPlaying)
            return;

        // Fuel consumption
        Fuel -= fuelConsumption * Time.deltaTime;

        if (Fuel <= 0f)
        {
            Fuel = 0f;
            EndGame();
            return;
        }

        // Increase difficulty over time
        difficultyTimer += Time.deltaTime;

        if (difficultyTimer >= difficultyStepSeconds)
        {
            difficultyTimer -= difficultyStepSeconds;

            DifficultyLevel++;

            CurrentSpeed = Mathf.Min(
                maximumSpeed,
                CurrentSpeed + speedIncreasePerStep
            );

            Debug.Log(
                "Difficulty Level: " + DifficultyLevel +
                " | Speed: " + CurrentSpeed
            );
        }
    }

    private void ResetDifficulty()
    {
        difficultyTimer = 0f;
        DifficultyLevel = 0;
        CurrentSpeed = initialSpeed;
    }

    public void StartGame()
    {
        Lives = startingLives;
        Fuel = maxFuel;
        Score = 0;

        ResetDifficulty();

        State = GameState.Playing;
        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.ResetGameOverFlag();

        Debug.Log("Game Started!");
    }

    public void LoseLife()
    {
        if (!IsPlaying)
            return;

        Lives--;

        // Reduce speed after collision
        CurrentSpeed = Mathf.Max(
            minimumSpeed,
            CurrentSpeed - collisionSpeedReduction
        );

        Debug.Log("Remaining Lives: " + Lives);
        Debug.Log("Speed After Collision: " + CurrentSpeed);

        if (Lives <= 0)
            EndGame();
    }

    public void AddFuel(float amount)
    {
        if (!IsPlaying)
            return;

        Fuel = Mathf.Min(maxFuel, Fuel + amount);

        Debug.Log("Current Fuel: " + Fuel);
    }

    public void AddScore(int amount)
    {
        if (!IsPlaying)
            return;

        Score += amount;

        if (Score > HighScore)
        {
            HighScore = Score;

            PlayerPrefs.SetInt("HighScore", HighScore);
            PlayerPrefs.Save();
        }
    }

    public void SetSpeed(float speed)
    {
        CurrentSpeed = Mathf.Clamp(
            speed,
            minimumSpeed,
            maximumSpeed
        );
    }

    public void PauseGame()
    {
        if (!IsPlaying)
            return;

        State = GameState.Paused;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (State != GameState.Paused)
            return;

        State = GameState.Playing;
        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        startImmediatelyAfterReload = true;

        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void ReturnToMenu()
    {
        startImmediatelyAfterReload = false;

        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void EndGame()
    {
        if (IsGameOver)
            return;

        State = GameState.GameOver;
        Time.timeScale = 0f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameOver();

        Debug.Log("GAME OVER");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
