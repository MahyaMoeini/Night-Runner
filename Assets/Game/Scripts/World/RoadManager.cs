
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    [Header("Road Settings")]
    [SerializeField] private ObjectPool roadPool;
    [SerializeField] private float segmentLength = 30f;
    [SerializeField] private float roadSpeed = 12f;

    private GameObject[] segments;

    private const int SegmentCount = 3;

    private void Awake()
    {
        // Three segments exist before the first gameplay frame, including the menu.
        segments = new GameObject[SegmentCount];

        for (int i = 0; i < SegmentCount; i++)
        {
            float z = (i - 1) * segmentLength;

            segments[i] = roadPool.Get(
                new Vector3(0f, 0f, z)
            );
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null &&
            !GameManager.Instance.IsPlaying)
            return;

        float currentSpeed = GameManager.Instance != null
            ? GameManager.Instance.CurrentSpeed
            : roadSpeed;

        foreach (GameObject segment in segments)
        {
            if (segment == null)
                continue;

            segment.transform.Translate(
                Vector3.back * currentSpeed * Time.deltaTime,
                Space.World
            );
        }

        for (int i = 0; i < SegmentCount; i++)
        {
            if (segments[i] == null)
                continue;

            // Recycle once the segment has passed the camera, while the
            // other two still cover the road in front of the player.
            if (segments[i].transform.position.z < -(segmentLength + 6f))
            {
                float furthestZ = float.MinValue;

                for (int j = 0; j < SegmentCount; j++)
                {
                    if (j == i || segments[j] == null)
                        continue;

                    furthestZ = Mathf.Max(
                        furthestZ,
                        segments[j].transform.position.z
                    );
                }

                roadPool.Return(segments[i]);

                segments[i] = roadPool.Get(
                    new Vector3(
                        0f,
                        0f,
                        furthestZ + segmentLength
                    )
                );

                AlignSegments();
            }
        }
    }

    private void AlignSegments()
    {
        System.Array.Sort(
            segments,
            (a, b) => a.transform.position.z.CompareTo(b.transform.position.z)
        );

        float origin = segments[0].transform.position.z;

        for (int i = 0; i < SegmentCount; i++)
        {
            if (segments[i] == null)
                continue;

            Vector3 position = segments[i].transform.position;
            position.x = 0f;
            position.y = 0f;
            position.z = origin + i * segmentLength;
            segments[i].transform.position = position;
        }
    }
}
