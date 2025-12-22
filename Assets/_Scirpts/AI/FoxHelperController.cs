using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class FoxHelperController : MonoBehaviour, ISaveable
{
    // ================= CONFIG =================

    [Header("Core References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform cabin;

    [Tooltip("Assign ALL collectible item transforms here")]
    [SerializeField] private List<Transform> itemTargets = new();

    [Header("Movement")]
    [SerializeField] private float guidingRadius = 2.5f;
    [SerializeField] private float followSpeed = 4f;
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Arrival Thresholds")]
    [SerializeField] private float reachedTargetDistance = 1.5f;
    [SerializeField] private float reachedCabinDistance = 2f;

    // ================= AUDIO =================

    [Header("Audio")]
    [SerializeField] private List<AudioClip> itemFoundBarks = new();
    [SerializeField] private int barkWaves = 3;

    [SerializeField] private float waveIntervalMin = 0.25f;
    [SerializeField] private float waveIntervalMax = 0.45f;
    [SerializeField] private float pitchMin = 0.95f;
    [SerializeField] private float pitchMax = 1.1f;

    // ================= COMPONENTS =================

    private NavMeshAgent agent;
    private Animator animator;
    private AudioSource audioSource;

    // ================= STATE =================

    private Queue<Transform> itemQueue;
    private Transform currentTarget;

    private enum FoxState
    {
        GuidingToItem,
        GuidingToCabin,
        Idle
    }

    private FoxState state = FoxState.Idle;
    private Coroutine audioCoroutine;

    // ================= UNITY =================

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        agent.stoppingDistance = 0.2f;
        agent.updateRotation = false;
    }

    private void OnEnable()
    {
        EventManager.Subscribe(EventManager.ON_ITEM_COLLECTED, OnItemCollected);
    }

    private void OnDisable()
    {
        EventManager.Unsubscribe(EventManager.ON_ITEM_COLLECTED, OnItemCollected);
    }

    private void Start()
    {
        agent.speed = followSpeed;
        BuildItemQueue();
        AssignNextItem();
    }

    private void Update()
    {
        agent.speed = followSpeed; // allows runtime tuning
        animator.SetFloat("Speed", agent.velocity.magnitude);

        if (state == FoxState.Idle || currentTarget == null)
            return;

        UpdateGuidingPosition();
        UpdateRotation();
        CheckArrival();
    }

    // ================= GUIDING =================

    private void UpdateGuidingPosition()
    {
        Vector3 direction = (currentTarget.position - player.position).normalized;
        Vector3 desiredPos = player.position + direction * guidingRadius;

        if (NavMesh.SamplePosition(desiredPos, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void UpdateRotation()
    {
        Vector3 lookDir = agent.velocity;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(lookDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotationSpeed
        );
    }

    private void CheckArrival()
    {
        float distanceToTarget = Vector3.Distance(player.position, currentTarget.position);

        if (state == FoxState.GuidingToItem && distanceToTarget <= reachedTargetDistance)
        {
            // Waiting for item collection event
            animator.SetBool("IsGuiding", false);
        }
        else if (state == FoxState.GuidingToCabin && distanceToTarget <= reachedCabinDistance)
        {
            OnReachedCabin();
        }
        else
        {
            animator.SetBool("IsGuiding", true);
        }
    }

    // ================= FLOW =================

    private void BuildItemQueue()
    {
        List<Transform> temp = new(itemTargets);

        for (int i = 0; i < temp.Count; i++)
        {
            int r = Random.Range(i, temp.Count);
            (temp[i], temp[r]) = (temp[r], temp[i]);
        }

        itemQueue = new Queue<Transform>(temp);
    }

    private void AssignNextItem()
    {
        if (itemQueue.Count == 0)
        {
            state = FoxState.Idle;
            currentTarget = null;
            return;
        }

        currentTarget = itemQueue.Dequeue();
        state = FoxState.GuidingToItem;

        PlayBarks();
        animator.SetTrigger("FoundItem");
    }

    private void GuideToCabin()
    {
        currentTarget = cabin;
        state = FoxState.GuidingToCabin;
    }

    private void OnReachedCabin()
    {
        AssignNextItem();
    }

    // ================= EVENTS =================

    private void OnItemCollected(object[] _)
    {
        if (state == FoxState.GuidingToItem)
        {
            GuideToCabin();
        }
    }

    // ================= AUDIO =================

    private void PlayBarks()
    {
        if (itemFoundBarks.Count == 0)
            return;

        if (audioCoroutine != null)
            StopCoroutine(audioCoroutine);

        audioCoroutine = StartCoroutine(BarkRoutine());
    }

    private IEnumerator BarkRoutine()
    {
        for (int i = 0; i < barkWaves; i++)
        {
            audioSource.pitch = Random.Range(pitchMin, pitchMax);
            audioSource.PlayOneShot(itemFoundBarks[Random.Range(0, itemFoundBarks.Count)]);
            yield return new WaitForSeconds(Random.Range(waveIntervalMin, waveIntervalMax));
        }

        audioSource.pitch = 1f;
    }

    // ================= SAVE =================

    public void SaveData(ref GameData data)
    {
        data.foxPosition = transform.position;
    }

    public void LoadData(GameData data)
    {
        agent.Warp(data.foxPosition);
        BuildItemQueue();
        AssignNextItem();
    }
}
