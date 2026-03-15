using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonoBehaviourEx), true)]
public class MonoBehaviourExInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Set Linker"))
            ProcessUILinker();
    }

    public void ProcessUILinker()
    {
        MonoBehaviourEx monoEx = (MonoBehaviourEx)target;
        var fields = monoEx.GetType().GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        bool isDirty = false;

        foreach (var field in fields)
        {
            var linker = field.GetCustomAttribute<LinkerAttribute>();
            if (linker == null || linker.paths.Length == 0) continue;

            bool isList = field.FieldType.IsGenericType
                          && field.FieldType.GetGenericTypeDefinition() == typeof(List<>);

            if (isList)
            {
                Type elementType = field.FieldType.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(field.FieldType);

                foreach (var path in linker.paths)
                {
                    Transform found = FindTarget(monoEx, path);
                    if (found == null) continue;
                    object value = GetValue(found, elementType);
                    if (value != null) list.Add(value);
                }

                field.SetValue(monoEx, list);
                isDirty = true;
            }
            else
            {
                Transform found = FindTarget(monoEx, linker.paths[0]);
                if (found == null) continue;
                object value = GetValue(found, field.FieldType);
                if (value == null) continue;
                field.SetValue(monoEx, value);
                isDirty = true;
            }
        }

        if (isDirty)
            EditorUtility.SetDirty(target);
    }

    private Transform FindTarget(MonoBehaviourEx monoEx, string path)
    {
        Transform found = monoEx.transform.Find(path);
        if (found == null)
        {
            var go = GameObject.Find(path);
            found = go != null ? go.transform : null;
        }
        return found;
    }

    private object GetValue(Transform target, Type fieldType)
    {
        if (fieldType == typeof(GameObject))
            return target.gameObject;
        if (typeof(Component).IsAssignableFrom(fieldType))
            return target.GetComponent(fieldType);
        return null;
    }
}
