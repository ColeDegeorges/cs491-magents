using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootAndShootAgent : Agent
{
    public int roundTime;
    private float roundStart;
    public float maxSpeed;
    public GameObject enemy;

    public float sightDistance;

    private Vector2 lootPos;
    private float lastLootCollection;

    public float respawnRange;

    public float shotDelay;
    public float range;
    private float lastShot;
    private float lastKill;
    public float turnSpeed;

    /// <summary>
    /// Use this method to initialize your agent. This method is called when the agent is created. 
    /// Do not use Awake(), Start() or OnEnable().
    /// </summary>
    public override void InitializeAgent()
    {
        base.InitializeAgent();
    }

    /// <summary>
    /// Must return a list of floats corresponding to the state the agent is in. If the state space type is discrete, 
    /// return a list of length 1 containing the float equivalent of your state.
    /// </summary>
    /// <returns></returns>
    public override List<float> CollectState()
    {
        Debug.Log("Collecting State");

        List<float> state = new List<float>();

        // New implementation, raycasts in 8 directions with information on what they hit
        RaycastHit2D[] rays = new RaycastHit2D[16];
        int counter = 0;
        for (float i = -1; i <= 1; i += 0.5f)
        {
            for (float j = -1; j <= 1; j += 0.5f)
            {
                if ((j != 1 && j != -1) && (i < 1 && i > -1))
                    continue;
                rays[counter] = Physics2D.Raycast(gameObject.transform.position, gameObject.transform.right * i + gameObject.transform.up * j, sightDistance);
                
                // Reward the player if they're facing an enemy, they're aiming correctly :D
                /*if (j == 1 && i == 0)
                {
                    if (rays[counter])
                    {
                        if (rays[counter].collider.tag == "Enemy")
                        {
                            reward += 0.05f;
                        }
                    }
                }*/

                // Increment through the array
                counter++;
            }
        }
            counter = 0;
        for (float i = -1; i <= 1; i += 0.5f)
        {
            for (float j = -1; j <= 1; j += 0.5f)
            {
                if ((j != 1 && j != -1) && (i < 1 && i > -1))
                    continue;
                // Add the raycast information into the state
                // Add the distance
                state.Add(rays[counter].distance / sightDistance);
                // Add information about what was hit
                if (rays[counter])
                {
                    if (rays[counter].collider.tag == "Gold")
                        state.Add(1.0f);
                    else
                        state.Add(0.0f);

                    if (rays[counter].collider.tag == "Enemy")
                        state.Add(1.0f);
                    else
                        state.Add(0.0f);

                    if (rays[counter].collider.tag == "Wall")
                        state.Add(1.0f);
                    else
                        state.Add(0.0f);
                }
                else
                {
                    // No gold
                    state.Add(0.0f);
                    // No enemy
                    state.Add(0.0f);
                    // No wall
                    state.Add(0.0f);
                }
                counter++;
            }
        }
        // We should probably know what our cooldowns are when moving around the enemy
        float cooldown = (Time.time - lastShot) / shotDelay;
        state.Add(cooldown > 1 ? 1 : cooldown);

        return state;
    }

    private void shuffleTarget(GameObject target)
    {
        target.transform.position = new Vector3(Random.Range(transform.parent.position.x - respawnRange, transform.parent.position.x + respawnRange), Random.Range(transform.parent.position.y - respawnRange, transform.parent.position.y + respawnRange), transform.parent.position.z);
    }

    private void punishMiss()
    {
        reward -= 0.5f;
    }

    private void meleeAttack()
    {
        if (Time.time - lastShot < shotDelay)
        {
            reward -= 0.5f;
            return;
        }
        else
            lastShot = Time.time;

        RaycastHit2D hit = Physics2D.Raycast(gameObject.transform.position, gameObject.transform.up, range);
        Debug.DrawLine(gameObject.transform.position, gameObject.transform.position + gameObject.transform.up * range);
        //GameObject projectile = GameObject.Instantiate(arrow, gameObject.transform.position, gameObject.transform.rotation, null);
        if (hit.collider != null)
        {
            Debug.DrawLine(gameObject.transform.position, hit.point);
            if (hit.collider.tag == "Enemy")
            {
                reward += 25;
                shuffleTarget(hit.collider.gameObject);
                lastKill = Time.time;

                // Feed a reward into the Movement agent for allowing us to kill it
                gameObject.transform.parent.GetComponent<LooterAgent>().reward += 25;
            }
            else
                punishMiss();
        }
        else
        {
            punishMiss();
        }
    }

    /// <summary>
    /// This function will be called every frame, you must define what your agent will do given the input actions. 
    /// You must also specify the rewards and whether or not the agent is done. To do so, modify the public fields of the agent reward and done.
    /// </summary>
    /// <param name="act"></param>
    public override void AgentStep(float[] act)
    {
        if (brain.brainParameters.actionSpaceType == StateType.discrete)
        {

        }
        else if (brain.brainParameters.actionSpaceType == StateType.continuous)
        {
            Debug.Log("We are here");
            // Give the AI more direct control, directly affect the velocity of the player. Allows for more precise movement
            gameObject.GetComponent<Rigidbody2D>().velocity = Vector2.ClampMagnitude(transform.right * act[0] + transform.up * act[1], maxSpeed);

            // TESTING
            // I wonder if I can get away with letting one script be in charge of movement and melee
            if (act[2] > 0)
            {
                meleeAttack();
            }
            Debug.Log(act[3]);
            gameObject.transform.Rotate(new Vector3(0, 0, act[3] * turnSpeed));
        }

        // You should be killing, why you not kill?
        // Punish
        if (Time.time - lastKill > shotDelay)
        {
            reward -= 0.1f;
        }

        // Just in case it somehow breaks physics while it's training
        if ((Mathf.Abs(gameObject.transform.position.x - transform.parent.position.x) > 2) ||
            (Mathf.Abs(gameObject.transform.position.y - transform.parent.position.y) > 2))
        {
            done = true;
            Debug.Log("Looter: Out of bounds.");
            reward -= 1000;
        }
        else if (Time.time - roundStart > roundTime)
        {
            Debug.Log("Looter: Out of time.");
            done = true;
        }
    }

    /// <summary>
    /// This function is called at start, when the Academy resets and when the agent is done (if Reset On Done is checked).
    /// </summary>
    public override void AgentReset()
    {
        roundStart = Time.time;
        gameObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        gameObject.GetComponent<Rigidbody2D>().angularVelocity = 0;
        gameObject.transform.position = transform.parent.position;
        gameObject.transform.up = Vector2.up;

        GameObject[] loot = GameObject.FindGameObjectsWithTag("Gold");
        foreach (GameObject gold in loot)
        {
            if (gold.transform.parent == transform.parent)
            {
                gold.GetComponent<Gold>().randomizeGold();
            }
            shuffleEnemy();
        }
    }

    private void shuffleEnemy()
    {
        enemy.transform.position = new Vector3(Random.Range(transform.parent.position.x - 2, transform.parent.position.x + 2), Random.Range(transform.parent.position.y - 2, transform.parent.position.y + 2), transform.parent.position.z);
    }

    /// <summary>
    /// If Reset On Done is not checked, this function will be called when the agent is done. 
    /// Reset() will only be called when the Academy resets.
    /// </summary>
    public override void AgentOnDone()
    {
    }

}