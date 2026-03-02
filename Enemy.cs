using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour {

    public LayerMask groundLayer, playerLayer;
    public float health, walkPointMin, walkPointRange, timeBetweenAttacks, attackRange, walkSpeed, runSpeed, chaseMeter = 100f, rotationSpeed = 5f;
    public int damage, invisibilityOdds = 3, pauseChance = 4, deAggroCooldown = 10;
    public ParticleSystem hitEffect;
    public bool invisible = false, freezingAura = false, attractedToSound = false, allowedToMove = true, geistAura = false;
    public SkinnedMeshRenderer[] meshRenderers;
    public GameObject[] horns;
    public ParticleSystem geistlightAura;
    public GameObject shadow;

    public enum Mode { chasing, patrolling }
    public Mode currentMode;

    public AudioSource musicSource, monsterSource, ambientSource;
    public AudioClip attackClip, chaseMusicClip, normalMusicClip;
    public AudioClip[] screamClips;

    public Death deathScript;
    public Vector3 walkPoint, playerLastSeen;
    public Animator animator, shadowAnimator;


    #region private vars
    private NavMeshAgent agent;
    private Transform player;
    private bool walkPointSet, alreadyAttacked, takeDamage, waitingForScream = false, pausingPatrolState = false;
    public bool  normalAggro = true;
    private ConeLOSDetector coneDetector;
    private ParticleSystem playersBreath;
    private float initSpeed;

    private ConeLOSDetector playerVision;
    #endregion


    private void Awake() {
        //animator = GetComponent<Animator>();
        player = GameObject.Find("Player").transform;
        playersBreath = player.GetComponentInChildren<ParticleSystem>();
        agent = GetComponent<NavMeshAgent>();
        coneDetector = GetComponent<ConeLOSDetector>();
        agent.updateRotation = false;
        playerVision = player.GetChild(1).GetChild(0).GetComponent<ConeLOSDetector>();
    }


    // Update is called once per frame
    private void Update() {
        if(allowedToMove) {
            bool playerSeen = coneDetector.targetVisible && !player.GetComponent<PlayerMovement>().isHiding && normalAggro;// && //!player.GetComponent<PlayerMovement>().isHiding;
            bool playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, playerLayer) && normalAggro;
            // If I can't see you and you're not in melee range OR you're hiding
            if(!playerSeen && !playerInAttackRange) {
                if((chaseMeter == 100f || invisible) || !normalAggro) {
                    if(currentMode == Mode.chasing) {
                        AudioController.FadeToAnother(this, musicSource, 4, normalMusicClip, .1f);
                        walkPointSet = false;
                    }
                    ModePatrolling();
                }
                else {
                    chaseMeter += 2 * Time.deltaTime;
                    if(chaseMeter > 100f) chaseMeter = 100f;
                    if(chaseMeter >= 100f) walkPointSet = false;

                    if(currentMode == Mode.chasing) ModeChase();
                }
            }

            // If I'm not invis and I see you and you're NOT in melee range
            else if(normalAggro && !invisible && playerSeen && !playerInAttackRange) {
                if(currentMode != Mode.chasing && !waitingForScream) {
                    if(GetComponent<ConeLOSDetector>().visibilityOverride) chaseMeter = -9999f;
                    else chaseMeter = 80f;

                    // if its not the ritual and I see your back, do silent. else:
                    if(!GetComponent<ConeLOSDetector>().visibilityOverride && !playerVision.targetVisible && transform.parent.GetComponentInChildren<ToolController>().heldIndex != 1) {
                        ModeChase();
                        // We still want to Fade to chase music if player now turns and sees. Or maybe not necessary.
                        Debug.Log("Saw you with back turned. ");
                    }
                    else {
                        StartCoroutine(ScreamAnimTimer());
                        AudioController.FadeToAnother(this, musicSource, .3f, chaseMusicClip, .1f);//FadeInAudio(this, chaseClip, 3, .1f);
                        Debug.Log("Saw you when you saw me. ");

                    }


                    playerLastSeen = player.position;
                }
                else if(currentMode == Mode.chasing) {
                    chaseMeter -= 1f * Time.deltaTime;
                    if(chaseMeter < 0f) chaseMeter = 0f;
                    playerLastSeen = player.position;
                }

                if(!waitingForScream) ModeChase();
            }

            // If I'm not invis and I see you and you're within melee range //OR if I'm not invis and I can't see you but you ARE in melee range AND hiding
            else if(normalAggro && ((!invisible && playerInAttackRange && chaseMeter != 100f))) {
                AttackPlayer();
                StartCoroutine(DeAggroTimer());
            }

            // If I can't see you but you hit me
            //else if(!playerSeen && takeDamage) {
            //    ModeChase();
            //}

            if(waitingForScream) {
                Vector3 direction = player.position - transform.position;
                direction.y = 0f; // ignore vertical difference

                if(direction.sqrMagnitude < 0.0001f)
                    return;

                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            if(animator.gameObject.activeInHierarchy) animator.SetFloat("Velocity", agent.velocity.magnitude);
            if(shadowAnimator.gameObject.activeInHierarchy) shadowAnimator.SetFloat("Velocity", agent.velocity.magnitude);
            #region Angular Rotation
            Vector3 desired = agent.desiredVelocity;

            Vector3 repulsion = Vector3.zero;
            float checkDistance = 1.2f;
            float strength = 2f;

            RaycastHit hit;

            // Left
            if(Physics.Raycast(transform.position, -transform.right, out hit, checkDistance)) {
                repulsion += hit.normal;
            }

            // Right
            if(Physics.Raycast(transform.position, transform.right, out hit, checkDistance)) {
                repulsion += hit.normal;
            }

            Vector3 finalVelocity = desired + repulsion * strength;
            finalVelocity.y = 0;

            agent.velocity = Vector3.Lerp(
                agent.velocity,
                finalVelocity,
                Time.deltaTime * 5f
            );
            if(finalVelocity.sqrMagnitude > 0.01f) {
                Quaternion rot = Quaternion.LookRotation(finalVelocity);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    rot,
                    Time.deltaTime * 6f
                );
            }
            #endregion

        }

    }

    private IEnumerator DeAggroTimer() {
        normalAggro = false;
        float prevWalkSpeed = walkSpeed;
        walkSpeed = 5f;
        yield return new WaitForSeconds(deAggroCooldown);
        walkSpeed = prevWalkSpeed;
        normalAggro = true;
    }

    private IEnumerator ScreamAnimTimer() {
        monsterSource.pitch = Random.Range(.85f, 1.2f);
        monsterSource.PlayOneShot(screamClips[Random.Range(0, screamClips.Length)]);
        waitingForScream = true;
        animator.Play("Scream");
        shadowAnimator.Play("Scream");

        float priorSpeed = agent.speed;
        agent.speed = 0;
        yield return new WaitForSeconds(1.533f);
        agent.speed = priorSpeed;
        waitingForScream = false;
        ModeChase(); // order above waiting = false?
    }

    private void ModePatrolling() {
        currentMode = Mode.patrolling;

        if(pausingPatrolState) { // I think this is the problem-the position in code. Causing scream and aggro to not work properly
            // because we are never getting a walkpointset?
            agent.SetDestination(transform.position); // stop agent
            return;
        }

        if(!walkPointSet) {
            Vector3 point = RandomNavSphere(transform.position, walkPointRange, groundLayer);
            
            // Means we found something valid.
            if(point != transform.position) {
                walkPoint = point;
                walkPointSet = true;
                if(!invisible && Random.Range(0, pauseChance) == 0) StartCoroutine(PausingPatrol());
                // Go invis, but only if not close to player and it's not the ritual.
                else if(!GetComponent<ConeLOSDetector>().visibilityOverride && ((Random.Range(0, invisibilityOdds) != 0 && !invisible) || 
                    (Random.Range(0, invisibilityOdds) == 0 && invisible && Vector3.Distance(player.position, transform.position) > walkPointRange * 0.15f)) ) {
                    InvertVisibility();
                }

                if(freezingAura && Vector3.Distance(player.position, transform.position) < walkPointRange * 1.75f && !playersBreath.isPlaying) playersBreath.Play();
                else if(freezingAura && Vector3.Distance(player.position, transform.position) > walkPointRange * 1.75f && playersBreath.isPlaying) playersBreath.Stop();
            }
        }
        else {
            agent.SetDestination(walkPoint);
        }
        Vector3 distanceToWalkPoint = transform.position - walkPoint;
        agent.speed = walkSpeed;
        if(distanceToWalkPoint.magnitude < 1f) {
            walkPointSet = false;
        }
    }

    private IEnumerator PausingPatrol() {
        pausingPatrolState = true;
        agent.ResetPath();
        yield return new WaitForSeconds(Random.Range(1, 14));
        pausingPatrolState = false;
    }

    private void SearchWalkPoint() {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);
        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if(Physics.Raycast(walkPoint, -transform.up, 2f, groundLayer)) {
           walkPointSet = true;
        }
    }

    private void ModeChase() {
        currentMode = Mode.chasing;
        if(player.gameObject.GetComponent<PlayerMovement>().isHiding) agent.SetDestination(playerLastSeen);
        else agent.SetDestination(player.position);
        if(player.gameObject.GetComponent<PlayerMovement>().isHiding) walkPoint = playerLastSeen;
        else walkPoint = player.position;
        walkPointSet = true;
        // animator.SetFloat("Velocity", 11);
        agent.speed = runSpeed;
        agent.isStopped = false; // Add this line
        
    }

    public void InvertVisibility() {
        invisible = !invisible;
        if(invisible) {
            foreach(SkinnedMeshRenderer meshRen in meshRenderers) {
                meshRen.enabled = false;
            }
            foreach(GameObject horn in horns) {
                horn.SetActive(false);
            }
        }
        
        if(geistAura && !invisible) geistlightAura.Play();
        else if(geistAura) geistlightAura.Stop();
        ambientSource.volume = invisible ? 0 : 1;
        StartCoroutine(ShadowAnimTimer(invisible));
    }

    private IEnumerator ShadowAnimTimer(bool leave) {
        shadow.SetActive(true);
        if(!leave) shadow.GetComponent<Animator>().Play("ShadowAnim 0");
        yield return new WaitForSeconds(1f);
        shadow.SetActive(false);

        if(!invisible) {
            foreach(SkinnedMeshRenderer meshRen in meshRenderers) {
                meshRen.enabled = true;
            }
            foreach(GameObject horn in horns) {
                horn.SetActive(true);
            }
        }
    }

    // Called by jumpscare to instantly make visible.
    public void MakeVisible() {
        if(invisible) {
            foreach(SkinnedMeshRenderer meshRen in meshRenderers) {
                meshRen.enabled = true;
            }
            foreach(GameObject horn in horns) {
                horn.SetActive(true);
            }
        }
    }

    private void AttackPlayer() {
        agent.SetDestination(transform.position);

        if(!alreadyAttacked) {
            transform.LookAt(player.position);
            alreadyAttacked = true;
            //animator.SetBool("Attack", true);

            Invoke(nameof(ResetAttack), timeBetweenAttacks);

            RaycastHit hit;
            if(Physics.Raycast(transform.position, transform.forward, out hit, attackRange + 4)) {
                /*
                 * You can use this to get the player HUD and call the take damage function.
                 * 
                 */
                animator.Play("Attack" + Random.Range(1, 4).ToString());
                monsterSource.pitch = 1;
                monsterSource.PlayOneShot(attackClip, 0.5f);
                Debug.Log("Hit");
                deathScript.LoseLife();
            }

        }

    }

    private void ResetAttack() {
        alreadyAttacked = false;
        animator.SetBool("Attack", false);
        shadowAnimator.SetBool("Attack", false);
    }

    public void TakeDamage(float damage) {
        health -= damage;
        hitEffect.Play();
        StartCoroutine(TakeDamageCoroutine());

        if(health <= 0) {
            Invoke(nameof(DestroyEnemy), 0.5f);
        }
    }

    private IEnumerator TakeDamageCoroutine() {
        takeDamage = true;
        yield return new WaitForSeconds(2f);
        takeDamage = false;
    }

    private void DestroyEnemy() {
        StartCoroutine(DestroyEnemyCoroutine());
    }

    private IEnumerator DestroyEnemyCoroutine() {
        animator.SetBool("Dead", true);
        shadowAnimator.SetBool("Dead", true);
        yield return new WaitForSeconds(1.8f);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, walkPointRange);
    }

    public Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask) {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit navHit;
        bool found = NavMesh.SamplePosition(randDirection, out navHit, dist, NavMesh.AllAreas);

        NavMeshPath path = new NavMeshPath();
        if(!agent.CalculatePath(navHit.position, path)) {
            walkPoint = Vector3.zero;
            found = false;
        }

        // 3. Path must be complete to be usable
        if(path.status != NavMeshPathStatus.PathComplete) {
            walkPoint = Vector3.zero;
            found = false;
        }

        if(!found) {
            // Return a safe fallback point (the origin)
            Debug.Log("failed");
            return origin;
        }
        //walkPointSet = true;
        return navHit.position;
    }
}
