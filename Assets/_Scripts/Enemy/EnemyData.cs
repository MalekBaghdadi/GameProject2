using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    public float sightRadius = 15f;
    public float viewAngle = 90f;
    public float audioRadius = 8f;

    public float wanderSpeed = 2f;
    public float chaseSpeed = 4.5f;

    public float attackRange = 1.6f;
    public float attackCooldown = 1.2f;
    public int damage = 10;

    [Range(0f, 1f)]
    public float interestIncreaseRate = 0.5f;
    public float interestDecayRate = 0.2f;
    public float maxInterest = 1f;
}