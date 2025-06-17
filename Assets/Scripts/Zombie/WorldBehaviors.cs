using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldBehaviors : MonoBehaviour
{
    [Header("Agent Settings")]
    [SerializeField]
    private MechAgentController _agentController;

    [Header("Zombie Settings")]
    [SerializeField]
    private ZombieController _zombieController;

    [SerializeField]
    private int _zombieCount = 50;

    [SerializeField]
    private float _spawnZombieInterval = 0.5f;

    [SerializeField]
    private List<ZombieController> _zombies = new List<ZombieController>();

    [Header("Environment")]
    [SerializeField]
    private Transform _enviromentLocation;

    [SerializeField]
    private List<GameObject> _spawnPoints = new List<GameObject>();

    [SerializeField]
    private float _cleanupDistance = 100f;

    [Header("Performance")]
    [SerializeField]
    private int _maxZombiesPerBatch = 5;

    [SerializeField]
    private float _physicsUpdateInterval = 0.2f;

    private bool _isSpawningZombies = false;
    private Coroutine _spawningCoroutine;
    private Coroutine _maintenanceCoroutine;

    void Start()
    {
        if (_agentController == null)
        {
            _agentController = FindFirstObjectByType<MechAgentController>();
            if (_agentController == null)
                Debug.LogError("No MechAgentController found in scene!");
        }

        // Don't start spawning in Start, let the agent controller call StartSpawningZombies
        _maintenanceCoroutine = StartCoroutine(ZombieMaintenanceRoutine());
    }

    public void SpawnAgent()
    {
        //find an object with the tag "Agent" if it exists destroy it
        GameObject agent = GameObject.FindGameObjectWithTag("Agent");
        //set it to the middle
        agent.transform.position = new Vector3(0, 0.5f, 0);
    }

    public void StartSpawningZombies()
    {
        // Prevent duplicate spawning coroutines
        StopSpawning();

        _isSpawningZombies = true;
        _spawningCoroutine = StartCoroutine(ContinuousZombieSpawning());
    }

    public IEnumerator ContinuousZombieSpawning()
    {
        while (_isSpawningZombies)
        {
            if (_zombies.Count < _zombieCount)
            {
                int zombiesToSpawn = Mathf.Min(_maxZombiesPerBatch, _zombieCount - _zombies.Count);
                for (int i = 0; i < zombiesToSpawn; i++)
                {
                    SpawnNewZombie();
                    yield return new WaitForSeconds(0.05f);
                }
            }
            yield return new WaitForSeconds(_spawnZombieInterval);
        }
    }

    private void SpawnNewZombie()
    {
        if (_spawnPoints.Count <= 0)
            return;

        int randomIndex = Random.Range(0, _spawnPoints.Count);
        GameObject spawnPoint = _spawnPoints[randomIndex];
        Vector3 spawnPosition = new Vector3(
            Random.Range(
                spawnPoint.transform.position.x - spawnPoint.transform.localScale.x / 2,
                spawnPoint.transform.position.x + spawnPoint.transform.localScale.x / 2
            ),
            1.5f,
            Random.Range(
                spawnPoint.transform.position.z - spawnPoint.transform.localScale.z / 2,
                spawnPoint.transform.position.z + spawnPoint.transform.localScale.z / 2
            )
        );

        ZombieController zombie = Instantiate(
            _zombieController,
            spawnPosition,
            Quaternion.identity
        );

        // Pass the agent's transform to the zombie directly
        if (_agentController != null)
            zombie.Initialize(_agentController.transform);

        _zombies.Add(zombie);
    }

    private IEnumerator ZombieMaintenanceRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_physicsUpdateInterval);

        while (true)
        {
            CleanupDeadZombies();
            CleanupDistantZombies();
            yield return wait;
        }
    }

    private void CleanupDeadZombies()
    {
        _zombies.RemoveAll(zombie => zombie == null);
    }

    private void CleanupDistantZombies()
    {
        if (_agentController == null)
            return;

        List<ZombieController> zombiesToRemove = new List<ZombieController>();

        foreach (ZombieController zombie in _zombies)
        {
            // Check distance from agent, not from world position
            if (
                zombie != null
                && Vector3.Distance(_agentController.transform.position, zombie.transform.position)
                    > _cleanupDistance
            )
            {
                zombiesToRemove.Add(zombie);
            }
        }

        if (zombiesToRemove.Count > 0)
        {
            RemoveZombie(zombiesToRemove);
        }
    }

    public void RemoveZombie(List<ZombieController> zombies_to_remove)
    {
        foreach (ZombieController zombie in zombies_to_remove)
        {
            if (zombie != null)
            {
                _zombies.Remove(zombie);
                Destroy(zombie.gameObject);
            }
        }
    }

    public void StopSpawning()
    {
        _isSpawningZombies = false;

        if (_spawningCoroutine != null)
        {
            StopCoroutine(_spawningCoroutine);
            _spawningCoroutine = null;
        }
    }

    public void ClearAllZombies()
    {
        StopSpawning(); // Stop spawning before clearing
        RemoveZombie(new List<ZombieController>(_zombies));
        _zombies.Clear();
    }

    private void OnDestroy()
    {
        StopSpawning();
        if (_maintenanceCoroutine != null)
            StopCoroutine(_maintenanceCoroutine);
    }
}
