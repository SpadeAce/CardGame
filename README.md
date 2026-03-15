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

## 기획서

| # | 문서 |
|---|------|
| 00 | [프로젝트 개요](https://github.com/SpadeAce/CardGame/tree/main/WorkReports/Design/00_%ED%94%84%EB%A1%9C%EC%A0%9D%ED%8A%B8_%EA%B0%9C%EC%9A%94.md) |
| 01 | [씬 흐름](https://github.com/SpadeAce/CardGame/tree/main/WorkReports/Design/01_%EC%94%AC_%ED%9D%90%EB%A6%84.md) |
| 02 | [전투 시스템](https://github.com/SpadeAce/CardGame/tree/main/WorkReports/Design/02_%EC%A0%84%ED%88%AC_%EC%8B%9C%EC%8A%A4%ED%85%9C.md) |
| 03 | [카드 시스템](https://github.com/SpadeAce/CardGame/tree/main/WorkReports/Design/03_%EC%B9%B4%EB%93%9C_%EC%8B%9C%EC%8A%A4%ED%85%9C.md) |
| 04 | [타일 맵 시스템](https://github.com/SpadeAce/CardGame/tree/main/WorkReports/Design/04_%ED%83%80%EC%9D%BC_%EB%A7%B5_%EC%8B%9C%EC%8A%A4%ED%85%9C.md) |
| 05 | [캐릭터 시스템](https://github.com/SpadeAce/CardGame/tree/main/WorkReports/Design/05_%EC%BA%90%EB%A6%AD%ED%84%B0_%EC%8B%9C%EC%8A%A4%ED%85%9C.md) |
| 06 | [UI 시스템](https://github.com/SpadeAce/CardGame/tree/main/WorkReports/Design/06_UI_%EC%8B%9C%EC%8A%A4%ED%85%9C.md) |
| 07 | [영입 시스템](https://github.com/SpadeAce/CardGame/tree/main/WorkReports/Design/07_%EC%98%81%EC%9E%85_%EC%8B%9C%EC%8A%A4%ED%85%9C.md) |
| 08 | [자원 시스템](https://github.com/SpadeAce/CardGame/tree/main/WorkReports/Design/08_%EC%9E%90%EC%9B%90_%EC%8B%9C%EC%8A%A4%ED%85%9C.md) |
| 09 | [상점 시스템](https://github.com/SpadeAce/CardGame/tree/main/WorkReports/Design/09_%EC%83%81%EC%A0%90_%EC%8B%9C%EC%8A%A4%ED%85%9C.md) |
| 10 | [덱편성 시스템](https://github.com/SpadeAce/CardGame/tree/main/WorkReports/Design/10_%EB%8D%B1%ED%8E%B8%EC%84%B1_%EC%8B%9C%EC%8A%A4%ED%85%9C.md) |
| 99 | [로드맵](https://github.com/SpadeAce/CardGame/tree/main/WorkReports/Design/99_%EB%A1%9C%EB%93%9C%EB%A7%B5.md) |
