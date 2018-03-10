using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LooterAgent : Agent
{
    public int roundTime;
    private float roundStart;
    public float maxSpeed;
    public GameObject enemy;

    public float sightDistance;

    private Vector2 lootPos;
    private float lastLootCollection;

    public float rotation;
    public float cooldown1;

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
                rays[counter] = Physics2D.Raycast(gameObject.transform.position, new Vector2(i, j), sightDistance);
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
        cooldown1 = (transform.GetChild(0).GetComponent<ArcherAgent>().shotDelay - (Time.time - transform.GetChild(0).GetComponent<ArcherAgent>().lastShot)) / transform.GetChild(0).GetComponent<ArcherAgent>().shotDelay;
        state.Add(cooldown1);

        return state;
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
            // Give the AI more direct control, directly affect the velocity of the player. Allows for more precise movement
            gameObject.GetComponent<Rigidbody2D>().velocity = Vector2.ClampMagnitude(new Vector2(act[0], act[1]), maxSpeed);

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
        gameObject.transform.position = transform.parent.position;

        GameObject[] loot = GameObject.FindGameObjectsWithTag("Gold");
        foreach(GameObject gold in loot)
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