
public class Coin : Interactable
{
    protected override void Interact()
    {
        if (GameManager.Instance == null ||
            !GameManager.Instance.IsPlaying)
            return;

        GameManager.Instance.AddScore(10);

        if (PlayerEffects.Instance != null)
            PlayerEffects.Instance.PlayCoin();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCoin();
    }
}
