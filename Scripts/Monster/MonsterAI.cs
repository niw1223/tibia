using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class MonsterAI_GridOnly : MonoBehaviour
{
    // Class Node สำหรับ A* Pathfinding (เหมือนเดิม)
    public class Node
    {
        public Vector3Int cellPosition;
        public int gCost;
        public int hCost;
        public Node parent;

        public Node(Vector3Int pos)
        {
            cellPosition = pos;
        }

        public int fCost => gCost + hCost;
    }

    [Header("Animation Clips")]
    public AnimationClip walkUp;
    public AnimationClip walkDown;
    public AnimationClip walkLeft;
    public AnimationClip walkRight;
    public AnimationClip idleUp;
    public AnimationClip idleDown;
    public AnimationClip idleLeft;
    public AnimationClip idleRight;
    public AnimationClip attackUp;
    public AnimationClip attackDown;
    public AnimationClip attackLeft;
    public AnimationClip attackRight;

    [Header("Audio")]
    public AudioClip[] attackSounds;
    public List<ElementalReactionData> elementalHitSounds = new List<ElementalReactionData>();
    private AudioSource audioSource;

    [Header("Grid & Movement")]
    public Tilemap blockedTilemap;
    public float moveSpeed = 1.2f;
    public float gridSize = 1f;
    public float moveInterval = 0.8f;
    public float attackAnimationDuration = 0.5f;
    public float timeBetweenSteps = 0.2f;

    [Header("Detection & Combat")]
    public float attackRange = 1.1f;
    public float chaseRange = 5.1f;
    public float attackCooldown = 1.5f;
    public Transform[] patrolPoints;
    public int randomPatrolRange = 10;
    public LayerMask playerLayerMask;

    private Transform player;
    private Animator animator;
    private bool isMoving = false;
    private bool isAttacking = false;
    private bool isHit = false;
    private Vector2 lastMoveDir = Vector2.down;
    private Queue<Vector3> currentPath = new Queue<Vector3>();
    private int currentPatrolIndex = 0;

    private float lastAttackTime = 0;
    private Vector3Int randomPatrolTargetCell;
    private Attacker attacker;
    private CharacterStats playerStats;

    private enum AIState { Patrol, Chase, Attack }
    private AIState currentState = AIState.Patrol;

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        attacker = GetComponent<Attacker>();

        player = GameObject.FindWithTag("Player")?.transform;
        if (player != null)
        {
            playerStats = player.GetComponent<CharacterStats>();
        }

        if (blockedTilemap == null)
        {
            Debug.LogError("Error: 'Blocked Tilemap' is not assigned in the Inspector! MonsterAI cannot work without it.");
            this.enabled = false;
            return;
        }

        transform.position = SnapToGrid(transform.position);

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            SetNewRandomPatrolTarget();
        }

        StartCoroutine(MonsterBehaviorRoutine());
        PlayIdleAnimation();
    }

    IEnumerator MonsterBehaviorRoutine()
    {
        while (true)
        {
            yield return new WaitUntil(() => !isMoving && !isAttacking && !isHit);
            yield return new WaitForSeconds(moveInterval);

            currentPath.Clear();

            if (player == null)
            {
                currentState = AIState.Patrol;
            }
            else
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);

                if (distanceToPlayer <= attackRange)
                {
                    currentState = AIState.Attack;
                }
                else if (distanceToPlayer <= chaseRange)
                {
                    currentState = AIState.Chase;
                }
                else
                {
                    currentState = AIState.Patrol;
                }
            }

            switch (currentState)
            {
                case AIState.Patrol:
                    if (patrolPoints != null && patrolPoints.Length > 0)
                        PatrolToAssignedPoints();
                    else
                        PatrolRandomly();
                    break;
                case AIState.Chase:
                    ChasePlayer();
                    break;
                case AIState.Attack:
                    if (Time.time > lastAttackTime + attackCooldown)
                    {
                        AttackPlayer();
                    }
                    break;
            }

            if (currentPath.Count > 0 && !isMoving)
            {
                StartCoroutine(MoveAlongPathRoutine());
            }
        }
    }

    void SetNewRandomPatrolTarget()
    {
        Vector3Int currentCell = blockedTilemap.WorldToCell(transform.position);
        for (int i = 0; i < 10; i++)
        {
            Vector3Int randomCell = currentCell + new Vector3Int(Random.Range(-randomPatrolRange, randomPatrolRange + 1), Random.Range(-randomPatrolRange, randomPatrolRange + 1), 0);
            if (!IsObstacle(randomCell))
            {
                randomPatrolTargetCell = randomCell;
                return;
            }
        }
        randomPatrolTargetCell = currentCell;
    }

    void PatrolRandomly()
    {
        Vector3 targetWorldPos = blockedTilemap.CellToWorld(randomPatrolTargetCell);
        if (Vector3.Distance(SnapToGrid(transform.position), SnapToGrid(targetWorldPos)) < gridSize * 0.5f || currentPath.Count == 0)
        {
            SetNewRandomPatrolTarget();
            targetWorldPos = blockedTilemap.CellToWorld(randomPatrolTargetCell);
        }
        MoveToTarget(targetWorldPos);
    }

    void PatrolToAssignedPoints()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (Vector3.Distance(SnapToGrid(transform.position), SnapToGrid(patrolPoints[currentPatrolIndex].position)) < gridSize * 0.5f)
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;

        MoveToTarget(SnapToGrid(patrolPoints[currentPatrolIndex].position));
    }

    void ChasePlayer()
    {
        if (player == null) return;
        MoveToTarget(player.position);
    }

    void MoveToTarget(Vector3 target)
    {
        if (isAttacking) return;
        currentPath = FindPathAStar(SnapToGrid(transform.position), target);

        if (currentPath == null || currentPath.Count == 0)
        {
            PlayIdleAnimation();
            return;
        }
    }


    public void TakeDamage(ElementType damageType, GameObject attackerObject = null)
    {
        if (isHit) return;
        isHit = true;
        isMoving = false;
        isAttacking = false;
        currentPath.Clear();
        StopAllCoroutines();
        StartCoroutine(HitRoutine(damageType));
    }

    private IEnumerator HitRoutine(ElementType damageType)
    {
        PlayHitAnimation(damageType);

        float hitDuration = 0.5f;
        yield return new WaitForSeconds(hitDuration);

        isHit = false;
        PlayIdleAnimation();
        StartCoroutine(MonsterBehaviorRoutine());
    }

    IEnumerator MoveAlongPathRoutine()
    {
        isMoving = true;
        while (currentPath.Count > 0 && !isAttacking && !isHit)
        {
            if (player != null && Vector3.Distance(transform.position, player.position) <= chaseRange)
            {
                if (Vector3.Distance(transform.position, player.position) <= attackRange * 1.05f)
                {
                    currentPath.Clear();
                    break;
                }
            }

            Vector3 nextPos = currentPath.Dequeue();
            if (CheckCollision(nextPos))
            {
                currentPath.Clear();
                break;
            }

            lastMoveDir = (nextPos - transform.position).normalized;
            PlayWalkAnimation(lastMoveDir);

            while ((transform.position - nextPos).sqrMagnitude > 0.0001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, nextPos, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = nextPos;

            yield return new WaitForSeconds(timeBetweenSteps);
        }
        isMoving = false;
        if (!isAttacking && !isHit)
        {
            PlayIdleAnimation();
        }
    }

    void AttackPlayer()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        UpdateDirectionToPlayer();
        PlayAttackAnimation(lastMoveDir);

        if (attacker != null && playerStats != null)
        {
            attacker.PerformAttack(playerStats);
        }

        StartCoroutine(AttackCooldownRoutine());
    }

    void PlayHitAnimation(ElementType type)
    {
        if (animator == null) return;

        AudioClip hitSound = elementalHitSounds.FirstOrDefault(e => e.elementType == type)?.hitSound;
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        List<string> animNames = elementalHitSounds.FirstOrDefault(e => e.elementType == type)?.hitAnimationTriggerNames;
        if (animNames != null && animNames.Count > 0)
        {
            string animationName = animNames[Random.Range(0, animNames.Count)];
            animator.Play(animationName);
        }
        else
        {
            // Fallback
            int randomHitIndex = Random.Range(1, 4);
            string genericAnimName = type.ToString() + "_Hit_" + randomHitIndex;
            animator.Play(genericAnimName);
        }
    }

    IEnumerator AttackCooldownRoutine()
    {
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
        PlayIdleAnimation();
    }

    void UpdateDirectionToPlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            lastMoveDir = (dir.x > 0) ? Vector2.right : Vector2.left;
        else
            lastMoveDir = (dir.y > 0) ? Vector2.up : Vector2.down;
    }

    bool CheckCollision(Vector3 targetPos)
    {
        if (IsObstacle(blockedTilemap.WorldToCell(targetPos))) return true;
        Collider2D playerCollider = Physics2D.OverlapPoint(targetPos, playerLayerMask);
        if (playerCollider != null && playerCollider.gameObject == player.gameObject)
        {
            return true;
        }
        return false;
    }

    Queue<Vector3> FindPathAStar(Vector3 startWorld, Vector3 targetWorld)
    {
        Vector3Int startCell = blockedTilemap.WorldToCell(startWorld);
        Vector3Int targetCell = blockedTilemap.WorldToCell(targetWorld);

        if (IsObstacle(targetCell) || IsObstacle(startCell))
        {
            return new Queue<Vector3>();
        }

        Dictionary<Vector3Int, Node> openSet = new Dictionary<Vector3Int, Node>();
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        Node startNode = new Node(startCell);
        startNode.gCost = 0;
        startNode.hCost = GetDistance(startCell, targetCell);
        openSet[startCell] = startNode;

        int searchLimit = 500;
        int nodesEvaluated = 0;

        while (openSet.Count > 0)
        {
            if (nodesEvaluated > searchLimit)
            {
                return new Queue<Vector3>();
            }
            nodesEvaluated++;

            Node currentNode = null;
            foreach (var node in openSet.Values)
            {
                if (currentNode == null || node.fCost < currentNode.fCost || (node.fCost == currentNode.fCost && node.hCost < currentNode.hCost))
                    currentNode = node;
            }
            if (currentNode == null) break;
            openSet.Remove(currentNode.cellPosition);
            closedSet.Add(currentNode.cellPosition);

            if (currentNode.cellPosition == targetCell)
                return RetracePath(currentNode);

            foreach (Vector3Int neighbourPos in GetNeighbours(currentNode.cellPosition))
            {
                if (closedSet.Contains(neighbourPos) || IsObstacle(neighbourPos) || CheckCollision(blockedTilemap.CellToWorld(neighbourPos) + new Vector3(gridSize / 2f, gridSize / 2f, 0f))) continue;

                int newG = currentNode.gCost + 1;
                Node neighbourNode = openSet.ContainsKey(neighbourPos) ? openSet[neighbourPos] : null;
                if (neighbourNode == null || newG < neighbourNode.gCost)
                {
                    if (neighbourNode == null)
                    {
                        neighbourNode = new Node(neighbourPos);
                        openSet[neighbourPos] = neighbourNode;
                    }
                    neighbourNode.gCost = newG;
                    neighbourNode.hCost = GetDistance(neighbourPos, targetCell);
                    neighbourNode.parent = currentNode;
                }
            }
        }
        return new Queue<Vector3>();
    }

    Queue<Vector3> RetracePath(Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;
        while (currentNode != null)
        {
            path.Add(SnapToGrid(blockedTilemap.CellToWorld(currentNode.cellPosition)));
            currentNode = currentNode.parent;
        }
        path.Reverse();
        if (path.Count > 0 && Vector3.Distance(path[0], transform.position) < 0.1f)
        {
            path.RemoveAt(0);
        }
        return new Queue<Vector3>(path);
    }

    int GetDistance(Vector3Int nodeA, Vector3Int nodeB)
    {
        int dstX = Mathf.Abs(nodeA.x - nodeB.x);
        int dstY = Mathf.Abs(nodeA.y - nodeB.y);
        return dstX + dstY;
    }

    List<Vector3Int> GetNeighbours(Vector3Int cellPos)
    {
        List<Vector3Int> neighbours = new List<Vector3Int>();
        neighbours.Add(cellPos + new Vector3Int(1, 0, 0));
        neighbours.Add(cellPos + new Vector3Int(-1, 0, 0));
        neighbours.Add(cellPos + new Vector3Int(0, 1, 0));
        neighbours.Add(cellPos + new Vector3Int(0, -1, 0));
        return neighbours;
    }

    Vector3 SnapToGrid(Vector3 pos)
    {
        Vector3Int cellPos = blockedTilemap.WorldToCell(pos);
        Vector3 center = blockedTilemap.CellToWorld(cellPos);
        return center + new Vector3(gridSize / 2f, gridSize / 2f, 0f);
    }

    bool IsObstacle(Vector3Int cellPos)
    {
        if (blockedTilemap == null) return false;
        return blockedTilemap.HasTile(cellPos);
    }

    void PlayWalkAnimation(Vector2 dir)
    {
        if (animator == null || isAttacking || isHit) return;
        if (dir == Vector2.up && walkUp != null) animator.Play(walkUp.name);
        else if (dir == Vector2.down && walkDown != null) animator.Play(walkDown.name);
        else if (dir == Vector2.left && walkLeft != null) animator.Play(walkLeft.name);
        else if (dir == Vector2.right && walkRight != null) animator.Play(walkRight.name);
    }

    void PlayIdleAnimation()
    {
        if (animator == null || isAttacking || isHit) return;
        if (lastMoveDir == Vector2.up && idleUp != null) animator.Play(idleUp.name);
        else if (lastMoveDir == Vector2.down && idleDown != null) animator.Play(idleDown.name);
        else if (lastMoveDir == Vector2.left && idleLeft != null) animator.Play(idleLeft.name);
        else if (lastMoveDir == Vector2.right && idleRight != null) animator.Play(idleRight.name);
        else if (idleDown != null) animator.Play(idleDown.name);
    }

    void PlayAttackAnimation(Vector2 dir)
    {
        if (animator == null) return;
        if (dir == Vector2.up && attackUp != null) animator.Play(attackUp.name);
        else if (dir == Vector2.down && attackDown != null) animator.Play(attackDown.name);
        else if (dir == Vector2.left && attackLeft != null) animator.Play(attackLeft.name);
        else if (dir == Vector2.right && attackRight != null) animator.Play(attackRight.name);

        PlayRandomAttackSound();
    }

    void PlayRandomAttackSound()
    {
        if (audioSource != null && attackSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, attackSounds.Length);
            AudioClip clipToPlay = attackSounds[randomIndex];
            audioSource.PlayOneShot(clipToPlay);
        }
    }
}
