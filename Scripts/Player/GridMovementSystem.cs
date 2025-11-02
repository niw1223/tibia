using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Linq; 

public class GridMovementSystem : MonoBehaviour
{
    // --- ตั้งค่า Grid และความเร็ว ---
    [Header("Grid & Movement Settings")]
    public Tilemap blockedTilemap;
    public Camera mainCamera;
    public float moveSpeed = 3f;
    public LayerMask groundLayerMask;
    public LayerMask obstacleLayerMask;
    [Tooltip("ระยะเวลาหน่วงระหว่างการเดินต่อเนื่องด้วย WASD")]
    public float wasdInputDelay = 0.3f;
    [Header("Attack Settings")]
    public float attackWindUpTime = 0.3f;
    public LayerMask monsterLayerMask;
    [Header("Collision Settings")]
    public float collisionPushbackDuration = 0.1f;

    // --- Animation Clips (ลากใส่ได้เลย) ---
    [Header("Animation Clips")]
    public Animator animator;
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

    // --- Sound Effects (ลากใส่ได้เลย) ---
    [Header("Sound Effects")]
    public AudioSource audioSource;
    [Tooltip("ใส่ได้สูงสุด 3 คลิป")]
    public List<AudioClip> attackSounds;
    public List<ElementalReactionData> elementalHitSounds = new List<ElementalReactionData>();

    // --- Private Variables ---
    private Vector3 targetWorldPosition;
    private bool isMoving = false;
    private bool isAttacking = false;
    private bool isHit = false; 
    private Queue<Vector3> pathQueue = new Queue<Vector3>();
    private Vector2 lastMoveDir = Vector2.down;
    private float lastMoveTime;
    private Vector2 wasdInputDirection = Vector2.zero;
    private Attacker attacker;
    private PlayerWeaponManager weaponManager;
    private GameObject attackTarget;
    private Vector3 lastClickedPosition; 

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        attacker = GetComponent<Attacker>();
        weaponManager = GetComponent<PlayerWeaponManager>();

        targetWorldPosition = GetGridWorldPos(transform.position);
        transform.position = targetWorldPosition;
        PlayIdleAnimation();
    }

    void Update()
    {
        if (!isAttacking)
        {
            HandleMouseInput();
            HandleWASDInput();
        }

        if (!isMoving && !isAttacking && pathQueue.Count > 0)
        {
            StartCoroutine(MoveAlongPathRoutine());
        }
        else if (!isMoving && !isAttacking && pathQueue.Count == 0)
        {
            if (wasdInputDirection == Vector2.zero)
            {
                if (!isHit)
                {
                   PlayIdleAnimation();
                }
            }
        }
    }

    Vector3 GetGridWorldPos(Vector3 worldPos)
    {
        Vector3Int cellPos = blockedTilemap.WorldToCell(worldPos);
        Vector3 centerWorldPos = blockedTilemap.CellToWorld(cellPos);
        return centerWorldPos + new Vector3(0.5f, 0.5f, 0);
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);
            if (hit.collider != null)
            {
                Vector3 hitPoint = hit.point;
                lastClickedPosition = GetGridWorldPos(hitPoint); 

                if (((1 << hit.collider.gameObject.layer) & monsterLayerMask) != 0)
                {
                    attackTarget = hit.collider.gameObject;
                    wasdInputDirection = Vector2.zero;
                    pathQueue.Clear();
                    TryAttack();
                }
                else if (((1 << hit.collider.gameObject.layer) & obstacleLayerMask) != 0 || ((1 << hit.collider.gameObject.layer) & groundLayerMask) != 0)
                {
                    Vector3Int clickedCell = blockedTilemap.WorldToCell(lastClickedPosition);
                    if (IsObstacle(clickedCell))
                    {
                        TurnTowards(lastClickedPosition); 
                        pathQueue.Clear();
                    }
                    else 
                    {
                        attackTarget = null;
                        wasdInputDirection = Vector2.zero;
                        List<Vector3> path = FindPathAStar(transform.position, lastClickedPosition);
                        if (path != null && path.Count > 0)
                        {
                            pathQueue.Clear();
                            foreach (Vector3 p in path) pathQueue.Enqueue(p);
                            if (!isMoving) StartCoroutine(MoveAlongPathRoutine());
                        } else if (path != null && path.Count == 0) {
                            TurnTowards(lastClickedPosition);
                        }
                    }
                }
            }
        }
    }

    void HandleWASDInput()
    {
        Vector2 currentDir = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) currentDir = Vector2.up;
        else if (Input.GetKey(KeyCode.S)) currentDir = Vector2.down;
        else if (Input.GetKey(KeyCode.A)) currentDir = Vector2.left;
        else if (Input.GetKey(KeyCode.D)) currentDir = Vector2.right;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))
        {
            wasdInputDirection = currentDir;
            pathQueue.Clear();
            AttemptMove(wasdInputDirection);
            return;
        }

        if (currentDir != Vector2.zero && currentDir != wasdInputDirection)
        {
            wasdInputDirection = currentDir;
            lastMoveTime = Time.time;
        }
        else if (currentDir == Vector2.zero)
        {
            wasdInputDirection = Vector2.zero;
        }

        if (wasdInputDirection != Vector2.zero && !isMoving)
        {
            if (Time.time - lastMoveTime > wasdInputDelay)
            {
                AttemptMove(wasdInputDirection);
                lastMoveTime = Time.time;
            }
        }
    }

    void AttemptMove(Vector2 dir)
    {
        Vector3 newPosWorld = transform.position + new Vector3(dir.x, dir.y, 0);
        newPosWorld = GetGridWorldPos(newPosWorld);
        Vector3Int newCell = blockedTilemap.WorldToCell(newPosWorld);

        if (!IsObstacle(newCell))
        {
            pathQueue.Clear();
            pathQueue.Enqueue(newPosWorld);
            lastMoveDir = dir;
            if (!isMoving)
            {
                StartCoroutine(MoveAlongPathRoutine());
            }
        }
        else
        {
            lastMoveDir = dir;
            PlayIdleAnimation();
        }
    }

    void TurnTowards(Vector3 targetPoint)
    {
        Vector3 direction = (targetPoint - transform.position).normalized;
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            lastMoveDir = (direction.x > 0) ? Vector2.right : Vector2.left;
        else
            lastMoveDir = (direction.y > 0) ? Vector2.up : Vector2.down;

        PlayIdleAnimation();
    }

    IEnumerator MoveAlongPathRoutine()
    {
        if (isMoving) yield break;
        isMoving = true;

        while (pathQueue.Count > 0)
        {
            if (isHit) { pathQueue.Clear(); break; } 

            targetWorldPosition = pathQueue.Dequeue();

            Vector3 diff = (targetWorldPosition - transform.position).normalized;
            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
                lastMoveDir = (diff.x > 0) ? Vector2.right : Vector2.left;
            else
                lastMoveDir = (diff.y > 0) ? Vector2.up : Vector2.down;

            PlayWalkAnimation();

            while (Vector3.Distance(transform.position, targetWorldPosition) > 0.001f)
            {
                 if (isHit) { break; } 
                transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetWorldPosition;
        }

        isMoving = false;
        Vector3Int clickedCell = blockedTilemap.WorldToCell(lastClickedPosition);
        if (IsObstacle(clickedCell) && Vector3.Distance(transform.position, lastClickedPosition) > 0.1f)
        {
             TurnTowards(lastClickedPosition);
        }
        else if (!isHit)
        {
            PlayIdleAnimation();
        }
    }

    private void TryAttack()
    {
        if (isAttacking || weaponManager == null || weaponManager.currentWeapon == null || attackTarget == null) return;

        isAttacking = true;
        isMoving = false;
        pathQueue.Clear();
        StartCoroutine(MoveToTargetTileForAttack(attackTarget));
    }

    private IEnumerator MoveToTargetTileForAttack(GameObject target)
    {
        if (target == null) 
        {
            isAttacking = false; attackTarget = null; PlayIdleAnimation(); yield break;
        }

        Vector3 playerPos = transform.position;
        Vector3 targetPos = target.transform.position;
        
        Vector3 directionToTarget = (targetPos - playerPos).normalized;
        Vector3 attackTilePos = targetPos;
        if (Mathf.Abs(directionToTarget.x) > Mathf.Abs(directionToTarget.y))
            attackTilePos = targetPos + new Vector3(directionToTarget.x > 0 ? -1 : 1, 0, 0);
        else
            attackTilePos = targetPos + new Vector3(0, directionToTarget.y > 0 ? -1 : 1, 0);
        attackTilePos = GetGridWorldPos(attackTilePos);

        List<Vector3> path = FindPathAStar(playerPos, attackTilePos);
        if (path != null && path.Count > 0)
        {
            foreach (Vector3 p in path) pathQueue.Enqueue(p);
            yield return StartCoroutine(MoveAlongPathRoutine()); 
        }
        
        if (target != null) TurnTowards(target.transform.position);

        yield return StartCoroutine(AttackRoutine());
    }
    
    private IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(attackWindUpTime); 

        PlayAttackAnimation();
        PlayAttackSound();

        float attackDuration = GetAnimationDuration(GetAttackAnimation());
        yield return new WaitForSeconds(attackDuration / 2f);

        if (attacker != null && attackTarget != null)
        {
            if (Vector3.Distance(transform.position, attackTarget.transform.position) <= attacker.attackRange + 0.5f)
            {
                CharacterStats targetStats = attackTarget.GetComponent<CharacterStats>();
                if (targetStats != null)
                {
                    attacker.PerformAttack(targetStats);
                }
            }
        }

        yield return new WaitForSeconds(attackDuration / 2f);
        isAttacking = false;
        attackTarget = null; 
        PlayIdleAnimation();
    }

    public void TakeDamage(ElementType damageType, GameObject attackerObject = null)
    {
        if (isHit) return;
        isHit = true;
        isMoving = false;
        isAttacking = false;
        pathQueue.Clear();
        StopAllCoroutines(); 
        StartCoroutine(HitRoutine(damageType, attackerObject));

        if (attackerObject != null && attackerObject != this.gameObject)
        {
            attackTarget = attackerObject;
        }
    }

    private IEnumerator HitRoutine(ElementType damageType, GameObject attackerObject = null)
    {
        PlayHitAnimation(damageType);
        float hitDuration = 0.5f;
        yield return new WaitForSeconds(hitDuration);
        
        isHit = false;
        
        if (attackTarget != null && Vector3.Distance(transform.position, attackTarget.transform.position) <= attacker.attackRange + 0.5f)
        {
            TryAttack();
        }
        else
        {
             PlayIdleAnimation();
        }
    }

    // --- Animation & Sound Logic ---
    void PlayWalkAnimation()
    {
        if (animator == null) return;
        if (walkUp != null && lastMoveDir == Vector2.up) animator.Play(walkUp.name);
        else if (walkDown != null && lastMoveDir == Vector2.down) animator.Play(walkDown.name);
        else if (walkLeft != null && lastMoveDir == Vector2.left) animator.Play(walkLeft.name);
        else if (walkRight != null && lastMoveDir == Vector2.right) animator.Play(walkRight.name);
    }
    void PlayIdleAnimation()
    {
        if (animator == null) return;
        if (idleUp != null && lastMoveDir == Vector2.up) animator.Play(idleUp.name);
        else if (idleDown != null && lastMoveDir == Vector2.down) animator.Play(idleDown.name);
        else if (idleLeft != null && lastMoveDir == Vector2.left) animator.Play(idleLeft.name);
        else if (idleRight != null && lastMoveDir == Vector2.right) animator.Play(idleRight.name);
    }
    void PlayAttackAnimation()
    {
        if (animator == null) return;
        AnimationClip clip = GetAttackAnimation();
        if (clip != null) animator.Play(clip.name);
    }
    void PlayHitAnimation(ElementType type)
    {
        if (animator == null) return;
        AudioClip hitSound = elementalHitSounds.FirstOrDefault(e => e.elementType == type)?.hitSound;
        if (hitSound != null && audioSource != null) audioSource.PlayOneShot(hitSound);
        List<string> animNames = elementalHitSounds.FirstOrDefault(e => e.elementType == type)?.hitAnimationTriggerNames;
        if (animNames != null && animNames.Count > 0)
        {
            string animationName = animNames[Random.Range(0, animNames.Count)];
            animator.Play(animationName);
        }
        else
        {
            int randomHitIndex = Random.Range(1, 4);
            string animationName = type.ToString() + "_Hit_" + randomHitIndex;
            animator.Play(animationName);
        }
    }
    AnimationClip GetAttackAnimation()
    {
        if (attackUp != null && lastMoveDir == Vector2.up) return attackUp;
        if (attackDown != null && lastMoveDir == Vector2.down) return attackDown;
        if (attackLeft != null && lastMoveDir == Vector2.left) return attackLeft;
        if (attackRight != null && lastMoveDir == Vector2.right) return attackRight;
        return null;
    }
    float GetAnimationDuration(AnimationClip clip)
    {
        if (clip == null) return 0f;
        return clip.length;
    }
    void PlayAttackSound()
    {
        if (audioSource == null || attackSounds.Count == 0) return;
        audioSource.PlayOneShot(attackSounds[Random.Range(0, attackSounds.Count)]);
    }

    bool IsObstacle(Vector3Int cellPos)
    {
        return blockedTilemap != null && blockedTilemap.HasTile(cellPos);
    }

    // --- A* Pathfinding Logic ---
    private List<Vector3> FindPathAStar(Vector3 startWorld, Vector3 targetWorld)
    {
        Vector3Int startCell = blockedTilemap.WorldToCell(GetGridWorldPos(startWorld));
        Vector3Int targetCell = blockedTilemap.WorldToCell(GetGridWorldPos(targetWorld));
        AStarUtil.Node startNode = new AStarUtil.Node(startCell);
        AStarUtil.Node targetNode = new AStarUtil.Node(targetCell);
        List<AStarUtil.Node> openSet = new List<AStarUtil.Node>();
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        openSet.Add(startNode);
        while (openSet.Count > 0)
        {
            AStarUtil.Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }
            openSet.Remove(currentNode);
            closedSet.Add(currentNode.cellPosition);
            if (currentNode.cellPosition == targetNode.cellPosition)
            {
                return RetracePath(startNode, currentNode);
            }
            foreach (AStarUtil.Node neighbor in GetNeighbors(currentNode))
            {
                if (closedSet.Contains(neighbor.cellPosition) || IsObstacle(neighbor.cellPosition))
                {
                    continue;
                }
                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
        return null;
    }
    List<Vector3> RetracePath(AStarUtil.Node startNode, AStarUtil.Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        AStarUtil.Node currentNode = endNode;
        while (currentNode != startNode)
        {
            Vector3 worldPos = blockedTilemap.CellToWorld(currentNode.cellPosition) + new Vector3(0.5f, 0.5f, 0);
            path.Add(worldPos);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }
    List<AStarUtil.Node> GetNeighbors(AStarUtil.Node node)
    {
        List<AStarUtil.Node> neighbors = new List<AStarUtil.Node>();
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        foreach (Vector3Int dir in directions)
        {
            Vector3Int neighborCell = node.cellPosition + dir;
            if (!IsObstacle(neighborCell))
            {
                neighbors.Add(new AStarUtil.Node(neighborCell));
            }
        }
        return neighbors;
    }
    int GetDistance(AStarUtil.Node nodeA, AStarUtil.Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.cellPosition.x - nodeB.cellPosition.x);
        int dstY = Mathf.Abs(nodeA.cellPosition.y - nodeB.cellPosition.y);
        return dstX + dstY;
    }
}
