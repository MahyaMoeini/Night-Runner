
public class FuelCan : Interactable
{
    protected override void Interact()
    {
        if (GameManager.Instance == null ||
            !GameManager.Instance.IsPlaying)
            return;

        GameManager.Instance.AddFuel(
            GameManager.Instance.FuelPickupAmount
        );

        if (PlayerEffects.Instance != null)
            PlayerEffects.Instance.PlayFuel();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayFuel();
    }
}
