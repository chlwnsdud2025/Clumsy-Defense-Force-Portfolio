using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 한 페이즈의 데이터. 제한 시간과 정상/악몽 웨이브 스케줄을 담는다.
/// StageTheme_SO.phases 에 순서대로 등록해 스테이지를 구성한다.
/// </summary>
[CreateAssetMenu(fileName = "NewStage_Phase", menuName = "Survivor/Stage_Phase")]
public class StagePhase_SO : ScriptableObject
{
    [Tooltip("제한 시간(초). 초과 시 nightmareWaves 로 전환")]
    public float timeLimit = 300f;

    [Tooltip("정상 스케줄. waveStartTime 은 페이즈 시작 기준")]
    public List<WaveEnemy_DataSO> phaseWaves;

    [Tooltip("시간 초과 시 악몽 스케줄. waveStartTime 은 악몽 진입 기준")]
    public List<WaveEnemy_DataSO> nightmareWaves;

    [Header("Enemy Scaling (이 페이즈에 스폰되는 잡몹·미니보스 배율)")]
    [Tooltip("프리팹 maxHealth 에 곱하는 배율")]
    [Min(0.01f)] public float healthMultiplier = 1f;

    [Tooltip("접촉·폭발·레이저·박격포 피해에 곱하는 배율")]
    [Min(0f)] public float damageMultiplier = 1f;

    [Tooltip("경험치·골드 보상에 곱하는 배율")]
    [Min(0f)] public float rewardMultiplier = 1f;
}

/// <summary>스폰 시점의 페이즈 배율. 프리팹 기본값에 곱해 적용한다.</summary>
public readonly struct EnemyScaling
{
    public static readonly EnemyScaling Identity = new EnemyScaling(1f, 1f, 1f);

    public readonly float Health;
    public readonly float Damage;
    public readonly float Reward;

    public EnemyScaling(float health, float damage, float reward)
    {
        Health = health;
        Damage = damage;
        Reward = reward;
    }

    public static EnemyScaling From(StagePhase_SO phase)
    {
        if (phase == null) return Identity;
        return new EnemyScaling(phase.healthMultiplier, phase.damageMultiplier, phase.rewardMultiplier);
    }
}
