using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class HunterController : Agent
{
    [Header("Agent")]
    [SerializeField]
    float _speed = 4f;

    [SerializeField]
    float _rotationSpeed = 4f;

    private Rigidbody _stijflichaam;

    [Header("Reward")]
    private float _reward = 10f;

    [SerializeField]
    private float _punishment = -15f;

    [SerializeField]
    private float _caughtPunishment = 13f;

    [Header("Prey")]
    [SerializeField]
    private GameObject _prey;

    [SerializeField]
    private AgentController _preyController;

    [Header("Enviroment")]
    private Material _material;

    [SerializeField]
    private GameObject _env;

    public override void Initialize()
    {
        _stijflichaam = GetComponent<Rigidbody>();
        _material = _env.GetComponent<Renderer>().material;
    }

    public override void OnEpisodeBegin()
    {
        //Huner
        Vector3 hunterPosition = new Vector3(Random.Range(-4f, 4f), 0.3f, Random.Range(-4f, 4f));

        bool distanceGood = _preyController.CheckOverlap(
            _prey.transform.localPosition,
            hunterPosition,
            5f
        );

        while (!distanceGood)
        {
            hunterPosition = new Vector3(Random.Range(-4f, 4f), 0.3f, Random.Range(-4f, 4f));
            distanceGood = _preyController.CheckOverlap(
                _prey.transform.localPosition,
                hunterPosition,
                5f
            );
        }
        transform.localPosition = hunterPosition;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveRotate = actions.ContinuousActions[0];
        float moveForward = actions.ContinuousActions[1];

        _stijflichaam.MovePosition(
            transform.position + transform.forward * moveForward * _speed * Time.deltaTime
        );
        transform.Rotate(0f, moveRotate * _rotationSpeed, 0f, Space.Self);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agent"))
        {
            AddReward(_reward);
            _material.color = Color.yellow;
            _preyController.AddReward(_caughtPunishment);
            _preyController.EndEpisode();
            EndEpisode();
        }
        if (other.CompareTag("Wall"))
        {
            _material.color = Color.red;
            AddReward(_punishment);
            _preyController.EndEpisode();
            EndEpisode();
        }
    }
}
