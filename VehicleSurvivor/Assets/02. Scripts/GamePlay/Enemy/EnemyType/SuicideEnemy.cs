using UnityEngine;

public class SuicideEnemy : Enemy
{
    [Header("Suicide Settings")]
    public float fuseTime = 1.0f;        
    public float explosionRadius = 3.0f; 
    public float explosionDamage = 50f;  

    private bool isFusing = false;       
    private float fuseTimer;
    private float originalSeparation;    

    public override void SpawnSetup(Transform target, Vector3 spawnPosition, EnemyManager manager)
    {
        base.SpawnSetup(target, spawnPosition, manager);
        isFusing = false;
        originalSeparation = separationWeight;
        isTrackingPlayer = true;
    }

    
    protected override void Attack()
    {
        if (isFusing) return;

        
        isFusing = true;
        fuseTimer = fuseTime;

        
        originalSeparation = separationWeight;
        separationWeight = 0f;

        
        isTrackingPlayer = false;

        Debug.Log("자폭병 점화");
        
    }

    protected override void ExecuteBehavior(float deltaTime, out Vector3 desiredVelocity)
    {
        if (!isFusing)
        {
            base.ExecuteBehavior(deltaTime, out desiredVelocity);
        }
        else
        {
            desiredVelocity = Vector3.zero;

            fuseTimer -= deltaTime;
            if (fuseTimer <= 0f)
            {
                Explode();
            }
        }
    }

    private void Explode()
    {
        //폭발 만들기!!

        Debug.Log("자폭");

        
        //자폭 시 경험치 X
        if (myManager != null)
        {
            myManager.ReturnToPool(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}