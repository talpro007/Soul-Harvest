using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform arenaCenter;

    private bool spawned;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (spawned) return;
        if (other.GetComponentInParent<PlayerMovement>() == null) return;

        spawned = true;
        GameObject boss = Instantiate(bossPrefab, spawnPoint.position, Quaternion.identity);
        boss.GetComponent<Boss>().Init(arenaCenter);
    }
}