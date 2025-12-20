using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class FoxHelperController : MonoBehaviour, ISaveable
{
    [Header("Configuration")]
    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private float wanderInterval = 4f;
    [SerializeField] private float itemDetectionRadius = 15f;
    [SerializeField] private float itemStoppingDistance = 1.2f;
    [SerializeField] private float playerAbandonDistance = 25f;
    [SerializeField] private float playerItemProximity = 5f;

    [Header("Bear Warning System")]
    [SerializeField] private float bearDetectionRadius = 20f;
    [SerializeField] private float bearDangerRadius = 12f;
    [SerializeField] private float bearWarningCooldown = 5f;

    [Header("Behavior Tuning")]
    [SerializeField] private float itemShowTimeout = 20f;
    [SerializeField] private float itemMemoryCooldown = 45f;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip barkItemFound;
    [SerializeField] private AudioClip barkBearWarning;
    [SerializeField] private AudioClip whimperRetreat;

    [Header("Audio Wave Settings")]
    [SerializeField] private int itemFoundBarkWaves = 3;
    [SerializeField] private int bearWarningBarkWaves = 2;
    [SerializeField] private int whimperWaves = 2;
    [SerializeField] private float waveIntervalMin = 0.25f;
    [SerializeField] private float waveIntervalMax = 0.45f;
    [SerializeField] private float pitchMin = 0.95f;
    [SerializeField] private float pitchMax = 1.1f;

    private NavMeshAgent agent;
    private Animator animator;
    private AudioSource audioSource;

    private Transform player;
    private Transform bear;

    private Coroutine wanderCoroutine;
    private Coroutine audioWaveCoroutine;

    private GameObject currentItem;
    private Vector3 currentItemPos;
    private float timeAtItem;

    private Dictionary<GameObject, float> shownItems = new Dictionary<GameObject, float>();
    private float lastBearWarningTime = -999f;

    private FoxState state = FoxState.WanderNearPlayer;

    private enum FoxState
    {
        WanderNearPlayer,
        InvestigatingItem,
        ShowingItem,
        FleeingFromBear,
        GuidingToItem
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        player = GameObject.FindWithTag("Player")?.transform;
        bear = GameObject.FindWithTag("Enemy")?.transform;
    }

    private void OnEnable()
    {
        EventManager.Subscribe(EventManager.ON_FOX_HINT_START, OnHintStart);
        EventManager.Subscribe(EventManager.ON_FOX_HINT_END, OnHintEnd);
        EventManager.Subscribe(EventManager.ON_ITEM_COLLECTED, OnItemCollected);
    }

    private void OnDisable()
    {
        EventManager.Unsubscribe(EventManager.ON_FOX_HINT_START, OnHintStart);
        EventManager.Unsubscribe(EventManager.ON_FOX_HINT_END, OnHintEnd);
        EventManager.Unsubscribe(EventManager.ON_ITEM_COLLECTED, OnItemCollected);
    }

    private void Start()
    {
        StartWandering();
    }

    private void Update()
    {
        if (!player) return;

        animator.SetFloat("Speed", agent.velocity.magnitude);

        CheckBear();
        CleanShownItems();

        switch (state)
        {
            case FoxState.WanderNearPlayer: WanderUpdate(); break;
            case FoxState.InvestigatingItem: InvestigatingUpdate(); break;
            case FoxState.ShowingItem: ShowingItemUpdate(); break;
            case FoxState.FleeingFromBear: FleeingUpdate(); break;
            case FoxState.GuidingToItem: GuidingUpdate(); break;
        }
    }

    // ---------------- BEAR ----------------

    private void CheckBear()
    {
        if (!bear) return;

        float dPlayer = Vector3.Distance(player.position, bear.position);
        float dFox = Vector3.Distance(transform.position, bear.position);

        if (dPlayer <= bearDangerRadius)
        {
            if (state != FoxState.FleeingFromBear)
                StartFleeing();
        }
        else if (dFox <= bearDetectionRadius || dPlayer <= bearDetectionRadius)
        {
            if (Time.time - lastBearWarningTime > bearWarningCooldown)
            {
                PlaySoundWaves(barkBearWarning, bearWarningBarkWaves);
                animator.SetTrigger("Bark");
                lastBearWarningTime = Time.time;
            }
        }
    }

    private void StartFleeing()
    {
        StopWander();
        state = FoxState.FleeingFromBear;

        agent.speed *= 1.3f;
        agent.stoppingDistance = 2f;
        PlaySoundWaves(whimperRetreat, whimperWaves);

        animator.SetTrigger("Fear");
    }

    private void FleeingUpdate()
    {
        agent.SetDestination(player.position);

        if (Vector3.Distance(player.position, bear.position) > bearDangerRadius + 5f)
        {
            agent.speed /= 1.3f;
            StartWandering();
        }
    }

    // ---------------- WANDER ----------------

    private void StartWandering()
    {
        state = FoxState.WanderNearPlayer;
        agent.speed = 3.5f;
        agent.stoppingDistance = 0.5f;

        StopWander();
        wanderCoroutine = StartCoroutine(WanderRoutine());
    }

    private IEnumerator WanderRoutine()
    {
        while (state == FoxState.WanderNearPlayer)
        {
            agent.SetDestination(RandomNavSphere(player.position, wanderRadius));
            yield return new WaitForSeconds(wanderInterval);
        }
    }

    private void StopWander()
    {
        if (wanderCoroutine != null)
            StopCoroutine(wanderCoroutine);
    }

    private void WanderUpdate()
    {
        GameObject item = FindNearestCollectible();
        if (item) StartInvestigating(item);
    }

    // ---------------- ITEM ----------------

    private GameObject FindNearestCollectible()
    {
        GameObject[] items = GameObject.FindGameObjectsWithTag("Collectible");
        float best = itemDetectionRadius;
        GameObject result = null;

        foreach (var i in items)
        {
            if (shownItems.ContainsKey(i) && Time.time - shownItems[i] < itemMemoryCooldown)
                continue;

            float d = Vector3.Distance(transform.position, i.transform.position);
            if (d < best)
            {
                best = d;
                result = i;
            }
        }
        return result;
    }

    private void StartInvestigating(GameObject item)
    {
        StopWander();
        state = FoxState.InvestigatingItem;
        currentItem = item;
        currentItemPos = item.transform.position;

        agent.speed = 4.5f;
        agent.stoppingDistance = itemStoppingDistance;
        agent.SetDestination(currentItemPos);
    }

    private void InvestigatingUpdate()
    {
        if (!currentItem) { StartWandering(); return; }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            ArriveAtItem();

        if (Vector3.Distance(transform.position, player.position) > playerAbandonDistance)
            StartWandering();
    }

    private void ArriveAtItem()
    {
        state = FoxState.ShowingItem;
        timeAtItem = 0f;

        PlaySoundWaves(barkItemFound, itemFoundBarkWaves);
        animator.SetTrigger("FoundItem");

        HighlightItem(currentItem);
    }

    private void ShowingItemUpdate()
    {
        if (!currentItem) { StartWandering(); return; }

        timeAtItem += Time.deltaTime;

        if (Vector3.Distance(player.position, currentItemPos) <= playerItemProximity)
        {
            MarkShown(currentItem);
            StartWandering();
        }

        if (timeAtItem >= itemShowTimeout)
        {
            MarkShown(currentItem);
            StartWandering();
        }
    }
    
    private void GuidingUpdate()
    {
        if (agent.pathPending)
            return;

        // If we reached the target point, idle and wait
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.ResetPath();

            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.SetBool("IsGuiding", true);
            }
        }

        // Safety: if player gets too far, abort guiding
        if (player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (distToPlayer > playerAbandonDistance)
            {
                if (animator != null)
                    animator.SetBool("IsGuiding", false);

                StartWandering();
            }
        }
    }


    private void MarkShown(GameObject item)
    {
        shownItems[item] = Time.time;
        UnhighlightItem(item);
    }

    private void CleanShownItems()
    {
        var dead = new List<GameObject>();
        foreach (var kv in shownItems)
            if (!kv.Key || Time.time - kv.Value > itemMemoryCooldown)
                dead.Add(kv.Key);

        foreach (var d in dead)
            shownItems.Remove(d);
    }

    // ---------------- AUDIO ----------------

    private void PlaySoundWaves(AudioClip clip, int waves)
    {
        if (!clip || waves <= 0) return;

        if (audioWaveCoroutine != null)
            StopCoroutine(audioWaveCoroutine);

        audioWaveCoroutine = StartCoroutine(SoundWaveRoutine(clip, waves));
    }

    private IEnumerator SoundWaveRoutine(AudioClip clip, int waves)
    {
        for (int i = 0; i < waves; i++)
        {
            audioSource.pitch = Random.Range(pitchMin, pitchMax);
            audioSource.PlayOneShot(clip);
            yield return new WaitForSeconds(Random.Range(waveIntervalMin, waveIntervalMax));
        }
        audioSource.pitch = 1f;
    }

    // ---------------- EVENTS ----------------

    private void OnHintStart(object[] data)
    {
        if (data.Length > 0 && data[0] is Vector3 pos)
        {
            state = FoxState.GuidingToItem;
            agent.SetDestination(pos);
        }
    }

    private void OnHintEnd(object[] data)
    {
        StartWandering();
    }

    private void OnItemCollected(object[] data)
    {
        if (data.Length > 0 && data[0] is GameObject item && item == currentItem)
            StartWandering();
    }

    // ---------------- UTIL ----------------

    private static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 rand = Random.insideUnitSphere * dist + origin;
        NavMesh.SamplePosition(rand, out NavMeshHit hit, dist, NavMesh.AllAreas);
        return hit.position;
    }

    private void HighlightItem(GameObject item)
    {
        EventManager.TriggerEvent("ON_FOX_MARKED_ITEM", item);
    }

    private void UnhighlightItem(GameObject item)
    {
        EventManager.TriggerEvent("ON_FOX_UNMARKED_ITEM", item);
    }

    // ---------------- SAVE ----------------

    public void SaveData(ref GameData data)
    {
        data.foxPosition = transform.position;
    }

    public void LoadData(GameData data)
    {
        agent.Warp(data.foxPosition);
        StartWandering();
    }
}
