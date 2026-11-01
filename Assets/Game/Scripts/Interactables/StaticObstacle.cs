
public class StaticObstacle : Interactable
{
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
