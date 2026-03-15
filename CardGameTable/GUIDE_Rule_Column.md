# Proto 스키마 — Rule 컬럼 가이드

## 개요

`Proto_*.xlsx`의 `_Schema` 시트에서 **Rule** 컬럼은 proto3 필드 규칙을 지정한다.

---

## 사용 가능한 Rule 값

| Rule | proto3 생성 | 데이터 Excel 입력 방식 | 설명 |
|------|------------|----------------------|------|
| `optional` | `optional FieldType name = N;` | 단일 컬럼, 단일 값 | 기본값. 값이 없으면 proto3 기본값 사용 |
| `repeated` | `repeated FieldType name = N;` | **다중 컬럼** 또는 쉼표 구분 문자열 | 리스트(배열) 필드 |

> **빈 칸 기본값**: Rule 컬럼이 비어 있으면 `optional`로 처리된다.

---

## optional (기본)

스칼라 값 1개를 저장한다.

### _Schema 시트
| MessageName | FieldName | FieldType | FieldNumber | Rule     | Comment |
|-------------|-----------|-----------|-------------|----------|---------|
| PawnData    | hp        | int32     | 3           | optional | 체력     |
| PawnData    | name      | string    | 2           | optional | 이름     |

### 데이터 시트 (PawnData 시트)
| id | name   | hp |
|----|--------|----|
| 1  | 스카우트 | 20 |
| 2  | 어썰트  | 30 |

### 생성 결과 (proto)
```protobuf
message PawnData {
  int32 id   = 1;
  string name = 2;
  int32 hp   = 3;
}
```

### C# 사용
```csharp
GameData.PawnData data = DataManager.Instance.Pawn.Get(1);
Debug.Log(data.Hp);    // 20
Debug.Log(data.Name);  // "스카우트"
```

---

## repeated (리스트)

0개 이상의 값을 리스트로 저장한다.

### _Schema 시트
| MessageName | FieldName | FieldType | FieldNumber | Rule     | Comment      |
|-------------|-----------|-----------|-------------|----------|--------------|
| PawnData    | skills    | int32     | 14          | repeated | 보유 스킬 ID 목록 |

### 데이터 시트 — 방식 A: 다중 컬럼 (권장)

동일 컬럼명을 여러 번 사용한다. 빈 셀은 자동으로 제외된다.

| id | name   | skills | skills | skills |
|----|--------|--------|--------|--------|
| 1  | 스카우트 | 101    | 102    | 103    |
| 2  | 어썰트  | 201    |        |        |
| 3  | 헤비    |        |        |        |

**결과:**
- 폰1: `skills = [101, 102, 103]`
- 폰2: `skills = [201]`
- 폰3: `skills = []` (빈 리스트)

### 데이터 시트 — 방식 B: 쉼표 구분 문자열 (하위 호환)

단일 컬럼에 쉼표로 구분하여 입력한다.

| id | name   | skills    |
|----|--------|-----------|
| 1  | 스카우트 | 101,102,103 |
| 2  | 어썰트  | 201       |
| 3  | 헤비    |           |

### 생성 결과 (proto)
```protobuf
message PawnData {
  int32 id                = 1;
  string name             = 2;
  repeated int32 skills   = 14;
}
```

### C# 사용
```csharp
GameData.PawnData data = DataManager.Instance.Pawn.Get(1);

// repeated 필드 → Google.Protobuf.Collections.RepeatedField<int>
foreach (int skillId in data.Skills)
{
    Debug.Log($"스킬 ID: {skillId}");
}

// Count, Contains 등 IList<T> 메서드 사용 가능
Debug.Log($"스킬 개수: {data.Skills.Count}");  // 3
bool hasSkill101 = data.Skills.Contains(101);   // true
```

---

## repeated + enum 예제

| MessageName | FieldName  | FieldType      | FieldNumber | Rule     | Comment     |
|-------------|------------|----------------|-------------|----------|-------------|
| PawnData    | equipSlots | enum:ItemType  | 15          | repeated | 장착 슬롯 타입 목록 |

**데이터 시트 (다중 컬럼, enum 이름 사용)**

| id | name   | equipSlots  | equipSlots  | equipSlots |
|----|--------|-------------|-------------|------------|
| 1  | 스카우트 | EQUIPMENT   | CARD        |            |
| 2  | 어썰트  | EQUIPMENT   | EQUIPMENT   | CARD       |

**C# 사용**
```csharp
GameData.PawnData data = DataManager.Instance.Pawn.Get(1);
foreach (GameData.ItemType slotType in data.EquipSlots)
{
    Debug.Log(slotType);  // ItemType.Equipment, ItemType.Card, ...
}
```

---

## 주의사항

1. **optional 필드에 다중 컬럼 사용 불가** — 변환 시 오류 발생
2. **repeated 필드의 다중 컬럼과 쉼표 방식 혼용 불가** — 한 시트 내에서 하나의 방식만 사용
3. **빈 셀** — 다중 컬럼 방식에서 빈 셀은 자동 제외됨 (None으로 수집 후 필터링)
4. **proto3 기본값** — repeated 필드에 값이 전혀 없으면 빈 리스트 `[]`로 처리됨
