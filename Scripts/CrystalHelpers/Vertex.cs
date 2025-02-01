using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The vertex of a crystal. Stores position and adjacent faces for determining edges.
/// </summary>
public class Vertex
{
  public Vector3d point;
  public ConcurrentDictionary<Planed, bool> planeHashes = new();
  public List<Planed> Planes { get => planeHashes.Keys.ToList<Planed>(); }

  public Vertex(Vector3d p, Planed plane1, Planed plane2, Planed plane3)
  {
    point = p;
    planeHashes.TryAdd(plane1, true);
    planeHashes.TryAdd(plane2, true);
    planeHashes.TryAdd(plane3, true);
  }
  /// <summary>
  /// Returns a list of planes that both vertices share
  /// </summary>
  /// <param name="other">Vertex to check for shared planes</param>
  /// <returns>the list of shared planes</returns>
  public List<Planed> SharedFaces(Vertex other)
  {
    LinkedList<Planed> shared = new();
    foreach (Planed p in Planes)
    {
      if (other.planeHashes.ContainsKey(p))
        shared.AddLast(p);
    }
    return shared.ToList<Planed>();
  }
  /// <summary>
  /// Merges the two lists of shared vertices into one.
  /// </summary>
  /// <param name="other">The duplicate vertex formed by a slightly different set of faces</param>
  public void MergeVertices(Vertex other)
  {
    foreach (Planed p in other.Planes)
    {
      if (!planeHashes.ContainsKey(p))
      {
        Planes.Add(p);
        planeHashes.TryAdd(p, true);
      }
    }
  }
}