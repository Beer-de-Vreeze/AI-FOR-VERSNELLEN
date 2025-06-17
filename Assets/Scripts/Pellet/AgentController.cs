using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

public class AgentController : Agent
{
    [Header("Agent")]
    [SerializeField]
    float _speed = 4f;

    [SerializeField]
    float _rotationSpeed = 4f;

    private Rigidbody _rb;

    [Header("Reward")]
    [SerializeField]
    private float _reward = 10f;

    [SerializeField]
    private float _punishment = -15f;

    [SerializeField]
    private float _endReward = 5f;

    [Header("Pellet")]
    [SerializeField]
    Transform _target;

    [SerializeField]
    private GameObject _pellet;

    [SerializeField]
    private int _pelletCount = 2;

    [SerializeField]
    private List<GameObject> _pellets;

    [Header("Distance")]
    [SerializeField]
    private List<float> distanceList = new List<float>();

    [SerializeField]
    private List<float> BaddistanceList = new List<float>();

    [Header("Timer")]
    [SerializeField]
    private float _timeForEpisode = 0f;
    private float _timeLeft;

    [Header("Enviroment")]
    [SerializeField]
    private Transform _enviromentLocation;

    private Material _material;

    [SerializeField]
    private GameObject _env;

    [Header("Hunter")]
    [SerializeField]
    private HunterController _hunterController;

    [SerializeField]
    private float _hunterPunishment = -5f;

    public override void Initialize()
    {
        _rb = GetComponent<Rigidbody>();
        _material = _env.GetComponent<Renderer>().material;
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(Random.Range(-4f, 4f), 0.3f, Random.Range(-4f, 4f));

        CreatePellet();
        // _target.localPosition = new Vector3(Random.Range(-4f, 4f), 0.3f, Random.Range(-4f, 4f));

        EpisodeTimerNew();
    }

    private void Update()
    {
        CheckRemainingTime();
    }

    private void CreatePellet()
    {
        distanceList.Clear();
        BaddistanceList.Clear();

        if (_pellets.Count != 0)
        {
            RemovePellet(_pellets);
        }
        for (int i = 0; i < _pelletCount; i++)
        {
            int counter = 0;
            bool distanceGood;
            bool alreadyDecrement = false;

            GameObject pellet = Instantiate(_pellet);
            pellet.transform.parent = _enviromentLocation;

            Vector3 pelletPosition = new Vector3(
                Random.Range(-4f, 4f),
                0.3f,
                Random.Range(-4f, 4f)
            );

            if (_pellets.Count != 0)
            {
                for (int j = 0; j < _pellets.Count; j++)
                {
                    if (counter < 10)
                    {
                        distanceGood = CheckOverlap(
                            pelletPosition,
                            _pellets[j].transform.localPosition,
                            5f
                        );
                        if (distanceGood == false)
                        {
                            pelletPosition = new Vector3(
                                Random.Range(-4f, 4f),
                                0.3f,
                                Random.Range(-4f, 4f)
                            );
                            j--;
                            alreadyDecrement = true;
                        }
                        distanceGood = CheckOverlap(pelletPosition, transform.localPosition, 5f);
                        if (distanceGood == false)
                        {
                            pelletPosition = new Vector3(
                                Random.Range(-4f, 4f),
                                0.3f,
                                Random.Range(-4f, 4f)
                            );
                            if (alreadyDecrement == false)
                            {
                                j--;
                            }
                        }

                        counter++;
                    }
                    else
                    {
                        j = _pellets.Count;
                    }
                }
            }
            pellet.transform.localPosition = pelletPosition;

            _pellets.Add(pellet);
        }
    }

    public bool CheckOverlap(
        Vector3 objectForOverlap,
        Vector3 alreadyExistingObject,
        float minDistance
    )
    {
        float distance = Vector3.Distance(objectForOverlap, alreadyExistingObject);
        if (distance <= minDistance)
        {
            distanceList.Add(distance);
            return true;
        }
        BaddistanceList.Add(distance);
        return false;
    }

    private void RemovePellet(List<GameObject> toBeDeletedGameObjectList)
    {
        foreach (GameObject pellet in toBeDeletedGameObjectList)
        {
            Destroy(pellet);
        }
        toBeDeletedGameObjectList.Clear();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Add agent's position to the observation space
        sensor.AddObservation(transform.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveRotate = actions.ContinuousActions[0];
        float moveForward = actions.ContinuousActions[1];

        // Move the agent forward based on the action value
        _rb.MovePosition(
            transform.position + transform.forward * moveForward * _speed * Time.deltaTime
        );

        // Rotate the agent around the y-axis based on the action value
        transform.Rotate(0f, moveRotate * _rotationSpeed, 0f, Space.Self);

        /*
        // Alternative movement approach (not currently used)
        Vector3 velocity = new Vector3(moveX, 0, moveZ);

        velocity = velocity.normalized * Time.deltaTime * _speed;

        transform.localPosition += velocity;
        */
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pellet"))
        {
            _pellets.Remove(other.gameObject);
            Destroy(other.gameObject);
            AddReward(_reward);
            if (_pellets.Count == 0)
            {
                _material.color = Color.green;
                RemovePellet(_pellets);
                AddReward(_endReward);
                _hunterController.AddReward(_hunterPunishment);
                _hunterController.EndEpisode();
                EndEpisode();
            }
        }
        if (other.CompareTag("Wall"))
        {
            _material.color = Color.red;
            RemovePellet(_pellets);
            AddReward(_punishment);
            _hunterController.EndEpisode();
            EndEpisode();
        }
    }

    private void EpisodeTimerNew()
    {
        _timeLeft = Time.time + _timeForEpisode;
    }

    private void CheckRemainingTime()
    {
        if (Time.time > _timeLeft)
        {
            _material.color = Color.blue;
            AddReward(_punishment);
            _hunterController.AddReward(_punishment);
            RemovePellet(_pellets);
            _hunterController.EndEpisode();
            EndEpisode();
        }
    }
}
