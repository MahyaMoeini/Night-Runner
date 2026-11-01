
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float laneChangeSpeed = 12f;

    private readonly float[] lanes = { -3f, 0f, 3f };
    private int currentLane = 1;

    private Rigidbody rb;
    private PlayerControls controls;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Gameplay.MoveLeft.performed += MoveLeft;
        controls.Gameplay.MoveRight.performed += MoveRight;
        controls.Gameplay.Pause.performed += TogglePause;

        controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        controls.Gameplay.MoveLeft.performed -= MoveLeft;
        controls.Gameplay.MoveRight.performed -= MoveRight;
        controls.Gameplay.Pause.performed -= TogglePause;

        controls.Gameplay.Disable();
    }

    private void MoveLeft(InputAction.CallbackContext context)
    {
        if (GameManager.Instance == null ||
            !GameManager.Instance.IsPlaying)
            return;

        int previous = currentLane;
        currentLane = Mathf.Max(0, currentLane - 1);
        PlayLaneScreech(previous);
    }

    private void MoveRight(InputAction.CallbackContext context)
    {
        if (GameManager.Instance == null ||
            !GameManager.Instance.IsPlaying)
            return;

        int previous = currentLane;
        currentLane = Mathf.Min(2, currentLane + 1);
        PlayLaneScreech(previous);
    }

    private void PlayLaneScreech(int previousLane)
    {
        if (previousLane == currentLane)
            return;

        if (AudioManager.Instance == null ||
            GameManager.Instance == null)
            return;

        // Tire screech only at higher speeds so it does not spam early game.
        if (GameManager.Instance.CurrentSpeed >=
            GameManager.Instance.MinimumSpeed + 4f)
        {
            AudioManager.Instance.PlayBrake();
        }
    }

    private void TogglePause(InputAction.CallbackContext context)
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.IsPlaying)
            GameManager.Instance.PauseGame();
        else if (GameManager.Instance.State ==
                 GameManager.GameState.Paused)
            GameManager.Instance.ResumeGame();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null ||
            !GameManager.Instance.IsPlaying)
            return;

        Vector3 target = rb.position;
        target.x = lanes[currentLane];

        rb.MovePosition(
            Vector3.MoveTowards(
                rb.position,
                target,
                laneChangeSpeed * Time.fixedDeltaTime
            )
        );
    }
}
