# Runtime Weapon Refactor Handoff

## 1. Scope

이 문서는 실제 저장소 상태를 기준으로 런타임 무기 리팩터링 현황을 정리한 핸드오프 문서다.

주의:
- 요청에 언급된 기존 `RuntimeWeaponRefactorHandoff.md`는 저장소에서 찾지 못했다.
- 따라서 본 문서는 현재 코드/프리팹/SO 상태를 다시 읽고 새로 작성한 기준 문서다.
- 추측은 따로 표시하고, 확인된 사실만 TODO의 근거로 사용한다.

## 2. Success Criteria

리팩터링 완료 기준은 아래 4가지다.

1. 무기 프리팹이 `WeaponInventory -> WeaponDefinitionSO -> WeaponBase.Initialize()` 흐름으로 정상 스폰된다.
2. 무기 수치가 무기 컴포넌트의 로컬 필드가 아니라 `WeaponLevelData`에서 읽힌다.
3. 전용 SO가 정말 필요한 "비수치 참조 데이터"만 남고, 중복 수치 SO는 제거 가능 상태가 된다.
4. 레벨업 시 무기 수치와 개수 변화가 실제 인스턴스 동작에 반영된다.

## 3. Verified Current State

### 3.1 Shared Runtime Structure

- 공통 정의 SO: `WeaponDefinitionSO`
- 공통 레벨 데이터: `WeaponLevelData`
- 런타임 인벤토리/스폰 진입점: `WeaponInventory`
- 런타임 인스턴스 초기화: `WeaponBase.Initialize(owner, runtimeData)`

확인된 핵심 제약:

- `WeaponBase`는 이미 `WeaponRuntimeData`와 `WeaponLevelData`를 읽는 구조를 갖고 있다.
- 하지만 다수 무기 스크립트가 아직 자체 `Update()`와 로컬 스탯 필드로 동작한다.
- 따라서 SO 교체만으로는 리팩터링이 끝나지 않는다.
- `WeaponDefinitionSO`가 존재해도 `weaponPrefab`가 비어 있으면 `WeaponInventory` 경로로 스폰되지 않는다.

### 3.2 Shared Risks

- 레거시 전용 SO는 아직 실제 무기 프리팹에 연결되어 있다.
- 일부 프리팹은 예전 `weaponData` 슬롯과 새 전용 SO 슬롯이 섞여 있다.
- `WeaponInventory.TryLevelUpWeapon()`는 `WeaponRuntimeData`만 증가시키고 무기 인스턴스 후처리를 호출하지 않는다.
- `OrbitWeapon`처럼 레벨업 시 재배치/재생성이 필요한 무기는 추가 훅이 필요하다.

## 4. Shared Work Items

### P0

- `ChainLightningWeapon` 프리팹 참조 정합성 수정
- 새 `WeaponDefinitionSO`를 쓰는 무기들의 `weaponPrefab` 연결
- 로컬 스탯 필드를 읽는 무기들을 `WeaponLevelData` 기반으로 전환

### P1

- 전용 SO를 "비주얼/발사체/히트 이펙트 참조 전용"으로 축소할지 결정
- `WeaponBase` 레벨업 후처리 훅 또는 별도 갱신 흐름 정리

### P2

- 실제로 쓰이지 않는 레거시 `SO_WeaponData.asset` 제거 여부 판단

## 5. Weapon-by-Weapon TODO

| Weapon | Legacy SO | Current Status | New Definition Status | Main Problems | TODO | Priority | Verify |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Projectile / CarrotCannon | `SO_ProjectileWeapon.asset` | 프리팹이 `projectileData`를 직접 참조하고, 수치는 `ProjectileWeapon` 로컬 필드에 남아 있음 | `WD_CarrotCannon.asset` 존재, 레벨 데이터/진화 정보 존재, `weaponPrefab` 비어 있음 | 새 정의는 있으나 런타임 스폰 경로에 연결 안 됨 | `weaponPrefab` 연결, `ProjectileWeapon`가 `WeaponLevelData`의 `damage/cooldown/projectileSpeed/projectileCount/spreadAngle` 사용하도록 변경, 전용 SO는 발사체/파티클 참조만 유지 | P0 | 런 시작 시 스폰, 레벨업 시 투사체 수/공속 반영, 진화 후보 표시 확인 |
| Orbit / ShellSpin | `SO_OrbitWeapon.asset` | 프리팹이 `orbitData`를 직접 참조하고, 수치는 `OrbitWeapon` 로컬 필드에 남아 있음 | `WD_ShellSpin.asset` 존재, 레벨 데이터/진화 정보 존재, `weaponPrefab` 비어 있음 | 오브젝트 개수 갱신이 `LevelUpWeapon()` 오버라이드에 묶여 있어 현재 인벤토리 레벨업 흐름과 충돌 가능 | `weaponPrefab` 연결, `OrbitWeapon`가 `WeaponLevelData` 기준으로 `damage/radius/projectileCount/tickInterval` 등을 읽도록 변경, 레벨업 후 `SyncOrbitObjects()`가 실제 호출되도록 훅 추가 | P0 | 레벨업 시 궤도체 개수 증가, 반경/피해량 반영, 기존 오브젝트 누락 없이 갱신 |
| Aura | `SO_AuraWeapon.asset` | 프리팹이 `auraData`를 직접 참조하고, 수치는 `AuraWeapon` 로컬 필드에 남아 있음 | 새 정의 자산 직접 확인 못함 | 구조는 단순함. SO는 `auraEffectPrefab`만 들고 있고 전투 수치는 코드에 남아 있음 | 새 `WeaponDefinitionSO` 생성 또는 기존 자산 확인, `AuraWeapon`가 `WeaponLevelData`의 `damage/cooldown/range` 기반으로 동작하게 변경, 전용 SO는 이펙트 참조만 유지 | P1 | 오라 범위 스케일, 틱 딜, 쿨다운이 레벨별로 반영되는지 확인 |
| Homing | `SO_HomingWeapon.asset` | 프리팹이 `homingData`를 직접 참조, `Attack(WeaponLevelData data)` 오버라이드는 되어 있으나 실제로는 로컬 필드 사용 | 새 정의 자산 직접 확인 못함 | 반쯤만 공통 구조에 올라와 있음. `firePoint` 비어 있고 전용 SO 의존도 높음 | 새 `WeaponDefinitionSO` 생성 또는 기존 자산 확인, `HomingWeapon`가 `data`의 `damage/projectileSpeed/projectileCount/spreadAngle/range`를 실제 사용하게 수정, `firePoint`와 타겟 레이어 프리팹 설정 점검 | P0 | 스폰 직후 발사 가능, 타겟 추적, 레벨업 시 발사체 수/속도/탐지 범위 반영 |
| Meteor | `SO_MeteorWeapon.asset` | 프리팹이 `meteorData`를 직접 참조, 전투 수치와 낙하 패턴 파라미터가 코드에 남아 있음 | 새 정의 자산 직접 확인 못함 | `damage/cooldown/explosionRadius/meteorDelay`는 공통화 가능하나 낙하 패턴 값은 별도 처리 필요 | 새 `WeaponDefinitionSO` 생성 또는 기존 자산 확인, 공통 수치는 `WeaponLevelData`로 이동, `spawnRadius/safeRadius/spawnHeight/diagonalOffset/randomAngleRange`는 전용 설정 또는 직렬화 필드 유지 여부 결정 | P1 | 레벨업 시 폭발 반경/딜/간격 반영, 낙하 위치 로직 유지 |
| ChainLightning | `SO_ChainLightningWeapon.asset` | 프리팹이 `weaponData`에는 레거시 SO를 물고 있으나 `chainData`는 비어 있음 | 새 정의 자산 직접 확인 못함 | 현재 코드 기준으로 이펙트 참조가 끊긴 상태. 자산의 `weaponID`, `weaponName`도 비어 있음 | 프리팹 참조 먼저 복구, `ChainLightningWeapon`가 실제 사용하는 SO 슬롯을 하나로 정리, 새 `WeaponDefinitionSO` 생성 또는 기존 자산 확인, `damage/cooldown/chainCount/chainRadius/range`를 `WeaponLevelData`로 이동 | P0 | 첫 타겟 탐지, 체인 횟수/반경 반영, 번개 이펙트 정상 생성 |

## 6. Legacy Asset Assessment

### `SO_WeaponData.asset`

판단:
- 현재 확인한 범위에서 참조를 찾지 못했다.
- 내용도 비어 있다.
- 운영 데이터라기보다 이전 설계 흔적일 가능성이 높다.

TODO:
- 삭제 전에 전체 프로젝트 참조 1회 재검증
- 참조가 끝까지 없으면 폐기 후보로 분류

우선순위:
- P2

## 7. Recommended Execution Order

1. `ChainLightningWeapon` 프리팹 참조부터 복구한다.
2. `CarrotCannon`, `ShellSpin`의 `WeaponDefinitionSO.weaponPrefab`를 연결한다.
3. `ProjectileWeapon`, `OrbitWeapon`, `HomingWeapon`부터 `WeaponLevelData` 읽기 구조로 바꾼다.
4. `Aura`, `Meteor`, `ChainLightning`을 같은 패턴으로 정리한다.
5. 무기별로 전용 SO를 "비주얼 참조 전용"으로 축소할지 최종 판단한다.
6. 마지막에 `SO_WeaponData.asset` 같은 레거시 흔적을 정리한다.

## 8. File References

- `Assets/02. Scripts/GameData/Weapons/WeaponData/WeaponDefinitionSO.cs`
- `Assets/02. Scripts/GameData/Weapons/WeaponData/WeaponLevelData.cs`
- `Assets/02. Scripts/GameData/Weapons/WeaponData/WeaponBase.cs`
- `Assets/02. Scripts/GameData/Weapons/WeaponData/WeaponInventory.cs`
- `Assets/02. Scripts/GameData/Weapons/Aura/SO_AuraWeapon.asset`
- `Assets/02. Scripts/GameData/Weapons/ChainLightning/SO_ChainLightningWeapon.asset`
- `Assets/02. Scripts/GameData/Weapons/Homing/SO_HomingWeapon.asset`
- `Assets/02. Scripts/GameData/Weapons/Meteor/SO_MeteorWeapon.asset`
- `Assets/02. Scripts/GameData/Weapons/OrbitWeapon/SO_OrbitWeapon.asset`
- `Assets/02. Scripts/GameData/Weapons/Tile/SO_ProjectileWeapon.asset`
- `Assets/03. Prefabs/Weapons/MachineGunWeapon.prefab`
- `Assets/03. Prefabs/Weapons/OrbitWeapon.prefab`
- `Assets/03. Prefabs/Weapons/MagneticAuraWeapon.prefab`
- `Assets/03. Prefabs/Weapons/HomingWeapon.prefab`
- `Assets/03. Prefabs/Weapons/MeteorWeapon.prefab`
- `Assets/03. Prefabs/Weapons/ChainLightningWeapon.prefab`
- `Assets/10. ScriptableObjects/GameData/Weapon/DefaultWeapon/WD_CarrotCannon.asset`
- `Assets/10. ScriptableObjects/GameData/Weapon/DefaultWeapon/WD_ShellSpin.asset`
