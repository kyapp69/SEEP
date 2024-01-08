using UnityEngine;

public static class UnityExtensions
{
    public static Vector2 ToVector2(this Vector3 vec3)
    {
        return new Vector2(vec3.x, vec3.y);
    }

    public static Vector3 ToVector3(this Vector2 vec2)
    {
        return new Vector3(vec2.x, vec2.y);
    }

    public static Vector2 Reverse(this Vector2 vec2)
    {
        return new Vector2(vec2.y, vec2.x);
    }
}