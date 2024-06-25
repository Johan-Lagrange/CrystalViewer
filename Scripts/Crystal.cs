using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
public class Crystal
{    
    public static readonly double threshold = 0.0000000001;

    public readonly List<List<Vector3d>> normalGroups;
    public readonly List<List<Planed>> planeGroups;
    public readonly List<List<Vector3d>> faces;
    public readonly List<List<List<Vector3d>>> faceGroups;
    /// <summary>
    /// Generates a mesh from a list of face normals and distances that will be duplicated according to symmetry. Generates a convex hull using halfspaces
    /// </summary>
    /// <param name="initialFaces">The array of normal directions of each symetrically unique face</param>
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
        List<Vector3d> initialFaces,
        List<double> distances,
        SymmetryOperations.PointGroup pointGroup)
    {
        if (distances.Count != initialFaces.Count)
            throw new ArgumentException("Every initial face must be given a distance!");

        Vector3d.ResetDebugLists();
        string s = "";
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < initialFaces.Count; i++)
        {
            if (initialFaces[i].IsZeroApprox() == true || distances[i] == 0)
            {
                initialFaces.RemoveAt(i);
                distances.RemoveAt(i);
                i--;//We would skip over the next one if we didn't do this.
            }
        }
        watch.Stop();
        GD.Print("First normals: " + watch.ElapsedMilliseconds);

        watch.Restart();
        normalGroups = new List<List<Vector3d>>();//List of normals for each initial face that was duplicated by the symmetry group
                                                  //Reflect every given face along the given symmetry group
        HashSet<Vector3d> vectorHashes = new() { };//For quick lookup
        for (int i = 0; i < initialFaces.Count; i++)
        {
            vectorHashes.Add(initialFaces[i]);
            normalGroups.Add(CreateCrystalSymmetry(initialFaces[i], vectorHashes, pointGroup));//Reflects every normal along the given point group's symmetry.
        }
        watch.Stop();
        GD.Print("Normal Groups: " + watch.ElapsedMilliseconds);
        // s = "NORMALS:\n";
        // foreach (List<Vector3d> group in normalGroups)
        // {
        //     s += "\n";
        //     foreach (Vector3d v in group)
        //         s += v.ToString() + ", ";
        // }
        // GD.Print(s);

        watch.Restart();
        planeGroups = GeneratePlanes(initialFaces.ToArray(), distances.ToArray(), normalGroups);//Create a plane with distance from center for every generated normal


        faceGroups = new List<List<List<Vector3d>>>();
        foreach (List<Planed> list in planeGroups) { faceGroups.Add(new List<List<Vector3d>>()); }


        List<Planed> planeFlat = planeGroups.SelectMany(plane => plane).ToList<Planed>();//Easier to iterate over

        //Associate each plane with the face group it belongs to, so we keep things sorted when adding the vertices
        Dictionary<Planed, int> planesToFaceGroups = new Dictionary<Planed, int>();
        for (int group = 0; group < planeGroups.Count; group++)
        {
            foreach (Planed plane in planeGroups[group])
            {
                planesToFaceGroups.Add(plane, group);
            }
        }
        watch.Stop();
        GD.Print("Plane Groups: " + watch.ElapsedMilliseconds);

        s = "PLANES:\n";
        foreach (List<Planed> group in planeGroups)
        {
            s += "\n";
            foreach (Planed v in group)
                s += v.originalNormal.ToString() + ", \n";
        }
        GD.Print(s);

        //TODO use watch for cumulative time of sections of methods
        watch.Restart();
        List<Vertex> vertices = GenerateVertices2(planeFlat);//Get all valid vertices on the crystal
        watch.Stop();
        GD.Print("Vertices: " + watch.ElapsedMilliseconds);
        s = "VERTICES:\n";
        foreach (Vertex v in vertices)
        {
            s += "planes: [";
            foreach (Planed p in v.planes)
            {
                s += p.originalNormal.ToStringSingleLetter();
            }
            s += "] point: ";
            s += v.point.ToStringWithCharComponents();
            s += "\n";
        }
        GD.Print(s);

        watch.Restart();
        //Create a dictionary that will take a plane and give an unordered list of each edge that makes up the plane's face
        Dictionary<Planed, Dictionary<Vertex, AdjacentEdges>> unorderedEdges = GenerateEdges(vertices);
        watch.Stop();
        GD.Print("UnorderedEdges: " + watch.ElapsedMilliseconds);
        s = "FACES\n";
        foreach (Planed p in unorderedEdges.Keys)
        {
            s += p.originalNormal.ToStringSingleLetter() + ": ";
            foreach (Vertex v in unorderedEdges[p].Keys)
                s += v.point.ToStringWithCharComponents() + ", ";
            s += "\n";
        }
        GD.Print(s);

        watch.Restart();

        s = "Faces:\n";
        faces = new();//The final mesh that we are building. Contains the vertices of each face in order.
        foreach (Planed plane in unorderedEdges.Keys)
        {
            List<Vector3d> face = CreateFaceFromEdges(unorderedEdges[plane]);
            if (face.Count >= 3)
            {
                foreach (Vector3d v in face)
                    s += v.ToStringWithCharComponents() + ", ";
                s += "\n";
                faces.Add(face);
                int index = planesToFaceGroups[plane];
                faceGroups[index].Add(face);
            }
        }
        GD.Print(s);
        GD.Print("Faces: " + watch.ElapsedMilliseconds);

    }
    /// <summary>
    /// Takes an initial vector and applies all point group operations on it, 
    /// returning a list of every vector therein, including the original. (Identity is an operation after all)
    /// </summary>
    /// <param name="v">The initial vector to do symmetry stuff on</param>
    /// <param name="group">The crystal's point group. Used to get a list of operations</param>
    /// <returns>A list of vectors, including the original, that are made from the symmetry operations</returns>
    public static List<Vector3d> CreateCrystalSymmetry(Vector3d v, HashSet<Vector3d> vectorHashes, SymmetryOperations.PointGroup group)
    {
        List<Vector3d> vectorList = new() { v };//To keep vectors in order
        foreach (Func<Vector3d, Vector3d> Operation in SymmetryOperations.PointGroupPositions[(int)group])
        {
            ApplyOperation(vectorList, vectorHashes, Operation);
        }

        /*Hexagonal crystals don't render correctly unless we do this.
        I think it has to do with -Z being forward in Godot. 
        It's strange that no other shape groups have this isssue though,
        May be because this is the only one that includes X and Y in one component.
        And yes I have tried messing around with the function and swapping variables, didn't work.*/
        if ((int)group >= 21 && (int)group <= 35)//Point groups that use the hexagonal method.
        {
            for (int j = 0; j < vectorList.Count; j++)
            {
                Vector3d v2 = vectorList[j];
                vectorList[j] = new Vector3d(v2.x, -v2.y, v2.z);
            }
        }
        return vectorList;
    }
    /// <summary>
    /// Applies a symmetry operation to every vector in a list, and adds every result to the list.
    /// </summary>
    /// <param name="vectorList">List of vectors to operate upon and expand</param>
    /// <param name="symmetryOperation">The symmetry operation to do</param>
    public static void ApplyOperation(List<Vector3d> vectorList, HashSet<Vector3d> vectorHashes, Func<Vector3d, Vector3d> symmetryOperation)
    {
        int count = vectorList.Count;//Get the count before we add to the list, so we can add to the list we're reading without causing an infinite loop by looking at stuff we just added
        for (int i = 0; i < count; i++)
        {
            Vector3d v = symmetryOperation(vectorList[i]);

            if (vectorHashes.Contains(v))
                continue;

            vectorList.Add(v);
            vectorHashes.Add(v);
        }
    }

    /// <summary>
    /// Generates the crystal's planes from faces, distances, and normals
    /// </summary>
    /// <param name="initialFaces"></param>
    /// <param name="distances"></param>
    /// <param name="normals"></param>
    /// <returns></returns>
    private static List<List<Planed>> GeneratePlanes(Vector3d[] initialFaces, double[] distances, List<List<Vector3d>> normals)
    {
        List<List<Planed>> planeGroups = new();
        for (int givenFace = 0; givenFace < initialFaces.Length; givenFace++)
        {
            List<Vector3d> normalsList = normals[givenFace];
            List<Planed> newPlanes = new List<Planed>();
            planeGroups.Add(newPlanes);

            foreach (Vector3d normal in normalsList)//Verify and add planes for each type of normal
            {
                Planed planeToAdd = new Planed(normal, distances[givenFace]);//Create new plane to add
                List<Planed> planesToRemove = new();
                bool valid = true;
                foreach (List<Planed> prevPlanes in planeGroups)//Go through each group of planes we added
                {
                    foreach (Planed p in prevPlanes)//Go through every plane previously added
                    {
                        if (planeToAdd.Normal.Dot(p.Normal) > 1 - threshold)//Skip adding duplicate plane (plane normals are already normalized)
                        {
                            if (planeToAdd.D <= p.D)
                            {                                

                                valid = false;//This face is behind another face so we can skip it
                                goto invalid;//Think of this as a break; we can't break 2 loops though
                            }
                            else
                            {
                                GD.Print("Removed old plane" + p.originalNormal + " of larger " + planeToAdd.originalNormal);
                                planesToRemove.Add(p);//Another face was behind this so we remove the old one
                            }
                        }
                    }
                    foreach (Planed pp in planesToRemove)
                        prevPlanes.Remove(pp);//Remove faces that are behind(but same direction) the one we just added
                }
                if (valid)
                    newPlanes.Add(planeToAdd);
                invalid:;
            }
        }
        return planeGroups;
    }

    /// <summary>
    /// Creates a vertex from every triplet of planes IF 1) the point is on or in the crystal and 2) it is not a duplicate
    /// </summary>
    /// <param name="planes">The planes to find the intersection points of.</param>
    /// <returns>A list of vertices with position and list of planes that intersect at that point. Can be more than three planes.</returns>
    private static List<Vertex> GenerateVertices(List<Planed> planes)
    {
        Dictionary<Vector3d, Vertex> vertexPoints = new();
        LinkedList<Planed>[] planeOctants = new LinkedList<Planed>[8];
        LinkedList<Vector3d>[] pointsOctants = new LinkedList<Vector3d>[8];//Pre-"Sorted" quick fuzzy searching
        
        for (int i = 0; i < 8; i++)
        {
            planeOctants[i] = new LinkedList<Planed>();
            pointsOctants[i] = new LinkedList<Vector3d>();
        }
        foreach (Planed p in planes)
        {
            int index = VectorToOctant(p.originalNormal);
            planeOctants[index].AddLast(p);
        }

        for (int i = 0; i < planes.Count - 2; i++)//By staggering the loops like this, we avoid checking the same combination twice
        {
            for (int j = i + 1; j < planes.Count - 1; j++)
            {
                for (int k = j + 1; k < planes.Count; k++)
                {
                    if (planes[i].Normal.Dot(-planes[j].Normal) > 1 - threshold//Two planes are opposite one another
                    || planes[i].Normal.Dot(-planes[k].Normal) > 1 - threshold
                    || planes[j].Normal.Dot(-planes[k].Normal) > 1 - threshold)
                        continue;

                    Vector3d? intersection = planes[i].Intersect3(planes[j], planes[k]);

                    if (intersection == null)
                    {
                        //GD.PrintErr("Null intersection between: " + planes[i].originalNormal.ToString() + " " + planes[j].originalNormal.ToString() + " " + planes[k].originalNormal.ToString());
                        continue;}

                    Vertex vertexToVerify = new((Vector3d)intersection, planes[i], planes[j], planes[k]);

                    if (VerifyVertex(planeOctants, pointsOctants, vertexPoints, vertexToVerify))
                    {
                        pointsOctants[VectorToOctant(vertexToVerify.point)].AddLast(vertexToVerify.point);
                        vertexPoints.Add(vertexToVerify.point, vertexToVerify);
                    }
                }
            }
        }

        return vertexPoints.Values.ToList<Vertex>();
    }


    /// <summary>
    /// Merges two vertices if they are in the same spot but has different faces
    /// Also checks to make sure the vertex is within or on the crystal. 
    /// Sometimes planes can generate outside of the crystal so we make sure that doesn't happen here.
    /// </summary>
    /// <param name="planesInOctants">The list of planes that make up the crystal faces, sorted by signs into octants(3d quadrants)</param>
    /// <param name="pointsInOctants">All existing verticies to check for duplicates against, sorted by signs into octants(3d quadrants)</param>
    /// <param name="veretxPoints">A dictionary corresponding each point in 'pointsInOctants' to a vertex, for when we need to merge</param>
    /// <param name="vertexToVerify">The vertex we want to validate</param>
    /// <returns>True if this vertex is unique, false if the vertex is either: outside of the crystal, or a duplicate that we merged</returns>
    private static bool VerifyVertex(IEnumerable<Planed>[] planesInOctants, IEnumerable<Vector3d>[] pointsInOctants, Dictionary<Vector3d, Vertex> vertexPoints, Vertex vertexToVerify)
    {
        if (vertexToVerify.point.IsZeroApprox())
            return false;

        for (int i = 0; i < 8; i++)//Look through planes that are in the same direction
        {
            if (ShouldCheckOctant(i, vertexToVerify.point) == false)
                continue;
            if (IsInPlanes(planesInOctants[i], vertexToVerify.point) == false)
                return false;
        }
        int currentOctant = VectorToOctant(vertexToVerify.point);
        Vector3d? match = null;
        foreach(Vector3d v in pointsInOctants[currentOctant])
        {
            if(v.IsEqualApprox(vertexToVerify.point))
            {
                match = v;
                break;
            }
        }
        if (match != null)
        {
            // GD.Print("Merging point " + v.point + " with " + vertexToVerify.point);
            vertexPoints[(Vector3d)match].MergeVertices(vertexToVerify);//Two different plane triplets made the same point. That means the point has more than 3 faces. 
            return false;//So we add the extra faces to one point and discard the other.
        }
        return true;
    }
    /// <summary>
    /// Creates a vertex from every triplet of planes IF 1) the point is on or in the crystal and 2) it is not a duplicate
    /// </summary>
    /// <param name="planes">The planes to find the intersection points of.</param>
    /// <returns>A list of vertices with position and list of planes that intersect at that point. Can be more than three planes.</returns>
    private static List<Vertex> GenerateVertices2(List<Planed> planes)
    {
        Dictionary<Vector3d, Vertex> vertexPoints = new();
        List<Vector3d> vertices = new();

        for (int i = 0; i < planes.Count - 2; i++)//By staggering the loops like this, we avoid checking the same combination twice
        {
            for (int j = i + 1; j < planes.Count - 1; j++)
            {
                for (int k = j + 1; k < planes.Count; k++)
                {
                    if (planes[i].Normal.Dot(-planes[j].Normal) > 1 - threshold//Two planes are opposite one another
                    || planes[i].Normal.Dot(-planes[k].Normal) > 1 - threshold
                    || planes[j].Normal.Dot(-planes[k].Normal) > 1 - threshold)
                        continue;

                    Vector3d? intersection = planes[i].Intersect3(planes[j], planes[k]);

                    if (intersection == null)
                    {
                        //GD.PrintErr("Null intersection between: " + planes[i].originalNormal.ToString() + " " + planes[j].originalNormal.ToString() + " " + planes[k].originalNormal.ToString());
                        continue;}

                    Vertex vertexToVerify = new((Vector3d)intersection, planes[i], planes[j], planes[k]);

                    if (VerifyVertex2(planes, vertices, vertexPoints, vertexToVerify))
                    {
                        vertices.Add(vertexToVerify.point);
                        vertexPoints.Add(vertexToVerify.point, vertexToVerify);
                    }
                }
            }
        }

        return vertexPoints.Values.ToList<Vertex>();
    }


    /// <summary>
    /// Merges two vertices if they are in the same spot but has different faces
    /// Also checks to make sure the vertex is within or on the crystal. 
    /// Sometimes planes can generate outside of the crystal so we make sure that doesn't happen here.
    /// </summary>
    /// <param name="planesInOctants">The list of planes that make up the crystal faces, sorted by signs into octants(3d quadrants)</param>
    /// <param name="pointsInOctants">All existing verticies to check for duplicates against, sorted by signs into octants(3d quadrants)</param>
    /// <param name="veretxPoints">A dictionary corresponding each point in 'pointsInOctants' to a vertex, for when we need to merge</param>
    /// <param name="vertexToVerify">The vertex we want to validate</param>
    /// <returns>True if this vertex is unique, false if the vertex is either: outside of the crystal, or a duplicate that we merged</returns>
    private static bool VerifyVertex2(IEnumerable<Planed> planes, IEnumerable<Vector3d> points, Dictionary<Vector3d, Vertex> vertexPoints, Vertex vertexToVerify)
    {
        if (vertexToVerify.point.IsZeroApprox())
            return false;

            if (IsInPlanes(planes, vertexToVerify.point) == false)
                return false;
        Vector3d? match = null;
        foreach(Vector3d v in points)
        {
            if(v.IsEqualApprox(vertexToVerify.point))
            {
                match = v;
                break;
            }
        }
        if (match != null)
        {
            // GD.Print("Merging point " + v.point + " with " + vertexToVerify.point);
            vertexPoints[(Vector3d)match].MergeVertices(vertexToVerify);//Two different plane triplets made the same point. That means the point has more than 3 faces. 
            return false;//So we add the extra faces to one point and discard the other.
        }
        return true;
    }
    /// <summary>
    /// Takes a list of vertices and returns a dictionary that contains each edge (not in order) that lies on a plane.
    /// </summary>
    /// <param name="vertices">The vertices to check for conections. A connection exists if two vertices share two planes</param>
    /// <returns>A dictionary that contains all surrounding edges (not in order) of each plane</returns>
    private static Dictionary<Planed, Dictionary<Vertex, AdjacentEdges>> GenerateEdges(List<Vertex> vertices)
    {
        Dictionary<Planed, Dictionary<Vertex, AdjacentEdges>> faces = new();

        for (int i = 0; i < vertices.Count - 1; i++)
        {
            for (int j = i + 1; j < vertices.Count; j++)//For every pair of vertices
            {
                // if (vertices[i].point == vertices[j].point)//Don't create an edge between a point and itself
                //     continue;

                List<Planed> sharedFaces = vertices[i].SharedFaces(vertices[j]);//Check for shared faces
                // if (sharedFaces.Count > 2)
                //     // GD.PrintErr("Vertices share more than two faces!");
                if (sharedFaces.Count >= 2)//If they share TWO faces, that means they have an edge together
                {
                    if (sharedFaces.Count > 2)
                    {
                        string s = "TOO MANY SHARED FACES: ";
                        foreach (Planed p in sharedFaces)
                        {
                            s += p.originalNormal.ToStringSingleLetter();
                        }
                        s += " between 1(";
                        foreach(Planed p in vertices[i].planes)
                            s += p.originalNormal.ToStringSingleLetter();
                        s += ") and 2(";
                        foreach(Planed p in vertices[j].planes)
                            s += p.originalNormal.ToStringSingleLetter();
                            s += ")";
                        GD.PrintErr(s);
                    }
                    Planed p1 = sharedFaces[0];//First plane that we found a new edge on
                    Planed p2 = sharedFaces[1];//Second plane ^
                    Vertex v1 = vertices[i];//First vertex that makes up the edge
                    Vertex v2 = vertices[j];//Second vertex ^

                    if (faces.ContainsKey(p1) == false) faces.Add(p1, new());//Create new dictionary if this is our first time looking at these planes
                    if (faces.ContainsKey(p2) == false) faces.Add(p2, new());
                    if (faces[p1].ContainsKey(v1) == false) faces[p1].Add(v1, new());//Create new edge if it doesnt exist yet
                    if (faces[p1].ContainsKey(v2) == false) faces[p1].Add(v2, new());//... In both directions
                    if (faces[p2].ContainsKey(v1) == false) faces[p2].Add(v1, new());//... On both planes.
                    if (faces[p2].ContainsKey(v2) == false) faces[p2].Add(v2, new());
                    try
                    {
                        // GD.Print(v1.point + "-" + v2.point);
                        // GD.Print("Adding vertex on plane " + p1.Normal + " for vertex " + v1.point + " - " + v2.point);
                        faces[p1][v1].AddVertex(v2);//Create link from v1 -> v2 on plane 1
                        // GD.Print("Adding vertex on plane " + p1.Normal + " for vertex " + v2.point + " - " + v1.point);
                        faces[p1][v2].AddVertex(v1);//Create link from v2 -> v1 on plane 1
                        // GD.Print("Adding vertex on plane " + p2.Normal + " for vertex " + v1.point + " - " + v2.point);
                        faces[p2][v1].AddVertex(v2);//Create link from v1 -> v2 on plane 2
                        // GD.Print("Adding vertex on plane " + p2.Normal + " for vertex " + v2.point + " - " + v1.point);
                        faces[p2][v2].AddVertex(v1);//Create link from v2 -> v1 on plane 2
                    }
                    catch (Exception e)
                    {
                        // GD.Print("Plane 1: " + p1.Normal);
                        // GD.Print("Plane 2: " + p2.Normal);
                        // GD.Print("Vertex 1: " + v1.point);
                        // GD.Print("Vertex 2: " + v2.point);
                         GD.PrintErr(e.GetType() + e.Message);
                        foreach (Planed p in faces.Keys)
                        {
                            // GD.Print("Plane: " + p.Normal);
                            foreach (Vertex v in faces[p].Keys)
                            {
                                string a = "", b = "";
                                if (faces[p][v].a != null)
                                    a = faces[p][v].a.point.ToString();
                                if (faces[p][v].b != null)
                                    b = faces[p][v].b.point.ToString();
                                // GD.PrintErr("Vertex: " + v.point + ", A: " + a + ", B: " + b);
                            }
                        }
                    }
                }
            }
        }

        // List<Planed> facesToRemove = new();
        // foreach (Planed p in faces.Keys)
        // {
        //     if (faces[p].Count < 3)//Generated an invalid polygon
        //         facesToRemove.Add(p);//So we remove it later
        // }
        // foreach (Planed p in facesToRemove)
        //     faces.Remove(p);
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

                string s3 = "[";
            foreach (Planed p in start.planes)
            {
                s3 += p.originalNormal.ToStringSingleLetter();
            }
            s3 += "]";
            s3 += start.point.ToStringWithCharComponents();
                GD.Print("First edge: " + s3);
        edges.Add(start.point);
        while (here.point != start.point)
        {
                string s2 = "[";
            foreach (Planed p in here.planes)
            {
                s2 += p.originalNormal.ToStringSingleLetter();
            }
            s2 += "]";
            s2 += here.point.ToStringWithCharComponents();
                GD.Print("Current edge: " + s2);
            edges.Add(here.point);//Add current point to list
            tmpNext = face[here].GetNext(previous);//Get next point without backtracking
            previous = here;
            here = tmpNext;

            if (here == null)
            {
                string s = "[";
            foreach (Planed p in previous.planes)
            {
                s += p.originalNormal.ToStringSingleLetter();
            }
            s += "]";
            s += previous.point.ToStringWithCharComponents();
                GD.PrintErr("Hit broken edge: " + s);
                return edges;
            }
            if (count++ > 20)
            {
                GD.PrintErr("Infinite loop in edge creation");
                return edges;//TODO throw error here
            }
        }

        //Some methods of rendering require clockwise orientation
        if (IsClockwise(edges[0], edges[1], edges[2]) != clockwise)
            edges.Reverse();

        return edges;
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
    /// Takes a vector and returns its 'octant index' (which side of the x, y, and z axis it is on) depending on sign of components. 0 is considered positive.
    /// </summary>
    /// <param name="v">The vector to find which octant index it is in</param>
    /// <returns>Encoded octant. bit is positive if the octant is on the positive side of the axis.
    /// Encoded like this: ZYX
    /// negative Z, positive Y, positive X would be (binary)011 = (int)3
    /// Components that are zero are assumed positive when placed in octants, 
    /// but not when checking against octants- That would include unnecessary octants in calculations.</returns>
    public static int VectorToOctant(Vector3d v)
    {
        return ((v.x >= 0) ? 1 : 0)
        + ((v.y >= 0) ? 2 : 0)
        + ((v.z >= 0) ? 4 : 0);
    }

    /// <summary>
    /// Optimization. Returns true for any octant that can hold a positive dot product with the given vector.
    /// </summary>
    /// <param name="n">Encoded octant. bit is positive if the octant is on the positive side of the axis.
    /// Encoded like this: ZYX
    /// negative Z, positive Y, positive X would be (binary)011 = (int)3
    /// Components that are zero are assumed positive when placed in octants, 
    /// but not when checking against octants- That would include unnecessary octants in calculations.</param>
    /// <param name="v">Vector to check against octants. Zero components are disregarded</param>
    /// <returns>True if the given octant can hold a vector with a positive dot product with v</returns>
    private static bool ShouldCheckOctant(int n, Vector3d v)
    {
        //https://stackoverflow.com/questions/2431732/checking-if-a-bit-is-set-or-not#2431759
        bool GetBit(int x, int pos) => (x & (1 << pos)) != 0;
        if (v.x != 0)
        {
            if (GetBit(n, 0) == (v.x >= 0))//Is on this half of the x axis
                return true;
        }
        if (v.y != 0)
        {
            if (GetBit(n, 1) == (v.y >= 0))//Is on this half of the y axis
                return true;
        }
        if (v.z != 0)
        {
            if (GetBit(n, 2) == (v.z >= 0))//Is on this half of the z axis
                return true;
        }
        return false;
    }

    /// <summary>
    /// Calculates the area between 3 vertices
    /// https://math.stackexchange.com/questions/128991/how-to-calculate-the-area-of-a-3d-triangle
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <returns></returns>
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
    public static Vector3d GetAverageVertex(List<Vector3d> vertices)
    {
        Vector3d total = new();
        foreach (Vector3d v in vertices)
            total += v;
        return total / vertices.Count;
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

    #endregion math

    #region exports
    /// <summary>
    /// Saves the mesh as an STL
    /// </summary>
    /// <param name="fileName">Name of the file to save to</param>
    /// <param name="mesh">Crystal mesh, not transformed by any crystal parameters</param>
    /// <param name="m">3x3 transformation matrix/Crystal parameters to transform the mesh by</param>
    public void ExportSTL(string fileName, Vector3d[] m = null)
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

        m ??= Vector3d.BasisIdentity;

        if (fileName.EndsWith(".stl") == false)
            fileName += ".stl";

        using System.IO.StreamWriter writer = new System.IO.StreamWriter(fileName);

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
            transformedFaces[i] = m * transformedFaces[i];


        writer.WriteLine("solid " + fileName.Substring(0, fileName.Length - 4));//Trim out .stl tag
        for (int i = 0; i < transformedFaces.Count - 1; i += 3)
        {
            Vector3d v1 = transformedFaces[i];
            Vector3d v2 = transformedFaces[i + 1];
            Vector3d v3 = transformedFaces[i + 2];//these 3 are one tri in the mesh

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
    /// <param name="m">3x3 transformation matrix/Crystal parameters to transform the mesh by</param>
    public void ExportOBJ(string fileName, Vector3d[] m = null)
    {
        //https://en.wikipedia.org/wiki/Wavefront_.obj_file
        /*
        o (name)
        v x y z <- referenced as 1
        v x y z <- referenced as 2
        n x y z <- referenced as 1
        f v1 v2 v3 v4... 
        f v1/uv1/n1 v2/uv2/n2 v3/uv3/n3...
        f v1//n1 v2//n2 v3//n3... <- we use normals but not UVs so we leave the UV slot empty
        */

        m ??= Vector3d.BasisIdentity;

        if (fileName.EndsWith(".obj") == false)
            fileName += ".obj";

        using System.IO.StreamWriter writer = new System.IO.StreamWriter(fileName);

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
                    Vector3d tv = m * v;
                    newFace.Add(tv);//Apply matrix transform while adding
                    if (vertexDict.ContainsKey(tv) == false)
                        vertexDict.Add(tv, vertexIndex++);
                }
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

        writer.WriteLine("o " + fileName.Substring(0, fileName.Length - 4));//Object name & trim out .obj tag

        //Create list of vertices and normals in file
        foreach (Vector3d v in vertexDict.Keys)
            writer.WriteLine($"v {v.X} {v.Y} {v.Z} #v{vertexDict[v]}");
        foreach (Vector3d n in normalDict.Keys)
            writer.WriteLine($"vn {n.X} {n.Y} {n.Z} #v{normalDict[n]}");

        writer.WriteLine("s " + 0);//No smooth shading
        int groupNum = 1;
        foreach (List<List<Vector3d>> faceGroup in transformedFaceGroups)
        {
            writer.WriteLine("g " + groupNum++);

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
                writer.WriteLine(str);
            }
        }
    }
    #endregion exports

    #region classes
    /// <summary>
    /// The vertex of a crystal. Stores position and adjacent faces for determining edges.
    /// </summary>
    private class Vertex
    {
        public Vector3d point;
        public List<Planed> planes = new();

        public Vertex(Vector3d p, Planed plane1, Planed plane2, Planed plane3)
        {
            point = p;
            planes.Add(plane1);
            planes.Add(plane2);
            planes.Add(plane3);
        }
        /// <summary>
        /// Returns a list of planes that both vertices share
        /// </summary>
        /// <param name="other">Vertex to check for shared vertices</param>
        /// <returns>the list of shared vertices</returns>
        public List<Planed> SharedFaces(Vertex other)
        {
            IEnumerable<Planed> query = from plane in planes
                                        join otherPlane in other.planes on plane equals otherPlane
                                        select plane;
            return query.ToList();
            // List<Planed> sharedFaces = new();
            // for (int i = 0; i < planes.Count; i++)
            // {
            //     for (int j = 0; j < other.planes.Count; j++)
            //     {
            //         if (planes[i] == other.planes[j] && !sharedFaces.Contains(planes[i]))
            //             sharedFaces.Add(planes[i]);
            //     }
            // }
            // return sharedFaces;
        }
        /// <summary>
        /// Merges the two lists of shared vertices into one.
        /// </summary>
        /// <param name="other">The duplicate vertex formed by a slightly different set of faces</param>
        public void MergeVertices(Vertex other)
        {
            foreach (Planed p in other.planes)
            {
                // GD.Print(p.Normal);
                if (!planes.Contains(p))
                    planes.Add(p);
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
            if (a != null && a.point.IsEqualApprox(v.point))//if we don't check null and short circuit then we'd get a null reference exception
                throw new Exception("Was given a point twice!");
            if (b != null && b.point.IsEqualApprox(v.point))
                throw new Exception("Was given a point twice!");
            if (b != null)
                throw new Exception("Was given more than two vertices, this should not happen: " + a.point + " " + b.point + " " + v.point);

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