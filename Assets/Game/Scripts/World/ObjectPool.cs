
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int initialSize = 5;

    private Queue<GameObject> available =
        new Queue<GameObject>();

    private HashSet<GameObject> stored =
        new HashSet<GameObject>();

    private void Awake()
    {
        if (prefab == null)
        {
            Debug.LogError("Pool Prefab is missing!", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = Instantiate(prefab, transform);

            obj.SetActive(false);
            available.Enqueue(obj);
            stored.Add(obj);
        }
    }

    public GameObject Get(Vector3 position, float speed = 0f)
    {
        if (available.Count == 0)
        {
            Debug.LogWarning("Object Pool is empty!", this);
            return null;
        }

        GameObject obj = available.Dequeue();
        stored.Remove(obj);

        obj.transform.SetPositionAndRotation(
            position,
            Quaternion.identity
        );

        Interactable item = obj.GetComponent<Interactable>();

        if (item != null)
            item.Initialize(this, speed);

        obj.SetActive(true);

        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null || stored.Contains(obj))
            return;

        obj.SetActive(false);
        obj.transform.SetParent(transform, true);

        available.Enqueue(obj);
        stored.Add(obj);
    }
}
