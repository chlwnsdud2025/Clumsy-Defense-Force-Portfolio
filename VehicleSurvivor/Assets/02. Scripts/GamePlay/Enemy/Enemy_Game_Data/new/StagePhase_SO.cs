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
}
