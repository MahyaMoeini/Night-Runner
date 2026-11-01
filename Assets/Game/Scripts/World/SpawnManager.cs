
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Object Pools")]
    [SerializeField] private ObjectPool staticObstaclePool;
    [SerializeField] private ObjectPool movingObstaclePool;
    [SerializeField] private ObjectPool coinPool;
    [SerializeField] private ObjectPool fuelCanPool;

    [Header("Normal Spawn Settings")]
    [SerializeField] private float spawnInterval = 2.5f;
    [SerializeField] private float minSpawnInterval = 1.2f;
    [SerializeField] private float intervalDecreasePerLevel = 0.15f;

    [SerializeField] private float spawnDistance = 78f;
    [SerializeField] private float laneWidth = 3f;

    [Header("Fuel Spawn Settings")]
    [SerializeField] private float fuelSpawnDistance = 52f;
    [SerializeField] private float fuelSpawnSafetyMargin = 0.25f;

    private float timer;
    private float fuelTimer;

    private bool firstFuelSpawned;
    private bool openingTrafficSeeded;
    private int lastFuelLane = -1;

    private void Update()
    {
        if (GameManager.Instance == null ||
            !GameManager.Instance.IsPlaying)
            return;

        if (!openingTrafficSeeded)
        {
            openingTrafficSeeded = true;
            SeedOpeningTraffic();
        }

        SpawnNormalObjects();
        HandleFuelSpawning();
    }

    private float AheadDistance()
    {
        float speed = GameManager.Instance != null
            ? GameManager.Instance.CurrentSpeed
            : 12f;

        float distance = spawnDistance + speed * 0.55f;
        return Mathf.Clamp(distance, 76f, 110f);
    }

    private void SpawnNormalObjects()
    {
        float currentInterval = Mathf.Max(
            minSpawnInterval,
            spawnInterval -
            GameManager.Instance.DifficultyLevel *
            intervalDecreasePerLevel
        );

        timer += Time.deltaTime;

        if (timer >= currentInterval)
        {
            timer = 0f;
            SpawnRandomObject();
        }
    }

    private void HandleFuelSpawning()
    {
        // Spawn the first fuel can immediately
        if (!firstFuelSpawned)
        {
            if (SpawnFuelCan())
            {
                firstFuelSpawned = true;
                fuelTimer = 0f;
            }

            return;
        }

        float interval = CalculateFuelSpawnInterval();

        fuelTimer += Time.deltaTime;

        if (fuelTimer >= interval)
        {
            if (SpawnFuelCan())
            {
                fuelTimer -= interval;
            }
        }
    }

    private float CalculateFuelSpawnInterval()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null || gm.FuelConsumptionRate <= 0f)
            return 8f;

        // Time that one full fuel pickup can cover
        float fuelCoverageTime =
            gm.FuelPickupAmount / gm.FuelConsumptionRate;

        // Maximum difference in travel time
        // between minimum and maximum game speeds
        float travelTimeDifference =
            fuelSpawnDistance *
            (1f / gm.MinimumSpeed - 1f / gm.MaximumSpeed);

        // Keep a safety margin so collecting every can
        // is sufficient to cover fuel consumption.
        float safeInterval =
            fuelCoverageTime -
            travelTimeDifference -
            fuelSpawnSafetyMargin;

        return Mathf.Max(1f, safeInterval);
    }

    private void SpawnRandomObject()
    {
        int type = Random.Range(0, 100);

        if (type < 40)
        {
            SpawnOnLane(staticObstaclePool);
        }
        else if (type < 65)
        {
            SpawnMovingObstacle();
        }
        else
        {
            SpawnOnLane(coinPool);
        }
    }

    private void SpawnOnLane(ObjectPool pool)
    {
        if (pool == null)
            return;

        int lane = Random.Range(0, 3);

        float laneX = (lane - 1) * laneWidth;

        pool.Get(
            new Vector3(
                laneX,
                0f,
                AheadDistance()
            ),
            GameManager.Instance.CurrentSpeed
        );
    }

    private void SpawnMovingObstacle()
    {
        if (movingObstaclePool == null)
            return;

        int firstLane = Random.Range(0, 2);
        int secondLane = firstLane + 1;

        float firstX = (firstLane - 1) * laneWidth;
        float secondX = (secondLane - 1) * laneWidth;

        GameObject obj = movingObstaclePool.Get(
            new Vector3(
                firstX,
                0f,
                AheadDistance()
            ),
            GameManager.Instance.CurrentSpeed
        );

        if (obj == null)
            return;

        MovingObstacle moving =
            obj.GetComponent<MovingObstacle>();

        if (moving != null)
            moving.SetLanePair(firstX, secondX);
    }

    private bool SpawnFuelCan()
    {
        if (fuelCanPool == null)
        {
            Debug.LogError("Fuel Can Pool is not assigned!");
            return false;
        }

        int lane = Random.Range(0, 3);

        // Avoid spawning two consecutive fuel cans
        // in the same lane.
        if (lane == lastFuelLane)
        {
            lane = (lane + Random.Range(1, 3)) % 3;
        }

        float laneX = (lane - 1) * laneWidth;

        GameObject obj = fuelCanPool.Get(
            new Vector3(
                laneX,
                0f,
                AheadDistance() + 12f
            ),
            GameManager.Instance.CurrentSpeed
        );

        if (obj == null)
            return false;

        lastFuelLane = lane;
        return true;
    }

    private void SeedOpeningTraffic()
    {
        float far = AheadDistance();

        SpawnAt(coinPool, -laneWidth, far * 0.82f);
        SpawnAt(coinPool, 0f, far * 0.82f);
        SpawnAt(coinPool, laneWidth, far * 0.82f);
        SpawnAt(staticObstaclePool, laneWidth, far * 0.94f);
        SpawnAt(coinPool, -laneWidth, far);
    }

    private void SpawnAt(ObjectPool pool, float laneX, float distance)
    {
        if (pool == null || GameManager.Instance == null)
            return;

        pool.Get(
            new Vector3(laneX, 0f, distance),
            GameManager.Instance.CurrentSpeed
        );
    }
}
