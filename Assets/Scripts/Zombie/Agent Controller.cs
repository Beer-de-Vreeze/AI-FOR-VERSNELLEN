using System.Collections;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MechAgentController : Agent
{
    [Header("Agent")]
    [SerializeField]
    private float _speed = 2f;

    [SerializeField]
    private float _rotationSpeed = 2f;

    [Header("Reward")]
    [SerializeField]
    private float _punishment = -15f;
    private Rigidbody _rb;

    [SerializeField]
    private float _rewardForHit = 30f;

    [SerializeField]
    private float _punishmentForMiss = -1f;

    [SerializeField]
    private float _rewardForClearingAllZombies = 50f;

    [Header("Gun")]
    [SerializeField]
    private GunController _gunController;
    private bool _canShoot,
        _hitTarget,
        _hasShot = false;

    private int _timeUntilNextShot = 0;

    [SerializeField]
    private int _minTimeUntilNextShot = 25;

    [Header("Observation")]
    [SerializeField]
    private float _zombieDetectionRadius = 15f;

    [SerializeField]
    private int _maxZombiesToObserve = 5;

    [Header("World Behaviors")]
    [SerializeField]
    private WorldBehaviors _worldBehaviors;

    public override void Initialize()
    {
        _rb = GetComponent<Rigidbody>();
        _worldBehaviors = FindFirstObjectByType<WorldBehaviors>();
        if (_gunController == null)
        {
            _gunController = GetComponentInChildren<GunController>();
            if (_gunController == null)
                Debug.LogError("Gun Controller not found on agent!");
        }
    }

    public override void OnEpisodeBegin()
    {
        // Ensure all necessary objects are initialized
        if (_worldBehaviors == null)
        {
            Debug.LogError("_worldBehaviors is not set!");
            EndEpisode();
            return;
        }

        _hasShot = false;
        _timeUntilNextShot = 0;
        _worldBehaviors.ClearAllZombies();
        _worldBehaviors.StartSpawningZombies();
        _worldBehaviors.SpawnAgent();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent position and rotation
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.forward);
        sensor.AddObservation(_rb.linearVelocity);

        // Gun state
        sensor.AddObservation(_hasShot);
        sensor.AddObservation((float)_timeUntilNextShot / _minTimeUntilNextShot);

        // Detect zombies in real-time
        Collider[] allColliders = Physics.OverlapSphere(transform.position, _zombieDetectionRadius);

        List<Collider> zombieColliders = new List<Collider>();
        foreach (Collider collider in allColliders)
        {
            if (collider != null && collider.CompareTag("Zombie"))
            {
                zombieColliders.Add(collider);
            }
        }

        // Add how many zombies we're observing
        int zombiesToObserve = Mathf.Min(zombieColliders.Count, _maxZombiesToObserve);
        sensor.AddObservation((float)zombiesToObserve / _maxZombiesToObserve);

        // Add zombie data - unsorted, let the agent learn priority
        for (int i = 0; i < _maxZombiesToObserve; i++)
        {
            if (i < zombiesToObserve)
            {
                // Zombie relative position
                Vector3 relativePos = transform.InverseTransformPoint(
                    zombieColliders[i].transform.position
                );
                sensor.AddObservation(relativePos / _zombieDetectionRadius);
            }
            else
            {
                // Padding for non-existent zombies
                sensor.AddObservation(Vector3.zero);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        _canShoot = false;

        float move_rotation = actions.ContinuousActions[0];
        float move_forward = actions.ContinuousActions[1];

        bool shoot = actions.DiscreteActions[0] > 0;

        _rb.MovePosition(
            transform.position + transform.forward * move_forward * _speed * Time.deltaTime
        );
        transform.Rotate(0f, move_rotation * _rotationSpeed, 0f, Space.Self);

        if (shoot && !_hasShot)
        {
            _canShoot = true;
        }
        if (_canShoot)
        {
            _hitTarget = _gunController.ShootGun();
            _timeUntilNextShot = _minTimeUntilNextShot;
            _hasShot = true;
            if (_hitTarget)
            {
                AddReward(_rewardForHit);

                // Check if all zombies are killed after a successful hit
                if (AreAllZombiesEliminated())
                {
                    AddReward(_rewardForClearingAllZombies);
                    EndEpisode();
                }
            }
            else
            {
                AddReward(_punishmentForMiss);
            }
        }
    }

    // Check if all zombies have been eliminated
    private bool AreAllZombiesEliminated()
    {
        Collider[] allColliders = Physics.OverlapSphere(transform.position, 100f);

        foreach (Collider collider in allColliders)
        {
            if (collider.CompareTag("Zombie"))
            {
                return false;
            }
        }

        return true;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");

        discreteActions[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            AddReward(_punishment);
            EndEpisode();
        }
        if (other.gameObject.CompareTag("Zombie"))
        {
            AddReward(_punishment);
            EndEpisode();
        }
    }

    private void FixedUpdate()
    {
        if (_hasShot)
        {
            _timeUntilNextShot--;
            if (_timeUntilNextShot <= 0)
            {
                _hasShot = false;
            }
        }
    }
}
