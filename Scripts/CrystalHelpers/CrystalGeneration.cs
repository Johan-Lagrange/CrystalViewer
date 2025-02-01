using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


public static class CrystalGeneration
{
  public static readonly double threshold = 0.0000000001;
  /// <summary>
  /// Creates a list of vectors according to the symmetry group, for every vector given
  /// </summary>
  /// <param name="initialFaces">List of initial vectors to apply symmetry to</param>
  /// <param name="pointGroup">Named after a group of operations that creates a symmetry group</param>
  /// <returns>A list of lists, which each contain an initial face and all the versions created by the symmetry group</returns>
  public static List<List<Vector3d>> GenerateSymmetryGroups(IEnumerable<Vector3d> initialFaces, SymmetryOperations.PointGroup pointGroup)
  {
    List<List<Vector3d>> normalGroups = new List<List<Vector3d>>();//List of normals for each initial face that was duplicated by the symmetry group
                                                                   //Reflect every given face along the given symmetry group
    HashSet<Vector3d> vectorHashes = new() { };//For quick "does this exist" lookup. We don't want to add duplicate faces.
    foreach (Vector3d v in initialFaces)
    {
      List<Vector3d> normalGroup = GenerateSymmetryList(v, vectorHashes, pointGroup);//Reflects every normal along the given point group's symmetry.
      if (normalGroup.Count == 0)
      {
        throw new Exception("Empty normal group!");
      }
      normalGroups.Add(normalGroup);
    }
    return normalGroups;
  }

  /// <summary>
  /// Takes an initial vector and applies all point group operations on it, 
  /// returning a list of every vector therein, including the original. (Identity is an operation after all)
  /// </summary>
  /// <param name="v">The initial vector to do symmetry stuff on</param>
  /// <param name="vectorHashes"></param>
  /// <param name="group">The crystal's point group. Used to get a list of operations</param>
  /// <returns>A list of vectors, including the original, that are made from the symmetry operations</returns>
  public static List<Vector3d> GenerateSymmetryList(Vector3d v, HashSet<Vector3d> vectorHashes, SymmetryOperations.PointGroup group)
  {
    LinkedList<Vector3d> vectorList = new();//To keep vectors in order
    vectorList.AddFirst(v);
    vectorHashes.Add(v);
    foreach (Func<Vector3d, Vector3d> Operation in SymmetryOperations.PointGroupOperations[(int)group])
    {
      ApplyOperation(vectorList, vectorHashes, Operation);
    }

    // /*Hexagonal crystals don't render correctly unless we do this.
    // I think it has to do with -Z being forward in Godot. 
    // It's strange that no other shape groups have this issue though,
    // May be because this is the only one that includes X and Y in one component.
    // Messing around with the function and swapping variables didn't fix it.*/
    if ((int)group >= 21 && (int)group <= 35)//Point groups that use the hexagonal method.
    {
      LinkedListNode<Vector3d> current = vectorList.First;
      while (current != null)
      {
        Vector3d v2 = current.Value;
        current.Value = new Vector3d(v2.x, -v2.y, v2.z);
        current = current.Next;
      }
    }
    return vectorList.ToList<Vector3d>();
  }

  /// <summary>
  /// Applies a symmetry operation to every vector in a list, and adds every result to the list.
  /// </summary>
  /// <param name="vectorList">List of vectors to operate upon and expand</param>
  /// <param name="vectorHashes">Hash set for quick lookup of duplicates</param>
  /// <param name="symmetryOperation">The symmetry operation to do</param>
  /// <param name="skipApproxVerify">Skip "fuzzy" searches of duplicate vectors if true</param>
  public static void ApplyOperation(LinkedList<Vector3d> vectorList, HashSet<Vector3d> vectorHashes, Func<Vector3d, Vector3d> symmetryOperation, bool skipApproxVerify = false)
  {
    int count = vectorList.Count;//Get the count before we add to the list, so we can add to the list we're reading without causing an infinite loop by looking at stuff we just added
    LinkedListNode<Vector3d> current = vectorList.First;
    for (int i = 0; i < count; i++)
    {
      Vector3d v = symmetryOperation(current.Value);

      if (vectorHashes.Contains(v) == false && (skipApproxVerify || CrystalMath.ApproxSearch(vectorHashes, v) == false))
      {
        vectorList.AddLast(v);
        vectorHashes.Add(v);
      }
      current = current.Next;
    }
  }

  /// <summary>
  /// Takes a list of normals groups and distances for the groups, and generates plane groups from that.
  /// </summary>
  /// <param name="normals">A list of normal groups</param>
  /// <param name="distances">Distances for each normal group</param>
  /// <returns>A list of plane groups</returns>
  public static List<List<Planed>> GeneratePlanes(List<List<Vector3d>> normals, IList<double> distances)
  {
    LinkedList<List<Planed>> planeGroups = new();//We do this since we modify the list as we are traversing it.

    for (int faceGroup = 0; faceGroup < normals.Count; faceGroup++)
    {
      if (normals[faceGroup].Count == 0)//Skip checking any empty groups
        continue;

      Planed planeToAdd = new Planed(normals[faceGroup][0], distances[faceGroup]);
      //Since every plane in a facegroup follows the same properties due to symmetry, we only verify the first one

      LinkedListNode<List<Planed>> currentGroup = planeGroups.First;
      bool valid = true;

      while (currentGroup != null)
      {
        Planed? overlap = FindOverlappingPlane(currentGroup.Value, planeToAdd);

        if (overlap == null)
        {
          //No overlap so we know this group is fine.
          currentGroup = currentGroup.Next;
          continue;
        }

        //Found duplicate plane: Remove (or skip) the one one the outside
        if (planeToAdd.D < ((Planed)overlap).D)
        {
          LinkedListNode<List<Planed>> prevGroup = currentGroup;//Making sure we don't lose our place.. (we can't do current.previous as current.next may be null and that has no previous)
          currentGroup = currentGroup.Next;
          planeGroups.Remove(prevGroup);//Group contains a redundant face, so entire group can be removed due to symmetry
        }
        else
        {
          //This face is behind another face so we can skip adding it
          valid = false;
          break;
        }
      }

      //Found to be a valid face
      if (valid == true)
        planeGroups.AddLast(normals[faceGroup].Select(v => new Planed(v, distances[faceGroup])).ToList<Planed>());
    }
    return planeGroups.ToList<List<Planed>>();
  }

  /// <summary>
  /// Searches for an overlap between planes in list and given plane. Returns found plane if found, null if not found.
  /// </summary>
  /// <param name="planes">List of planes to check for overlap.</param>
  /// <param name="planeToCheck">Plane to check for overlaps of.</param>
  /// <returns>Matching plane from original list if overlap is found, null otherwise.</returns>
  public static Planed? FindOverlappingPlane(List<Planed> planes, Planed planeToCheck)
  {
    foreach (Planed p in planes)
    {
      //plane normals are already normalized to length 1
      if (planeToCheck.Normal.Dot(p.Normal) > (1 - threshold))
        return p;
    }
    return null;
  }

  /// <summary>
  /// Creates a vertex from every triplet of planes IF 1) the point is on or in the crystal and 2) it is not a duplicate
  /// </summary>
  /// <param name="planeGroups">The planes to find the intersection points of.</param>
  /// <param name="allowParallel">If true, uses less polished but 2x faster 
  /// parallel threads to generate vertices. 
  /// Only does so for large amounts of planes where it would actually help 
  /// and is less likely to collide.</param>
  /// <returns>A list of vertices with position and list of planes that intersect at that point. Can be more than three planes.</returns>
  public static List<Vertex> GenerateVertices(List<List<Planed>> planeGroups, bool allowParallel = true)
  {
    List<Planed> planes = planeGroups.SelectMany(plane => plane).ToList<Planed>();//Easier to iterate over

    ConcurrentDictionary<Vector3d, Vertex> faceVertices = new(); //Vertices on each face
    void GenerateVertex(Tuple<Planed, Planed, Planed> triplet)
    {
      Vector3d? intersection = Planed.Intersect3(triplet.Item1, triplet.Item2, triplet.Item3);

      if (intersection == null)
      {
        //Either two planes are parallel or all 3 planes make a tube 
        //and thus don't meet at 1 point
        //..oor they all meet at zero
        return;
      }

      //Sometimes two threads create the same vertex and can't merge them. Unsure how to fix that.
      Vertex vertexToVerify = new((Vector3d)intersection, triplet.Item1, triplet.Item2, triplet.Item3);

      bool valid = VerifyVertex(planes, faceVertices, vertexToVerify);

      if (valid)
      {
        //We know the vertex is valid at this point
        faceVertices.TryAdd(vertexToVerify.point, vertexToVerify);
      }
    }

    if (allowParallel && planes.Count > 40)//Only use parallel on cases where it would really help
    {
      Parallel.ForEach(CrystalMath.GetUniqueTriplets(planes), triplet => GenerateVertex(triplet));
    }
    else
    {
      foreach (Tuple<Planed, Planed, Planed> triplet in CrystalMath.GetUniqueTriplets(planes))
      {
        GenerateVertex(triplet);
      }
    }

    return faceVertices.Values.ToList();
  }

  /// <summary>
  /// Returns true if vertex is within planes and has no duplicates.
  /// Will merge duplicates if one is found.
  /// </summary>
  /// <param name="planes">Planes to check if vertex is within</param>
  /// <param name="faceVertices">Vertices to merge with if duplicate is found</param>
  /// <param name="vertexToVerify">Vertex we want to verify, or merge if duplicate.</param>
  /// <returns>True if the vertex is on the plane and has no duplicates.</returns>
  public static bool VerifyVertex(List<Planed> planes, ConcurrentDictionary<Vector3d, Vertex> faceVertices, Vertex vertexToVerify)
  {
    if (vertexToVerify.point.IsZeroApprox())
      return false;
    //Merge checks
    if (faceVertices.ContainsKey(vertexToVerify.point))
    {
      faceVertices[vertexToVerify.point].MergeVertices(vertexToVerify);//Two different plane triplets made the same point. That means the point has more than 3 faces. 
      return false;//So we add the extra faces to the pre existing point and skip adding the new one.
    }
    else if (CrystalMath.ApproxSearch(faceVertices.Keys, vertexToVerify.point, out Vector3d? match))//Check if we match any currently generated vertices.
    {
      //Since we use v here and floating points can be slightly off, we can't use Enumerable.Contains here.
      faceVertices[(Vector3d)match].MergeVertices(vertexToVerify);//Merge with matched point
      return false;
    }

    //New + Valid checks
    if (CrystalMath.IsInPlanes(planes, vertexToVerify.point))
      return true;

    return false;
  }
  /// <summary>
  /// Scans for planes with invalid numbers of vertices, and removes them if found. Returns number of removed planes
  /// </summary>
  /// <param name="vertices">Vertices with planes to validate.</param>
  /// <returns>The number of removed planes.</returns>
  public static int RemoveInvalidPlanes(IEnumerable<Vertex> vertices)
  {
    int removed = 0;
    Dictionary<Planed, LinkedList<Vertex>> planeVertices = new();
    foreach (Vertex v in vertices)
    {
      foreach (Planed vp in v.Planes)
      {
        if (planeVertices.ContainsKey(vp) == false)
          planeVertices.Add(vp, new());
        planeVertices[vp].AddLast(v);
      }
    }
    foreach (Planed p in planeVertices.Keys)
    {
      if (planeVertices[p].Count < 3)
      {
        foreach (Vertex pv in planeVertices[p])
        {
          removed++;
          pv.Planes.Remove(p);
        }
      }
    }
    return removed;
  }

  /// <summary>
  /// Takes a list of vertices and returns a dictionary that contains each edge (not in order) that lies on a plane.
  /// </summary>
  /// <param name="vertices">The vertices to check for conections. A connection exists if two vertices share two planes</param>
  /// <returns>A dictionary that contains all surrounding edges (not in order) of each plane</returns>
  public static Dictionary<Planed, Dictionary<Vertex, AdjacentEdges>> GenerateEdges(IList<Vertex> vertices)
  {
    Dictionary<Planed, Dictionary<Vertex, AdjacentEdges>> faces = new();

    foreach (Tuple<Vertex, Vertex> pair in CrystalMath.GetUniquePairs<Vertex>(vertices))
    {
      if (pair.Item1.point.IsEqualApprox(pair.Item2.point))//Don't create an edge between a point and itself
        pair.Item1.MergeVertices(pair.Item2);
      //throw new Exception("Two separate points have the same position");

      List<Planed> sharedFaces = pair.Item1.SharedFaces(pair.Item2);//Check for shared faces

      if (sharedFaces.Count >= 2)//If they share TWO faces, that means they have an edge together
      {
        if (sharedFaces.Count > 2)
        {
          //GD.PrintErr("Too many shared faces");
          continue;
          // string s = "TOO MANY SHARED FACES: ";
          // foreach (Planed p in sharedFaces)
          // {
          //   s += p.originalNormal.ToStringSingleLetter();
          // }
          // s += " between 1(";
          // foreach (Planed p in pair.Item1.Planes)
          //   s += p.originalNormal.ToStringSingleLetter();
          // s += ") and 2(";
          // foreach (Planed p in pair.Item2.Planes)
          //   s += p.originalNormal.ToStringSingleLetter();
          // s += ")";
          // GD.PrintErr(s);
        }
        Planed p1 = sharedFaces[0];//First plane that we found a new edge on
        Planed p2 = sharedFaces[1];//Second plane ^
        Vertex v1 = pair.Item1;//First vertex that makes up the edge
        Vertex v2 = pair.Item2;//Second vertex ^

        if (faces.ContainsKey(p1) == false) faces.Add(p1, new());//Create new dictionary if this is our first time looking at these planes
        if (faces.ContainsKey(p2) == false) faces.Add(p2, new());
        if (faces[p1].ContainsKey(v1) == false) faces[p1].Add(v1, new());//Create new edge if it doesnt exist yet
        if (faces[p1].ContainsKey(v2) == false) faces[p1].Add(v2, new());//... In both directions
        if (faces[p2].ContainsKey(v1) == false) faces[p2].Add(v1, new());//... On both planes.
        if (faces[p2].ContainsKey(v2) == false) faces[p2].Add(v2, new());

        faces[p1][v1].AddVertex(v2);//Create link from v1 -> v2 on plane 1
        faces[p1][v2].AddVertex(v1);//Create link from v2 -> v1 on plane 1
        faces[p2][v1].AddVertex(v2);//Create link from v1 -> v2 on plane 2
        faces[p2][v2].AddVertex(v1);//Create link from v2 -> v1 on plane 2
      }
    }

    LinkedList<Planed> illegalPlanes = new();
    foreach (Planed p in faces.Keys)
    {
      if (faces[p].Count < 3)//Generated an invalid polygon
      {
        illegalPlanes.AddLast(p);
        //  throw new Exception("Invalid planes still present");
      }
    }
    foreach (Planed ip in illegalPlanes)
      faces.Remove(ip);

    return faces;
  }

  /// <summary>
  /// Builds a coplanar loop of vertices that encircle a face from a list of vertexes and edges of a single given face
  /// </summary>
  /// <param name="face">The list of vertices and adjacents that make up the face</param>
  /// <returns>The list of vertices in order around a face</returns>
  public static List<Vector3d> CreateFaceFromEdges(Dictionary<Vertex, AdjacentEdges> face, bool clockwise = false)
  {
    List<Vector3d> edges = new();//list of vertices around the perimeter of the face
    Vertex start = face.First().Key;//Arbitrary first vertex
    Vertex here = face[start].a;//Arbitrary vertex adjacent to first vertex
    Vertex tmpNext;
    Vertex previous = start;//Keep track of previous because vertices store adjacent ones out of order. We dont want to backtrack.
    if (here == null)
      return edges;//Was given empty face

    int count = 0;//To avoid infinite loops. Hopefully never needed. (Is still needed.)

    edges.Add(start.point);
    while (here.point != start.point)
    {
      edges.Add(here.point);//Add current point to list
      tmpNext = face[here].GetNext(previous);//Get next point without backtracking
      previous = here;
      here = tmpNext;

      if (here == null)
      {
        return edges;
      }
      if (count++ > 100)
      {
        //Infinite loop failsafe
        return edges;
      }
    }
    if (edges.Count < 3)
      return new List<Vector3d>();//Not valid face so we dont return anything
    //Some methods of rendering require clockwise orientation
    if (CrystalMath.IsClockwise(edges[0], edges[1], edges[2]) != clockwise)
      edges.Reverse();

    return edges;
  }

}
