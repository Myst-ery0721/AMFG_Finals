using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class LinePhysicsGameComplete : MonoBehaviour
{
    #region Inspector Settings
    [Header("Platform (line rectangle)")]
    public Vector3 platformCenter = new Vector3(0f, -1f, 0f);
    public Vector3 platformSize = new Vector3(20f, 1f, 4f);

    [Header("Player Cube Settings")]
    public Vector3 cubeSize = Vector3.one;
    public Vector3 cubeStartPosition = new Vector3(0f, 2f, 0f);
    public int maxPlayerHP = 3;
    public int maxPlayerLives = 3;
    public float damageInvincibilityTime = 1f;

    [Header("Physics")]
    public float gravity = -20f;
    public float jumpVelocity = 10f;
    public float terminalVelocity = -50f;
    public float horizontalSpeed = 5f;

    [Header("Fireball Settings")]
    public float fireballSpeed = 10f;
    public float fireballSize = 0.5f;
    public KeyCode shootKey = KeyCode.F;

    [Header("Enemy Settings")]
    public int enemyCount = 3;
    public Vector3 enemySize = new Vector3(0.8f, 0.8f, 0.8f);
    public float enemySpeed = 2f;
    public float enemyPatrolRange = 3f;

    [Header("Obstacle Settings")]
    public int normalObstacleCount = 5;
    public int instakillObstacleCount = 2;
    public int spikeCount = 3;
    public Vector3 obstacleSize = new Vector3(1.5f, 0.15f, 1.5f);
    public Vector3 instakillSize = new Vector3(2f, 0.2f, 2f);
    public Vector3 spikeSize = new Vector3(0.8f, 1.2f, 0.8f);

    [Header("Healing Fireball Settings")]
    public int healingFireballCount = 3;
    public float healingFireballSize = 0.6f;
    public int healAmount = 1;

    [Header("Rendering")]
    public float lineWidth = 0.02f;
    public Color cubeColorIdle = Color.white;
    public Color cubeColorCollision = Color.green;
    public Color cubeColorDamaged = Color.red;
    public Color platformColor = Color.gray;
    public Color enemyColor = Color.red;
    public Color obstacleColor = new Color(0.5f, 0.3f, 0.1f);
    public Color instakillColor = new Color(0.8f, 0f, 0f);
    public Color fireballColor = Color.yellow;
    public Color spikeColor = new Color(1f, 0f, 0f);
    public Color healingFireballColor = new Color(0f, 1f, 0.5f);

    [Header("UI Settings")]
    public Vector2 uiOffset = new Vector2(10, 10);
    public int uiFontSize = 24;
    public Color uiColor = Color.white;
    #endregion

    #region Game State
    // Player state
    Vector3 cubePosition;
    Vector3 velocity;
    bool isGrounded = false;
    bool collidedThisFrame = false;
    int playerHP;
    int playerLives;
    bool isDamageInvincible = false;
    float damageInvincibilityTimer = 0f;
    Vector3 facingDirection = Vector3.right; // Track which direction player is facing

    // Game state
    float gameTimer = 0f;
    bool gameWon = false;
    bool gameLost = false;
    bool gameStarted = false;

    // Entities
    List<Enemy> enemies = new List<Enemy>();
    List<Obstacle> obstacles = new List<Obstacle>();
    List<HealingFireball> healingFireballs = new List<HealingFireball>();
    List<Fireball> fireballs = new List<Fireball>();

    // Rendering resources
    Mesh cubeLineMesh;
    Mesh platformLineMesh;
    Mesh enemyMesh;
    Mesh obstacleMesh;
    Mesh instakillMesh;
    Mesh spikeMesh;
    Mesh healingFireballMesh;
    Mesh fireballMesh;
    Material lineMaterial;
    Camera mainCamera;

    // UI
    GUIStyle uiStyle;
    #endregion

    #region Data Classes
    class Enemy
    {
        public Vector3 position;
        public Vector3 size;
        public float speed;
        public Vector3 patrolStart;
        public Vector3 patrolEnd;
        public bool movingRight;
        public bool isAlive = true;
        public float rotationAngle = 0f;
    }

    class Obstacle
    {
        public Vector3 position;
        public Vector3 size;
        public bool isInstakill;
        public bool isSpike;
        public float rotationAngle = 0f;
    }

    class HealingFireball
    {
        public Vector3 position;
        public Vector3 size;
        public bool collected = false;
        public float rotationAngle = 0f;
    }

    class Fireball
    {
        public Vector3 position;
        public Vector3 velocity;
        public float size;
        public bool active = true;
    }
    #endregion

    void Awake()
    {
        InitializeGame();
        InitializeMaterials();
        InitializeMeshes();
    }

    void InitializeGame()
    {
        cubePosition = cubeStartPosition;
        velocity = Vector3.zero;
        playerHP = maxPlayerHP;
        playerLives = maxPlayerLives;
        gameTimer = 0f;
        gameWon = false;
        gameLost = false;
        gameStarted = true;
        facingDirection = Vector3.right;

        // Reset invincibility states
        isDamageInvincible = false;
        damageInvincibilityTimer = 0f;

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = new GameObject("MainCamera");
            mainCamera = camObj.AddComponent<Camera>();
            mainCamera.transform.position = new Vector3(0f, 3f, -10f);
            mainCamera.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        }

        // Calculate platform top once for all spawning
        float platformTopY = platformCenter.y + platformSize.y * 0.5f;

        // Spawn enemies - all on same Z plane for side-scrolling
        enemies.Clear();

        for (int i = 0; i < enemyCount; i++)
        {
            Enemy e = new Enemy();
            e.size = enemySize;
            e.speed = enemySpeed;
            float spacing = 6f;
            float xPos = -8f + (i * spacing);
            e.position = new Vector3(xPos, platformTopY + e.size.y * 0.5f, 0f); // All at Z=0
            e.patrolStart = e.position + Vector3.left * enemyPatrolRange;
            e.patrolEnd = e.position + Vector3.right * enemyPatrolRange;
            e.movingRight = Random.value > 0.5f;
            enemies.Add(e);
        }

        // Spawn obstacles - all on same Z plane
        obstacles.Clear();

        Vector3[] normalObstaclePositions = new Vector3[]
        {
            new Vector3(4f, platformTopY + obstacleSize.y * 0.5f, 0f),
            new Vector3(8f, platformTopY + obstacleSize.y * 0.5f, 0f),
            new Vector3(11f, platformTopY + obstacleSize.y * 0.5f, 0f),
            new Vector3(-3f, platformTopY + obstacleSize.y * 0.5f, 0f),
            new Vector3(14f, platformTopY + obstacleSize.y * 0.5f, 0f)
        };

        for (int i = 0; i < Mathf.Min(normalObstacleCount, normalObstaclePositions.Length); i++)
        {
            Obstacle o = new Obstacle();
            o.size = obstacleSize;
            o.isInstakill = false;
            o.position = normalObstaclePositions[i];
            obstacles.Add(o);
        }

        // Instakill obstacles - on same Z plane
        Vector3[] instakillPositions = new Vector3[]
        {
            new Vector3(16f, platformTopY + instakillSize.y * 0.5f, 0f),
            new Vector3(-5f, platformTopY + instakillSize.y * 0.5f, 0f)
        };

        for (int i = 0; i < Mathf.Min(instakillObstacleCount, instakillPositions.Length); i++)
        {
            Obstacle o = new Obstacle();
            o.size = instakillSize;
            o.isInstakill = true;
            o.isSpike = false;
            o.position = instakillPositions[i];
            obstacles.Add(o);
        }

        // Spawn spike obstacles (instakill, spinning pyramids)
        Vector3[] spikePositions = new Vector3[]
        {
            new Vector3(-6f, platformTopY + spikeSize.y * 0.5f, 0f),
            new Vector3(6f, platformTopY + spikeSize.y * 0.5f, 0f),
            new Vector3(10f, platformTopY + spikeSize.y * 0.5f, 0f)
        };

        for (int i = 0; i < Mathf.Min(spikeCount, spikePositions.Length); i++)
        {
            Obstacle o = new Obstacle();
            o.size = spikeSize;
            o.isInstakill = true;
            o.isSpike = true;
            o.position = spikePositions[i];
            o.rotationAngle = 0f;
            obstacles.Add(o);
        }

        // Spawn healing fireballs (collectible health items)
        healingFireballs.Clear();

        Vector3[] healingPositions = new Vector3[]
        {
            new Vector3(-4f, platformTopY + 1f, 0f),
            new Vector3(2f, platformTopY + 1f, 0f),
            new Vector3(12f, platformTopY + 1f, 0f)
        };

        for (int i = 0; i < Mathf.Min(healingFireballCount, healingPositions.Length); i++)
        {
            HealingFireball hf = new HealingFireball();
            hf.size = Vector3.one * healingFireballSize;
            hf.position = healingPositions[i];
            hf.collected = false;
            hf.rotationAngle = 0f;
            healingFireballs.Add(hf);
        }

        fireballs.Clear();
    }

    void InitializeMaterials()
    {
        Shader s = Shader.Find("Unlit/Color");
        if (s == null) s = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(s);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
    }

    void InitializeMeshes()
    {
        cubeLineMesh = CreateCubeLineMesh(cubeSize);
        platformLineMesh = CreatePlatformLineMesh(platformCenter, platformSize);
        enemyMesh = CreateCubeLineMesh(enemySize);
        obstacleMesh = CreateCubeLineMesh(obstacleSize);
        instakillMesh = CreateCubeLineMesh(instakillSize);
        spikeMesh = CreatePyramidLineMesh(spikeSize);
        healingFireballMesh = CreateSphereLineMesh(healingFireballSize);
        fireballMesh = CreateSphereLineMesh(fireballSize);
    }

    void OnDestroy()
    {
        if (cubeLineMesh != null) DestroyImmediate(cubeLineMesh);
        if (platformLineMesh != null) DestroyImmediate(platformLineMesh);
        if (enemyMesh != null) DestroyImmediate(enemyMesh);
        if (obstacleMesh != null) DestroyImmediate(obstacleMesh);
        if (instakillMesh != null) DestroyImmediate(instakillMesh);
        if (spikeMesh != null) DestroyImmediate(spikeMesh);
        if (healingFireballMesh != null) DestroyImmediate(healingFireballMesh);
        if (fireballMesh != null) DestroyImmediate(fireballMesh);
        if (lineMaterial != null) DestroyImmediate(lineMaterial);
    }

    void Update()
    {
        gameTimer += Time.deltaTime;

        // Allow restart anytime
        if (Input.GetKeyDown(KeyCode.R))
        {
            InitializeGame();
            InitializeMeshes();
            return; // Exit early after restart
        }

        if (gameWon || gameLost) return;

        // Update damage invincibility timer
        if (isDamageInvincible)
        {
            damageInvincibilityTimer -= Time.deltaTime;
            if (damageInvincibilityTimer <= 0f)
            {
                isDamageInvincible = false;
                damageInvincibilityTimer = 0f;
            }
        }

        HandlePlayerInput();
        UpdateEnemies();
        UpdateHealingFireballs();
        UpdateSpikes();
        UpdateFireballs();
    }

    void HandlePlayerInput()
    {
        // Horizontal movement only (left/right)
        float h = Input.GetAxis("Horizontal");

        // Update facing direction based on movement input
        if (h > 0.1f)
        {
            facingDirection = Vector3.right;
        }
        else if (h < -0.1f)
        {
            facingDirection = Vector3.left;
        }

        Vector3 horiz = new Vector3(h, 0f, 0f) * horizontalSpeed * Time.deltaTime;
        Vector3 newPos = cubePosition + horiz;

        // Check collision with obstacles before moving
        if (!CheckObstacleCollision(newPos, cubeSize))
        {
            cubePosition = newPos;
        }

        // Jump input
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = jumpVelocity;
            isGrounded = false;
            collidedThisFrame = false;
        }

        // Shoot fireball
        if (Input.GetKeyDown(shootKey))
        {
            ShootFireball();
        }
    }

    void ShootFireball()
    {
        Fireball fb = new Fireball();
        // Spawn fireball slightly ahead of player in facing direction
        fb.position = cubePosition + facingDirection * (cubeSize.x * 0.5f + fireballSize);
        fb.velocity = facingDirection * fireballSpeed;
        fb.size = fireballSize;
        fb.active = true;
        fireballs.Add(fb);
    }

    void UpdateEnemies()
    {
        foreach (Enemy e in enemies)
        {
            if (!e.isAlive) continue;

            Vector3 movement = Vector3.zero;

            // Patrol movement
            if (e.movingRight)
            {
                movement = Vector3.right * e.speed * Time.deltaTime;
                Vector3 newPos = e.position + movement;

                // Check if would hit obstacle
                bool hitObstacle = false;
                foreach (Obstacle o in obstacles)
                {
                    if (CheckAABBCollision(newPos, e.size, o.position, o.size))
                    {
                        hitObstacle = true;
                        break;
                    }
                }

                if (!hitObstacle && newPos.x < e.patrolEnd.x)
                {
                    e.position = newPos;
                }
                else
                {
                    e.movingRight = false;
                }
            }
            else
            {
                movement = Vector3.left * e.speed * Time.deltaTime;
                Vector3 newPos = e.position + movement;

                // Check if would hit obstacle
                bool hitObstacle = false;
                foreach (Obstacle o in obstacles)
                {
                    if (CheckAABBCollision(newPos, e.size, o.position, o.size))
                    {
                        hitObstacle = true;
                        break;
                    }
                }

                if (!hitObstacle && newPos.x > e.patrolStart.x)
                {
                    e.position = newPos;
                }
                else
                {
                    e.movingRight = true;
                }
            }

            e.rotationAngle += 30f * Time.deltaTime;
        }
    }

    void UpdateHealingFireballs()
    {
        foreach (HealingFireball hf in healingFireballs)
        {
            if (!hf.collected)
            {
                hf.rotationAngle += 100f * Time.deltaTime;
            }
        }
    }

    void UpdateSpikes()
    {
        foreach (Obstacle o in obstacles)
        {
            if (o.isSpike)
            {
                o.rotationAngle += 120f * Time.deltaTime;
            }
        }
    }

    void UpdateFireballs()
    {
        for (int i = fireballs.Count - 1; i >= 0; i--)
        {
            Fireball fb = fireballs[i];
            if (!fb.active) continue;

            fb.position += fb.velocity * Time.deltaTime;

            // Check if out of bounds
            if (Mathf.Abs(fb.position.x) > 30f || Mathf.Abs(fb.position.z) > 30f)
            {
                fb.active = false;
                continue;
            }

            // Check collision with enemies
            foreach (Enemy e in enemies)
            {
                if (!e.isAlive) continue;
                if (CheckAABBCollision(fb.position, Vector3.one * fb.size, e.position, e.size))
                {
                    e.isAlive = false;
                    fb.active = false;
                    break;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (gameWon || gameLost) return;

        float dt = Time.fixedDeltaTime;

        // Apply gravity
        velocity.y += gravity * dt;
        if (velocity.y < terminalVelocity) velocity.y = terminalVelocity;

        Vector3 pos = cubePosition;
        Vector3 nextPos = pos + velocity * dt;

        // Platform collision
        HandlePlatformCollision(ref nextPos, dt);

        // Check all collisions
        CheckAllCollisions();

        // Check win condition - reaching either edge of the platform
        float platformLeftEdge = platformCenter.x - platformSize.x * 0.5f;
        float platformRightEdge = platformCenter.x + platformSize.x * 0.5f;

        if (cubePosition.x >= platformRightEdge - 1f || cubePosition.x <= platformLeftEdge + 1f)
        {
            gameWon = true;
        }

        // Check if player fell off
        if (cubePosition.y < -10f)
        {
            TakeDamage(playerHP); // Die
        }
    }

    void HandlePlatformCollision(ref Vector3 nextPos, float dt)
    {
        Vector3 pos = cubePosition;
        float cubeBottomNow = pos.y - cubeSize.y * 0.5f;
        float cubeBottomNext = nextPos.y - cubeSize.y * 0.5f;
        float platformTop = platformCenter.y + platformSize.y * 0.5f;

        // Swept XZ AABB
        float cubeMinX = Mathf.Min(pos.x - cubeSize.x * 0.5f, nextPos.x - cubeSize.x * 0.5f);
        float cubeMaxX = Mathf.Max(pos.x + cubeSize.x * 0.5f, nextPos.x + cubeSize.x * 0.5f);
        float cubeMinZ = Mathf.Min(pos.z - cubeSize.z * 0.5f, nextPos.z - cubeSize.z * 0.5f);
        float cubeMaxZ = Mathf.Max(pos.z + cubeSize.z * 0.5f, nextPos.z + cubeSize.z * 0.5f);

        float platMinX = platformCenter.x - platformSize.x * 0.5f;
        float platMaxX = platformCenter.x + platformSize.x * 0.5f;
        float platMinZ = platformCenter.z - platformSize.z * 0.5f;
        float platMaxZ = platformCenter.z + platformSize.z * 0.5f;

        bool overlapXZ = (cubeMaxX > platMinX) && (cubeMinX < platMaxX)
                       && (cubeMaxZ > platMinZ) && (cubeMinZ < platMaxZ);

        collidedThisFrame = false;
        bool platformCollision = false;

        if (overlapXZ && ((cubeBottomNow > platformTop && cubeBottomNext <= platformTop) ||
                         (cubeBottomNow < platformTop && cubeBottomNext >= platformTop)))
        {
            float totalDy = cubeBottomNext - cubeBottomNow;
            float t = 0f;
            if (Mathf.Abs(totalDy) > 1e-6f)
                t = (platformTop - cubeBottomNow) / totalDy;
            t = Mathf.Clamp01(t);

            Vector3 contactPos = Vector3.Lerp(pos, nextPos, t);

            if (cubeBottomNow > platformTop && cubeBottomNext <= platformTop)
            {
                contactPos.y = platformTop + cubeSize.y * 0.5f;
                cubePosition = contactPos;
                velocity.y = 0f;
                isGrounded = true;
                collidedThisFrame = true;
                platformCollision = true;
            }
            else if (cubeBottomNow < platformTop && cubeBottomNext >= platformTop)
            {
                contactPos.y = platformTop - cubeSize.y * 0.5f;
                cubePosition = contactPos;
                velocity.y = 0f;
                isGrounded = false;
                collidedThisFrame = true;
                platformCollision = true;
            }
        }

        // Check collision with obstacles (treating them as platforms too)
        if (!platformCollision)
        {
            bool obstacleCollision = HandleObstacleVerticalCollision(pos, nextPos);
            if (!obstacleCollision)
            {
                cubePosition = nextPos;
                float bottom = cubePosition.y - cubeSize.y * 0.5f;
                if (bottom > platformTop + 0.001f)
                {
                    if (isGrounded) isGrounded = false;
                }
            }
        }
    }

    bool HandleObstacleVerticalCollision(Vector3 pos, Vector3 nextPos)
    {
        float cubeBottomNow = pos.y - cubeSize.y * 0.5f;
        float cubeBottomNext = nextPos.y - cubeSize.y * 0.5f;
        float cubeTopNow = pos.y + cubeSize.y * 0.5f;
        float cubeTopNext = nextPos.y + cubeSize.y * 0.5f;

        foreach (Obstacle o in obstacles)
        {
            // Skip spikes - they should only kill, not act as platforms
            if (o.isSpike) continue;

            float obstMinX = o.position.x - o.size.x * 0.5f;
            float obstMaxX = o.position.x + o.size.x * 0.5f;
            float obstMinZ = o.position.z - o.size.z * 0.5f;
            float obstMaxZ = o.position.z + o.size.z * 0.5f;
            float obstTop = o.position.y + o.size.y * 0.5f;
            float obstBottom = o.position.y - o.size.y * 0.5f;

            // Check if player overlaps with obstacle in XZ plane (with tolerance)
            float tolerance = 0.05f;
            float cubeMinX = Mathf.Min(pos.x - cubeSize.x * 0.5f, nextPos.x - cubeSize.x * 0.5f) + tolerance;
            float cubeMaxX = Mathf.Max(pos.x + cubeSize.x * 0.5f, nextPos.x + cubeSize.x * 0.5f) - tolerance;
            float cubeMinZ = Mathf.Min(pos.z - cubeSize.z * 0.5f, nextPos.z - cubeSize.z * 0.5f) + tolerance;
            float cubeMaxZ = Mathf.Max(pos.z + cubeSize.z * 0.5f, nextPos.z + cubeSize.z * 0.5f) - tolerance;

            bool overlapXZ = (cubeMaxX > obstMinX) && (cubeMinX < obstMaxX)
                           && (cubeMaxZ > obstMinZ) && (cubeMinZ < obstMaxZ);

            if (!overlapXZ) continue;

            // Landing on top of obstacle
            if (cubeBottomNow >= obstTop - 0.1f && cubeBottomNext < obstTop + 0.1f && velocity.y <= 0f)
            {
                cubePosition.x = nextPos.x;
                cubePosition.z = nextPos.z;
                cubePosition.y = obstTop + cubeSize.y * 0.5f;
                velocity.y = 0f;
                isGrounded = true;
                collidedThisFrame = true;
                return true;
            }
            // Hitting bottom of obstacle from below
            else if (cubeTopNow <= obstBottom + 0.1f && cubeTopNext > obstBottom - 0.1f && velocity.y > 0f)
            {
                cubePosition.x = nextPos.x;
                cubePosition.z = nextPos.z;
                cubePosition.y = obstBottom - cubeSize.y * 0.5f;
                velocity.y = 0f;
                collidedThisFrame = true;
                return true;
            }
        }

        return false;
    }

    void CheckAllCollisions()
    {
        // Check enemy collisions
        foreach (Enemy e in enemies)
        {
            if (!e.isAlive) continue;
            if (CheckAABBCollision(cubePosition, cubeSize, e.position, e.size))
            {
                if (!isDamageInvincible)
                {
                    TakeDamage(1);
                }
            }
        }

        // Check powerup collisions
        foreach (HealingFireball hf in healingFireballs)
        {
            if (hf.collected) continue;
            if (CheckAABBCollision(cubePosition, cubeSize, hf.position, hf.size))
            {
                CollectHealingFireball(hf);
            }
        }

        // Check instakill obstacle collisions
        foreach (Obstacle o in obstacles)
        {
            if (!o.isInstakill) continue;

            if (CheckAABBCollision(cubePosition, cubeSize, o.position, o.size))
            {
                if (o.isSpike)
                {
                    // Spikes = immediate game over
                    gameLost = true;
                }
                else
                {
                    // Other instakill obstacles = normal death/respawn
                    TakeDamage(playerHP);
                }
            }
        }
    }

    bool CheckObstacleCollision(Vector3 pos, Vector3 size)
    {
        foreach (Obstacle o in obstacles)
        {
            // Skip spikes - they should only kill, not block movement
            if (o.isSpike) continue;

            if (CheckAABBCollision(pos, size, o.position, o.size))
            {
                return true;
            }
        }
        return false;
    }

    bool CheckAABBCollision(Vector3 pos1, Vector3 size1, Vector3 pos2, Vector3 size2)
    {
        Vector3 min1 = pos1 - size1 * 0.5f;
        Vector3 max1 = pos1 + size1 * 0.5f;
        Vector3 min2 = pos2 - size2 * 0.5f;
        Vector3 max2 = pos2 + size2 * 0.5f;

        // Add small epsilon for edge cases
        float epsilon = 0.01f;

        return (min1.x < max2.x - epsilon && max1.x > min2.x + epsilon) &&
               (min1.y < max2.y - epsilon && max1.y > min2.y + epsilon) &&
               (min1.z < max2.z - epsilon && max1.z > min2.z + epsilon);
    }

    void CollectHealingFireball(HealingFireball hf)
    {
        hf.collected = true;
        playerHP = Mathf.Min(playerHP + healAmount, maxPlayerHP);
    }

    void TakeDamage(int damage)
    {
        if (isDamageInvincible) return;

        playerHP -= damage;

        Debug.Log($"Took {damage} damage! HP: {playerHP}, Lives: {playerLives}");

        isDamageInvincible = true;
        damageInvincibilityTimer = damageInvincibilityTime;

        if (playerHP <= 0)
        {
            playerLives--;

            Debug.Log($"Died! Lives remaining: {playerLives}");

            // Check if game over
            if (playerLives <= 0)
            {
                // No more lives - GAME OVER
                playerLives = 0; // Ensure it doesn't go negative
                gameLost = true;
                Debug.Log("GAME OVER!");
            }
            else
            {
                // Still have lives - Respawn
                playerHP = maxPlayerHP;
                cubePosition = cubeStartPosition;
                velocity = Vector3.zero;
                isGrounded = false;

                // Reset damage invincibility after a short grace period
                isDamageInvincible = true;
                damageInvincibilityTimer = 2f; // Give 2 seconds of invincibility after respawn

                Debug.Log("Respawning with invincibility...");
            }
        }
    }

    void LateUpdate()
    {
        if (cubeLineMesh == null) cubeLineMesh = CreateCubeLineMesh(cubeSize);
        UpdateCubeMeshVertices(cubeLineMesh, cubePosition, cubeSize);
        if (platformLineMesh == null) platformLineMesh = CreatePlatformLineMesh(platformCenter, platformSize);
    }

    void OnRenderObject()
    {
        if (lineMaterial == null) return;

        // Draw platform
        DrawMeshWithFrustumCulling(platformLineMesh, platformColor, platformCenter);

        // Draw player
        Color playerColor = cubeColorIdle;
        if (isDamageInvincible) playerColor = cubeColorDamaged;
        else if (collidedThisFrame) playerColor = cubeColorCollision;
        DrawMeshWithFrustumCulling(cubeLineMesh, playerColor, cubePosition);

        // Draw enemies
        foreach (Enemy e in enemies)
        {
            if (!e.isAlive) continue;
            UpdateCubeMeshVerticesWithRotation(enemyMesh, e.position, e.size, Quaternion.Euler(0, e.rotationAngle, 0));
            DrawMeshWithFrustumCulling(enemyMesh, enemyColor, e.position);
        }

        // Draw obstacles
        foreach (Obstacle o in obstacles)
        {
            if (o.isSpike)
            {
                // Draw spinning spike pyramid
                UpdatePyramidMeshVerticesWithRotation(spikeMesh, o.position, o.size, Quaternion.Euler(0, o.rotationAngle, 0));
                DrawMeshWithFrustumCulling(spikeMesh, spikeColor, o.position);
            }
            else
            {
                // Draw regular obstacles
                Mesh mesh = o.isInstakill ? instakillMesh : obstacleMesh;
                UpdateCubeMeshVertices(mesh, o.position, o.size);
                Color col = o.isInstakill ? instakillColor : obstacleColor;
                DrawMeshWithFrustumCulling(mesh, col, o.position);
            }
        }

        // Draw healing fireballs
        foreach (HealingFireball hf in healingFireballs)
        {
            if (hf.collected) continue;
            UpdateSphereMeshVerticesWithRotation(healingFireballMesh, hf.position, hf.size.x, Quaternion.Euler(hf.rotationAngle, hf.rotationAngle * 0.7f, hf.rotationAngle * 1.3f));
            DrawMeshWithFrustumCulling(healingFireballMesh, healingFireballColor, hf.position);
        }

        // Draw fireballs
        foreach (Fireball fb in fireballs)
        {
            if (!fb.active) continue;
            UpdateSphereMeshVertices(fireballMesh, fb.position, fb.size);
            DrawMeshWithFrustumCulling(fireballMesh, fireballColor, fb.position);
        }
    }

    void DrawMeshWithFrustumCulling(Mesh mesh, Color color, Vector3 worldPosition)
    {
        if (mainCamera == null) return;

        // Frustum culling using dot product
        Vector3 toObject = worldPosition - mainCamera.transform.position;
        float dot = Vector3.Dot(mainCamera.transform.forward, toObject.normalized);

        // If behind camera or too far to the side, don't draw
        if (dot < 0.1f)
        {
            return; // Skip drawing instead of scaling to zero
        }

        lineMaterial.SetColor("_Color", color);
        lineMaterial.SetPass(0);
        Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
    }

    void OnGUI()
    {
        if (uiStyle == null)
        {
            uiStyle = new GUIStyle(GUI.skin.label);
            uiStyle.fontSize = uiFontSize;
            uiStyle.normal.textColor = uiColor;
            uiStyle.fontStyle = FontStyle.Bold;
        }

        // Timer
        string timeStr = string.Format("Time: {0:F1}s", gameTimer);
        GUI.Label(new Rect(uiOffset.x, uiOffset.y, 200, 30), timeStr, uiStyle);

        // HP
        string hpStr = string.Format("HP: {0}/{1}", playerHP, maxPlayerHP);
        GUI.Label(new Rect(uiOffset.x, uiOffset.y + 30, 200, 30), hpStr, uiStyle);

        // Lives
        string livesStr = string.Format("Lives: {0}", playerLives);
        GUI.Label(new Rect(uiOffset.x, uiOffset.y + 60, 200, 30), livesStr, uiStyle);

        // Goal reminder
        GUIStyle goalStyle = new GUIStyle(uiStyle);
        goalStyle.fontSize = 16;
        goalStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(uiOffset.x, uiOffset.y + 90, 400, 30), "Goal: Reach LEFT or RIGHT edge to win!", goalStyle);

        // Game over messages
        if (gameWon)
        {
            GUIStyle bigStyle = new GUIStyle(uiStyle);
            bigStyle.fontSize = 48;
            bigStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 50, 400, 100), "YOU WIN!", bigStyle);
            GUIStyle smallStyle = new GUIStyle(uiStyle);
            smallStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 50, 400, 30), "Press R to Restart", smallStyle);
        }
        else if (gameLost)
        {
            GUIStyle bigStyle = new GUIStyle(uiStyle);
            bigStyle.fontSize = 48;
            bigStyle.alignment = TextAnchor.MiddleCenter;
            bigStyle.normal.textColor = Color.red;
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 50, 400, 100), "GAME OVER", bigStyle);
            GUIStyle smallStyle = new GUIStyle(uiStyle);
            smallStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 50, 400, 30), "Press R to Restart", smallStyle);
        }

        // Controls
        GUIStyle controlStyle = new GUIStyle(uiStyle);
        controlStyle.fontSize = 14;
        controlStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
        int yPos = Screen.height - 90;
        GUI.Label(new Rect(uiOffset.x, yPos, 300, 20), "Left/Right Arrows or A/D: Move", controlStyle);
        GUI.Label(new Rect(uiOffset.x, yPos + 20, 300, 20), "Space: Jump", controlStyle);
        GUI.Label(new Rect(uiOffset.x, yPos + 40, 400, 20), "F: Shoot Fireball (left/right)", controlStyle);
        GUI.Label(new Rect(uiOffset.x, yPos + 60, 300, 20), "R: Restart", controlStyle);
    }

    #region Mesh Creation
    Mesh CreateCubeLineMesh(Vector3 size)
    {
        Mesh m = new Mesh();
        m.name = "CubeLineMesh";

        Vector3 half = size * 0.5f;

        Vector3[] verts = new Vector3[8];
        verts[0] = new Vector3(-half.x, -half.y, -half.z);
        verts[1] = new Vector3(half.x, -half.y, -half.z);
        verts[2] = new Vector3(half.x, -half.y, half.z);
        verts[3] = new Vector3(-half.x, -half.y, half.z);
        verts[4] = new Vector3(-half.x, half.y, -half.z);
        verts[5] = new Vector3(half.x, half.y, -half.z);
        verts[6] = new Vector3(half.x, half.y, half.z);
        verts[7] = new Vector3(-half.x, half.y, half.z);

        int[] indices = {
            0,1, 1,2, 2,3, 3,0,
            4,5, 5,6, 6,7, 7,4,
            0,4, 1,5, 2,6, 3,7
        };

        m.vertices = verts;
        m.SetIndices(indices, MeshTopology.Lines, 0);
        m.RecalculateBounds();
        m.hideFlags = HideFlags.HideAndDontSave;
        return m;
    }

    void UpdateCubeMeshVertices(Mesh mesh, Vector3 worldCenter, Vector3 size)
    {
        Vector3 half = size * 0.5f;

        Vector3[] verts = new Vector3[8];
        verts[0] = new Vector3(-half.x, -half.y, -half.z) + worldCenter;
        verts[1] = new Vector3(half.x, -half.y, -half.z) + worldCenter;
        verts[2] = new Vector3(half.x, -half.y, half.z) + worldCenter;
        verts[3] = new Vector3(-half.x, -half.y, half.z) + worldCenter;
        verts[4] = new Vector3(-half.x, half.y, -half.z) + worldCenter;
        verts[5] = new Vector3(half.x, half.y, -half.z) + worldCenter;
        verts[6] = new Vector3(half.x, half.y, half.z) + worldCenter;
        verts[7] = new Vector3(-half.x, half.y, half.z) + worldCenter;

        mesh.vertices = verts;
        mesh.RecalculateBounds();
    }

    void UpdateCubeMeshVerticesWithRotation(Mesh mesh, Vector3 worldCenter, Vector3 size, Quaternion rotation)
    {
        Vector3 half = size * 0.5f;

        Vector3[] verts = new Vector3[8];
        // Create vertices in local space
        Vector3[] localVerts = new Vector3[8];
        localVerts[0] = new Vector3(-half.x, -half.y, -half.z);
        localVerts[1] = new Vector3(half.x, -half.y, -half.z);
        localVerts[2] = new Vector3(half.x, -half.y, half.z);
        localVerts[3] = new Vector3(-half.x, -half.y, half.z);
        localVerts[4] = new Vector3(-half.x, half.y, -half.z);
        localVerts[5] = new Vector3(half.x, half.y, -half.z);
        localVerts[6] = new Vector3(half.x, half.y, half.z);
        localVerts[7] = new Vector3(-half.x, half.y, half.z);

        // Rotate and then translate to world position
        for (int i = 0; i < 8; i++)
        {
            verts[i] = rotation * localVerts[i] + worldCenter;
        }

        mesh.vertices = verts;
        mesh.RecalculateBounds();
    }

    Mesh CreatePlatformLineMesh(Vector3 center, Vector3 size)
    {
        Mesh m = new Mesh();
        m.name = "PlatformLineMesh";

        Vector3 half = size * 0.5f;
        Vector3 topCenter = new Vector3(center.x, center.y + half.y, center.z);

        Vector3[] verts = new Vector3[8];
        verts[0] = new Vector3(-half.x, 0f, -half.z) + topCenter;
        verts[1] = new Vector3(half.x, 0f, -half.z) + topCenter;
        verts[2] = new Vector3(half.x, 0f, half.z) + topCenter;
        verts[3] = new Vector3(-half.x, 0f, half.z) + topCenter;
        float t = Mathf.Min(0.05f, half.y);
        verts[4] = verts[0] + Vector3.down * t;
        verts[5] = verts[1] + Vector3.down * t;
        verts[6] = verts[2] + Vector3.down * t;
        verts[7] = verts[3] + Vector3.down * t;

        int[] indices = {
            0,1, 1,2, 2,3, 3,0,
            0,4, 1,5, 2,6, 3,7,
            4,5, 5,6, 6,7, 7,4
        };

        m.vertices = verts;
        m.SetIndices(indices, MeshTopology.Lines, 0);
        m.RecalculateBounds();
        m.hideFlags = HideFlags.HideAndDontSave;
        return m;
    }

    Mesh CreateSphereLineMesh(float radius)
    {
        Mesh m = new Mesh();
        m.name = "SphereLineMesh";

        int segments = 12;
        List<Vector3> verts = new List<Vector3>();
        List<int> indices = new List<int>();

        // Create 3 circles (XY, XZ, YZ planes)
        // XY circle
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }

        // XZ circle
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        // YZ circle
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            verts.Add(new Vector3(0f, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
        }

        // Indices for circles
        for (int circle = 0; circle < 3; circle++)
        {
            int offset = circle * segments;
            for (int i = 0; i < segments; i++)
            {
                indices.Add(offset + i);
                indices.Add(offset + (i + 1) % segments);
            }
        }

        m.vertices = verts.ToArray();
        m.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
        m.RecalculateBounds();
        m.hideFlags = HideFlags.HideAndDontSave;
        return m;
    }

    Mesh CreatePyramidLineMesh(Vector3 size)
    {
        Mesh m = new Mesh();
        m.name = "PyramidLineMesh";

        Vector3 half = size * 0.5f;

        // 5 vertices: 4 corners at base + 1 apex at top
        Vector3[] verts = new Vector3[5];

        // Base square (at bottom)
        verts[0] = new Vector3(-half.x, -half.y, -half.z);
        verts[1] = new Vector3(half.x, -half.y, -half.z);
        verts[2] = new Vector3(half.x, -half.y, half.z);
        verts[3] = new Vector3(-half.x, -half.y, half.z);

        // Apex (at top center)
        verts[4] = new Vector3(0f, half.y, 0f);

        // Lines: base square + 4 edges to apex
        int[] indices = {
            0,1, 1,2, 2,3, 3,0,  // Base square
            0,4, 1,4, 2,4, 3,4   // Edges to apex
        };

        m.vertices = verts;
        m.SetIndices(indices, MeshTopology.Lines, 0);
        m.RecalculateBounds();
        m.hideFlags = HideFlags.HideAndDontSave;
        return m;
    }

    void UpdatePyramidMeshVerticesWithRotation(Mesh mesh, Vector3 worldCenter, Vector3 size, Quaternion rotation)
    {
        Vector3 half = size * 0.5f;

        // Create vertices in local space
        Vector3[] localVerts = new Vector3[5];
        localVerts[0] = new Vector3(-half.x, -half.y, -half.z);
        localVerts[1] = new Vector3(half.x, -half.y, -half.z);
        localVerts[2] = new Vector3(half.x, -half.y, half.z);
        localVerts[3] = new Vector3(-half.x, -half.y, half.z);
        localVerts[4] = new Vector3(0f, half.y, 0f);

        // Rotate and then translate to world position
        Vector3[] verts = new Vector3[5];
        for (int i = 0; i < 5; i++)
        {
            verts[i] = rotation * localVerts[i] + worldCenter;
        }

        mesh.vertices = verts;
        mesh.RecalculateBounds();
    }

    void UpdateSphereMeshVerticesWithRotation(Mesh mesh, Vector3 worldCenter, float radius, Quaternion rotation)
    {
        int segments = 12;
        List<Vector3> verts = new List<Vector3>();

        // Create 3 circles in local space
        // XY circle
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 localPos = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            verts.Add(rotation * localPos + worldCenter);
        }

        // XZ circle
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 localPos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            verts.Add(rotation * localPos + worldCenter);
        }

        // YZ circle
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 localPos = new Vector3(0f, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            verts.Add(rotation * localPos + worldCenter);
        }

        mesh.vertices = verts.ToArray();
        mesh.RecalculateBounds();
    }

    void UpdateSphereMeshVertices(Mesh mesh, Vector3 worldCenter, float radius)
    {
        int segments = 12;
        List<Vector3> verts = new List<Vector3>();

        // XY circle
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f) + worldCenter);
        }

        // XZ circle
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius) + worldCenter);
        }

        // YZ circle
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            verts.Add(new Vector3(0f, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius) + worldCenter);
        }

        mesh.vertices = verts.ToArray();
        mesh.RecalculateBounds();
    }
    #endregion
}