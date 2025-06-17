using System.Collections;
using UnityEngine;

public class GunController : MonoBehaviour
{
    [SerializeField]
    private Transform _SpawnPoint;

    [SerializeField]
    LineRenderer _laser_line;

    [Header("Laser Settings")]
    [SerializeField]
    float _laserDuration = 0.05f;

    [SerializeField]
    float _laserRange = 600f;

    [Header("Gun Statistics")]
    [SerializeField]
    private float _damage = 25f;

    [SerializeField]
    private float _spread = 0.02f;

    [Header("Visual Effects")]
    [SerializeField]
    private GameObject _zombieHitEffectPrefab;

    [SerializeField]
    private GameObject _wallHitEffectPrefab;

    public bool ShootGun()
    {
        // Get the agent's transform (parent of this gun)
        Transform agentTransform = transform.parent;
        
        // Use the agent's forward direction as the base shooting direction
        Vector3 shotDirection = agentTransform.forward;
        
        // Apply spread if configured
        if (_spread > 0)
        {
            shotDirection += new Vector3(
                Random.Range(-_spread, _spread),
                Random.Range(-_spread, _spread),
                Random.Range(-_spread, _spread)
            );
        }

        // Use spawn point position but agent direction
        if (Physics.Raycast(_SpawnPoint.position, shotDirection, out RaycastHit hit, _laserRange))
        {
            _laser_line.SetPosition(0, _SpawnPoint.position);
            _laser_line.SetPosition(1, hit.point);
            _laser_line.enabled = true;

            // Play hit effect based on what was hit
            PlayHitEffect(hit);

            if (hit.collider.CompareTag("Zombie"))
            {
                // Apply damage instead of destroying
                ZombieController zombie = hit.collider.GetComponent<ZombieController>();
                if (zombie != null)
                {
                    zombie.TakeDamage(_damage);
                }

                StartCoroutine(ShootLaser());
                return true;
            }
            else if (hit.collider.CompareTag("Wall"))
            {
                StartCoroutine(ShootLaser());
                return false;
            }
        }

        StartCoroutine(ShootLaser());
        return false;
    }

    private void PlayHitEffect(RaycastHit hit)
    {
        if (hit.collider.CompareTag("Zombie") && _zombieHitEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                _zombieHitEffectPrefab,
                hit.point,
                Quaternion.LookRotation(hit.normal)
            );
            Destroy(effect, 1f); // Clean up after 1 second
        }
        else if (hit.collider.CompareTag("Wall") && _wallHitEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                _wallHitEffectPrefab,
                hit.point,
                Quaternion.LookRotation(hit.normal)
            );
            Destroy(effect, 1f); // Clean up after 1 second
        }
    }

    private IEnumerator ShootLaser()
    {
        yield return new WaitForSeconds(_laserDuration);
        _laser_line.enabled = false;
    }
}
