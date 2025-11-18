using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHp = 100;
    public int hp;

    void Awake()
    {
        hp = maxHp;
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        Debug.Log($"Player took {amount} damage. HP = {hp}");
        if (hp <= 0) Die();
    }

    void Die()
    {
        Debug.Log("Player died — handle respawn/game over here.");
        hp = maxHp;
    }
}

