using System;
using System.Collections.Generic;
using System.Linq;

public class CrystalMath
{  /// <summary>
   /// Returns true if v is behind every plane in the list
   /// </summary>
   /// <param name="planes">the list of planes to check if v is behind</param>
   /// <param name="v">the vector to check is behind each plane</param>
   /// <returns>true if the vector is behind every plane in the list.</returns>
  public static bool IsInPlanes(IEnumerable<Planed> planes, Vector3d v)
  {
    foreach (Planed p in planes)
    {
      if (p.IsVectorInFrontOf(v))//Vertex is in front of a face and therefore not on the crystal. Or it's concave and this is broken.
        return false;
    }
    return true;
  }

  /// <summary>
  /// Calculates the area between 3 vertices
  /// https://math.stackexchange.com/questions/128991/how-to-calculate-the-area-of-a-3d-triangle
  /// </summary>
  /// <param name="a">The first vertex</param>
  /// <param name="b">The second vertex</param>
  /// <param name="c">The third vertex</param>
  /// <returns>The area between 3 vertices</returns>
  public static double CalculateTriangleArea(Vector3d a, Vector3d b, Vector3d c)
  {
    return (b - a).Cross(c - a).Length() / 2;
  }

  /// <summary>
  /// Calculates the area of a face defined by a list of vertices
  /// </summary>
  /// <param name="vertices">The list of vertices that defines the face</param>
  /// <returns>The area of the face</returns>
  public static double CalculateFaceArea(IList<Vector3d> vertices, Vector3d[] b = null)
  {
    if (vertices.Count < 3)
      return 0;

    double total = 0;
    for (int i = 1; i <= vertices.Count - 2; i++)
      total += CalculateTriangleArea(b * vertices[0], b * vertices[i], b * vertices[i + 1]);
    return total;
  }

  /// <summary>
  /// Calculates the surface area. 
  /// As we add all all the face areas together to get the surface area, 
  /// we can deduce that "sur" is created somewhere in the summing process.
  /// </summary>
  /// <param name="faces"> list of faces, each face is a list of vertices</param>
  /// <returns>The surface area of this solid</returns>
  public static double CalculateTotalSurfaceArea(List<List<Vector3d>> faces, Vector3d[] b = null)
  {
    b ??= Vector3d.BasisIdentity;

    double sur = 0;
    double faceArea = 0;
    foreach (List<Vector3d> face in faces)
      faceArea += CalculateFaceArea(face, b);
    return sur + faceArea;
  }

  /// <summary>
  /// Calculates the volume of a convex polyhedron given a list of faces made of vertices
  /// https://en.wikipedia.org/wiki/Polyhedron#Volume
  /// </summary>
  /// <param name="faces">A list of faces, each face is a list of vertices</param>
  /// <returns>The volume of this solid</returns>
  public static double CalculateVolume(List<List<Vector3d>> faces, Vector3d[] b = null)
  {
    b ??= Vector3d.BasisIdentity;

    double total = 0;
    foreach (List<Vector3d> face in faces)
      total += (b * GetAverageVertex(face)).Dot(CalculateNormal(face[0], face[1], face[2], b) * CalculateFaceArea(face, b));
    return Math.Abs(total) / 3;
  }
  public static Vector3d GetAverageVertex(IEnumerable<Vector3d> vertices)
  {
    Vector3d total = new();
    foreach (Vector3d v in vertices)
      total += v;
    return total / vertices.Count();
  }

  //https://stackoverflow.com/questions/1988100/how-to-determine-ordering-of-3d-vertices
  /// <summary>
  /// Checks if a, b, c, are ordered in clockwise order
  /// </summary>
  /// <returns>True if a, b, c are in clockwise order</returns>
  public static bool IsClockwise(Vector3d a, Vector3d b, Vector3d c)
  {
    Vector3d normal = (b - a).Cross(c - a);
    return normal.Dot(a) > 0;
  }
  public static Vector3d CalculateNormal(Vector3d a, Vector3d b, Vector3d c, Vector3d[] m)
  {
    return CalculateNormal(m * a, m * b, m * c);
  }
  public static Vector3d CalculateNormal(Vector3d a, Vector3d b, Vector3d c)
  {
    Vector3d normal = (c - a).Cross(b - a).Normalized();
    if (normal.Dot(a) < 0)
      normal = -normal;
    return normal.Normalized();
  }

  /// <summary>
  /// Converts miller indices (x, y, z intercept reciprocals) to its corresponding plane-unit cell intersection's vertices. The unit cell is just the base orthogonal unit square in this case- we can transform them later.
  /// </summary>
  /// <param name="h">reciprocal of the a/x intercept</param>
  /// <param name="k">reciprocal of the b/y intercept</param>
  /// <param name="l">reciprocal of the c/z intercept</param>
  /// <param name="b">3x3 transformation matrix for the mesh</param>
  /// <returns>3 or 4 vertices of the planar intersection of the millers and unit cell</returns>
  public static List<Vector3d> MillerToVertices(Vector3d v, Vector3d[] b = null) => MillerToVertices((int)v.X, (int)v.Y, (int)v.Z, b);

  /// <summary>
  /// Converts miller indices (x, y, z intercept reciprocals) to its corresponding plane-unit cell intersection's vertices. The unit cell is just the base orthogonal unit square in this case- we can transform them later.
  /// </summary>
  /// <param name="h">reciprocal of the a/x intercept</param>
  /// <param name="k">reciprocal of the b/y intercept</param>
  /// <param name="l">reciprocal of the c/z intercept</param>
  /// <param name="b">3x3 transformation matrix for the mesh</param>
  /// <returns>3 or 4 vertices of the planar intersection of the millers and unit cell</returns>
  public static List<Vector3d> MillerToVertices(int h, int k, int l, Vector3d[] b)
  {
    b ??= Vector3d.BasisIdentity;
    List<Vector3d> vertices = new();
    Stack<Vector3d> skipped = new();
    if (h == 0)
      skipped.Push(new(1, 0, 0));
    else
      vertices.Add(new(1f / h, 0, 0));

    if (k == 0)
      skipped.Push(new(0, 1f, 0));
    else
      vertices.Add(new(0, 1f / k, 0));

    if (l == 0)
      skipped.Push(new(0, 0, 1));
    else
      vertices.Add(new(0, 0, 1f / l));

    while (skipped.Count > 0)
    {
      int count = vertices.Count;
      Vector3d axis = skipped.Pop();
      for (int i = 0; i < count; i++)
      {
        Vector3d vert = vertices[i];
        Vector3d newVert = vert + axis;
        if (vertices.Contains(newVert) == false)
          vertices.Add(newVert);
      }
    }
    return vertices;
  }

  public static bool ApproxSearch(IEnumerable<Vector3d> list, Vector3d vectorToCheck)
  {
    foreach (Vector3d v in list)
    {
      if (vectorToCheck.IsEqualApprox(v))
        return true;
    }
    return false;
  }
  public static bool ApproxSearch(IEnumerable<Vector3d> list, Vector3d vectorToCheck, out Vector3d? matched)
  {
    foreach (Vector3d v in list)
    {
      if (vectorToCheck.IsEqualApprox(v))
      {
        matched = v;
        return true;
      }
    }
    matched = null;
    return false;
  }

  public static IEnumerable<Tuple<T, T>> GetUniquePairs<T>(IList<T> list)
  {
    for (int i = 0; i < list.Count - 1; i++)//For every plane triplet, generate a vertex and validate
      for (int j = i + 1; j < list.Count; j++)
        yield return new Tuple<T, T>(list[i], list[j]);
  }

  public static IEnumerable<Tuple<T, T, T>> GetUniqueTriplets<T>(IList<T> list)
  {
    for (int i = 0; i < list.Count - 2; i++)//For every plane triplet, generate a vertex and validate
      for (int j = i + 1; j < list.Count - 1; j++)
        for (int k = j + 1; k < list.Count; k++)
          yield return new Tuple<T, T, T>(list[i], list[j], list[k]);
  }
}
