
using System;

public struct Planed
{
    public Vector3d Normal { get => normal; set => normal = value; }
    public double Distance { get => distance; set => distance = value; }
    public double D { get => distance; set => distance = value; }

    public Vector3d normal;
    public double distance;

    public Planed(Vector3d normal, double distance) { this.normal = normal; this.distance = distance; }

    //https://stackoverflow.com/a/41897378
    public Vector3d Project(Vector3d v) => v - normal * (normal.Dot(v) + distance);
    public double DistanceTo(Vector3d v) => normal.Dot(v) + distance;
    public static bool operator ==(Planed a, Planed b) => Vector3d.SqrDistance(a.normal, b.normal) < Vector3d.threshold && System.Math.Abs(a.distance - b.distance) < Vector3d.threshold;
    public static bool operator !=(Planed a, Planed b) => !(a == b);

    /// <summary>
    /// Finds the point of intersection between 3 planes, returns null if not found
    /// https://github.com/godotengine/godot/blob/master/core/math/plane.cpp
    /// </summary>
    /// <param name="planed1"></param>
    /// <param name="planed2"></param>
    /// <returns></returns>
    public Vector3d? Intersect3(Planed plane1, Planed plane2)
    {
        Vector3d normal0 = this.normal;
        Vector3d normal1 = plane1.normal;
        Vector3d normal2 = plane2.normal;
        double denom = normal0.Cross(normal1).Dot(normal2);

        if (denom * denom < 0.000001)
        {
            return null;
        }

        return ((Vector3d.Cross(normal1, normal2) * this.D) +
                (Vector3d.Cross(normal2, normal0) * plane1.D) +
                (Vector3d.Cross(normal0, normal1) * plane2.D)) /
                denom;
    }
}
