# Runtime Weapon Refactor Progress

## Summary

이 문서는 이번 채팅에서 실제로 무엇을 변경했는지, 어디까지 끝났는지, 다음 작업자가 무엇부터 이어서 하면 되는지 정리한 진행 로그다.

참고:
- 별도 분석/계획 문서는 `RuntimeWeaponRefactorHandoff.md`
- 이 문서는 "실제 반영된 변경" 중심

## What Was Confirmed Before Editing

- 현재 프로젝트는 새 공통 구조와 레거시 구조가 섞여 있었다.
- 공통 구조:
  - `WeaponDefinitionSO`
  - `WeaponLevelData`
  - `WeaponRuntimeData`
  - `WeaponInventory`
  - `WeaponBase.Initialize(owner, runtimeData)`
- 하지만 여러 무기 스크립트가 아직도 로컬 필드(`damage`, `cooldown`, `projectileCount` 등)를 직접 사용하고 있었다.
- 일부 새 `WeaponDefinitionSO` 자산은 이미 존재했지만 `weaponPrefab`가 비어 있어 실제 스폰 경로에는 연결되지 않은 상태였다.
- `ChainLightningWeapon`은 프리팹 직렬화가 `weaponData`를 사용하고 있었고, 현재 코드 필드명 `chainData`와 어긋나 있었다.

## Completed Changes

### 1. Runtime level-up propagation added

변경 파일:
- `Assets/02. Scripts/GameData/Weapons/WeaponData/WeaponRuntimeData.cs`
- `Assets/02. Scripts/GameData/Weapons/WeaponData/WeaponBase.cs`

반영 내용:
- `WeaponRuntimeData`에 `LevelChanged` 이벤트 추가
- `WeaponBase.Initialize()`에서 런타임 데이터와 이벤트 연결
- 레벨이 바뀌면 무기 인스턴스가 `OnLevelDataChanged()`를 통해 즉시 갱신될 수 있게 변경
- `WeaponBase.Update()`는 `RuntimeData`뿐 아니라 legacy `levelData[]`도 읽을 수 있게 유지

의도:
- 런타임 레벨업 후 무기 인스턴스가 재설정 없이 새 레벨 데이터를 반영하게 만들기 위함

### 2. Weapon scripts moved toward WeaponLevelData

변경 파일:
- `Assets/02. Scripts/GameData/Weapons/Tile/ProjectileWeapon.cs`
- `Assets/02. Scripts/GameData/Weapons/OrbitWeapon/OrbitWeapon.cs`
- `Assets/02. Scripts/GameData/Weapons/Aura/AuraWeapon.cs`
- `Assets/02. Scripts/GameData/Weapons/Homing/HomingWeapon.cs`
- `Assets/02. Scripts/GameData/Weapons/Meteor/MeteorWeapon.cs`
- `Assets/02. Scripts/GameData/Weapons/ChainLightning/ChainLightningWeapon.cs`

반영 내용:
- 공통 `WeaponLevelData` 기반 `Attack(data)` 경로 추가 또는 정리
- 기존 로컬 필드 기반 동작은 legacy fallback으로 유지
- 새 구조가 연결된 경우에는 공통 데이터 사용
- 아직 연결이 안 된 경우에는 기존 방식으로도 동작 가능하게 유지

무기별 핵심:
- `ProjectileWeapon`
  - `damage`, `cooldown`, `projectileSpeed`, `projectileCount`, `spreadAngle`를 `WeaponLevelData`에서 읽도록 정리
- `OrbitWeapon`
  - 레벨 데이터 변경 시 궤도체 수, 반경, 회전 속도, 히트 쿨다운, 피해량 재적용
  - `SyncOrbitObjects()`가 레벨 변화에 맞춰 다시 실행되도록 정리
- `AuraWeapon`
  - 범위와 피해를 레벨 데이터 기반으로 읽도록 정리
- `HomingWeapon`
  - 발사 수, 속도, 탐지 범위, 확산각을 레벨 데이터 기반으로 읽도록 정리
- `MeteorWeapon`
  - 피해, 폭발 반경, 딜레이를 레벨 데이터에서 읽도록 정리
- `ChainLightningWeapon`
  - 피해, 탐지 범위, 체인 횟수, 체인 반경을 레벨 데이터에서 읽도록 정리
  - `[FormerlySerializedAs("weaponData")]` 추가로 기존 프리팹 직렬화와 현재 필드명 연결

### 3. New definition assets connected to actual prefabs

변경 파일:
- `Assets/10. ScriptableObjects/GameData/Weapon/DefaultWeapon/WD_CarrotCannon.asset`
- `Assets/10. ScriptableObjects/GameData/Weapon/DefaultWeapon/WD_ShellSpin.asset`

반영 내용:
- `WD_CarrotCannon.weaponPrefab` -> `MachineGunWeapon.prefab` 연결
- `WD_ShellSpin.weaponPrefab` -> `OrbitWeapon.prefab` 연결

의도:
- `WeaponInventory -> WeaponDefinitionSO -> prefab spawn` 경로를 실제로 열기 위함

## Build Verification

실행:
- `dotnet build C:\Users\user\team-project-may-5-1\VehicleSurvivor\VehicleSurvivor.sln`

결과:
- build 성공
- error 0
- 기존 프로젝트 경고만 존재
- 이번 변경으로 새 컴파일 에러는 발생하지 않음

## Current Status

### Done

- 공통 레벨 데이터 반영 훅 추가 완료
- P0 대상 무기 스크립트 6종 1차 정리 완료
- `CarrotCannon`, `ShellSpin` 정의 SO에 프리팹 연결 완료
- `ChainLightning` 구형 직렬화 필드명 호환 처리 완료

### Not Done Yet

- Unity 에디터에서 실제 프리팹/인스펙터 상태 최종 확인
- `Aura`, `Homing`, `Meteor`, `ChainLightning`용 새 `WeaponDefinitionSO` 자산 존재 여부 및 연결 정리
- 레거시 전용 SO를 완전히 "비주얼 참조 전용"으로 축소할지 결정
- `firePoint`, `LayerMask`, 이펙트 연결 같은 프리팹 인스펙터 값 직접 점검
- `WeaponDatabase`에 새 정의 SO가 실제로 모두 등록돼 있는지 확인

## Known Risks / Things To Check Next

### 1. Prefab inspector values may still need manual review

특히 아래 항목은 코드만으로 확정할 수 없다:
- `HomingWeapon.firePoint`
- `HomingWeapon.targetLayer`
- `ChainLightning` 이펙트 참조 실제 동작
- `Aura/Meteor` 프리팹의 scene/runtime 배치 상태

### 2. Orbit behavior depends on runtime level change flow

`OrbitWeapon`은 레벨업 시 개수 동기화가 필요한 무기라서 실제 플레이 중:
- 무기 획득
- 레벨업
- 궤도체 증가
를 꼭 에디터에서 확인해야 한다.

### 3. Legacy SOs still exist

현재는 안전하게 가기 위해 legacy SO를 삭제하지 않았다.
즉 지금 상태는:
- 새 공통 데이터 읽기 구조를 우선 반영
- 기존 SO는 비주얼/프리팹 참조 fallback으로 남겨둔 상태

## Recommended Next Step

다음 채팅에서는 아래 순서로 이어가면 된다.

1. Unity 에디터에서 `CarrotCannon`, `ShellSpin`이 실제로 새 `WeaponDefinitionSO` 경로로 스폰되는지 확인
2. `ChainLightningWeapon.prefab` 동작 확인
3. `Aura`, `Homing`, `Meteor`, `ChainLightning`용 `WeaponDefinitionSO` 자산 생성 또는 기존 자산 연결
4. `WeaponDatabase.asset`에 빠진 정의 SO가 있으면 등록
5. 각 무기 프리팹의 인스펙터 참조값 점검 후 legacy SO 정리 범위 결정

## Files Changed In This Chat

- `Assets/02. Scripts/GameData/Weapons/WeaponData/WeaponRuntimeData.cs`
- `Assets/02. Scripts/GameData/Weapons/WeaponData/WeaponBase.cs`
- `Assets/02. Scripts/GameData/Weapons/Aura/AuraWeapon.cs`
- `Assets/02. Scripts/GameData/Weapons/ChainLightning/ChainLightningWeapon.cs`
- `Assets/02. Scripts/GameData/Weapons/Homing/HomingWeapon.cs`
- `Assets/02. Scripts/GameData/Weapons/Meteor/MeteorWeapon.cs`
- `Assets/02. Scripts/GameData/Weapons/OrbitWeapon/OrbitWeapon.cs`
- `Assets/02. Scripts/GameData/Weapons/Tile/ProjectileWeapon.cs`
- `Assets/10. ScriptableObjects/GameData/Weapon/DefaultWeapon/WD_CarrotCannon.asset`
- `Assets/10. ScriptableObjects/GameData/Weapon/DefaultWeapon/WD_ShellSpin.asset`
- `Assets/RuntimeWeaponRefactorHandoff.md`

## One-Line Handoff

공통 레벨 데이터 기반 리팩터링의 첫 번째 안전 구간은 끝났고, 다음 단계는 Unity 에디터에서 프리팹/정의 SO 연결 상태를 확인하면서 나머지 무기 정의 자산을 실제 런타임 경로에 완전히 묶는 작업이다.
