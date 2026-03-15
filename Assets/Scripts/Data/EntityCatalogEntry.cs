using UnityEngine;

[System.Serializable]
public class EntityCatalogEntry
{
    public int id;                    // 고유 정수 ID (0 = 미사용, 유효값 1 이상)
    public string displayName;        // 에디터 표시명
    public EntityCategory category;   // 카테고리 분류
    public GameObject prefab;         // 직접 레퍼런스 (ScriptableObject 방식)
    public string resourcePath;       // Resources 상대 경로 (Protobuf 전환 시 사용)
}
