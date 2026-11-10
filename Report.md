# [Report] 사운드 매니저 구현 및 리소스 관리 체계 구축 보고서

본 문서는 `Request.md`의 요청 사항에 따라 신규 전역 사운드 관리자([`SoundManager.cs`](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Core/SoundManager.cs)) 및 리소스 식별자 테이블([`ResourceName.cs`](file:///d:/Unity/Github/YoutubeGame_1/Assets/Scripts/Data/ResourceName.cs))을 구현하고, 기존 프로젝트 코드에서 사운드 매니저를 호출하도록 연동한 작업 결과를 상세히 정리한 보고서입니다.

---

## 1. 작업 개요

* **요청 요구사항**:
  1. **사운드 매니저 (`SoundManager`) 구현**:
     - `Play(string soundName, bool force)`: 1회성 효과음(SFX) 재생
     - `PlayBGM(bool soundName)` 및 `PlayBGM(string soundName, bool loop = true)`: 배경음 재생/정지
     - 전역 인스턴스(Singleton)로 동작하며 `Resources`에서 이름을 통해 `AudioClip` 로드
     - 효과음: 사용 후 **10초 동안 캐싱** (10초 경과 시 자동 리소스 해제)
     - 배경음: **현재 사용 중인 배경음 1개만 캐싱**하고 미사용 시 즉시 리소스 해제
     - `AudioSource` 컴포넌트 풀링: 각 사운드 종류별(BGM/SFX) 기본 10개씩 보유, 모두 재생 중일 시 동적으로 신규 생성 확장
     - 리소스 미존재 시 `Warning` 로그 출력 (`Debug.LogWarning`)
  2. **리소스 이름 파일 (`ResourceName.cs`)**:
     - 사운드 및 리소스 경로를 `ResourceName` const 상수로 모아서 작성
     - 향후 모든 리소스 이름은 해당 파일을 통해 관리하도록 체계화
  3. **기존 프로젝트 코드 사운드 연동**:
     - `GameManager.cs`: 최종 10단계 클리어 빵빠레 사운드 및 동물 머지 사운드 연동
     - `CountdownUI.cs`: 데드라인 10초 카운트다운 경고 사이렌 사운드 연동
     - `Generator.cs`: 씬 자동 생성 시 `SoundManager` 포함

---

## 2. 변경 내역 상세 (What Changed)

### ① `Assets/Scripts/Data/ResourceName.cs` [신규 추가]
* **`ResourceName` (static 클래스)**
  * **[추가]** 사운드 리소스 경로 상수:
    - `SOUND_BGM_MAIN` (`"Sounds/BGM_Main"`)
    - `SOUND_FANFARE` (`"Sounds/Fanfare"`)
    - `SOUND_WARNING` (`"Sounds/Warning"`)
    - `SOUND_MERGE` (`"Sounds/Merge"`)
    - `SOUND_POP` (`"Sounds/Pop"`)
    - `SOUND_BOUNCE` (`"Sounds/Bounce"`)
    - `SOUND_STAR` (`"Sounds/Star"`)
    - `SOUND_TROLL` (`"Sounds/Troll"`)
    - `SOUND_FEVER` (`"Sounds/Fever"`)
    - `SOUND_DROP` (`"Sounds/Drop"`)
  * **[추가]** UI 및 아이콘 리소스 경로 상수 (`UI_MAIN`, `ICON_BAD_BLOCK`, `ICON_1~4`, `ICON_ANIMAL_0~8` 등)

### ② `Assets/Scripts/Core/SoundManager.cs` [신규 추가]
* **`SoundManager` (MonoBehaviour, Singleton)**
  * **[추가]** `Instance` 프로퍼티 및 `DontDestroyOnLoad` 전역 상주 라이프사이클
  * **[추가]** `Play(string soundName, bool force = false)`: 1회성 효과음 재생 및 10초 캐시 갱신
  * **[추가]** `PlayBGM(string soundName, bool loop = true)`: 단일 배경음 캐싱/재생 및 기존 배경음 즉시 해제
  * **[추가]** `PlayBGM(bool soundName)`: 요구서 명세 시그니처 대응 (true: 재생/재개, false: BGM 정지 및 리소스 해제)
  * **[추가]** `Stop(string soundName)`: 특정 효과음 재생 중지
  * **[추가]** `StopBGM()`: 배경음 정지 및 리소스 즉시 언로드
  * **[추가]** `InitializeAudioPools()` / `CreateNewAudioSource()`: SFX 10개, BGM 10개 초기 풀링 및 동적 확장 처리
  * **[추가]** `UpdateSFXCacheLifetime()`: `Update()`에서 10초 경과한 미사용 효과음 클립 자동 캐시 제거 및 `Resources.UnloadAsset` 메모리 해제

### ③ `Assets/Scripts/Core/GameManager.cs` [변경]
* **[변경]** `OnMergeSuccess`:
  - 머지 성공 시 `SoundManager.Instance.Play(ResourceName.SOUND_MERGE, false);` 호출 추가
* **[변경]** `ClearSequenceRoutine`:
  - 10단계 완성 빵빠레 연출 시 `SoundManager.Instance.Play(ResourceName.SOUND_FANFARE, true);` 호출 (미존재 시 기존 `fanfareAudioSource` fallback 지원)

### ④ `Assets/Scripts/UI/CountdownUI.cs` [변경]
* **[변경]** `UpdateCountdown`:
  - 10초 카운트다운 위험 상태 진입 시 `SoundManager.Instance.Play(ResourceName.SOUND_WARNING, false);` 호출
* **[변경]** `HideCountdown`:
  - 위험 해제 시 `SoundManager.Instance.Stop(ResourceName.SOUND_WARNING);` 호출

### ⑤ `Assets/Scripts/Core/Generator.cs` [변경]
* **[변경]** `GenerateAllManagers`:
  - `[GameManagers]` 생성 파이프라인에 `SoundManager sound = EnsureComponent<SoundManager>(managersRoot);` 추가

---

## 3. 인스펙터 및 외부 인터페이스 영향도 (Critical Check)

| 항목 | 상세 내용 |
| :--- | :--- |
| **인스펙터 참조 변수** | • **`SoundManager`**: `masterVolume`, `bgmVolume`, `sfxVolume` 슬라이더 노출 (0~1 범위 볼륨 조절 가능)<br>• **`GameManager` / `CountdownUI`**: 기존 인스펙터 직렬화 필드(`fanfareAudioSource`, `warningAudioSource`)를 삭제하지 않고 100% 보존하여 유니티 에디터 상의 컴포넌트 누락/Missing 에러 원천 방지 |
| **공개 API/함수** | • **`SoundManager.Instance`**: 전역 접근 싱글톤<br>• `Play(string, bool)`: 1회성 효과음 재생<br>• `PlayBGM(bool)` 및 `PlayBGM(string, bool)`: 배경음 재생/정지<br>• `Stop(string)`, `StopBGM()`: 사운드 정지 인터페이스 |
| **상속 및 인터페이스** | 변경 없음 (`MonoBehaviour` 상속 유지) |
| **빌드 안정성** | `dotnet build Assembly-CSharp.csproj` 검증 완료 (**오류 0개, 정상 빌드 성공**) |

---

## 4. 일관성 검증 결과

1. **24시간 무인 스트리밍 메모리 안정성**:
   - 효과음은 사용 후 10초 동안만 캐싱되고 자동으로 해제되어 무한한 메모리 점유를 방지합니다.
   - 배경음은 현재 사용 중인 1개만 캐싱되며, 곡이 바뀌거나 정지되면 이전 곡 리소스가 즉시 언로드되어 메모리 누수를 원천 차단합니다.
   - 사운드 재생 폭주 시 기본 10개의 AudioSource 풀을 초과하더라도 동적으로 생성하여 소리가 씹히거나 끊기지 않습니다.
2. **하위 호환성 및 예외 안전성**:
   - 리소스 파일이 실제로 존재하지 않아도 크래시 없이 경고(`Debug.LogWarning`)만 출력되고 게임 루프가 중단 없이 유지됩니다.
   - `SoundManager.Instance`가 초기화되기 전이거나 부재하더라도 기존 직렬화 `AudioSource`로 fallback 동작하도록 안전 가드를 적용했습니다.
3. **문서 동기화**:
   - [`Script.md`](file:///d:/Unity/Github/YoutubeGame_1/Script.md)의 디렉터리 트리 및 `Core/SoundManager.cs`, `Data/ResourceName.cs` 상세 명세를 최신화했습니다.
