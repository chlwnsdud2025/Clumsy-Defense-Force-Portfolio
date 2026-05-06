[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/pYPBpAWz)

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
