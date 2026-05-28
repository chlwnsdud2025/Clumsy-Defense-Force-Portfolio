[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/pYPBpAWz)

# VehicleSurvivor

3D 서바이벌 액션 게임입니다. 캐릭터를 선택한 뒤 맵에서 적 웨이브를 피하며 생존하고, 경험치를 모아 레벨업 보상(무기·패시브)을 고르고, 스테이지 목표를 달성해 다음 페이즈로 진행합니다.

## 프로젝트 검토용 안내

### 실행 방법

| 항목 | 내용 |
|------|------|
| Unity 버전 | **6000.4.5f1** |
| 프로젝트 폴더 | `VehicleSurvivor/` |
| 시작 씬 (Build Settings 0번) | `Assets/01. Scenes/GamePlay/TitleScene.unity` |
| 인게임 씬 (Build Settings 1번) | `Assets/01. Scenes/GamePlay/InGameScene.unity` |

Unity Hub에서 `VehicleSurvivor` 폴더를 연 뒤 **Play**를 누르면 타이틀 씬부터 실행됩니다. (Build Settings에 위 두 씬만 등록되어 있습니다.)

### 빌드 파일

프로젝트 빌드 파일은 아래 구글드라이브 링크에서 다운로드할 수 있습니다.

- [빌드 파일 다운로드](https://drive.google.com/file/d/1oNmeh3siO_rDWiSACH8VF8TwcTK79cmE/view?usp=sharing)

### 플레이 흐름

1. **타이틀** — 화면을 클릭해 시작
2. **캐릭터 선택** — 원하는 캐릭터 선택 후 게임 시작
3. **인게임** — 이동·회피, 자동/획득 무기로 적 처치, 경험치·레벨업 보상 선택, 스테이지 클리어

### 조작 (키보드·마우스)

| 입력 | 동작 |
|------|------|
| `W` `A` `S` `D` | 이동 |
| 마우스 | 시점(카메라) |
| `Space` | 점프 |
| `Left Shift` | 달리기 |

게임패드 입력도 지원합니다.

### 주요 폴더

- `Assets/01. Scenes/GamePlay/` — 타이틀·인게임 씬
- `Assets/02. Scripts/` — 게임 로직·UI
- `Assets/03. Prefabs/` — 플레이어 등 프리팹

---

# Unity 프로젝트 코딩 컨벤션

## 명명 규칙

### 대소문자 규칙
- **PascalCase**: 클래스, 메서드, 네임스페이스, 열거형, 프로퍼티, 속성, 코루틴에 사용
  - 예: `ExamplePlayerController`, `MaxHealthPoints`, `EndOfFile`
- **camelCase**: 필드, 지역 변수, 매개변수에 사용
  - 예: `examplePlayerController`, `maxHealthPoints`, `endOfFile`
- **상수**: 대문자와 언더스코어 사용
  - 예: `MAX_HEALTH`, `DEFAULT_SPEED`

### 접두사, 접미사 규칙
- 인터페이스: `I` 접두사 사용 (예: `IDamageable`)
- 베이스 클래스: `Base` 접미사 사용 (예: `CharacterBase`)
- 열거형: `E` 접두사 사용 (예: `EGameState`)
- 클릭이벤트 함수: `OnClick` 접두사 사용 (예: `OnClickSetting`)

## 코드 포맷팅

### 들여쓰기 및 중괄호

```csharp
// 권장
if (playerWasHit)
{
    PlaySound(playerHitSound);
    Damage(player, damageAmount);
}
```

### 속성 및 프로퍼티
- 간단한 단일 문장 프로퍼티는 표현식 본문 정의 연산자(`=>`) 사용 가능

```csharp
public float Health
{
    get => health;
    set => health = value;
}
```

## 파일 및 폴더 구조

### 폴더 구조 원칙
- 에셋, 스크립트, 프리팹 등을 체계적으로 정리
- 파일 및 폴더 이름에 공백 사용 금지
- 네임스페이스에 따라 폴더 구조 분리

### 권장 폴더 구조
- Assets/
  - Scenes/
  - Scripts/
    - Core/
    - UI/
    - Gameplay/
    - Utils/
  - Prefabs/
  - Models/
  - Materials/
  - Textures/
  - Sounds/
  - Animations/

## 코드 구성

### 클래스 구성 순서
1. 중첩 클래스
2. 상수
3. 열거형
4. 필드
5. 프로퍼티
6. 이벤트
7. 생성자
8. Unity 메시지 (Start, Update 등)
9. 공개 메서드
10. 비공개 메서드
11. 버튼 이벤트


## 일반 원칙

- 일관된 명명 규칙 유지하기
- 한 가지를 그대로 부르기 (새는 Bird라고 부르기)
- 발음하고 기억할 수 있는 이름 선택하기
- 버전 번호나 진행 상태를 나타내는 단어(WIP, final) 사용하지 않기
- 약어 사용 자제하기
- 변수 명명시 구체적인 단어 먼저 쓰기 (예: `UserNameText`)

---
