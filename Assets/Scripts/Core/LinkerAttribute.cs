using UnityEngine;

public class LinkerAttribute : PropertyAttribute
{
    public string[] paths;

    public LinkerAttribute(params string[] paths)
    {
        this.paths = paths;
    }
}
