using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieController : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Transform _target;

    [SerializeField]
    private float _updateSpeed = 0.1f;

    [SerializeField]
    private float _speed = 5f;

    [Header("Health System")]
    [SerializeField]
    private float _maxHealth = 100f;
    private float _currentHealth;

    [SerializeField]
    private GameObject _damageEffectPrefab;

    [SerializeField]
    private GameObject _deathEffectPrefab;

    [SerializeField]
    private Renderer _zombieRenderer;

    private Coroutine _followRoutine;
    private bool _isActive = true;
    private Material _originalMaterial;
    private Color _originalColor;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _speed;
        _currentHealth = _maxHealth;

        if (_zombieRenderer == null)
            _zombieRenderer = GetComponentInChildren<Renderer>();

        if (_zombieRenderer != null && _zombieRenderer.material != null)
        {
            _originalMaterial = _zombieRenderer.material;
            _originalColor = _originalMaterial.color;
        }
    }

    public void Initialize(Transform target)
    {
        _target = target;

        if (_followRoutine != null)
            StopCoroutine(_followRoutine);

        _followRoutine = StartCoroutine(FollowTarget());
    }

    private void Start()
    {
        // Fallback if Initialize wasn't called
        if (_target == null)
        {
            GameObject targetObj = GameObject.FindGameObjectWithTag("Agent");
            if (targetObj != null)
                _target = targetObj.transform;
            else
                Debug.LogWarning("ZombieController: No target found with 'Agent' tag");

            if (_followRoutine == null)
                _followRoutine = StartCoroutine(FollowTarget());
        }
    }

    private IEnumerator FollowTarget()
    {
        WaitForSeconds wait = new WaitForSeconds(_updateSpeed);

        while (_isActive && enabled)
        {
            GameObject targetObj = GameObject.FindGameObjectWithTag("Agent");
            if (targetObj != null)
            {
                _target = targetObj.transform;
                _agent.SetDestination(_target.position);
            }
            else
            {
                Debug.LogWarning("ZombieController: No target found with 'Agent' tag");
            }
            yield return wait;
        }
    }

    public void MoveZombie(Vector3 pos)
    {
        if (_agent != null && _agent.isActiveAndEnabled)
            _agent.SetDestination(pos);
    }

    public void SetActive(bool active)
    {
        _isActive = active;
        if (!active && _agent != null)
            _agent.isStopped = true;
        else if (_agent != null)
            _agent.isStopped = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Agent"))
        {
            // Zombie reached the agent, can add damage or other effects here
            Debug.Log("Zombie reached agent!");
        }
    }

    public void TakeDamage(float damage)
    {
        if (_currentHealth <= 0)
            return;

        _currentHealth -= damage;

        // Visual feedback
        StartCoroutine(FlashDamage());

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashDamage()
    {
        if (_zombieRenderer != null && _zombieRenderer.material != null)
        {
            _zombieRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            _zombieRenderer.material.color = _originalColor;
        }
        else
        {
            yield return null;
        }

        if (_damageEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                _damageEffectPrefab,
                transform.position + Vector3.up,
                Quaternion.identity
            );
            Destroy(effect, 1f);
        }
    }

    private void Die()
    {
        // Play death effect if available
        if (_deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                _deathEffectPrefab,
                transform.position,
                Quaternion.identity
            );
            Destroy(effect, 2f);
        }

        // Deactivate and stop zombie behavior
        SetActive(false);

        // Destroy after a short delay to allow effects to play
        Destroy(gameObject, 0.1f);
    }
}
