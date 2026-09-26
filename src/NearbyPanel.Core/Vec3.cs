namespace NearbyPanel.Core;

/// <summary>
/// A position, independent of UnityEngine. The plugin converts Unity vectors into
/// this at the adapter boundary so that every calculation below stays testable.
/// </summary>
public readonly struct Vec3
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;

    public Vec3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
