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
        if (Double.IsNegative(x) && x == -0.0)
            x = 0;
        if (Double.IsNegative(y) && y == -0.0)
            y = 0;
        if (Double.IsNegative(z) && z == -0.0)
            z = 0;
        this.x = x; this.y = y; this.z = z;
    }
    /// <summary>
    /// Returns Squared distance of this vector from the origin. 
    /// Best used for relative comparisons, and is faster than normal Length()
    /// </summary>
    /// <returns>The length of this vector, squared</returns>
    public double LengthSquared() => x * x + y * y + z * z;
    /// <summary>
    /// Returns the distance of this vector from the origin
    /// Note: For relative comparisons, use LengthSquared(), since it is faster
    /// </summary>
    /// <returns>The length of this vector</returns>
    public double Length() => Math.Sqrt(LengthSquared());
    /// <summary>
    /// Returns the squared distance between this vector and the other
    /// Best used for relative comparisons, and is faster than normal DistanceTo()
    /// </summary>
    /// <param name="other">The vector to find the distance to</param>
    /// <returns>The distance between this vector and the other</returns>
    public double SqrDistanceTo(Vector3d other) => SqrDistance(this, other);
    /// <summary>
    /// Returns the distance between this vector and the other    
    /// Note: For relative comparisons, use SqrDistanceTo(), since it is faster
    /// </summary>
    /// <param name="other">The vector to find the distance to</param>
    /// <returns>The distance between this vector and the other</returns>
    public double DistanceTo(Vector3d other) => Distance(this, other);
    /// <summary>
    /// Returns the scalar dot product of this vector and the other. Is commutative.    
    /// The dot product is how much two vectors "Agree" with each other. 
    /// If they align it is >0, if they are perpendicular it is 0, if they do not align it is <0
    /// If they align perfectly it is |a| * |b|
    /// If the second vector is normalized(|b| == 1) then the dot product can be thought of as a projection onto it.
    /// </summary>
    /// <param name="other">The vector to calculte the dot product with</param>
    /// <returns>The dot product of of the two vectors.</returns>
    /// <remarks>
    /// We use a.x * b.x + a.y * b.y + a.z * b.z for this, 
    /// though |a|*|b|*cos(angle between the two) is clearer mathematically
    /// </remarks>
    public double Dot(Vector3d other) => Dot(this, other);
    /// <summary>
    /// Returns the vector cross product of this vector and the other. 
    /// Is anti-commutative, so flipping inputs returns the result * -1.
    /// The cross product returns a perpendicular vector whose length is the area of the parallelegram of the two input vectors.
    /// If a, b are colinear, the cross product has a magnitude of zero.
    /// If a, b are perpendicular, the cross product has a magnitude of |a| * |b|
    /// </summary>
    /// <param name="other">The vector to calculate the cross product with</param>
    /// <returns>The cross product of the two vectors</returns>
    /// <remarks>The formula is (a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x)</remarks>
    public Vector3d Cross(Vector3d other) => Cross(this, other);
    /// <summary>
    /// Returns this vector with a length of 1, or (0,0,0) if it already has a length of zero
    /// </summary>
    /// <returns>Returns this vector with a length of 1, or (0,0,0) if it already has a length of zero</returns>
    public Vector3d Normalized()
    {
        double l = Length();
        if (Length() < threshold)//I could do this branchless, but the compiler likely already does
            return this;
        return this / Length();
    }
    /// <summary>
    /// Returns true if these two vectors are approximately equal. The equality operator does this anyway.
    /// </summary>
    /// <param name="other">The vector to check equalitty with</param>
    /// <returns>Returns true if these two vectors are approximately equal.</returns>
    public bool IsEqualApprox(Vector3d other) => IsEqualApprox(this, other);
    /// <summary>
    /// Returns true if this vector is approximately equal
    /// </summary>
    /// <returns>Returns true if this vector is approximately equal</returns>
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
    /// <param name="m">3x3 Basis matrix to multiply with</param>
    /// <param name="v">Vector to transform</param>
    /// <returns>Transformed vector</returns>
    public static Vector3d operator *(Vector3d[] m, Vector3d v)
    {
        if (m.Length != 3)
            throw new ArgumentException("Must be a 3x3 matrix!");
        return m[0] * v.x + m[1] * v.y + m[2] * v.z;
    }
    public override readonly bool Equals(object obj)
    {
        if (obj is null)
            return false;
        if (obj is Vector3d v)
            return this == v;
        return false;
    }
    public override readonly string ToString() => $"({x}, {y}, {z})";

    public override readonly int GetHashCode() => ((x * 2).GetHashCode()
    + (y * 3).GetHashCode()
    + (z * 5).GetHashCode()).GetHashCode();
}

