using System;

/// <summary>
/// Double precision Vector3 for accurate calculations.
/// We CAN compile godot for double precision but:
/// 1) I don't want to
/// 2) reimplementing it here allows us to make the crystal program more portable 
/// </summary>
public struct Vector3d
{
    public static Vector3d Zero { get => new(0, 0, 0); }
    public static Vector3d One { get => new(1, 1, 1); }
    public static Vector3d Right { get => new(1, 0, 0); }
    public static Vector3d Left { get => new(-1, 0, 0); }
    public static Vector3d Up { get => new(0, 1, 0); }
    public static Vector3d Down { get => new(0, -1, 0); }
    public static Vector3d Forward { get => new(0, 0, -1); }//Godot does -1 as forward. Although the whole reason I implemented this is so that the code can be removed from godot...
    public static Vector3d Backward { get => new(0, 0, 1); }//I still left it this way to be consistent.
    public static readonly double threshold = 0.00001;
    public double X { get => x; set => x = value; }
    public double Y { get => y; set => y = value; }
    public double Z { get => z; set => z = value; }
    public double x, y, z;
    public Vector3d() { this.x = 0; this.y = 0; this.z = 0; }
    public Vector3d(double x, double y, double z)
    {
        if (x is double.NaN || y is double.NaN || z is double.NaN)
            throw new ArgumentException("Was given NaN as value!");
        this.x = x; this.y = y; this.z = z;
    }
    public double LengthSquared() => x * x + y * y + z * z;
    public double Length() => Math.Sqrt(LengthSquared());
    public double DistanceTo(Vector3d other) => Distance(this, other);
    public double Dot(Vector3d other) => Dot(this, other);
    public Vector3d Cross(Vector3d other) => Cross(this, other);
    public Vector3d Normalized()
    {
        double l = Length();
        if (Length() == 0)
            return this;
        return this / Length();
    }
    public bool IsEqualApprox(Vector3d other) => IsEqualApprox(this, other);
    public bool IsZeroApprox() => IsZeroApprox(this);

    public static double Distance(Vector3d a, Vector3d b) => (a - b).Length();
    public static double SqrDistance(Vector3d a, Vector3d b) => (a - b).LengthSquared();
    public static double Dot(Vector3d a, Vector3d b) => a.x * b.x + a.y * b.y + a.z * b.z;
    public static Vector3d Cross(Vector3d a, Vector3d b) => new(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
    public static bool IsEqualApprox(Vector3d a, Vector3d b) => a == b;
    public static bool IsExactlyEqual(Vector3d a, Vector3d b) => a.x == b.x && a.y == b.y && a.z == b.z;
    public static bool IsZeroApprox(Vector3d v) => v == Zero;


    public static bool operator ==(Vector3d a, Vector3d b) => SqrDistance(a, b) < threshold;
    public static bool operator !=(Vector3d a, Vector3d b) => SqrDistance(a, b) >= threshold;
    public static Vector3d operator *(Vector3d v, double s) => new(v.x * s, v.y * s, v.z * s);
    public static Vector3d operator /(Vector3d v, double s)
    {
        if (s == 0)
            throw new DivideByZeroException();
        return v * (1 / s);
    }
    public static Vector3d operator +(Vector3d a, Vector3d b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
    public static Vector3d operator -(Vector3d a, Vector3d b) => a + -b;
    public static Vector3d operator -(Vector3d v) => new(-v.x, -v.y, -v.z);
    /// <summary>
    /// Basis/Linear transformation
    /// </summary>
    /// <param name="m">Basis matrix to multiply with</param>
    /// <param name="v">Vector to transform</param>
    /// <returns>Transformed vector</returns>
    public static Vector3d operator *(Vector3d[] m, Vector3d v)
    {
        if (m.Length < 3)
            throw new ArgumentException("Must be a 3x3 matrix!");
        return m[0] * v.x + m[1] * v.y + m[2] * v.z;
    }


    public override bool Equals(object obj)
    {
        if (obj is null)
            return false;
        if (obj is Vector3d v)
            return this == v;
        return false;
    }
    public override string ToString() => $"({x}, {y}, {z})";

    public override int GetHashCode() => base.GetHashCode();
}

