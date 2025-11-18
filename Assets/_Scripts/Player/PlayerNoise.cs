using UnityEngine;

public class PlayerNoise : MonoBehaviour
{
    public float baseLoudness = 1f; // 1 = normal, 2 = louder
    public float broadcastRadius = 20f; // how far to search for enemies to notify

    // Call this when player makes a noise (sprint, heavy step, shoot, etc.)
    public void MakeNoise(float loudnessMultiplier = 1f)
    {
        float loud = baseLoudness * loudnessMultiplier;
        Collider[] hits = Physics.OverlapSphere(transform.position, broadcastRadius);
        foreach (var c in hits)
        {
            EnemyAI e = c.GetComponent<EnemyAI>();
            if (e != null)
            {
                e.OnPlayerMadeNoise(transform.position, loud);
            }
        }
    }

    // small debug: press N in play mode to broadcast a noise
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            MakeNoise(1.5f); // test loud noise
            Debug.Log("Player made test noise");
        }
    }
}

