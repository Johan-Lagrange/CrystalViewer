using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public static class CrystalGenerator
{
    /// <summary>
    /// Generates a mesh from a list of face normals and distances that will be duplicated according to symmetry.
    /// </summary>
    /// <param name="initialFaces">The array of normal directions of each symetrically unique face</param>
    /// <param name="distances">The array of distances per face from crystal center</param>
    /// <param name="pointGroup">The type of symmetry this crystal has. Dictates which symmetry operations (mirror, rotate) are used</param>
    /// <returns></returns>
    public static ArrayMesh CreateMesh(
        Vector3[] initialFaces,
        float[] distances,
        SymmetryOperations.PointGroup pointGroup,
        out List<Vector3>[] normals,
        out List<Plane> planes,
        out ArrayMesh mesh,
        out List<List<Vector3>> faceEdges)
    {
        if (distances.Length != initialFaces.Length)
            throw new ArgumentException("Every initial face must be given a distance!");

        normals = new List<Vector3>[initialFaces.Length];//List of normals for each initial face that was duplicated by the symmetry group

        //Reflect every given face along the given symmetry group
        for (int i = 0; i < initialFaces.Length; i++)
        {
            normals[i] = CreateCrystalSymmetry(initialFaces[i].Normalized(), pointGroup);//Reflects every normal along the given point group's symmetry.
        }

        planes = GeneratePlanes(initialFaces, distances, normals);//Create a plane with distance from center for every generated normal


        List<Vertex> vertices = GenerateVertices(planes);//Get all valid vertices on the crystal

        Dictionary<Plane, Dictionary<Vertex, AdjacentEdges>> faces = GenerateEdges(vertices);//Create a dictionary that will take a plane and give an unordered list of each edge that makes up the plane's face

        faceEdges = new();//The final mesh that we are building. Contains the vertices of each face in order.
        foreach (Dictionary<Vertex, AdjacentEdges> unorderedFace in faces.Values)
        {

            List<Vector3> face = CreateFaceFromEdges(unorderedFace);
            if (face.Count >= 3)
                faceEdges.Add(face);
        }

        // Create the Mesh.
        mesh = new ArrayMesh();
        foreach (List<Vector3> face in faceEdges)
        {

            Godot.Collections.Array arrays = new();//Array of surface data
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = face.ToArray();//Note: Different vertex type than the one we use in this class. These are just Vector3s
            Vector3 normal = GetNormal(face[0], face[1], face[2]);

            Vector3 tangentVector = (face[1] - face[0]).Normalized();
            float[] tangent = new float[] { tangentVector[0], tangentVector[1], tangentVector[2], 1 };

            List<Vector3> meshVertices = new();
            List<Vector3> meshNormals = new();
            List<float> tangents = new();

            for (int i = 1; i <= face.Count - 2; i++)//We work two vertices at a time. That's why we do face.count - 2
            {
                meshVertices.AddRange(new Vector3[] { face[0], face[i], face[i + 1] });
                meshNormals.AddRange(new Vector3[] { normal, normal, normal });
                tangents.AddRange(new float[] { tangent[0], tangent[1], tangent[2], tangent[3], tangent[0], tangent[1], tangent[2], tangent[3], tangent[0], tangent[1], tangent[2], tangent[3] });
            }

            arrays[(int)Mesh.ArrayType.Vertex] = meshVertices.ToArray();
            arrays[(int)Mesh.ArrayType.Normal] = meshNormals.ToArray();
            arrays[(int)Mesh.ArrayType.Tangent] = tangents.ToArray();

            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        }
        return mesh;
    }
    /// <summary>
    /// Takes an initial vector and applies all point group operations on it, 
    /// returning a list of every vector therein, including the original. (Identity is an operation after all)
    /// </summary>
    /// <param name="v">The initial vector to do symmetry stuff on</param>
    /// <param name="group">The crystal's point group. Used to get a list of operations</param>
    /// <returns>A list of vectors, including the original, that are made from the symmetry operations</returns>
    public static List<Vector3> CreateCrystalSymmetry(Vector3 v, SymmetryOperations.PointGroup group)
    {
        List<Vector3> vectorList = new() { v };

        foreach (Func<Vector3, Vector3> Operation in SymmetryOperations.PointGroupPositions[(int)group])
        {
            ApplyOperation(vectorList, Operation);
        }
        return vectorList;
    }
    /// <summary>
    /// Applies a symmetry operation to every vector in a list, and adds every result to the list.
    /// </summary>
    /// <param name="vectorList">List of vectors to operate upon and expand</param>
    /// <param name="symmetryOperation">The symmetry operation to do</param>
    public static void ApplyOperation(List<Vector3> vectorList, Func<Vector3, Vector3> symmetryOperation)
    {
        int count = vectorList.Count;//Get the count before we add to the list so we aren't in an infinite loop
        for (int i = 0; i < count; i++)
        {
            Vector3 v = symmetryOperation(vectorList[i]);
            v = FormatVector3(v);

            bool valid = true;
            foreach (Vector3 vl in vectorList)
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
    private static Vector3 GetNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 normal = (c - a).Cross(b - a).Normalized();
        if (normal.Dot(a) < 0)
        {
            GD.Print("Normal was backwards");
            normal = -normal;
        }
        return normal;
    }

    /// <summary>
    /// Generates the crystal's planes from faces, distances, and normals
    /// </summary>
    /// <param name="initialFaces"></param>
    /// <param name="distances"></param>
    /// <param name="normals"></param>
    /// <returns></returns>
    private static List<Plane> GeneratePlanes(Vector3[] initialFaces, float[] distances, List<Vector3>[] normals)
    {
        List<Plane> planes = new();
        for (int givenFace = 0; givenFace < initialFaces.Length; givenFace++)
        {
            List<Vector3> normalsList = normals[givenFace];

            foreach (Vector3 normal in normalsList)//Verify and add planes for each type of normal
            {
                Plane planeToAdd = new Plane(normal, distances[givenFace]);
                List<Plane> planesToRemove = new();
                bool valid = true;
                foreach (Plane p in planes)
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
                            planesToRemove.Add(p);//Another face was behind this so we remove that
                        }
                    }
                }
                foreach (Plane pp in planesToRemove)
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
    private static List<Vertex> GenerateVertices(List<Plane> planes)
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

                    Vector3? intersection = planes[i].Intersect3(planes[j], planes[k]);

                    if (intersection == null)//Only happens if two faces are parallel
                        continue;
                    //intersection = FormatVector3((Vector3)intersection);

                    Vertex vertexToVerify = new((Vector3)intersection, planes[i], planes[j], planes[k]);

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
    private static bool VerifyVertex(List<Plane> planes, List<Vertex> vertices, Vertex vertexToVerify)
    {
        if (vertexToVerify.point.IsZeroApprox())
            return false;

        if (IsInMesh(planes, vertexToVerify.point) == false)
            return false;

        foreach (Vertex v in vertices)
        {
            if ((v.point - vertexToVerify.point).LengthSquared() < .00001f)
            {
                //GD.Print("Merging point " + v.point + " with " + vertexToVerify.point);
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
    private static Dictionary<Plane, Dictionary<Vertex, AdjacentEdges>> GenerateEdges(List<Vertex> vertices)
    {
        Dictionary<Plane, Dictionary<Vertex, AdjacentEdges>> faces = new();

        for (int i = 0; i < vertices.Count - 1; i++)
        {
            for (int j = i + 1; j < vertices.Count; j++)//For every pair of vertices
            {
                if (vertices[i].point.IsEqualApprox(vertices[j].point))//Don't create an edge between a point and itself
                    continue;

                List<Plane> sharedFaces = vertices[i].SharedFaces(vertices[j]);//Check for shared faces
                if (sharedFaces.Count > 2)
                    GD.PrintErr("Vertices share more than two faces!");
                if (sharedFaces.Count >= 2)//If they share TWO faces, that means they have an edge together
                {
                    Plane p1 = sharedFaces[0];//First plane that we found a new edge on
                    Plane p2 = sharedFaces[sharedFaces.Count - 1];//Second plane ^
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
                        //GD.Print(v1.point + "-" + v2.point);
                        //GD.Print("Adding vertex on plane " + p1.Normal + " for vertex " + v1.point + " - " + v2.point);
                        faces[p1][v1].AddVertex(v2);//Create link from v1 -> v2 on plane 1
                        //GD.Print("Adding vertex on plane " + p1.Normal + " for vertex " + v2.point + " - " + v1.point);
                        faces[p1][v2].AddVertex(v1);//Create link from v2 -> v1 on plane 1
                        //GD.Print("Adding vertex on plane " + p2.Normal + " for vertex " + v1.point + " - " + v2.point);
                        faces[p2][v1].AddVertex(v2);//Create link from v1 -> v2 on plane 2
                        //GD.Print("Adding vertex on plane " + p2.Normal + " for vertex " + v2.point + " - " + v1.point);
                        faces[p2][v2].AddVertex(v1);//Create link from v2 -> v1 on plane 2
                    }
                    catch (Exception e)
                    {
                        GD.Print("Plane 1: " + p1.Normal);
                        GD.Print("Plane 2: " + p2.Normal);
                        GD.Print("Vertex 1: " + v1.point);
                        GD.Print("Vertex 2: " + v2.point);
                        GD.PrintErr(e.GetType() + e.Message);
                        foreach (Plane p in faces.Keys)
                        {
                            GD.Print("Plane: " + p.Normal);
                            foreach (Vertex v in faces[p].Keys)
                            {
                                string a = "", b = "";
                                if (faces[p][v].a != null)
                                    a = faces[p][v].a.point.ToString();
                                if (faces[p][v].b != null)
                                    b = faces[p][v].b.point.ToString();
                                GD.PrintErr("Vertex: " + v.point + ", A: " + a + ", B: " + b);
                            }
                        }
                    }
                }
            }
        }

        List<Plane> facesToRemove = new();
        foreach (Plane p in faces.Keys)
        {
            if (faces[p].Count < 3)//Generated an invalid polygon
                facesToRemove.Add(p);//So we remove it later
        }
        foreach (Plane p in facesToRemove)
            faces.Remove(p);
        return faces;
    }

    /// <summary>
    /// Builds a coplanar loop of vertices that encircle a face from a list of vertexes and edges of a single given face
    /// </summary>
    /// <param name="face">The list of vertices and adjacents that make up the face</param>
    /// <returns>The list of vertices in order around a face</returns>
    private static List<Vector3> CreateFaceFromEdges(Dictionary<Vertex, AdjacentEdges> face, bool clockwise = false)
    {
        List<Vector3> edges = new();//list of vertices around the perimeter of the face
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
                GD.PrintErr("Hit broken edge");
                return edges;
            }
            if (count++ > 20)
            {
                GD.PrintErr("Infinite loop in edge creation");
                return edges;
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
    public static Vector3 FormatVector3(Vector3 v, float tolerance = 0.00001f)
    {
        float x = v.X * v.X < tolerance ? 0 : v.X;
        float y = v.Y * v.Y < tolerance ? 0 : v.Y;
        float z = v.Z * v.Z < tolerance ? 0 : v.Z;
        return new Vector3(x, y, z);
    }

    public static bool IsInMesh(List<Plane> planes, Vector3 v, float tolerance = .00001f)
    {
        foreach (Plane p in planes)
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
    public static float CalculateTriangleArea(Vector3 a, Vector3 b, Vector3 c)
    {
        return (b - a).Cross(c - a).Length() / 2;
    }

    /// <summary>
    /// Calculates the area of a face defined by a list of vertices
    /// </summary>
    /// <param name="vertices">The list of vertices that defines the face</param>
    /// <returns>The area of the face</returns>
    public static float CalculateFaceArea(Vector3[] vertices)
    {
        if (vertices.Length < 3)
            return 0;

        float total = 0;
        for (int i = 1; i <= vertices.Length - 2; i++)
            total += CalculateTriangleArea(vertices[0], vertices[i], vertices[i + 1]);
        return total;
    }

    public static Vector3 GetAverageVertex(Vector3[] vertices)
    {
        Vector3 total = new();
        foreach (Vector3 v in vertices)
            total += v;
        return total / vertices.Length;
    }

    //https://stackoverflow.com/questions/1988100/how-to-determine-ordering-of-3d-vertices
    /// <summary>
    /// Checks if a, b, c, are ordered in clockwise order
    /// </summary>
    /// <returns>True if a, b, c are in clockwise order</returns>
    public static bool IsClockwise(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 normal = (b - a).Cross(c - a);
        return normal.Dot(a) > 0;
    }

    #endregion math

    /// <summary>
    /// Saves the mesh as an STL
    /// </summary>
    /// <param name="fileName">Name of the file to save to</param>
    /// <param name="mesh">Crystal mesh, not transformed by any crystal parameters</param>
    /// <param name="basis">Crystal parameters to transform the mesh by</param>
    public static void ExportSTL(string fileName, ArrayMesh mesh, Basis basis)
    {
        //https://docs.godotengine.org/en/stable/tutorials/io/saving_games.html
        /*
        solid [name]
        facet normal ni nj nk
            outer loop
                vertex v1x v1y v1z
                vertex v2x v2y v2z
                vertex v3x v3y v3z
            endloop
        endfacet
        endsolid [name]*/

        if (fileName.EndsWith(".stl") == false)
            fileName += ".stl";

        using FileAccess file = FileAccess.Open(fileName, FileAccess.ModeFlags.Write);

        file.StoreLine("solid " + fileName);
        int count = mesh.GetSurfaceCount();
        for (int i = 0; i < count; i++)
        {
            Vector3[] face = (Vector3[])mesh.SurfaceGetArrays(i)[(int)Mesh.ArrayType.Vertex];
            Vector3 normal = ((Vector3[])mesh.SurfaceGetArrays(i)[(int)Mesh.ArrayType.Normal])[0];//Gets the first normal of the face

            for (int j = 1; j <= face.Length - 2; j++)//We work two vertices at a time. That's why we do face.count - 2
            {
                Vector3 v0 = basis * face[0];
                Vector3 vj = basis * face[j];
                Vector3 vj1 = basis * face[j + 1];

                file.StoreLine("facet normal " + normal.X + " " + normal.Y + " " + normal.Z);
                file.StoreLine("\touter loop");
                file.StoreLine("\t\t vertex " + v0.X + " " + v0.Y + " " + v0.Z);
                file.StoreLine("\t\t vertex " + vj.X + " " + vj.Y + " " + vj.Z);
                file.StoreLine("\t\t vertex " + vj1.X + " " + vj1.Y + " " + vj1.Z);
                file.StoreLine("\tendloop");
                file.StoreLine("endfacet");
            }
        }
        file.StoreLine("endsolid " + fileName);
    }

    /// <summary>
    /// The vertex of a crystal. Stores position and adjacent faces for determining edges.
    /// </summary>
    private class Vertex
    {
        public Vector3 point;
        public List<Plane> planes = new();

        public Vertex(Vector3 p, Plane plane1, Plane plane2, Plane plane3)
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
        public List<Plane> SharedFaces(Vertex other)
        {
            List<Plane> sharedFaces = new();
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
            foreach (Plane p in other.planes)
            {
                //GD.Print(p.Normal);
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
    private static void DebugPrintVertexAdjacentEdges(Dictionary<Plane, Dictionary<Vertex, AdjacentEdges>> faces)
    {
        foreach (Plane p in faces.Keys)
        {
            GD.Print("Plane: " + p.Normal);
            foreach (Vertex v in faces[p].Keys)
            {
                string a = "", b = "";
                if (faces[p][v].a != null)
                    a = faces[p][v].a.point.ToString();
                if (faces[p][v].b != null)
                    b = faces[p][v].b.point.ToString();
                GD.Print("Vertex: " + v.point + ", A: " + a + ", B: " + b);
            }
        }
    }

    private static void DebugPrintPlanesWithVertices(List<Plane> planes, List<Vertex> vertices)
    {
        string s = "Planes: ";
        foreach (Plane p in planes)
            s += p.Normal + ", ";
        GD.Print(s);
        foreach (Vertex v in vertices)
        {
            s = "Vertices: ";
            s += v.point + ":";
            foreach (Plane p in v.planes)
            {
                s += p.Normal + "; ";
            }
            GD.Print(s);
        }
    }
    #endregion debug
}