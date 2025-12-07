using System.Collections.Generic;
using UnityEngine;

public class SparklePool : MonoBehaviour
{
    public static SparklePool Instance;

    [Header("Pooling Settings")]
    public GameObject sparklePrefab;
    public int poolSize = 20;

    private Queue<GameObject> sparklePool = new Queue<GameObject>();

    void Awake()
    {
        // Singleton pattern
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Pre-instantiate sparkles
        for (int i = 0; i < poolSize; i++)
        {
            GameObject s = Instantiate(sparklePrefab);
            s.SetActive(false);
            sparklePool.Enqueue(s);
        }
    }

    // Get a sparkle object from the pool
    public GameObject GetSparkle(Vector3 position)
    {
        GameObject s;

        if (sparklePool.Count > 0)
        {
            s = sparklePool.Dequeue();
        }
        else
        {
            // Optional: Expand pool if needed
            s = Instantiate(sparklePrefab);
        }

        s.transform.position = position;
        s.SetActive(true);
        return s;
    }

    // Return it to the pool
    public void ReturnSparkle(GameObject s)
    {
        s.SetActive(false);
        sparklePool.Enqueue(s);
    }
}