using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class SpaceInvaderMiniGame : MonoBehaviour
{
    [Header("WORLD SPACE GAME BOUNDS")]
    public Transform spaceContainer;
    public float containerWidth = 6f;
    public float containerHeight = 9f;

    [Header("Ship")]
    public Transform ship;
    public float shipSpeed = 4f;
    public float shipCollisionRadius = 0.5f;

    [Header("Shooting")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float bulletSpeed = 14f;
    public float alienCollisionRadius = 0.45f;
    public float bulletScale = 0.2f;

    [Header("Aliens")]
    public GameObject alienPrefab;
    public Transform alienContainer;
    public int rows = 3;
    public int columns = 6;
    public Vector2 alienSpacing = new Vector2(1f, 0.9f);
    public float alienMoveSpeed = 1.2f;
    public float dropDistance = 0.7f;
    public float alienScale = 0.3f;

    [Header("Rounds")]
    public int maxRounds = 3;
    public float spawnHeightOffset = 3.5f;
    [Range(0f, 1f)]
    public float columnSpawnChance = 0.75f;

    [Header("Input")]
    public InputActionReference fireAction;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip alienHitSound;
    public AudioClip gameOverSound;
    public AudioClip gameWinSound;

    [Header("Debug")]
    public bool startOnAwake = false;

    // Public State
    public bool GameWon { get; private set; }

    // Internal
    private readonly List<GameObject> aliens = new();
    private bool isGameActive = false;
    private int currentRound = 0;
    private int alienDirection = 1;

    private float leftBound;
    private float rightBound;
    private float topBound;
    private float bottomBound;

    void Awake()
    {
        CacheBounds();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (startOnAwake)
        {
            ActivateGame();
        }
    }

    void OnEnable()
    {
        if (fireAction != null)
        {
            fireAction.action.Enable();
            fireAction.action.performed += OnFire;
        }
    }

    void OnDisable()
    {
        if (fireAction != null)
            fireAction.action.performed -= OnFire;
    }

    void CacheBounds()
    {
        Vector3 c = spaceContainer.position;
        leftBound   = c.x - containerWidth * 0.5f;
        rightBound  = c.x + containerWidth * 0.5f;
        topBound    = c.y + containerHeight * 0.5f;
        bottomBound = c.y - containerHeight * 0.5f;
    }

    // =====================================================
    // PUBLIC CONTROL API
    // =====================================================
    public void ActivateGame()
    {
        if (spaceContainer != null) 
            spaceContainer.gameObject.SetActive(true);

        StopAllCoroutines();
        ClearAliens();

        isGameActive = true;
        currentRound = 0;
        alienDirection = 1;

        StartNextRound();
    }

    public void DeactivateGame()
    {
        isGameActive = false;
        StopAllCoroutines();
        ClearAliens();
        if (spaceContainer != null) 
            spaceContainer.gameObject.SetActive(false);
    }

    private void EndGame(bool win)
    {
        isGameActive = false;
        GameWon = win;

        if (win)
        {
            if (audioSource != null && gameWinSound != null)
                audioSource.PlayOneShot(gameWinSound);
        }
        else
        {
            if (audioSource != null && gameOverSound != null)
                audioSource.PlayOneShot(gameOverSound);
        }

        StartCoroutine(GameEndSequence());
    }

    IEnumerator GameEndSequence()
    {
        // Blink 4 times
        for (int i = 0; i < 4; i++)
        {
            spaceContainer.gameObject.SetActive(!spaceContainer.gameObject.activeSelf);
            yield return new WaitForSeconds(0.2f);
        }
        
        DeactivateGame();
    }

    // =====================================================
    // GAME FLOW
    // =====================================================
    void StartNextRound()
    {
        currentRound++;

        if (currentRound > maxRounds)
        {
            EndGame(true);
            return;
        }

        SpawnAliens();
    }

    void SpawnAliens()
    {
        ClearAliens();

        float startX = -((columns - 1) * alienSpacing.x) * 0.5f;
        Vector3 basePos = spaceContainer.position + Vector3.up * spawnHeightOffset;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (Random.value > columnSpawnChance) continue;

                GameObject alien = Instantiate(alienPrefab, alienContainer);
                alien.transform.localScale = Vector3.one * alienScale;
                alien.transform.position =
                    basePos +
                    Vector3.right * (startX + c * alienSpacing.x) +
                    Vector3.down * (r * alienSpacing.y);

                aliens.Add(alien);
            }
        }
    }

    void ClearAliens()
    {
        for (int i = aliens.Count - 1; i >= 0; i--)
        {
            if (aliens[i] != null)
                Destroy(aliens[i]);
        }
        aliens.Clear();
    }

    // =====================================================
    // UPDATE
    // =====================================================
    void Update()
    {
        if (!isGameActive) return;

        MoveShip();
        MoveAliens();
        CheckShipCollision();

        if (aliens.Count == 0)
            StartNextRound();
    }

    void MoveShip()
    {
        float t = Mathf.PingPong(Time.time * shipSpeed, 1f);
        float x = Mathf.Lerp(leftBound + 0.6f, rightBound - 0.6f, t);
        ship.position = new Vector3(x, ship.position.y, ship.position.z);
    }

    void MoveAliens()
    {
        alienContainer.position +=
            Vector3.right * alienDirection * alienMoveSpeed * Time.deltaTime;

        bool hitEdge = false;

        foreach (var a in aliens)
        {
            if (a == null) continue;
            float x = a.transform.position.x;

            if ((x > rightBound && alienDirection > 0) ||
                (x < leftBound && alienDirection < 0))
            {
                hitEdge = true;
                break;
            }
        }

        if (hitEdge)
        {
            alienDirection *= -1;
            alienContainer.position += Vector3.down * dropDistance;
        }

        DespawnBelow();
    }

    void DespawnBelow()
    {
        for (int i = aliens.Count - 1; i >= 0; i--)
        {
            if (aliens[i] == null)
            {
                aliens.RemoveAt(i);
                continue;
            }

            if (aliens[i].transform.position.y < bottomBound)
            {
                Destroy(aliens[i]);
                aliens.RemoveAt(i);
            }
        }
    }

    void CheckShipCollision()
    {
        foreach (var a in aliens)
        {
            if (a == null) continue;

            if (Vector3.Distance(a.transform.position, ship.position) < shipCollisionRadius)
            {
                EndGame(false);
                break;
            }
        }
    }

    // =====================================================
    // SHOOTING
    // =====================================================
    void OnFire(InputAction.CallbackContext ctx)
    {
        if (!isGameActive || ctx.interaction is not MultiTapInteraction) return;
        Fire();
    }

    void Fire()
    {
        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);

        // Parent to spaceContainer so it hides with the game
        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity, spaceContainer);
        bullet.transform.localScale = Vector3.one * bulletScale;
        StartCoroutine(MoveBullet(bullet.transform));
    }

    IEnumerator MoveBullet(Transform bullet)
    {
        Vector3 prev = bullet.position;

        while (bullet != null && isGameActive)
        {
            Vector3 next = bullet.position + Vector3.up * bulletSpeed * Time.deltaTime;

            for (int i = aliens.Count - 1; i >= 0; i--)
            {
                var a = aliens[i];
                if (a == null)
                {
                    aliens.RemoveAt(i);
                    continue;
                }

                float d = DistancePointToSegment(a.transform.position, prev, next);
                if (d <= alienCollisionRadius)
                {
                    if (audioSource != null && alienHitSound != null)
                        audioSource.PlayOneShot(alienHitSound);

                    Destroy(a);
                    aliens.RemoveAt(i);
                    Destroy(bullet.gameObject);
                    yield break;
                }
            }

            bullet.position = next;
            prev = next;

            if (bullet.position.y > topBound)
            {
                Destroy(bullet.gameObject);
                yield break;
            }

            yield return null;
        }
    }

    float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(p - a, ab) / Vector3.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        return Vector3.Distance(p, a + ab * t);
    }
}
