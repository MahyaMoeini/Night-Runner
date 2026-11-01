
using UnityEngine;

public class MovingObstacle : Interactable
{
    [SerializeField] private float laneChangeSpeed = 3.5f;

    private float laneA;
    private float laneB;
    private float targetX;
    private bool configured;

    public void SetLanePair(float firstLane, float secondLane)
    {
        laneA = firstLane;
        laneB = secondLane;

        Vector3 position = transform.position;
        position.x = laneA;
        transform.position = position;

        targetX = laneB;
        configured = true;
    }

    protected override void Update()
    {
        base.Update();

        if (!gameObject.activeInHierarchy || !configured)
            return;

        Vector3 position = transform.position;

        position.x = Mathf.MoveTowards(
            position.x,
            targetX,
            laneChangeSpeed * Time.deltaTime
        );

        transform.position = position;

        if (Mathf.Abs(position.x - targetX) < 0.02f)
        {
            targetX = Mathf.Approximately(targetX, laneA)
                ? laneB
                : laneA;
        }
    }

    protected override float HitReturnDelay => 0.35f;

    protected override void Interact()
    {
        if (GameManager.Instance == null ||
            !GameManager.Instance.IsPlaying)
            return;

        if (PlayerEffects.Instance != null)
            PlayerEffects.Instance.PlayCrash();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCollision();

        GameManager.Instance.LoseLife();
    }
}
