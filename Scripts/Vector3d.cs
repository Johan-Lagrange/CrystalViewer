using System;
using System.Linq;

/// <summary>
/// Double precision Vector3 for accurate BUT FUZZY calculations.
/// We CAN compile godot for double precision but:
/// 1) I don't want to
/// 2) reimplementing it here allows us to make the crystal program more portable 
/// </summary>
public struct Vector3d : IComparable
{
    //TODO maybe make NAN vectors instead of returning null for intersect3?
    public static Vector3d Zero { get => new(0, 0, 0); }
    public static Vector3d One { get => new(1, 1, 1); }
    public static Vector3d Right { get => new(1, 0, 0); }
    public static Vector3d Left { get => new(-1, 0, 0); }
    public static Vector3d Up { get => new(0, 1, 0); }
    public static Vector3d Down { get => new(0, -1, 0); }
    public static Vector3d Forward { get => new(0, 0, -1); }//Godot does -1 as forward. Although the whole reason I implemented this is so that the code can be removed from godot...
    public static Vector3d Backward { get => new(0, 0, 1); }//I still left it this way to be consistent.
    public static readonly Vector3d[] BasisIdentity = { new(1, 0, 0), new(0, 1, 0), new(0, 0, 1) };
    public static readonly double threshold = 0.0000000001;
    public double X { get => x; set => x = value; }
    public double Y { get => y; set => y = value; }
    public double Z { get => z; set => z = value; }
    public double x, y, z;
    static System.Collections.Generic.Dictionary<double, string> debugDoubleToStr;//turns doubles into "a", "b", "c" in the order they appear
    static System.Collections.Generic.Dictionary<Vector3d, string> debugVectorToStr;//ditto but "A", "B"...

    public Vector3d() { this.x = 0; this.y = 0; this.z = 0; }
    public Vector3d(double x, double y, double z)
    {
        if (x is double.NaN || y is double.NaN || z is double.NaN)
            throw new ArgumentException("Was given NaN as value!");

        this.x = RoundZero(x); this.y = RoundZero(y); this.z = RoundZero(z);
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
    public double SqrDistanceTo(in Vector3d other) => SqrDistance(this, other);
    /// <summary>
    /// Returns the distance between this vector and the other    
    /// Note: For relative comparisons, use SqrDistanceTo(), since it is faster
    /// </summary>
    /// <param name="other">The vector to find the distance to</param>
    /// <returns>The distance between this vector and the other</returns>
    public double DistanceTo(in Vector3d other) => Distance(this, other);
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
    public double Dot(in Vector3d other) => Dot(this, other);
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
    public Vector3d Cross(in Vector3d other) => Cross(this, other);
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
    public bool IsEqualApprox(in Vector3d other) => IsEqualApprox(this, other);
    /// <summary>
    /// Returns true if this vector is approximately equal
    /// </summary>
    /// <returns>Returns true if this vector is approximately equal</returns>
    public bool IsZeroApprox() => IsZeroApprox(this);

    public static double Distance(in Vector3d a, in Vector3d b) => (a - b).Length();
    public static double SqrDistance(in Vector3d a, in Vector3d b) => (a - b).LengthSquared();
    public static double Dot(in Vector3d a, in Vector3d b) => a.x * b.x + a.y * b.y + a.z * b.z;
    public static Vector3d Cross(in Vector3d a, in Vector3d b) => new(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
    public static bool IsEqualApprox(in Vector3d a, in Vector3d b) => a == b;
    public static bool IsExactlyEqual(in Vector3d a, in Vector3d b) => a.x == b.x && a.y == b.y && a.z == b.z;
    public static bool IsZeroApprox(in Vector3d v) => v == Zero;

    public static bool IsZeroApprox(double d) => RoundZero(d) == 0;
    public static double RoundZero(double d) => (d * d) < threshold ? 0 : d;
    public static double RoundAwayFromZero(double d)
    {
        d = RoundZero(d);
        return Math.Sign(d) * Math.Ceiling(Math.Abs(d));
    }
    public static bool operator >(in Vector3d a, in Vector3d b)
    {
        if (a == b)
            return false;
        double delta = a.LengthSquared() - b.LengthSquared();
        if (delta > threshold)
            return true;
        if (delta < -threshold)
            return false;
        //Lengths are equal, tiebreak with individual components
        delta = a.X - b.X;//Sort by X value first
        if (delta > threshold)
            return true;
        if (delta < -threshold)
            return false;

        delta = a.Y - b.Y;//If X values are same, sort by Y...
        if (delta > threshold)
            return true;
        if (delta < -threshold)
            return false;

        delta = a.Z - b.Z;//...Then Z.
        if (delta > threshold)
            return true;
        if (delta < -threshold)
            return false;

        return false;//We shouldn't ever reach this point since we check for equality first.
    }
    public static bool operator <(in Vector3d a, in Vector3d b)
    {
        if (a == b)
            return false;

        double delta = a.LengthSquared() - b.LengthSquared();
        if (delta > threshold)
            return false;
        if (delta < -threshold)
            return true;

        //Lengths are equal, tiebreak with individual components
        delta = a.X - b.X;//Sort by X value first
        if (delta > threshold)
            return false;
        if (delta < -threshold)
            return true;

        delta = a.Y - b.Y;//If X values are same, sort by Y...
        if (delta > threshold)
            return false;
        if (delta < -threshold)
            return true;

        delta = a.Z - b.Z;//...Then Z.
        if (delta > threshold)
            return false;
        if (delta < -threshold)
            return true;

        return false;//We shouldn't ever reach this point since we check for equality first.
    }

    public static bool operator ==(in Vector3d a, in Vector3d b) => SqrDistance(a, b) < threshold;
    public static bool operator !=(in Vector3d a, in Vector3d b) => SqrDistance(a, b) >= threshold;
    public static Vector3d operator *(in Vector3d v, double s) => new(v.x * s, v.y * s, v.z * s);
    public static Vector3d operator /(in Vector3d v, double s)
    {
        if (IsZeroApprox(s))
            throw new DivideByZeroException();
        return v * (1 / s);
    }
    public static Vector3d operator +(in Vector3d a, in Vector3d b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
    public static Vector3d operator -(in Vector3d a, in Vector3d b) => a + -b;
    public static Vector3d operator -(in Vector3d v) => new(-v.x, -v.y, -v.z);
    /// <summary>
    /// Basis/Linear transformation
    /// </summary>
    /// <param name="m">3x3 Basis matrix to multiply with</param>
    /// <param name="v">Vector to transform</param>
    /// <returns>Transformed vector</returns>
    public static Vector3d operator *(in Vector3d[] m, in Vector3d v)
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
    public readonly string ToStringWithCharComponents()
    {
        if (debugDoubleToStr == null)
            debugDoubleToStr = new();
        void AddIfMissing(double d)
        {
            d = Math.Abs(d);
            if (debugDoubleToStr.ContainsKey(d) == false && RoundZero(d) != 0)
                debugDoubleToStr.Add(d, "" + (char)(97 + debugDoubleToStr.Keys.Count()));//Starts at lowercase a
        }
        AddIfMissing(x);
        AddIfMissing(y);
        AddIfMissing(z);

        string TurnToString(double d)
        {
            if (IsZeroApprox(d))
                return " 0";
            if (d == 1)
                return " 1";
            if (d == -1)
                return "-1";
            return (d < 0 ? "-" : " ") + debugDoubleToStr[Math.Abs(d)];
        }
        return $"({TurnToString(x)}, {TurnToString(y)}, {TurnToString(z)})";
    }

    public readonly string ToStringSingleLetter()
    {
        if (debugVectorToStr == null)
            debugVectorToStr = new();
        if (debugVectorToStr.ContainsKey(this) == false)
            debugVectorToStr.Add(this, "" + (char)(65 + debugVectorToStr.Keys.Count()));//Starts at uppercase A

        return debugVectorToStr[this];
    }
    public override readonly int GetHashCode() => ((x * 2).GetHashCode()
    + (y * 3).GetHashCode()
    + (z * 5).GetHashCode()).GetHashCode();

    public int CompareTo(object obj)
    {
        if (obj is Planed p)
            return (int)RoundAwayFromZero(p.DistanceTo(this));

        if (obj is Vector3d b)
        {
            Vector3d a = this;
            if (a == b)
                return 0;
            double delta = a.LengthSquared() - b.LengthSquared();
            if (delta > threshold)
                return 1 + (int)delta;
            if (delta < -threshold)
                return -1 + (int)delta;
            //Lengths are equal, tiebreak with individual components
            delta = a.X - b.X;//Sort by X value first
            if (delta > threshold)
                return 1 + (int)delta;
            if (delta < -threshold)
                return -1 + (int)delta;

            delta = a.Y - b.Y;//If X values are same, sort by Y...
            if (delta > threshold)
                return 1 + (int)delta;
            if (delta < -threshold)
                return -1 + (int)delta;

            delta = a.Z - b.Z;//...Then Z.
            if (delta > threshold)
                return 1 + (int)delta;
            if (delta < -threshold)
                return -1 + (int)delta;

            return 0;//We shouldn't ever reach this point since we check for equality first.}
        }
        return 0;
    }
    public static void ResetDebugLists()
    {
        debugDoubleToStr = new();
        debugVectorToStr = new();
    }
}
