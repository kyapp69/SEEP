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

    public static Quaternion SmoothDamp(this Quaternion rot, Quaternion target, ref Quaternion deriv, float time)
    {
        if (Time.deltaTime < Mathf.Epsilon) return rot;
        // account for double-cover
        var Dot = Quaternion.Dot(rot, target);
        var Multi = Dot > 0f ? 1f : -1f;
        target.x *= Multi;
        target.y *= Multi;
        target.z *= Multi;
        target.w *= Multi;
        // smooth damp (nlerp approx)
        var Result = new Vector4(
            Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time),
            Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time),
            Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time),
            Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)
        ).normalized;

        // ensure deriv is tangent
        var derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), Result);
        deriv.x -= derivError.x;
        deriv.y -= derivError.y;
        deriv.z -= derivError.z;
        deriv.w -= derivError.w;

        return new Quaternion(Result.x, Result.y, Result.z, Result.w);
    }
}