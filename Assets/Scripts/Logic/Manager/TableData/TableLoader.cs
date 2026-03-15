using System;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;

/// <summary>
/// .bytes 리소스를 파싱해 id 기반 Dictionary를 반환하는 공용 헬퍼.
/// 각 TableData 클래스가 내부적으로 사용한다.
/// </summary>
internal static class TableLoader
{
    internal static Dictionary<int, TRow> Load<TTable, TRow>(
        string resourcePath,
        MessageParser<TTable> parser,
        Func<TTable, IEnumerable<TRow>> getItems,
        Func<TRow, int> getId)
        where TTable : IMessage<TTable>
        => Load<TTable, TRow, int>(resourcePath, parser, getItems, getId);

    internal static Dictionary<TKey, TRow> Load<TTable, TRow, TKey>(
        string resourcePath,
        MessageParser<TTable> parser,
        Func<TTable, IEnumerable<TRow>> getItems,
        Func<TRow, TKey> getKey)
        where TTable : IMessage<TTable>
    {
        var asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            Debug.LogError($"[TableLoader] 리소스를 찾을 수 없습니다: Resources/{resourcePath}");
            return new Dictionary<TKey, TRow>();
        }

        var table = parser.ParseFrom(asset.bytes);
        var dict = new Dictionary<TKey, TRow>();
        foreach (var row in getItems(table))
            dict[getKey(row)] = row;
        return dict;
    }
}
