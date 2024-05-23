using System;
using System.Collections.Generic;
using System.Linq;

public class Crystal
{
    public readonly List<Vector3d>[] normals;
    public readonly List<Planed> planes;
    public readonly List<List<Vector3d>> faces;
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
        Vector3d[] initialFaces,
        float[] distances,
        SymmetryOperations.PointGroup pointGroup)
    {
        if (distances.Length != initialFaces.Length)
            throw new ArgumentException("Every initial face must be given a distance!");

        List<Vector3d> validFaces = new List<Vector3d>();
        foreach (Vector3d v in initialFaces)
        {
            if (v.IsZeroApprox() == false)
                validFaces.Add(v);
        }

        //TODO instead of JUST doing normals, also keep track of which original face they are a part of with a 2d array.
        normals = new List<Vector3d>[validFaces.Count];//List of normals for each initial face that was duplicated by the symmetry group

        //Reflect every given face along the given symmetry group
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = CreateCrystalSymmetry(validFaces[i].Normalized(), pointGroup);//Reflects every normal along the given point group's symmetry.
        }

        planes = GeneratePlanes(validFaces.ToArray(), distances, normals);//Create a plane with distance from center for every generated normal

        List<Vertex> vertices = GenerateVertices(planes);//Get all valid vertices on the crystal

        Dictionary<Planed, Dictionary<Vertex, AdjacentEdges>> edges = GenerateEdges(vertices);//Create a dictionary that will take a plane and give an unordered list of each edge that makes up the plane's face

        faces = new();//The final mesh that we are building. Contains the vertices of each face in order.
        foreach (Dictionary<Vertex, AdjacentEdges> unorderedFace in edges.Values)
        {
            List<Vector3d> face = CreateFaceFromEdges(unorderedFace);
            if (face.Count >= 3)
                faces.Add(face);
        }

    }
    /// <summary>
    /// Takes an initial vector and applies all point group operations on it, 
    /// returning a list of every vector therein, including the original. (Identity is an operation after all)
    /// </summary>
    /// <param name="v">The initial vector to do symmetry stuff on</param>
    /// <param name="group">The crystal's point group. Used to get a list of operations</param>
    /// <returns>A list of vectors, including the original, that are made from the symmetry operations</returns>
    public static List<Vector3d> CreateCrystalSymmetry(Vector3d v, SymmetryOperations.PointGroup group)
    {
        List<Vector3d> vectorList = new() { v };
        foreach (Func<Vector3d, Vector3d> Operation in SymmetryOperations.PointGroupPositions[(int)group])
        {
            ApplyOperation(vectorList, Operation);
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
    public static void ApplyOperation(List<Vector3d> vectorList, Func<Vector3d, Vector3d> symmetryOperation)
    {
        int count = vectorList.Count;//Get the count before we add to the list so we aren't in an infinite loop
        for (int i = 0; i < count; i++)
        {
            Vector3d v = symmetryOperation(vectorList[i]);
            v = FormatVector3d(v);

            bool valid = true;
            foreach (Vector3d vl in vectorList)
            {
                if (v.IsEqualApprox(vl))
                {
                    valid = false;
                    break;
                }
            }
            if (valid)
                vectorList.Add(v);
        }
    }

    /// <summary>
    /// Generates the crystal's planes from faces, distances, and normals
    /// </summary>
    /// <param name="initialFaces"></param>
    /// <param name="distances"></param>
    /// <param name="normals"></param>
    /// <returns></returns>
    private static List<Planed> GeneratePlanes(Vector3d[] initialFaces, float[] distances, List<Vector3d>[] normals)
    {
        List<Planed> planes = new();
        for (int givenFace = 0; givenFace < initialFaces.Length; givenFace++)
        {
            List<Vector3d> normalsList = normals[givenFace];

            foreach (Vector3d normal in normalsList)//Verify and add planes for each type of normal
            {
                Planed planeToAdd = new Planed(normal, distances[givenFace]);
                List<Planed> planesToRemove = new();
                bool valid = true;
                foreach (Planed p in planes)
                {
                    if (planeToAdd.Normal.Normalized().Dot(p.Normal.Normalized()) > .9999f)//Skip adding duplicate plane
                    {
                        if (planeToAdd.D >= p.D)
                        {
                            valid = false;//This face is behind another face so we can skip it
                            break;
                        }
                        else
                        {
                            planesToRemove.Add(p);//Another face was behind this so we remove this
                        }
                    }
                }
                foreach (Planed pp in planesToRemove)
                    planes.Remove(pp);//Remove faces that are behind(but same direction) the one we just added

                if (valid)
                    planes.Add(planeToAdd);
            }
        }
        if (planes.Count < 4)
            throw new Exception("Not enough faces are present to build a crystal!");

        return planes;
    }

    /// <summary>
    /// Creates a vertex from every triplet of planes IF 1) the point is on or in the crystal and 2) it is not a duplicate
    /// </summary>
    /// <param name="planes">The planes to find the intersection points of.</param>
    /// <returns>A list of vertices with position and list of planes that intersect at that point. Can be more than three planes.</returns>
    private static List<Vertex> GenerateVertices(List<Planed> planes)
    {
        List<Vertex> vertices = new();//Create a vertex from every plane triplet and verify it is on the crystal
        for (int i = 0; i < planes.Count - 2; i++)
        {
            for (int j = i + 1; j < planes.Count - 1; j++)
            {
                for (int k = j + 1; k < planes.Count; k++)
                {
                    if (planes[i].Normal.Dot(-planes[j].Normal) > .9999f//Two planes are opposite one another
                    || planes[i].Normal.Dot(-planes[k].Normal) > .9999f
                    || planes[j].Normal.Dot(-planes[k].Normal) > .9999f)
                        continue;

                    Vector3d? intersection = planes[i].Intersect3(planes[j], planes[k]);

                    if (intersection == null)//Only happens if two faces are parallel
                        continue;
                    //intersection = FormatVector3d((Vector3d)intersection);

                    Vertex vertexToVerify = new((Vector3d)intersection, planes[i], planes[j], planes[k]);

                    bool verified = VerifyVertex(planes, vertices, vertexToVerify);
                    if (verified)
                        vertices.Add(vertexToVerify);
                }
            }
        }

        return vertices;
    }

    /// <summary>
    /// Merges two vertices if they are in the same spot but has different faces
    /// Also checks to make sure the vertex is within or on the crystal. 
    /// Sometimes planes can generate outside of the crystal so we make sure that doesn't happen here.
    /// </summary>
    /// <param name="planes">The list of planes that make up the crystal faces</param>
    /// <param name="vertices">All existing verticies to check for duplicates against</param>
    /// <param name="vertexToVerify">The vertex we want to validate</param>
    /// <returns></returns>
    private static bool VerifyVertex(List<Planed> planes, List<Vertex> vertices, Vertex vertexToVerify)
    {
        if (vertexToVerify.point.IsZeroApprox())
            return false;

        if (IsInMesh(planes, vertexToVerify.point) == false)
            return false;

        foreach (Vertex v in vertices)
        {
            if (v.point == vertexToVerify.point)
            {
                // GD.Print("Merging point " + v.point + " with " + vertexToVerify.point);
                v.MergeVertices(vertexToVerify);//Two different plane triplets made the same point. That means the point has more than 3 faces. 
                return false;//So we add the extra faces to one point and discard the other.
            }
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
                if (vertices[i].point.IsEqualApprox(vertices[j].point))//Don't create an edge between a point and itself
                    continue;

                List<Planed> sharedFaces = vertices[i].SharedFaces(vertices[j]);//Check for shared faces
                // if (sharedFaces.Count > 2)
                //     // GD.PrintErr("Vertices share more than two faces!");
                if (sharedFaces.Count >= 2)//If they share TWO faces, that means they have an edge together
                {
                    Planed p1 = sharedFaces[0];//First plane that we found a new edge on
                    Planed p2 = sharedFaces[sharedFaces.Count - 1];//Second plane ^
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
                    catch (Exception)
                    {
                        // // GD.Print("Plane 1: " + p1.Normal);
                        // // GD.Print("Plane 2: " + p2.Normal);
                        // // GD.Print("Vertex 1: " + v1.point);
                        // // GD.Print("Vertex 2: " + v2.point);
                        // // GD.PrintErr(e.GetType() + e.Message);
                        // foreach (Plane p in faces.Keys)
                        // {
                        //     // GD.Print("Plane: " + p.Normal);
                        //     foreach (Vertex v in faces[p].Keys)
                        //     {
                        //         string a = "", b = "";
                        //         if (faces[p][v].a != null)
                        //             a = faces[p][v].a.point.ToString();
                        //         if (faces[p][v].b != null)
                        //             b = faces[p][v].b.point.ToString();
                        //         // GD.PrintErr("Vertex: " + v.point + ", A: " + a + ", B: " + b);
                        //     }
                        // }
                    }
                }
            }
        }

        List<Planed> facesToRemove = new();
        foreach (Planed p in faces.Keys)
        {
            if (faces[p].Count < 3)//Generated an invalid polygon
                facesToRemove.Add(p);//So we remove it later
        }
        foreach (Planed p in facesToRemove)
            faces.Remove(p);
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
                // GD.PrintErr("Hit broken edge");
                return edges;
            }
            if (count++ > 20)
            {
                // GD.PrintErr("Infinite loop in edge creation");
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
    /// Intended to remove -0 from vectors
    /// </summary>
    public static Vector3d FormatVector3d(Vector3d v, double tolerance = 0.00001f)
    {
        double x = v.x * v.x < tolerance ? 0 : v.x;
        double y = v.y * v.y < tolerance ? 0 : v.y;
        double z = v.z * v.z < tolerance ? 0 : v.z;
        return new Vector3d(x, y, z);
    }

    public static bool IsInMesh(List<Planed> planes, Vector3d v, double tolerance = .00001f)
    {
        foreach (Planed p in planes)
        {
            if (p.DistanceTo(v) > tolerance)//Vertex is in front of a face and therefore not on the crystal. Or it's concave and this is broken.
                return false;
        }
        return true;
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
    public static double CalculateFaceArea(List<Vector3d> vertices, Vector3d[] b)
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
    public static double CalculateSurfaceArea(List<List<Vector3d>> faces, Vector3d[] b)
    {
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
    public static double CalculateVolume(List<List<Vector3d>> faces, Vector3d[] b)//TODO multiply all vectors by basis
    {
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

    #endregion math

    /// <summary>
    /// Saves the mesh as an STL
    /// </summary>
    /// <param name="fileName">Name of the file to save to</param>
    /// <param name="mesh">Crystal mesh, not transformed by any crystal parameters</param>
    /// <param name="m">Crystal parameters to transform the mesh by</param>
    public void ExportSTL(string fileName, Vector3d[] m)
    {
        //https://docs.godotengine.org/en/stable/tutorials/io/saving_games.html
        /*
        solid [name]
        facet normal ni nj nk
            outer loop
                vertex v1x v1y v1z
                vertex v2x v2y v2z
                vertex v3x v3y v3z //CAN ONLY do tris
            endloop
        endfacet
        endsolid [name]*/

        if (fileName.EndsWith(".stl") == false)
            fileName += ".stl";

        using System.IO.StreamWriter writer = new System.IO.StreamWriter(fileName);

        List<Vector3d> transformedFaces = new List<Vector3d>();//Every 3 vertices is a new tri
        foreach (List<Vector3d> face in this.faces)
        {
            for (int i = 1; i < face.Count - 1; i++)
            {
                transformedFaces.Add(face[0]);
                transformedFaces.Add(face[i]);
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
    /// <param name="m">Crystal parameters to transform the mesh by</param>
    public void ExportOBJ(string fileName, Vector3d[] m)
    {
        //https://docs.godotengine.org/en/stable/tutorials/io/saving_games.html
        //https://en.wikipedia.org/wiki/Wavefront_.obj_file
        /*
        o (name)
        v x y z
        n x y z
        f v1 v2 v3 v4... <- mesh.GetFaces() only does tris so we export 3 at a time
        f v1/uv1/n1 v2/uv2/n2 v3/uv3/n3
        f v1//n1 v2//n2 v3//n3 <- we use normals but not UVs so we we use this
        */

        if (fileName.EndsWith(".obj") == false)
            fileName += ".obj";

        using System.IO.StreamWriter writer = new System.IO.StreamWriter(fileName);

        List<List<Vector3d>> transformedFaces = new List<List<Vector3d>>();

        foreach (List<Vector3d> face in faces)
        {
            List<Vector3d> newFace = new List<Vector3d>();
            foreach (Vector3d v in face)
                newFace.Add(m * v);//Apply matrix transform while adding
            transformedFaces.Add(newFace);
        }

        //Index each vertex and normal
        int vertexIndex = 1, normalIndex = 1;
        Dictionary<Vector3d, int> vertexDict = new Dictionary<Vector3d, int>();
        Dictionary<Vector3d, int> normalDict = new Dictionary<Vector3d, int>();
        foreach (List<Vector3d> face in transformedFaces)
        {
            foreach (Vector3d v in face)
            {
                if (vertexDict.ContainsKey(v) == false)
                    vertexDict.Add(v, vertexIndex++);
            }
        }
        //Creates an alias for each normal. We go by 3 since every 3 vertices is a surface triangle
        foreach (List<Vector3d> face in transformedFaces)
        {
            Vector3d normal = CalculateNormal(face[0], face[1], face[2]);

            if (normalDict.ContainsKey(normal) == false)
                normalDict.Add(normal, normalIndex++);
        }


        writer.WriteLine("o " + fileName.Substring(0, fileName.Length - 4));//Trim out .obj tag

        //Create list of vertices and normals in file
        foreach (Vector3d v in vertexDict.Keys)
            writer.WriteLine($"v {v.X} {v.Y} {v.Z} #v{vertexDict[v]}");
        foreach (Vector3d n in normalDict.Keys)
            writer.WriteLine($"vn {n.X} {n.Y} {n.Z} #v{normalDict[n]}");

        writer.WriteLine("s " + 0);//Surface number 0

        foreach (List<Vector3d> face in transformedFaces)
        {
            //Yes this is redundant. But we have to create an alias for each vertex before we use it
            Vector3d v1 = face[0];
            Vector3d v2 = face[1];
            Vector3d v3 = face[2];//these 3 are one tri in the mesh

            Vector3d normal = CalculateNormal(v1, v2, v3);

            int n = normalDict[normal];

            //A .obj face is structured like this:
            //f vertex1/texturecoords1/normal1 vertex2...
            //We skip texturecoords by doing vertex1//normal1
            string s = "f";
            for (int i = 0; i < face.Count; i++)//Add vertex and normal for each vertex on face
                s += $" {vertexDict[face[i]]}//{n}";//The leading space is intentional
            writer.WriteLine(s);
        }
    }

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
            List<Planed> sharedFaces = new();
            for (int i = 0; i < planes.Count; i++)
            {
                for (int j = 0; j < other.planes.Count; j++)
                {
                    if (planes[i] == other.planes[j] && !sharedFaces.Contains(planes[i]))
                        sharedFaces.Add(planes[i]);
                }
            }
            return sharedFaces;
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