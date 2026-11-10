# 밈 애니멀 진화 연구소 (Meme Animal Lab) 스크립트 구조 및 역할 명세서 (Script.md)

이 문서는 **밈 애니멀 진화 연구소 (Meme Animal Lab)** 프로젝트의 전체 스크립트 디렉터리 하이라키 구조와 폴더별 역할, 그리고 프로젝트 내에 존재하는 모든 C# 스크립트의 클래스 정보, 핵심 메서드 및 세부 역할을 체계적으로 정리한 마스터 문서입니다.

---

## 1. 전체 디렉터리 하이라키 포맷 규정 (Directory Tree)

GEMINI 개발 하네스 규격(`폴더 > - 하위폴더 > - 스크립트 : 사용처, 주요 기능`)에 따른 전체 트리 구조입니다.

```text
Assets/Scripts/
├── Core/                                  # 게임 코어 루프, 스폰, 데드라인 판정, 환경 자동 생성
│   ├── DeadlineSensor.cs                  : 상단 한계선(Dead Line) 10초 체류 감지, 카운트다운 및 실패 리셋 트리거
│   ├── GameManager.cs                     : 싱글톤, 게임 상태(Playing, Clear, GameOver) 제어, 클리어 빵빠레 및 리셋 순환 총괄
│   ├── Generator.cs                       : 인게임 물리 상자/경계, 스포너, 핵심 매니저 및 컴포넌트 자동 생성/참조 연결 툴
│   ├── SoundManager.cs                    : 싱글톤, 1회성 효과음(10초 캐싱) 및 배경음(사용 중 1개만 캐싱) 관리, AudioSource 풀링(기본 10개)
│   └── Spawner.cs                         : 상단 크레인 좌우 왕복(구역 a~b, 속도 n), m초 주기 v초 팅클 생성(최종 스케일 1) 및 자동 낙하, 채팅/후원 특수 스폰 제어
├── Data/                                  # 게임 데이터 에셋 및 ScriptableObject
│   ├── AnimalStageDataSO.cs               : 1~10단계 동물 및 트롤링 방해 블록의 단계, 스프라이트, 스케일, 질량(Mass) 설정 에셋
│   └── ResourceName.cs                    : 프로젝트 전역에서 사용하는 사운드/UI/아이콘 등 Resources 에셋 경로 상수 모음
├── Effects/                               # 물리 기믹 및 후원 연출 이펙트
│   └── FeverController.cs                 : 싱글톤, 좋아요 바운스, $1 잔챙이 폭파 소탕, $5 10초 자석 폭풍(인력/척력) 물리 제어
├── Network/                               # 외부 스트리밍 플랫폼 연동 및 이벤트 디스패치
│   └── YouTubeEventReceiver.cs            : 유튜브 라이브 채팅/좋아요/구독/슈퍼챗 이벤트 큐 수신 및 인게임 시스템 라우팅
├── Objects/                               # 인게임 물리 머지 객체 및 부착 컴포넌트
│   ├── MergeObject.cs                     : PolygonCollider2D 물리 연산, 동일 레벨 충돌 합체, 후원자 이름/가치 계승
│   ├── NameTagUI.cs                       : 부모 물리 객체 회전과 무관하게 항상 정방향(Quaternion.identity)을 유지하는 닉네임 텍스트
│   └── RainbowStarObject.cs               : $2 후원 만능 별 아이템, 충돌한 일반 동물을 즉시 +1단계 강제 진화 및 후원자 각인
├── UI/                                    # HTML 모달 기반 UI 프레임워크 및 화면 컴포넌트
│   ├── CountdownUI.cs                     : 한계선 10초 카운트다운 경고 및 위험 알림 사운드 제어 UI
│   ├── DonorRankSlotUI.cs                 : 메인 UI 우상단 후원 랭킹 보드의 개별 순위 슬롯 컴포넌트 ("아이콘 : 텍스트", SetData/SetVisible)
│   ├── DonorRankUI.cs                     : 메인 UI 우상단 후원 금액 상위 5명 실시간 랭킹 리스트 ("아이콘 : 텍스트")
│   ├── EventLogSlotUI.cs                  : 메인 UI 좌상단 시청자 참여 이벤트 로그의 개별 슬롯 컴포넌트 ("아이콘 : 텍스트", SetLog/SetVisible)
│   ├── EventLogUI.cs                      : 메인 UI 좌상단 시청자 참여 이벤트 로그 리스트 (최대 3개 슬롯 재사용, 새 이벤트 시 이전 메시지 비활성화)
│   ├── FullScreenEventUI.cs               : 메인 UI 중앙 대형 전면 이벤트 연출 (구독 소환, $1 폭발, $2 별, $3 트롤, $5 자석, 클리어 현수막)
│   ├── MainUI.cs                          : UIBase 상속, 메인 인게임 통합 UI 컨트롤러 (로그, 랭킹, 전면 연출, 카운트다운)
│   ├── UIBase.cs                          : 모든 UI 화면 및 모달 팝업의 기반 클래스 (Init, OnShow, OnClose)
│   └── UIManager.cs                       : 싱글톤, Stack 기반 모달 UI 생성/스택 관리 및 Resources 기반 로드/해제
└── Utility/                               # 오브젝트 풀러, 리소스 로더, 디버그 테스터
    ├── MergeObjectPool.cs                 : 싱글톤, 동물 및 트롤링 블록의 인스턴스화 오버헤드와 GC Alloc을 방지하는 오브젝트 풀
    ├── ResourceManager.cs                 : 싱글톤, Resources.Load 래핑 및 메모리 캐싱, 언로드 관리자
    └── TestInputController.cs             : 에디터/키보드 입력을 통해 크레인 이동, 채팅 투하, 좋아요, 구독, $1~$5 후원, 치트를 시뮬레이션하는 테스트 제어기
```

---

## 2. 폴더별 핵심 기능 및 역할 개요 (Folder Category Overview)

| 디렉터리 경로 | 핵심 기능 및 역할 |
| :--- | :--- |
| **`Assets/Scripts/Core`** | 24시간 무인 순환 라이프사이클 총괄, 상단 크레인 스포너 이동/낙하, 상단 한계선(Dead Line) 10초 타이머 판정 및 인게임 매니저/물리 환경 자동 생성기 |
| **`Assets/Scripts/Data`** | 1~10단계 동물 단계별 스프라이트, 질량, 스케일 및 -1단계 무적 트롤링 블록의 메타데이터를 저장하는 ScriptableObject 컨테이너 |
| **`Assets/Scripts/Effects`** | 좋아요 물리 바운스, $1 소형 동물 일괄 폭파 삭제, $5 10초 자석 폭풍(방해물 척력 + 일반 동물 인력) 등 물리 역학 기반 후원 특수효과 제어 |
| **`Assets/Scripts/Network`** | 유튜브 라이브 채팅(!좌, !우, !1~!5, !드롭), 좋아요, 신규 구독, 슈퍼챗 이벤트를 메인 스레드 큐로 수신하여 각 게임 시스템에 전달 |
| **`Assets/Scripts/Objects`** | PolygonCollider2D 기반 2D 물리 동물, 동일 단계 충돌 합체 및 후원자 닉네임/금액 계승 로직, 정방향 닉네임 UI, 만능 진화 별 오브젝트 |
| **`Assets/Scripts/UI`** | HTML 모달 구조 기반 UI 체계(`UIBase`, `UIManager`), 좌상단 이벤트 로그, 우상단 후원 랭킹, 중앙 전면 이벤트 연출, 데드라인 카운트다운 UI |
| **`Assets/Scripts/Utility`** | 머지 객체 풀링(`MergeObjectPool`), 리소스 로드 캐시(`ResourceManager`), 에디터/키보드 인게임 시뮬레이션 테스터(`TestInputController`) |

---

## 3. 전체 스크립트 상세 명세 (Script Specifications)

### 3.1 Core (`Assets/Scripts/Core/`)

* **[DeadlineSensor.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Core/DeadlineSensor.cs)** `DeadlineSensor` (MonoBehaviour, `[RequireComponent(typeof(BoxCollider2D))]`)
  * **사용처:** 물리 상자 상단 한계선(Dead Line) 영역에 배치되는 센서 컴포넌트.
  * **주요 기능 및 동작:**
    * 상자 상단 수평 트리거 영역 내에 진입한 동물/방해물 객체 감지 (`OnTriggerEnter2D`, `OnTriggerExit2D`).
    * 낙하 중인 객체는 제외하고 속도(`Velocity.magnitude`)가 `0.1f` 이하로 멈춰 서 있는 정체 객체만 위험 상태로 판단.
    * 위험 객체가 1개 이상 감지되면 `dangerTimer`를 누적하고 `OnDangerTick(float)` 이벤트를 발행하여 카운트다운 UI 갱신.
    * 10초 도달 시 `GameManager.Instance.OnDeadlineExceeded()`를 호출하여 실패 리셋 시퀀스 발동.
    * 객체가 내려가 위험이 해제되면 즉시 `ResetSensor()`를 수행하고 `OnDangerCleared` 호출.
  * **핵심 이벤트/프로퍼티:** `RemainingTime`, `IsDangerous`, `OnDangerTick`, `OnDangerCleared`, `OnDeadlineExceeded`

* **[GameManager.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Core/GameManager.cs)** `GameManager` (MonoBehaviour, Singleton)
  * **사용처:** 24시간 무인 자동화 게임의 전체 상태 머신 및 라이프사이클 중앙 관리자.
  * **주요 기능 및 동작:**
    * 게임 상태(`GameState`: `Playing`, `GameOverReset`, `ClearCeremony`) 전이 및 이벤트 통보.
    * 활성 객체 등록/해제 관리(`RegisterObject`, `UnregisterObject`).
    * **합체 진화 제어 (`OnMergeSuccess`):** 동일 단계 충돌 시 새 레벨 동물을 스폰하고, 최종 10단계 완성 시 즉시 클리어 빵빠레 시퀀스 실행.
    * **클리어 시퀀스 (`ClearSequenceRoutine`):** 빵빠레 사운드 및 폭죽 파티클 재생, 10단계 동물을 화면 중앙으로 연출 강조, 기여자 닉네임 배너 박제(`OnGameClear`), 약 8초 후 전체 소멸 및 새 판 자동 시작.
    * **실패 리셋 시퀀스 (`FailResetSequenceRoutine`):** 한계선 10초 초과 시 모든 동물/방해물을 팝(Pop) 이펙트와 함께 일괄 소멸시키고 3초 후 빈 상자에서 새 판 자동 시작.
  * **핵심 이벤트/프로퍼티:** `CurrentState`, `ActiveObjects`, `OnGameStateChanged`, `OnGameClear`, `OnGameOver`, `OnGameRestart`, `OnMergeEvent`

* **[Generator.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Core/Generator.cs)** `Generator` (MonoBehaviour, `[ExecuteAlways]`), `GeneratorEditor` (Editor), `SerializedPropertyExtensions` (Editor static 클래스)
  * **사용처:** UIManager를 제외한 모든 인게임 핵심 매니저 및 물리 환경, 크레인 스포너를 코드로 자동 생성하고 참조를 자동 연결하는 개발/환경 구축 헬퍼.
  * **주요 기능 및 동작:**
    * 에디터 메뉴(`Tools/MemeLab/...`) 및 런타임 `Awake()` 자동 생성(`autoGenerateOnAwake`) 지원.
    * `[GameManagers]` 루트 생성: `GameManager`, `SoundManager`, `MergeObjectPool`, `FeverController`, `YouTubeEventReceiver`, `TestInputController`.
    * `[Physics Box]` 루트 생성: 좌/우 벽, 바닥 Collider 및 `DeadlineSensor` 자동 배치 (`PhysicsMaterial2D` 마찰 0.4, 반발 0.15 자동 세팅).
    * `[Crane Spawner]` 루트 생성: `Spawner` 배치.
    * 컴포넌트 간 상호 의존성 및 직렬화 필드 자동 바인딩(`WireReferences`).
    * **`SerializedPropertyExtensions`**: 에디터 스크립트에서 null-safe하게 `SerializedProperty`에 `objectReferenceValue`를 할당하는 확장 메서드(`SetObjectReference`) 제공.

* **[SoundManager.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Core/SoundManager.cs)** `SoundManager` (MonoBehaviour, Singleton)
  * **사용처:** 24시간 무인 스트리밍 라이브 방송을 위한 오디오/사운드 중앙 집중 관리자.
  * **주요 기능 및 동작:**
    * **1회성 효과음 재생 (`Play`)**: Resources 경로 또는 `ResourceName` 상수를 통해 오디오 클립을 비동기/동적 로드하여 재생.
    * **효과음 10초 캐싱 규칙**: 사용된 SFX 클립은 마지막 사용 시점부터 10초간 메모리에 캐싱되며, 10초간 미사용 시 캐시에서 안전하게 자동 해제.
    * **배경음 캐싱 규칙 (`PlayBGM`)**: 현재 재생 중인 단 1개의 BGM 클립만 캐싱하며, 새 BGM 전환 또는 정지(`StopBGM`, `PlayBGM(false)`) 시 기존 BGM 리소스 즉시 해제.
    * **AudioSource 풀링**: BGM 풀 10개, SFX 풀 10개를 기본 생성하여 보유하며, 모든 소스가 재생 중일 경우 새로운 AudioSource를 동적으로 생성/확장하여 끊김 없는 사운드 재생 보장.
    * **리소스 예외 처리**: 요청한 사운드 리소스가 없을 경우 크래시 없이 경고 로그(`Debug.LogWarning`) 출력.
  * **핵심 메서드/프로퍼티**: `Play(string, bool)`, `PlayBGM(string, bool)`, `PlayBGM(bool)`, `Stop(string)`, `StopBGM()`, `MasterVolume`, `BGMVolume`, `SFXVolume`

* **[Spawner.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Core/Spawner.cs)** `Spawner` (MonoBehaviour)
  * **사용처:** 상단 크레인 이동 제어 및 시청자 상호작용/후원 오브젝트 투하 제어기.
  * **주요 기능 및 동작:**
    * **크레인 좌우 왕복 이동**: 상단 구역 `minX(a) ~ maxX(b)` 사이에서 `moveSpeed(n)` 속도로 왕복 이동.
    * **[자동 스폰 - 팅클 연출]**: `autoDropInterval(m)` 초 주기마다 크레인 위치에서 `spawnDuration(v)` 초 동안 팅클 효과(`tinkleCurve`, 0 -> 오버슈트 -> 최종 1.0)를 연출하며 생성 완료 후 물리 낙하(`AutoDrop`).
    * **[채팅 이동 제어]**: `!좌`, `!우`(`MoveSpawnerDirection`), `!1 ~ !5` 열 지정(`MoveSpawnerColumn`).
    * **[채팅 드롭]**: `!드롭` 호출 시 20초 개인 쿨타임(`userDropCooldown`) 검증 후 1~3단계 동물 즉시 투하 (`CommandDrop`).
    * **[구독 스폰]**: 4단계 중형 동물 확정 투하 (`SpawnSubAnimal`).
    * **[후원 특수 스폰]**: $2 무지개 만능 진화 별 투하 (`SpawnRainbowStar`), $3 무적 방해 블록 투하 (`SpawnTrollBlock`).
    * `MergeObjectPool`과 연동하여 동물 오브젝트 대여 및 동적 런타임 Fallback 생성 지원.
  * **핵심 프로퍼티/메서드**: `IsSpawning`, `SpawnInterval`, `SpawnDuration`, `AutoDrop()`, `SpawnAnimal(...)`

---

### 3.2 Data (`Assets/Scripts/Data/`)

* **[AnimalStageDataSO.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Data/AnimalStageDataSO.cs)** `AnimalStageDataSO` (ScriptableObject), `AnimalStageInfo` (Serializable 클래스)
  * **사용처:** 1~10단계 동물 및 -1단계 트롤링 방해 블록의 비주얼/물리 메타데이터 저장소.
  * **주요 기능 및 동작:**
    * 단계별 `level`, `animalName`, `sprite`, `scale`, `mass`(일반 1.0~3.0, 방해물 5.0), `debugColor`, `physicsMaterial`, `customPrefab` 정의.
    * `GetStageInfo(int level)`: 단계에 해당하는 동물 정보 조회 (데이터 미등록 시 수식 기반 동적 Fallback 생성).
    * `GetObstacleInfo()`: 트롤링 방해물(Level -1, Mass 5.0, 파괴 불가) 전용 정보 반환.

* **[ResourceName.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Data/ResourceName.cs)** `ResourceName` (static 클래스)
  * **사용처:** 프로젝트 전역에서 사용하는 사운드, UI 프리팹, 아이콘 등 Resources 에셋 경로 상수 모음.
  * **주요 기능 및 동작:**
    * 하드코딩 방지를 위한 리소스 경로 상수화 (`SOUND_FANFARE`, `SOUND_WARNING`, `SOUND_MERGE`, `SOUND_BGM_MAIN`, `UI_MAIN`, 아이콘 등).
    * 향후 모든 신규 리소스 등록 시 해당 파일에 상수를 기입하여 일관된 리소스 로딩 파이프라인 유지.

---

### 3.3 Effects (`Assets/Scripts/Effects/`)

* **[FeverController.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Effects/FeverController.cs)** `FeverController` (MonoBehaviour, Singleton)
  * **사용처:** 시청자 협동 및 피버 후원 발생 시 물리적 특수효과 연출 총괄.
  * **주요 기능 및 동작:**
    * **[무료 협동 1 - 좋아요 바운스] (`TriggerBounce`):** 상자 내 모든 활성 객체에 위쪽 방향 순간 물리 충격량(`AddForce(Impulse)`)을 가해 정체된 동물 틈새 결합 유도.
    * **[협동 후원 1 - $1 잔챙이 소탕] (`ClearSmallAnimals`):** 상자 내 `Level <= 2` 소형 일반 동물만 일괄 팝 이펙트와 함께 폭파 삭제 (`Despawn`). 방해물은 파괴 불가 규칙 철저 보존.
    * **[피버 후원 - $5 10초 자석 폭풍] (`StartMagnetFever`):**
      1. 트롤링 블록(`IsObstacle == true`)은 절대 파괴하지 않고 상자 외곽으로 밀어내는 물리 척력(`obstacleRepelForce`) 적용.
      2. 동일 `Level`의 일반 동물끼리 무게중심(Centroid)으로 서로 끌어당기는 물리 인력(`magnetPullForce`)을 지속 적용하여 강제 연쇄 머지 유발.
  * **핵심 이벤트/프로퍼티:** `IsFeverActive`, `OnFeverStateChanged`, `OnSmallAnimalsCleared`

---

### 3.4 Network (`Assets/Scripts/Network/`)

* **[YouTubeEventReceiver.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Network/YouTubeEventReceiver.cs)** `YouTubeEventReceiver` (MonoBehaviour, Singleton), `YouTubeStreamEvent` (struct), `YouTubeEventType` (enum)
  * **사용처:** 유튜브 라이브 채팅, 좋아요, 구독, 슈퍼챗 이벤트를 수신하는 중앙 네트워크 이벤트 수신/분배기.
  * **주요 기능 및 동작:**
    * 외부 비동기 스레드/소켓/웹훅으로부터 안전한 이벤트 수신을 위해 `ConcurrentQueue<YouTubeStreamEvent>` 메인 스레드 큐잉 시스템 채택.
    * `EnqueueEvent`: 외부 API 및 인스펙터 테스트 이벤트 진입점.
    * **이벤트 디스패치 (`ProcessEvent`):**
      * `ChatCommand`: `!좌`, `!우`, `!1~5`, `!드롭` 분석 후 `Spawner` 이동/낙하 명령.
      * `Like`: `FeverController.TriggerBounce()` 호출.
      * `Subscribe`: `Spawner.SpawnSubAnimal(user)` 호출 및 신규 구독 전면 연출 통보.
      * `Donation`: 금액별($1 소탕, $2 무지개 별, $3 트롤 블록, $5 자석 피버) 로직 분기 및 전면 연출 통보.
    * 에디터 인스펙터 우클릭 `ContextMenu`를 통한 8종 이상의 즉시 테스트 기능 제공.
  * **핵심 이벤트:** `OnEventLogged`, `OnDonationLogged`, `OnFullScreenEvent`

---

### 3.5 Objects (`Assets/Scripts/Objects/`)

* **[MergeObject.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Objects/MergeObject.cs)** `MergeObject` (MonoBehaviour, `[RequireComponent(typeof(Rigidbody2D))]`)
  * **사용처:** 1~10단계 동물 및 트롤링 방해물의 물리 상호작용 및 합체(진화) 처리 주체.
  * **주요 기능 및 동작:**
    * **물리 설정 (`ConfigurePhysics`):** `CollisionDetectionMode2D.Continuous`, 질량(단계별 1.0~3.0, 방해물 5.0), 마찰 0.4, 반발 0.15 설정. Unity 6 `linearVelocity` 호환 프로퍼티 지원.
    * **오브젝트 풀링 반환 (`Despawn`):** `MergeObjectPool.Instance.Return(this)` 호출을 통한 GC Alloc 최소화.
    * **충돌 합체 연산 (`OnCollisionEnter2D`):**
      * 방해물이거나 이미 합체 중(`IsMerging`)인 경우 무시.
      * 동일 레벨 충돌 시 양방향 중복 실행 방지(`GetInstanceID` 비교) 후 중간 위치에서 상위 단계 스폰 요청.
    * **후원자 닉네임 계승 알고리즘:**
      1. 한쪽만 닉네임이 있는 경우: 해당 닉네임/금액 계승.
      2. 양쪽 모두 닉네임이 있는 경우: `DonatedAmount`가 높은 쪽 우선 계승.
      3. 금액까지 동일한 경우: 먼저 생성된 객체(`CreationTime`) 우선 계승.
  * **핵심 프로퍼티:** `Level`, `IsObstacle`, `OwnerName`, `DonatedAmount`, `IsMerging`, `CreationTime`, `Velocity`

* **[NameTagUI.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Objects/NameTagUI.cs)** `NameTagUI` (MonoBehaviour)
  * **사용처:** 각 동물 및 방해물 머리 위에 표시되는 후원자 닉네임 및 후원 금액 태그.
  * **주요 기능 및 동작:**
    * `LateUpdate`에서 `transform.rotation = Quaternion.identity`를 강제 적용하여 부모 물리 객체가 구르고 뒤집혀도 항상 시청자가 읽기 쉬운 정방향 유지.
    * 부모 위치 기반 `localOffset` 추적.
    * 후원자 닉네임 및 금액(`[닉네임] ($금액)`) 텍스트 렌더링.

* **[RainbowStarObject.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Objects/RainbowStarObject.cs)** `RainbowStarObject` (MonoBehaviour, `[RequireComponent(typeof(Rigidbody2D))]`)
  * **사용처:** $2 후원 전용 만능 진화 별 아이템.
  * **주요 기능 및 동작:**
    * 일반 동물(`IsObstacle == false`, `Level < 10`)과 첫 충돌 시 즉시 대상 동물을 `Level + 1`로 강제 진화시킴.
    * 진화된 동물에 $2 후원자 닉네임을 각인(`DonatedAmount = 2`)하고 별 오브젝트는 소멸.

---

### 3.6 UI (`Assets/Scripts/UI/`)

* **[UIBase.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/UI/UIBase.cs)** `UIBase` (MonoBehaviour)
  * **사용처:** 프로젝트 내 모든 화면/모달 UI의 기반 추상/가상 클래스.
  * **주요 기능 및 동작:** `UIName` 식별자 관리, `Init(string name)`, 가상 메서드 `OnShow()`, `OnClose()` 수명주기 훅 제공.

* **[UIManager.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/UI/UIManager.cs)** `UIManager` (MonoBehaviour, Singleton)
  * **사용처:** HTML 모달 형태의 UI 스택 및 팝업 생명주기 관리자.
  * **주요 기능 및 동작:**
    * `Show(string name)`: `Resources/UI/` 경로에서 프리팹을 `ResourceManager`로 로드하고 인스턴스화 후 스택(`uiStack`)에 푸시.
    * `Close()`: 최상단 UI를 닫고 파괴.
    * `Close(string name)`: 스택 내부의 특정 이름 UI를 탐색하여 안전하게 제거 및 복구.

* **[MainUI.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/UI/MainUI.cs)** `MainUI` (UIBase)
  * **사용처:** 인게임 메인 화면 통합 HUD 컨트롤러.
  * **주요 기능 및 동작:**
    * 좌상단 `EventLogUI`, 우상단 `DonorRankUI`, 중앙 `FullScreenEventUI`, 중앙 경고 `CountdownUI`를 단일 뷰포트에서 통합 관리.
    * HTML 모달 컨셉에 맞추어 `Resources/UI/MainUI`로부터 인스턴스화되어 실행.

* **[CountdownUI.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/UI/CountdownUI.cs)** `CountdownUI` (MonoBehaviour)
  * **사용처:** 데드라인 위험 진입 시 화면 중앙에 노출되는 10.00초 카운트다운 경고 UI.
  * **주요 기능 및 동작:**
    * `DeadlineSensor`의 이벤트(`OnDangerTick`, `OnDangerCleared`, `OnDeadlineExceeded`)를 구독.
    * 잔여 시간을 소수점 둘째 자리(`10.00s`) 포맷으로 실시간 갱신.
    * 위험 진입 시 `SoundManager.Instance.Play(ResourceName.SOUND_WARNING)` 경고음 재생 및 위험 해제 시 `Stop(ResourceName.SOUND_WARNING)` 즉시 정지.

* **[DonorRankSlotUI.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/UI/DonorRankSlotUI.cs)** `DonorRankSlotUI` (MonoBehaviour)
  * **사용처:** 메인 UI 우상단 후원자 랭킹 보드의 개별 순위 슬롯 컴포넌트.
  * **주요 기능 및 동작:**
    * 인스펙터에 직접 대입된 순위 아이콘(`Image`)을 보존하고, 후원자 정보 텍스트(`TMP_Text`) 및 가시성을 캡슐화.
    * `SetData(rank, userName, amount)`: 순위, 유저명, 금액을 전달받아 슬롯 텍스트를 갱신하고 활성화.
    * `SetVisible(bool)`: 슬롯 루트 오브젝트 활성/비활성 제어.
    * `Clear()`: 슬롯 텍스트 초기화 및 숨김 처리 (기존 인스펙터 아이콘 스프라이트는 훼손하지 않음).

* **[DonorRankUI.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/UI/DonorRankUI.cs)** `DonorRankUI` (MonoBehaviour)
  * **사용처:** 메인 UI 우상단 후원자 랭킹 보드 (순위 아이콘은 인스펙터 슬롯에 직접 대입하여 사용).
  * **주요 기능 및 동작:**
    * 유저별 누적 후원 금액 딕셔너리(`donorTotals`) 관리.
    * `DonorRankSlotUI` 컴포넌트 목록(`rankSlots`)을 관리하며, 후원 금액 기준 내림차순 정렬 상위 5명을 선별하여 순위 데이터(`SetData(rank, user, amount)`) 전달 및 빈 슬롯 자동 숨김.
    * 슬롯 미할당 시 `Awake`에서 자식 `DonorRankSlotUI` 컴포넌트 자동 탐색 지원.

* **[EventLogSlotUI.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/UI/EventLogSlotUI.cs)** `EventLogSlotUI` (MonoBehaviour)
  * **사용처:** 메인 UI 좌상단 시청자 참여 이벤트 로그의 개별 슬롯 컴포넌트 ("아이콘 : 텍스트").
  * **주요 기능 및 동작:**
    * 이벤트 아이콘(`Image`) 및 내용 텍스트(`TMP_Text`)를 캡슐화.
    * `SetLog(message, icon)`: 이벤트 문구 및 아이콘을 설정하고 슬롯 활성화.
    * `SetVisible(bool)`: 슬롯 가시성(활성/비활성) 제어.
    * `Clear()`: 슬롯 내용 초기화 및 비활성화.

* **[EventLogUI.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/UI/EventLogUI.cs)** `EventLogUI` (MonoBehaviour)
  * **사용처:** 메인 UI 좌상단 시청자 참여 이벤트 실시간 로그 컨트롤러.
  * **주요 기능 및 동작:**
    * `YouTubeEventReceiver`의 `OnEventLogged` 수신.
    * 최대 3개의 `EventLogSlotUI` 고정 객체를 재사용하여 GC Alloc을 원천 차단.
    * 새 이벤트 발생 시 직전 이전 메시지 슬롯을 비활성화(`SetVisible(false)`)하고 새 메시지 슬롯을 활성화(`SetVisible(true)`).
    * `singleActiveMode` (단일 활성화 / 3개 순차 누적) 옵션 지원.
    * 슬롯 미할당 시 `Awake`에서 자식 `EventLogSlotUI` 자동 탐색 또는 최대 3개 인스턴스 자동 보장 풀링.

* **[FullScreenEventUI.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/UI/FullScreenEventUI.cs)** `FullScreenEventUI` (MonoBehaviour)
  * **사용처:** 화면 전체를 활용하는 중앙 다목적 전면 연출 공간.
  * **주요 기능 및 동작:**
    * 신규 구독, $1 소탕, $2 무지개 별, $3 트롤 블록, $5 자석 피버, 최종 10단계 클리어 빵빠레 발생 시 대형 배너 노출.
    * `CanvasGroup`을 활용한 부드러운 페이드 인(0.3초) -> 유지 -> 페이드 아웃(0.5초) 코루틴 연출.

---

### 3.7 Utility (`Assets/Scripts/Utility/`)

* **[MergeObjectPool.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Utility/MergeObjectPool.cs)** `MergeObjectPool` (MonoBehaviour, Singleton)
  * **사용처:** 1~10단계 동물 및 -1단계 트롤링 블록의 잦은 생성/소멸로 인한 GC Alloc과 프레임 드랍을 원천 차단하는 전용 오브젝트 풀.
  * **주요 기능 및 동작:**
    * 단계별 Key(`1~10`, 트롤링 `-1`)를 기준으로 `Dictionary<int, Stack<MergeObject>>` 풀 관리.
    * `Get(...)`: 비활성화된 기존 인스턴스를 재활용하거나 없을 시 새로 생성하여 데이터 세팅 후 활성화.
    * `Return(...)`: 사용 종료된 객체의 물리/속도를 초기화하고 비활성화하여 풀 스택에 반환.
    * `ClearAllPools()`: 씬 종료 또는 리셋 시 풀링된 객체 일괄 메모리 해제.

* **[ResourceManager.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Utility/ResourceManager.cs)** `ResourceManager` (C# Singleton 클래스)
  * **사용처:** Resources 폴더 내 UI 프리팹 및 에셋 로드 및 메모리 캐시 제어.
  * **주요 기능 및 동작:**
    * `Load<T>(string path)`: 내부 딕셔너리 캐시를 우선 확인하고 미캐싱 시 `Resources.Load<T>` 호출 후 캐싱.
    * `ClearCache()`: 캐시 비우기 및 `Resources.UnloadUnusedAssets` 호출.

* **[TestInputController.cs](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Utility/TestInputController.cs)** `TestInputController` (MonoBehaviour)
  * **사용처:** 실제 유튜브 스트리밍 API 연결 없이도 키보드로 모든 인게임 시스템을 완벽히 검증할 수 있는 디버그 시뮬레이터.
  * **주요 기능 및 단축키 매핑:**
    * New Input System과 Legacy Input 자동 호환.
    * `[H]`: 온스크린 조작 가이드 GUI On/Off 토글.
    * `[A] / [D] / [←] / [→]`: 크레인 좌/우 이동 (`!좌`, `!우`).
    * `[1] ~ [5]`: 크레인 1~5번 열 즉시 이동 (`!1` ~ `!5`).
    * `[Space]`: 시청자 채팅 동물 투하 (`!드롭` - 쿨타임 20초 검증 포함).
    * `[L]`: 좋아요 바운스 시뮬레이션.
    * `[S]`: 신규 구독 4단계 동물 소환.
    * `[Q]`: $1 소형 동물 일괄 폭파 청소.
    * `[W]`: $2 무지개 별 투하 (만능 1단계 진화).
    * `[E]`: $3 무적 방해 블록 투하.
    * `[R]`: $5 10초 자석 피버 폭풍 발동.
    * `[9]`: 9단계 동물 2마리 즉시 스폰 (클리어 머지 테스트용 치트).
    * `[0]`: 10단계 최종 동물 즉시 완성 (클리어 빵빠레 세리머니 테스트용 치트).
    * `[X]`: 한계선 10초 초과 실패 리셋 강제 발동.

---

## 4. GEMINI 하네스 선별 업데이트 원칙 (Selective Update Rules)

1. **선별 업데이트 원칙**:
   * 향후 기능 개발 및 수정 작업 완료 시 **전체 문서를 덮어쓰지 않고**, **해당 작업에서 신규 추가되거나 변경된 스크립트 항목만** 찾아 내용을 갱신합니다.
2. **디렉터리 포맷 규정 준수**:
   * 신규 스크립트 작성 시 1번 항목 트리에 `디렉터리 > - 스크립트 : 사용처, 주요 기능` 형식으로 등록합니다.
3. **코드 보존 및 일관성**:
   * 기존 클래스의 공개 인터페이스와 이벤트 계약을 보존하며 변경 사항을 반영합니다.
