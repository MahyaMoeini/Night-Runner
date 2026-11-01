using System.Collections;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    private ObjectPool ownerPool;

    protected float moveSpeed;

    [Header("Movement Settings")]
    [SerializeField] private float despawnZ = -10f;

    public void Initialize(ObjectPool pool, float speed)
    {
        ownerPool = pool;
        moveSpeed = speed;
    }

    protected virtual void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.IsPlaying)
        {
            moveSpeed = GameManager.Instance.CurrentSpeed;
        }

        transform.Translate(
            Vector3.back * moveSpeed * Time.deltaTime,
            Space.World
        );

        if (transform.position.z <= despawnZ)
            ReturnToPool();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Interact();

        float delay = HitReturnDelay;
        if (delay > 0f)
        {
            VehicleVisualBuilder visual =
                GetComponent<VehicleVisualBuilder>();

            if (visual != null)
                visual.PlayCrashReaction();

            StartCoroutine(ReturnAfterDelay(delay));
        }
        else
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// Obstacle cars can delay pool return to show a crash reaction.
    /// </summary>
    protected virtual float HitReturnDelay => 0f;

    protected abstract void Interact();

    private IEnumerator ReturnAfterDelay(float delay)
    {
        // Keep the crashed car briefly visible, but stop further trigger hits.
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        yield return new WaitForSecondsRealtime(delay);

        if (col != null)
            col.enabled = true;

        ReturnToPool();
    }

    protected void ReturnToPool()
    {
        if (!gameObject.activeSelf)
            return;

        if (ownerPool != null)
            ownerPool.Return(gameObject);
        else
            gameObject.SetActive(false);
    }
}
