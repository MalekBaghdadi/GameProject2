using UnityEngine;

public class PlayerDamageTester : MonoBehaviour
{
    public int damagePerKey = 10;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            var ph = GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damagePerKey);
        }
    }
}

