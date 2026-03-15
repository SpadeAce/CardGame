using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class)]
public class AssetPathAttribute : Attribute
{
    public string Path { get; }
    public AssetPathAttribute(string path) => Path = path;
}