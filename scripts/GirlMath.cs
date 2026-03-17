using Godot;

internal static class GirlMath {
    public static bool IsEqualApprox(Quaternion first, Quaternion other, float tolerance) {
        if (
            Mathf.IsEqualApprox(first.X, other.X, tolerance) &&
            Mathf.IsEqualApprox(first.Y, other.Y, tolerance) &&
            Mathf.IsEqualApprox(first.Z, other.Z, tolerance)
        ) {
            return Mathf.IsEqualApprox(first.W, other.W, tolerance);
        }

        return false;
    }

    public static bool IsEqualApprox(Vector3 first, Vector3 other, float tolerance) {
        if (
            Mathf.IsEqualApprox(first.X, other.X, tolerance) &&
            Mathf.IsEqualApprox(first.Y, other.Y, tolerance)
        ) {
            return Mathf.IsEqualApprox(first.Z, other.Z, tolerance);
        }

        return false;
    }
}