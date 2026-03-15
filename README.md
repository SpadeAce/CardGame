# CardGame

턴제 전술 카드 게임. 타일맵 위에서 폰(Pawn)을 이동시키고 카드를 사용해 몬스터를 처치하는 전략 RPG입니다.

## 기술 스택

| 항목 | 내용 |
|------|------|
| 엔진 | Unity |
| 언어 | C# |
| 데이터 직렬화 | Google Protocol Buffer (Protobuf) |
| 데이터 저장 | `.bytes` 파일 (Resources 폴더) |

## 주요 시스템

- **전투 시스템** — 턴제 행동력(AP) 기반, 카드 사용 및 폰 이동
- **카드 시스템** — 폰별 카드풀, 덱 빌딩
- **타일맵 시스템** — 격자 기반 이동 및 범위 공격
- **캐릭터(Pawn) 시스템** — 스탯, 장비 장착, 카드풀 관리
- **상점 / 영입 시스템** — 골드로 카드 구매 및 Pawn 영입
- **덱 편성 UI** — 드래그 & 우클릭 기반 편성

## 씬 구성

| 씬 | 설명 |
|----|------|
| BootScene | 데이터 로드 후 타이틀로 전환 |
| TitleScene | 타이틀 화면 |
| LobbyScene | 덱 세팅, 상점, 영입 |
| StageScene | 전투 진행 |

## 아키텍처 개요

```
UI 레이어      TitlePage / LobbyPage / StagePage / Popup
                          ↕ 이벤트 / OpenView
Logic 레이어   StageManager · TurnManager · DeckManager
               TileManager  · PawnManager · ItemManager
                          ↕ 읽기/쓰기
Data 레이어    DPawn · DMonster · DCard · DEquipment (Protobuf)
```

- `MonoSingleton<T>` — Instance 최초 접근 시 자동 생성, `ManagerRoot` 하위 배치
- `Singleton<T>` — 순수 C# 싱글톤 (`SceneController`, `DataManager`)
