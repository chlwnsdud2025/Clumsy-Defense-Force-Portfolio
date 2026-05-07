using Unity.Entities;
using Unity.Mathematics;

// 1. 적의 길찾기 상태를 관리하는 컴포넌트
//public struct EnemyNavData : IComponentData
//{
//    public float NextCalculateTime; // 다음 길 찾기 연산을 할 시간 (쿨타임)
//    public int CurrentWaypointIndex; // 현재 향하고 있는 버퍼의 인덱스
//    public bool HasPath;             // 유효한 경로가 있는지 여부
//}

//// 2. 경로의 꺾이는 점들을 저장할 동적 버퍼 배열
//[InternalBufferCapacity(16)] // 메모리 최적화를 위해 기본 크기를 16으로 잡음
//public struct WaypointBuffer : IBufferElementData
//{
//    public float3 Position;
//}