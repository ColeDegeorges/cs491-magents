﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcherAgent : Agent
{
    [Header("Specific to ArchetTesting")]
    public Transform home;
    public GameObject arrow;
    public GameObject looter;
    public GameObject enemy;
    public float range;
    public float turnSpeed;
	public float shotDistance;
    public bool debug;
    public bool spawnSafety;

    public float shotDelay;
    public float lastShot;
    private float startTime = 0;
    public float roundTime;

    public float currentAngle;

    public float x, y, fire;
    public uint numRays;

    public float spawnDelay;
    private bool hasKilled;
    private float lastKill;

    public override List<float> CollectState()
    {
        List<float> state = new List<float>();
        float cd = (Time.time - lastShot) / shotDelay;
        // Keep the cooldown normalized
        state.Add((cd > 1 ? 1 : 0));

        // New implementation, raycasts in 8 directions with information on what they hit
        RaycastHit2D[] rays = new RaycastHit2D[numRays];
        // New loop
        for (int i = 0; i < rays.Length; i++)
        {
            rays[i] = Physics2D.Raycast(gameObject.transform.position, Quaternion.AngleAxis((360f / rays.Length) * i, Vector3.forward) * transform.up, shotDistance);
            // Debug
            if (debug)
                if (rays[i])
                    Debug.DrawLine(gameObject.transform.position, rays[i].point, rays[i].collider.tag == "Enemy" ? Color.green : Color.blue);
                else
                    Debug.DrawLine(gameObject.transform.position, (gameObject.transform.position + (Quaternion.AngleAxis((360f / rays.Length) * i, Vector3.forward) * transform.up).normalized * shotDistance));
        }
        for (int i = 0; i < rays.Length; i++)
        {
            state.Add(rays[i].distance / shotDistance);
            // Add information about what was hit
            if (rays[i])
            {
                // We only need information about whether it's an enemy or not, really
                if (rays[i].collider.tag == "Enemy")
                {
                    state.Add(1.0f);
                    // Nudge it towards aiming correctly
                    if (i == 0)
                       reward += 0.01f;
                }
                else
                    state.Add(0.0f);
            }
            else
                state.Add(0.0f);
        }
        return state;
    }

    private void shuffleTarget(GameObject target)
    {
        // Generate a random position within the battleground for the enemy to spawn
        Vector3 randV = new Vector3(Random.Range(-range, range), Random.Range(-range, range), 0);

        // Push the random position to an outer radius
        randV.Normalize();
        randV.Scale(new Vector3(range, range, 0));

        target.transform.position = home.position + randV;
        if ((target.transform.position - gameObject.transform.position).magnitude < 1.0f && spawnSafety)
            shuffleTarget(target);
    }

    private void punishMiss()
    {
        reward -= 2f;
    }

    private void fireArrow()
    {
        if (Time.time - lastShot < shotDelay)
            return;
        else
            lastShot = Time.time;

		RaycastHit2D hit = Physics2D.Raycast(gameObject.transform.position, gameObject.transform.up, shotDistance);
        // Debug
		Debug.DrawLine (gameObject.transform.position, gameObject.transform.position + gameObject.transform.up * shotDistance);
        //GameObject projectile = GameObject.Instantiate(arrow, gameObject.transform.position, gameObject.transform.rotation, null);
        if (hit.collider != null)
        {
            if (hit.collider.tag == "Enemy")
            {
                reward += 5;
                GameObject.Destroy(hit.collider.gameObject);
                hasKilled = true;
                lastKill = Time.time;
                if (looter.GetComponent<Agent>())
                    looter.GetComponent<Agent>().reward += 5;
            }
            else
                punishMiss();
        }
        else
            punishMiss();
    }

    // to be implemented by the developer
    public override void AgentStep(float[] act)
    {
        if (brain.brainParameters.actionSpaceType == StateType.discrete)
        {

        }
        else if (brain.brainParameters.actionSpaceType == StateType.continuous)
        {
            gameObject.transform.Rotate(new Vector3(0, 0, act[0] * turnSpeed));
            // Debug
            x = act[0]; fire = act[1];
            // Fire
            if (act[1] > 0)
            {
                // Just leave it be, I need it to aim before it can shoot
                fireArrow();
            }
        }
        // Reset the round if necessary
        if (Time.time - startTime > roundTime)
        {
            done = true;
            return;
        }

        // Spawn a new enemy if necessary
        if (hasKilled && Time.time - lastKill > spawnDelay)
            spawnEnemy();
    }

    private void spawnEnemy()
    {
        GameObject spawn = GameObject.Instantiate(enemy, transform.parent.parent);
        spawn.GetComponent<AttackPlayer>().player = gameObject;
        shuffleTarget(spawn);
        hasKilled = false;
    }

    // to be implemented by the developer
    public override void AgentReset()
    {
        gameObject.transform.up = new Vector3(0, 1, 0);
        Transform[] targets = new Transform[transform.parent.parent.childCount];
        for (int i = 0; i < transform.parent.parent.childCount; i++)
        {
            if (transform.parent.parent.GetChild(i).tag == "Enemy")
                targets[i] = transform.parent.parent.GetChild(i);
            else
                targets[i] = null;
        }
        for (int i = 0; i < targets.Length; i++)
            if (targets[i] != null)
                Destroy(targets[i].gameObject);

        startTime = Time.time;
        lastShot = Time.time - shotDelay;
        hasKilled = true;
        lastKill = Time.time - spawnDelay;
    }
}
