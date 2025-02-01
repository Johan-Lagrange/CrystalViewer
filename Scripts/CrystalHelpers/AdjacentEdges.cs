
/// <summary>
/// Represents adjacent edges of a vertex ON A GIVEN PLANE.
/// Said plane is stored as a key on a dictionary.
/// </summary>
public class AdjacentEdges//We don't know what order edges will be added so we use this confusing two-way edge system
{
  public Vertex a, b;
  public void AddVertex(Vertex v)
  {
    // if (a != null && a.point.IsEqualApprox(v.point))//if we don't check null and short circuit then we'd get a null reference exception
    //   throw new Exception("Was given a point twice!" + v.point);
    // if (b != null && b.point.IsEqualApprox(v.point))
    //   throw new Exception("Was given b point twice!" + v.point);
    // if (b != null)
    //   throw new Exception("Was given more than two vertices, this should not happen: " + a.point + " " + b.point + " " + v.point);

    if (a == null)
      a = v;
    else
      b = v;
  }
  /// <summary>
  /// Takes the previous vertex in the chain and returns the next vertex. We use this to avoid unnecessary cycles.
  /// </summary>
  /// <param name="previous">The last vertex that was visited</param>
  /// <returns>The next vertex to visit</returns>
  public Vertex GetNext(Vertex previous)
  {
    if (a.point != previous.point)
      return a;
    else
      return b;
  }
}