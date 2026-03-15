using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Spawn))]
public class SpawnInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        // 기본 인스펙터 그리기 (기존 base.OnInspectorGUI() 와 동일하게 동작하지만 변경된 체크 포함)
        DrawDefaultInspector();

        // 수동으로 링크를 지웠을 때 감지하여 캐시 비우기
        SerializedProperty prefab = serializedObject.FindProperty("_prefab");
        SerializedProperty cachedPath = serializedObject.FindProperty("_cachedPath");

        if (EditorGUI.EndChangeCheck())
        {
            if (prefab.objectReferenceValue == null)
            {
                cachedPath.stringValue = string.Empty;
                Debug.Log("[Spawn] 인스펙터에서 수동으로 프리팹 링크를 제거했습니다.");
            }
        }

        serializedObject.ApplyModifiedProperties();

        Spawn spawn = (Spawn)target;

        // 캐싱된 경로 표시 (읽기 전용)
        if (!string.IsNullOrEmpty(cachedPath.stringValue))
        {
            EditorGUILayout.HelpBox($"캐싱 경로: {cachedPath.stringValue}", MessageType.Info);
        }

        // 프리팹 링크가 빠진 경우 복구 버튼 표시
        if (prefab.objectReferenceValue == null && !string.IsNullOrEmpty(cachedPath.stringValue))
        {
            EditorGUILayout.HelpBox("프리팹 링크가 소실되었습니다.", MessageType.Warning);

            if (GUILayout.Button("경로 기반 링크 복구"))
            {
                GameObject recovered = AssetDatabase.LoadAssetAtPath<GameObject>(cachedPath.stringValue);
                if (recovered != null)
                {
                    prefab.objectReferenceValue = recovered;
                    serializedObject.ApplyModifiedProperties();
                    Debug.Log($"[SpawnInspector] 프리팹 링크 복구 성공: {cachedPath.stringValue}");
                }
                else
                {
                    Debug.LogWarning($"[SpawnInspector] 프리팹 링크 복구 실패: {cachedPath.stringValue}");
                }
            }
        }

        // 완전히 비우는 Clear 버튼 (새로 추가했던 기능)
        GUILayout.Space(10);
        if (GUILayout.Button("Clear Prefab Link", GUILayout.Height(25)))
        {
            Undo.RecordObject(spawn, "Clear Prefab Link");
            spawn.ClearPrefabLink();
            EditorUtility.SetDirty(spawn);
        }
    }
}
