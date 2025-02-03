using UnityEngine;

public class QuaternionMy
{
    public static Quaternion SlerpUnclamped(Quaternion from, Quaternion to, float t)
    {
        float dot = Quaternion.Dot(from, to);
        if (dot < 0f)
        {
            to = new Quaternion(-to.x, -to.y, -to.z, -to.w);
            dot = -dot;
        }
        dot = Mathf.Clamp(dot, -1f, 1f);
        float theta = Mathf.Acos(dot);
        if (Mathf.Abs(theta) < 1e-6f) return from;
        float sinTheta = Mathf.Sqrt(1f - dot * dot);
        float alpha = Mathf.Sin((1f - t) * theta) / sinTheta;
        float beta = Mathf.Sin(t * theta) / sinTheta;
        return new Quaternion(
            from.x * alpha + to.x * beta,
            from.y * alpha + to.y * beta,
            from.z * alpha + to.z * beta,
            from.w * alpha + to.w * beta
        );
    }
}
