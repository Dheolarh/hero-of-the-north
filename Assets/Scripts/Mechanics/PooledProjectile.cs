using UnityEngine;
[DisallowMultipleComponent]
public class PooledProjectile : MonoBehaviour
{
    private Vector2 moveDir;
    private float   speed;
    private bool    movesOnYAxis;   // true = Up/Down,  false = Left/Right
    private float   despawnValue;
    private ProjectileSpawner spawner;

    public void Initialize(
        Vector2 moveDirection,
        float   speed,
        bool    movesOnYAxis,
        float   despawnValue,
        ProjectileSpawner spawner)
    {
        this.moveDir      = moveDirection.normalized;
        this.speed        = speed;
        this.movesOnYAxis = movesOnYAxis;
        this.despawnValue = despawnValue;
        this.spawner      = spawner;
    }

    void Update()
    {
        // Translate in world space so parenting doesn't affect direction
        transform.Translate(moveDir * speed * Time.deltaTime, Space.World);

        // Check despawn boundary
        if (HasPassedDespawn())
            spawner?.ReturnToPool(gameObject);
    }

    private bool HasPassedDespawn()
    {
        if (movesOnYAxis)
        {
            float y = transform.position.y;
            // Down → despawnValue is below spawn (e.g. -12).  Up → above (e.g. +12).
            return moveDir.y < 0 ? y < despawnValue : y > despawnValue;
        }
        else
        {
            float x = transform.position.x;
            return moveDir.x < 0 ? x < despawnValue : x > despawnValue;
        }
    }
}
