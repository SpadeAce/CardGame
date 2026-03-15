using System;
using UnityEngine;

/// <summary>
/// EdgeTile 식별을 위한 좌표 쌍 키 (내부 Dictionary 전용)
/// 두 인접 타일의 좌표를 정규화하여 순서 무관하게 동일한 Edge를 가리킴
/// </summary>
public readonly struct EdgeTileKey : IEquatable<EdgeTileKey>
{
    public readonly Vector2Int PosA;
    public readonly Vector2Int PosB;

    public EdgeTileKey(Vector2Int a, Vector2Int b)
    {
        // 정규화: 항상 작은 좌표가 PosA에 오도록 정렬
        if (Compare(a, b) <= 0)
        {
            PosA = a;
            PosB = b;
        }
        else
        {
            PosA = b;
            PosB = a;
        }
    }

    public bool Equals(EdgeTileKey other)
    {
        return PosA == other.PosA && PosB == other.PosB;
    }

    public override bool Equals(object obj)
    {
        return obj is EdgeTileKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PosA, PosB);
    }

    public static bool operator ==(EdgeTileKey left, EdgeTileKey right) => left.Equals(right);
    public static bool operator !=(EdgeTileKey left, EdgeTileKey right) => !left.Equals(right);

    public override string ToString() => $"Edge({PosA} - {PosB})";

    /// <summary>
    /// Vector2Int 비교 (x → y 순)
    /// </summary>
    private static int Compare(Vector2Int a, Vector2Int b)
    {
        if (a.x != b.x) return a.x.CompareTo(b.x);
        return a.y.CompareTo(b.y);
    }
}
