using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// This class generates crystals given face vectors and symmetry
/// It also has some extra methods for calculating things like surface area
/// </summary>
public class Crystal
{
  public static readonly double threshold = 0.0000000001;

  public readonly SymmetryOperations.PointGroup pointGroup;
  public readonly List<Vector3d> initialNormals;
  public readonly List<double> initialDistances;

  /// <summary>
  /// Every plane normal that makes up the crystal, grouped based on which original normal generated them.
  /// </summary>
  public readonly List<List<Vector3d>> normalGroups;

  /// <summary>
  /// Every plane group that makes up the crystal, grouped based on which original plane generated them.
  /// </summary>
  public readonly List<List<Planed>> planeGroups;

  /// <summary>
  /// A list of every face stored as a clockwise list of Vector3d
  /// </summary>

  public readonly List<List<Vector3d>> faces;
  /// <summary>
  /// A list of every group of faces, groups are created by mirroring/rotating an initial plane, and are separated based on which plane they were created from.
  /// Faces are stored as a clockwise list of Vector3d
  /// </summary>
  public readonly List<List<List<Vector3d>>> faceGroups;

  private Vector3d[] unitCell;
  /// <summary>
  /// 3x3 Linear transformation that determines how crystals are displayed.
  /// Some crystals take more space to repeat in one direction
  /// compared to another, or grow at an angle compared to another direction,
  /// so transforming them allows us to use the same miller indices
  ///  to describe crystals that are stretched and skewed.
  /// </summary>
  public Vector3d[] UnitCell
  {
    get { return unitCell; }
    set
    {
      if (value.Length != 3) throw new ArgumentException("Unit cell parameters must be 3x3");
      else unitCell = value;
    }
  }

  /// <summary>
  /// Generates a mesh from a list of face normals and distances that will be duplicated according to symmetry. Generates a convex hull using halfspaces
  /// </summary>
  /// <param name="initialNormals">The array of normal directions of each symetrically unique face</param>
  /// <param name="distances">The array of distances per face from crystal center</param>
  /// <param name="pointGroup">The type of symmetry this crystal has. Dictates which symmetry operations (mirror, rotate) are used</param>
  /// <returns></returns>
  /// <remarks>
  /// The way this algorithm generates a crystal is:
  /// 1. Acquire a list of symmetry operations (reflections, rotations, retroinversions) from the given point group
  /// 2. For every given face vector, apply every combination of symmetry operations in the list to it.
  ///     As an example, to make a 2D square with starting vector (1, 0), operation 1 would be mirror over y,
  ///     after which the list is (1, 0) and (-1, 0), and operation 2 would rotate 90 degrees, which we apply to both items
  ///     for a result of (1, 0), (-1, 0), (0, -1), (0, 1).
  /// 2a. if a plane is in front of another (parallel and has a greater or equal distance) it is redundant and can be removed.
  /// 3. Generate a list of vertices by intersecting every trio of planes.
  ///     Vertices keep track of which planes they are a part of. This is imporant.
  /// 3a. Remove any vertex that is in front of a plane, since that lies outside of the crystal.
  /// 3b. Combine any two vertices that are in the same spot, otherwise vertices would only ever keep track of 3 faces
  /// 4. Create an unordered linked list of every edge on the face by comparing every pair of vertices creating an edge if they share 2 faces
  ///     The AdjacentEdges type is there to keep track of which vertices are adjacent to which edges. We use this to traverse a face's edges
  /// 4a. A vertex on a face should only have neighboring vertices on that face. If there are more, we know the face is malformed and can disregard it
  ///     This tends to happen with a face of zero area (Think of an octagon where the corners shrink to a square)
  /// 5. Create a clockwise-ordered array of vertices that make up the face by traversing the linked list, making sure we don't backtrack.
  /// 5a. Check the first couple vertices to see if they are clockwise, and if not, reverse the array to make it clockwise.
  /// 6. Generate normals, tangents, and tris for each trio of vertices, and use Godot's mesh builder to create a mesh from there.
  ///  </remarks>
  public Crystal(
      List<Vector3d> initialNormals,
      List<double> distances,
      SymmetryOperations.PointGroup pointGroup,
      Vector3d[] unitCell = null)
  {
    if (distances.Count != initialNormals.Count)
      throw new ArgumentException("Every initial face must be given a distance!");

    this.pointGroup = pointGroup;
    this.initialNormals = initialNormals;
    this.initialDistances = distances;
    unitCell ??= Vector3d.BasisIdentity;
    this.UnitCell = unitCell;

    Vector3d.ResetDebugLists();
    for (int i = 0; i < initialNormals.Count; i++)
    {
      if (initialNormals[i].IsZeroApprox() == true || distances[i] == 0)
      {
        initialNormals.RemoveAt(i);
        distances.RemoveAt(i);
        i--;//We would skip over the next one if we didn't do this.
      }
    }
    normalGroups = GenerateSymmetryGroups(initialNormals, pointGroup);

    planeGroups = GeneratePlanes(normalGroups, distances);//Create a plane with distance from center for every generated normal

    //Associate each plane with the face group it belongs to, so we keep things sorted when adding the vertices
    Dictionary<Planed, int> planesToFaceGroups = new Dictionary<Planed, int>();
    for (int group = 0; group < planeGroups.Count; group++)
    {
      foreach (Planed plane in planeGroups[group])
      {
        planesToFaceGroups.Add(plane, group);
      }
    }

    List<Vertex> vertices = GenerateVertices(planeGroups);

    RemoveInvalidPlanes(vertices);

    //Create a dictionary that will take a plane and give an unordered list of each edge that makes up the plane's face
    Dictionary<Planed, Dictionary<Vertex, AdjacentEdges>> unorderedEdges = GenerateEdges(vertices);

    faceGroups = new List<List<List<Vector3d>>>();
    foreach (List<Planed> list in planeGroups)
      faceGroups.Add(new List<List<Vector3d>>());

    faces = new();//The final mesh that we are building. Contains the vertices of each face in order.
    foreach (Planed plane in unorderedEdges.Keys)
    {
      List<Vector3d> face = CreateFaceFromEdges(unorderedEdges[plane]);
      if (face.Count >= 3)
      {
        faces.Add(face);
        int index = planesToFaceGroups[plane];
        faceGroups[index].Add(face);
      }
    }

    faceGroups.RemoveAll(group => group.Count == 0);
  }

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

      if (vectorHashes.Contains(v) == false && (skipApproxVerify || ApproxSearch(vectorHashes, v) == false))
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
  private static List<List<Planed>> GeneratePlanes(List<List<Vector3d>> normals, IList<double> distances)
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
  private static Planed? FindOverlappingPlane(List<Planed> planes, Planed planeToCheck)
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
  /// <param name="pointGroup">The point group to use for quick vertex generation of previously verified points.</param>
  /// <returns>A list of vertices with position and list of planes that intersect at that point. Can be more than three planes.</returns>
  private static List<Vertex> GenerateVertices(List<List<Planed>> planeGroups, bool allowParallel = true)
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
      Parallel.ForEach(GetUniqueTriplets(planes), triplet => GenerateVertex(triplet));
    }
    else
    {
      foreach (Tuple<Planed, Planed, Planed> triplet in GetUniqueTriplets(planes))
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
  private static bool VerifyVertex(List<Planed> planes, ConcurrentDictionary<Vector3d, Vertex> faceVertices, Vertex vertexToVerify)
  {
    if (vertexToVerify.point.IsZeroApprox())
      return false;
    //Merge checks
    if (faceVertices.ContainsKey(vertexToVerify.point))
    {
      faceVertices[vertexToVerify.point].MergeVertices(vertexToVerify);//Two different plane triplets made the same point. That means the point has more than 3 faces. 
      return false;//So we add the extra faces to the pre existing point and skip adding the new one.
    }
    else if (ApproxSearch(faceVertices.Keys, vertexToVerify.point, out Vector3d? match))//Check if we match any currently generated vertices.
    {
      //Since we use v here and floating points can be slightly off, we can't use Enumerable.Contains here.
      faceVertices[(Vector3d)match].MergeVertices(vertexToVerify);//Merge with matched point
      return false;
    }

    //New + Valid checks
    if (IsInPlanes(planes, vertexToVerify.point))
      return true;

    return false;
  }
  /// <summary>
  /// Scans for planes with invalid numbers of vertices, and removes them if found. Returns number of removed planes
  /// </summary>
  /// <param name="vertices">Vertices with planes to validate.</param>
  /// <returns>The number of removed planes.</returns>
  private static int RemoveInvalidPlanes(IEnumerable<Vertex> vertices)
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
  private static Dictionary<Planed, Dictionary<Vertex, AdjacentEdges>> GenerateEdges(IList<Vertex> vertices)
  {
    Dictionary<Planed, Dictionary<Vertex, AdjacentEdges>> faces = new();

    foreach (Tuple<Vertex, Vertex> pair in GetUniquePairs<Vertex>(vertices))
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
  private static List<Vector3d> CreateFaceFromEdges(Dictionary<Vertex, AdjacentEdges> face, bool clockwise = false)
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
    if (IsClockwise(edges[0], edges[1], edges[2]) != clockwise)
      edges.Reverse();

    return edges;
  }

  /// <summary>
  /// Returns the final facegroup that is created from the given initial index if it exists, or -1 if not found. Since the final facegroup ignores empty and invalid groups, the indices may not match up.
  /// </summary>
  /// <param name="initialIndex">The index of the original face parameters</param>
  /// <returns>Index of matching face group, or -1 if none are found.</returns>
  public int FindSurfaceMadeByIndex(int initialIndex)
  {

    if (initialIndex < 0 || initialIndex >= initialNormals.Count)//Out of bounds
      return -1;
    if (faceGroups.Count == initialNormals.Count)
      return initialIndex;//No surfaces were removed so we don't need to do any extra checks

    Planed plane = new Planed(initialNormals[initialIndex], initialDistances[initialIndex]);

    int skipped = 0;
    for (int i = 0; i < faceGroups.Count; i++)
    {
      if (faceGroups[i].Count == 0)
      {
        skipped++;//Group generated no faces, but since it counts as a face group, we still need to keep track that we skipped it.
        continue;//Since it has no faces, it creates no "surface" on the mesh, and thus desyncs with the initial group.
      }
      if (i > initialIndex)
      {
        //Face groups are generated in-order, if we are past the original index, then we've already checked any faces that should have been generated by it and found no matches.
        return -1;
      }

      foreach (List<Vector3d> face in faceGroups[i])//For every face in this group
      {
        if (face.Count < 3)
        {
          continue;
        }

        bool valid = true;
        foreach (Vector3d v in face)//check if it matches. Skip if any vertex does not match the plane.
        {
          if (Math.Abs(plane.DistanceTo(v)) > threshold)
          {
            valid = false;
            break;
          }

        }
        if (valid)
        {
          return i - skipped;//If the face matches the plane generated initially, then we know this group matches. Return index of the group accounting for groups that dont create a mesh surface.
        }
      }
    }
    return -1;//No matching group was found.
  }


  #region math
  /// <summary>
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

  private static bool ApproxSearch(IEnumerable<Vector3d> list, Vector3d vectorToCheck)
  {
    foreach (Vector3d v in list)
    {
      if (vectorToCheck.IsEqualApprox(v))
        return true;
    }
    return false;
  }
  private static bool ApproxSearch(IEnumerable<Vector3d> list, Vector3d vectorToCheck, out Vector3d? matched)
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
  #endregion math

  #region exports

  public void SaveCrystal(string path)
  {
    if (path.EndsWith(".json"))
      path = path.Substr(0, path.Length - 4);//We add it later and need the default name
    if (path.EndsWith(".json"))
      path = path.Substr(0, path.Length - 4);//We add it later and need the default name
    if (path.EndsWith("json"))
      path = path.Substr(0, path.Length - 3);//The dialog adds an extra stl for some reason

    CrystalSerialize exportInfo = new();
    exportInfo.Name = path.Split("/").Last();
    exportInfo.SpaceGroup = SymmetryOperations.names[(int)this.pointGroup];
    exportInfo.Normals = initialNormals.ToArray();
    exportInfo.Distances = initialDistances.ToArray();
    string jsonString = JsonSerializer.Serialize(exportInfo);
    using System.IO.StreamWriter writer = new System.IO.StreamWriter(path + ".json");
    writer.Write(jsonString);
  }
  public void SaveCrystal(string path, CrystalMaterial[] materials, Vector3d axisAngles, Vector3d axisLengths)
  {
    GD.Print(path);
    if (path.EndsWith(".json"))
    {
      GD.Print("HELLO");
      path = path.Substr(0, path.Length - 5);//We add it later and need the default name}
    }
    if (path.EndsWith(".json"))
      path = path.Substr(0, path.Length - 5);//We add it later and need the default name
    if (path.EndsWith("json"))
      path = path.Substr(0, path.Length - 4);//The dialog adds an extra stl for some reason
    GD.Print(path);

    CrystalSerialize exportInfo = new();
    exportInfo.Name = path.Split("/").Last();
    GD.Print(exportInfo.Name);
    exportInfo.SpaceGroup = SymmetryOperations.groupNames[(int)this.pointGroup];
    exportInfo.Materials = materials;
    exportInfo.Normals = initialNormals.ToArray();
    exportInfo.Distances = initialDistances.ToArray();
    exportInfo.AxisAngles = axisAngles;
    exportInfo.AxisLengths = axisLengths;
    string jsonString = JsonSerializer.Serialize(exportInfo);
    GD.Print(jsonString);
    using System.IO.StreamWriter writer = new System.IO.StreamWriter(path + ".json");
    writer.Write(jsonString);
  }


  public static Crystal LoadCrystal(string path)
  {
    using System.IO.StreamReader reader = new System.IO.StreamReader(path);
    string jsonString = reader.ReadLine();
    CrystalSerialize importInfo = JsonSerializer.Deserialize<CrystalSerialize>(jsonString);
    Crystal crystal = new Crystal(importInfo.Normals.ToList(), importInfo.Distances.ToList(), SymmetryOperations.nameToGroup[importInfo.SpaceGroup]);
    return crystal;
  }

  public static Crystal LoadCrystal(string path, out CrystalSerialize data)
  {
    using System.IO.StreamReader reader = new System.IO.StreamReader(path);
    string jsonString = reader.ReadLine();
    data = JsonSerializer.Deserialize<CrystalSerialize>(jsonString);
    GD.Print(JsonSerializer.Serialize(data));

    Crystal crystal = new Crystal(data.Normals.ToList(), data.Distances.ToList(), SymmetryOperations.nameToGroup[data.SpaceGroup]);
    return crystal;
  }
  /// <summary>
  /// Saves the mesh as an STL
  /// </summary>
  /// <param name="fileName">Name of the file to save to</param>
  /// <param name="mesh">Crystal mesh, not transformed by any crystal parameters</param>
  public void ExportSTL(string fileName)
  {
    /*
    solid [name]
    facet normal nx ny nz
        outer loop
            vertex v1x v1y v1z
            vertex v2x v2y v2z
            vertex v3x v3y v3z //CAN ONLY do tris
        endloop
    endfacet
    endsolid [name]*/

    UnitCell ??= Vector3d.BasisIdentity;

    if (fileName.EndsWith(".stl"))
      fileName = fileName.Substr(0, fileName.Length - 4);//We add it later and 
    if (fileName.EndsWith(".stl"))
      fileName = fileName.Substr(0, fileName.Length - 4);//We add it later and need the default name
    if (fileName.EndsWith("stl"))
      fileName = fileName.Substr(0, fileName.Length - 3);//The dialog adds an extra stl for some reason

    using System.IO.StreamWriter writer = new System.IO.StreamWriter(fileName + ".stl");

    List<Vector3d> transformedFaces = new List<Vector3d>();//Every 3 vertices is a new tri

    foreach (List<Vector3d> face in this.faces)//Vertices are currently in a "loop" around the face
    {
      for (int i = 1; i < face.Count - 1; i++)
      {
        transformedFaces.Add(face[0]);//Create a "fan shaped" sequence of tris that cover the 
        transformedFaces.Add(face[i]);//entire loop
        transformedFaces.Add(face[i + 1]);
      }
    }
    for (int i = 0; i < transformedFaces.Count; i++)
      transformedFaces[i] = UnitCell * transformedFaces[i];

    fileName = fileName.Split("/").Last();//Trim directories from filename for internal name
    writer.WriteLine("solid " + fileName);
    for (int i = 0; i < transformedFaces.Count - 1; i += 3)
    {
      Vector3d v1 = transformedFaces[i];
      Vector3d v2 = transformedFaces[i + 2];//Swapped because STL uses other direction
      Vector3d v3 = transformedFaces[i + 1];//these 3 are one tri in the mesh

      Vector3d normal = CalculateNormal(v1, v2, v3);

      writer.WriteLine("facet normal " + normal.X + " " + normal.Y + " " + normal.Z);
      writer.WriteLine("\touter loop");
      writer.WriteLine("\t\t vertex " + v1.X + " " + v1.Y + " " + v1.Z);
      writer.WriteLine("\t\t vertex " + v2.X + " " + v2.Y + " " + v2.Z);
      writer.WriteLine("\t\t vertex " + v3.X + " " + v3.Y + " " + v3.Z);
      writer.WriteLine("\tendloop");
      writer.WriteLine("endfacet");
    }
    writer.WriteLine("endsolid " + fileName);
  }

  /// <summary>
  /// Saves the mesh as an OBJ
  /// </summary>
  /// <param name="fileName">Name of the file to save to</param>
  /// <param name="mesh">Crystal mesh, not transformed by any crystal parameters</param>
  public void ExportOBJ(string fileName, CrystalMaterial[] materials = null)
  {

    UnitCell ??= Vector3d.BasisIdentity;//can't set it to basis in params

    if (fileName.EndsWith(".obj"))
      fileName = fileName.Substr(0, fileName.Length - 4);
    if (fileName.EndsWith("obj"))
      fileName = fileName.Substr(0, fileName.Length - 3);//The dialog adds an extra stl(no dot) for some reason

    fileName = fileName.Split("/").Last();// trim out directories and file tag

    //Export materials
    if (materials != null)
    {
      using System.IO.StreamWriter matWriter = new System.IO.StreamWriter(fileName + ".mtl");

      for (int i = 0; i < materials.Length; i++)
      {
        CrystalMaterial mat = materials[i];
        matWriter.WriteLine("newmtl " + FindSurfaceMadeByIndex(i));
        matWriter.WriteLine($"Kd {mat.R} {mat.G} {mat.B}");
        matWriter.WriteLine($"Ks 1 1 1");//Fully white shines. Gives glassy look, but inaccurate for metals
        matWriter.WriteLine($"Ns {Math.Pow(1000, mat.Roughness * 2) / 1000f}");//specular exponent. between 0.001 and 1000
        matWriter.WriteLine($"d {mat.A}");//Transparency
        matWriter.WriteLine($"Tr {1 - mat.A}");//Also transparency
        matWriter.WriteLine($"Tf {mat.R} {mat.G} {mat.B}");//Transmitted light color
        matWriter.WriteLine($"Ni {Math.Pow(1000, mat.Refraction + 1) / 1000f}");//refraction. between 0.001 and 1000

      }
    }

    //Transform and index each vertex.
    // We index each normal after we add the vertices
    int vertexIndex = 1, normalIndex = 1;
    Dictionary<Vector3d, int> vertexDict = new Dictionary<Vector3d, int>();
    Dictionary<Vector3d, int> normalDict = new Dictionary<Vector3d, int>();
    List<List<List<Vector3d>>> transformedFaceGroups = new();

    foreach (List<List<Vector3d>> faceGroup in faceGroups)
    {
      List<List<Vector3d>> transformedFaces = new();
      foreach (List<Vector3d> face in faceGroup)
      {
        List<Vector3d> newFace = new List<Vector3d>();
        foreach (Vector3d v in face)
        {
          Vector3d tv = UnitCell * v;
          newFace.Add(tv);//Apply matrix transform while adding
          if (vertexDict.ContainsKey(tv) == false)
            vertexDict.Add(tv, vertexIndex++);
        }
        newFace.Reverse();//obj files use other direction for normals
        transformedFaces.Add(newFace);
      }
      transformedFaceGroups.Add(transformedFaces);
    }

    //Creates an alias for each normal. We go by 3 since every 3 vertices is a surface triangle
    foreach (List<List<Vector3d>> faceGroup in transformedFaceGroups)
    {
      foreach (List<Vector3d> face in faceGroup)
      {
        Vector3d normal = CalculateNormal(face[0], face[1], face[2]);

        if (normalDict.ContainsKey(normal) == false)
          normalDict.Add(normal, normalIndex++);
      }
    }

    //https://en.wikipedia.org/wiki/Wavefront_.obj_file
    /*
    mtllib (material file name).mtl
    o (name)
    v x y z <- referenced as 1 (vertices)
    v x y z <- referenced as 2
    n x y z <- referenced as 1 (normals)
    s 0 <- no smooth shading on these faces
    f v1 v2 v3 v4... 
    f v1/uv1/n1 v2/uv2/n2 v3/uv3/n3... <- v, uv, and n are not in the file, just here for readability
    f v1//n1 v2//n2 v3//n3... <- we use normals but not UVs so we leave the UV slot empty
    */
    using System.IO.StreamWriter objWriter = new System.IO.StreamWriter(fileName + ".obj");

    if (materials != null)
      objWriter.WriteLine($"mtllib {fileName}.mtl");

    objWriter.WriteLine("o " + fileName);//Object name
    //Create list of vertices and normals in file
    foreach (Vector3d v in vertexDict.Keys)
      objWriter.WriteLine($"v {v.X} {v.Y} {v.Z} #v{vertexDict[v]}");
    foreach (Vector3d n in normalDict.Keys)
      objWriter.WriteLine($"vn {n.X} {n.Y} {n.Z} #v{normalDict[n]}");

    objWriter.WriteLine("s " + 0);//No smooth shading
    int groupNum = 0;
    foreach (List<List<Vector3d>> faceGroup in transformedFaceGroups)
    {
      if (materials != null)
        objWriter.WriteLine($"usemtl {groupNum}");
      objWriter.WriteLine($"g {groupNum}");

      foreach (List<Vector3d> face in faceGroup)
      {
        Vector3d normal = CalculateNormal(face[0], face[1], face[2]);

        int n = normalDict[normal];

        //A .obj face is structured like this:
        //f vertex1/texturecoords1/normal1 vertex2...
        //We skip texturecoords by doing vertex1//normal1
        string str = "f";
        for (int i = 0; i < face.Count; i++)//Add vertex and normal for each vertex on face
          str += $" {vertexDict[face[i]]}//{n}";//The leading space is intentional
        objWriter.WriteLine(str);
      }
      groupNum++;
    }
  }
  #endregion exports

  #region classes

  public class CrystalSerialize
  {
    public string Name { get; set; }
    public string SpaceGroup { get; set; }
    public Vector3d[] Normals { get; set; }
    public double[] Distances { get; set; }
    public CrystalMaterial[] Materials { get; set; }
    public Vector3d AxisAngles { get; set; }
    public Vector3d AxisLengths { get; set; }
  }
  public struct CrystalMaterial
  {
    //All values between 0 and 1 except refraction which is -1 to 1
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; }
    public float Roughness { get; set; }
    public float Refraction { get; set; }
  }
  /// <summary>
  /// The vertex of a crystal. Stores position and adjacent faces for determining edges.
  /// </summary>
  private class Vertex
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

  /// <summary>
  /// Represents adjacent edges of a vertex ON A GIVEN PLANE.
  /// Said plane is stored as a key on a dictionary.
  /// </summary>
  private class AdjacentEdges//We don't know what order edges will be added so we use this confusing two-way edge system
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
  #endregion classes

  #region debug
  // private static void DebugPrintNormals(List<List<Vector3d>> normalGroups, string s = "NORMALS:\n")
  // {
  //   foreach (List<Vector3d> group in normalGroups)
  //   {
  //     foreach (Vector3d v in group)
  //       s += v.ToString() + ", ";
  //     s += "\n";
  //   }
  //   GD.Print(s);
  // }
  // private static void DebugPrintFaces(Dictionary<Planed, Dictionary<Vertex, AdjacentEdges>> unorderedEdges, string s = "FACES\n")
  // {
  //   foreach (Planed p in unorderedEdges.Keys)
  //   {
  //     s += p.originalNormal.ToStringSingleLetter() + ": ";
  //     foreach (Vertex v in unorderedEdges[p].Keys)
  //       s += v.point.ToStringWithCharComponents() + ", ";
  //     s += "\n";
  //   }
  //   GD.Print(s);
  // }
  // private static void DebugPrintPlanes(List<List<Planed>> planeGroups, string s = "PLANES:\n")
  // {
  //   foreach (List<Planed> group in planeGroups)
  //   {
  //     s += "\n";
  //     foreach (Planed v in group)
  //       s += v.originalNormal.ToString() + ", \n";
  //   }
  //   GD.Print(s);
  // }
  // private static void DebugPrintVertices(List<Vertex> vertices, string s = "VERTICES:\n")
  // {
  //   foreach (Vertex v in vertices)
  //   {
  //     s += "planes: [";
  //     foreach (Planed p in v.Planes)
  //     {
  //       s += p.originalNormal.ToStringSingleLetter();
  //     }
  //     s += "] point: ";
  //     s += v.point.ToStringWithCharComponents();
  //     s += "\n";
  //   }
  //   GD.Print(s);
  // }
  // private static void DebugPrintVertexAdjacentEdges(Dictionary<Plane, Dictionary<Vertex, AdjacentEdges>> faces)
  // {
  //     foreach (Planed p in faces.Keys)
  //     {
  //         // GD.Print("Plane: " + p.Normal);
  //         foreach (Vertex v in faces[p].Keys)
  //         {
  //             string a = "", b = "";
  //             if (faces[p][v].a != null)
  //                 a = faces[p][v].a.point.ToString();
  //             if (faces[p][v].b != null)
  //                 b = faces[p][v].b.point.ToString();
  //             // GD.Print("Vertex: " + v.point + ", A: " + a + ", B: " + b);
  //         }
  //     }
  // }

  // private static void DebugPrintPlanesWithVertices(List<Planed> planes, List<Vertex> vertices)
  // {
  //     string s = "Planes: ";
  //     foreach (Planed p in planes)
  //         s += p.Normal + ", ";
  //     // GD.Print(s);
  //     foreach (Vertex v in vertices)
  //     {
  //         s = "Vertices: ";
  //         s += v.point + ":";
  //         foreach (Plane p in v.planes)
  //         {
  //             s += p.Normal + "; ";
  //         }
  //         // GD.Print(s);
  //     }
  // }
  #endregion debug
}