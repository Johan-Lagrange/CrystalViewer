using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public static class CrystalGenerator
{
    public static ArrayMesh CreateMesh(Vector3[] initialFaces, float[] distances, SymmetryOperations.PointGroup pointGroup)
    {
        if (distances.Length != initialFaces.Length)
            throw new ArgumentException("Every initial face must be given a distance!");

        List<Vector3>[] normals = new List<Vector3>[initialFaces.Length];//List of normals for each initial face that was duplicated by the symmetry group

        //Reflect every given face along the given symmetry group
        for (int i = 0; i < initialFaces.Length; i++)
        {
            normals[i] = SymmetryOperations.CreateCrystalSymmetry(initialFaces[i].Normalized(), pointGroup);//Reflects every normal along the given point group's symmetry.
        }

        List<Plane> planes = new();//Create a plane with distance from center for every generated normal

        for (int givenFace = 0; givenFace < initialFaces.Length; givenFace++)
        {
            List<Vector3> normalsList = normals[givenFace];
            List<Plane> planesToRemove = new();

            foreach (Vector3 normal in normalsList)
            {
                Plane planeToAdd = new Plane(normal, distances[givenFace]);
                bool valid = true;
                foreach (Plane p in planes)
                {
                    if (planeToAdd.Normal.Normalized().Dot(p.Normal.Normalized()) > .9999f)//Skip adding duplicate plane
                    {
                        //GD.Print("Similar faces" + planeToAdd.Normal + " " + p.Normal);
                        if (planeToAdd.D >= p.D)
                        {
                            valid = false;
                            break;
                        }
                        else
                        {
                            planesToRemove.Add(p);
                        }
                    }
                }
                foreach (Plane pp in planesToRemove)
                {
                    planes.Remove(pp);
                }
                if (valid)
                    planes.Add(planeToAdd);
            }
        }
        if (planes.Count < 4)
        {
            throw new Exception("Not enough faces are present to build a crystal!");
        }

        // Vector3[] vertices = Geometry3D.ComputeConvexMeshPoints(new Godot.Collections.Array<Plane>(planes.ToArray()));
        // ConvexPolygonShape3D shape = new ConvexPolygonShape3D();
        // shape.Points = vertices;
        // ArrayMesh mesh = shape.GetDebugMesh().CreateConvexShape(true).GetDebugMesh();
        // GD.Print(mesh._Surfaces.Count);
        // var arrays = mesh.SurfaceGetArrays(0);
        // ArrayMesh mesh2 = new ArrayMesh();
        // mesh2.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        // mesh = mesh2;
        // SurfaceTool surfaceTool = new SurfaceTool();
        // surfaceTool.CreateFrom(mesh, 0);
        // surfaceTool.GenerateNormals();
        // surfaceTool.GenerateTangents();
        // mesh = surfaceTool.Commit();
        // return mesh;

        List<Vertex> vertices = GenerateVertices(planes);//Get all valid vertices on the crystal

        //DebugPrintPlanesWithVertices(planes, vertices);
        Dictionary<Plane, Dictionary<Vertex, AdjacentEdges>> faces = GenerateEdges(vertices);//Create a dictionary that will take a plane and give an unordered list of each edge that makes up the plane's face
        //DebugPrintVertexAdjacentEdges(faces);

        List<List<Vector3>> faceEdges = new();//The final mesh that we are building. Contains the vertices of each face in order.

        foreach (Dictionary<Vertex, AdjacentEdges> unorderedFace in faces.Values)
        {
            if (unorderedFace.Values.Count < 3)
            {
                string s = "Face: ";
                foreach (KeyValuePair<Vertex, AdjacentEdges> edge in unorderedFace)
                {
                    s += edge.Key.point;
                    s += edge.Value.a.point;
                    s += edge.Value.b.point;
                }
                GD.PrintErr("Not enough vertices to make this face!: " + s);
            }

            List<Vector3> face = CreateFaceFromEdges(unorderedFace);
            if (face.Count >= 3)
                faceEdges.Add(face);
        }

        // Create the Mesh.

        ArrayMesh mesh = new ArrayMesh();
        foreach (List<Vector3> face in faceEdges)
        {
            // string s = "Created face: ";
            // foreach (Vector3 v in face)
            // 	s += "(" + v.X + " " + v.Y + " " + v.Z + ")";
            // GD.Print(s);

            Godot.Collections.Array arrays = new();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = face.ToArray();



            Vector3 normal = -(face[1] - face[0]).Cross(face[2] - face[0]).Normalized();
            if (normal.Dot(face[0]) < 0)
                normal = -normal;
            Vector3 tangentVector = (face[1] - face[0]).Normalized();
            float[] tangent = new float[] { tangentVector[0], tangentVector[1], tangentVector[2], 1 };
            List<Vector3> meshVertices = new();
            List<Vector3> meshNormals = new();
            List<float> tangents = new();
            //GD.Print(face.ToString());
            // for (int i = 0; i < face.Count - 1; i++)//We work two vertices at a time. That's why we do face.count - 1
            // {
            // 	arrays[(int)Mesh.ArrayType.Vertex] = new Vector3[] { face[0], face[i], face[i + 1] };
            // 	arrays[(int)Mesh.ArrayType.Normal] = new Vector3[] { normal, normal, normal };
            // 	arrays[(int)Mesh.ArrayType.Tangent] = new float[] { tangent[0], tangent[1], tangent[2], tangent[3], tangent[0], tangent[1], tangent[2], tangent[3], tangent[0], tangent[1], tangent[2], tangent[3] };

            // 	mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            // }
            for (int i = 1; i <= face.Count - 2; i++)//We work two vertices at a time. That's why we do face.count - 2
            {
                meshVertices.AddRange(new Vector3[] { face[0], face[i], face[i + 1] });
                meshNormals.AddRange(new Vector3[] { normal, normal, normal });
                tangents.AddRange(new float[] { tangent[0], tangent[1], tangent[2], tangent[3], tangent[0], tangent[1], tangent[2], tangent[3], tangent[0], tangent[1], tangent[2], tangent[3] });
            }

            // string s = "Created face: ";
            // foreach (Vector3 v in vertices)
            // 	s += "(" + v.X + " " + v.Y + " " + v.Z + ")";
            // GD.Print(s);

            arrays[(int)Mesh.ArrayType.Vertex] = meshVertices.ToArray();
            arrays[(int)Mesh.ArrayType.Normal] = meshNormals.ToArray();
            arrays[(int)Mesh.ArrayType.Tangent] = tangents.ToArray();

            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        }
        return mesh;
    }

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

    //https://stackoverflow.com/questions/1988100/how-to-determine-ordering-of-3d-vertices
    public static bool IsClockwise(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 normal = (b - a).Cross(c - a);
        return normal.Dot(a) > 0;
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

        foreach (Plane p in planes)
        {
            if (p.DistanceTo(vertexToVerify.point) > .00001f)//Vertex is in front of a face and therefore not on the crystal. Or it's concave and this is broken.
            {
                return false;
            }
        }
        foreach (Vertex v in vertices)
        {
            if ((v.point - vertexToVerify.point).LengthSquared() < .000000001f)
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
                if (vertices[i].point.IsEqualApprox(vertices[j].point))
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
                        // GD.Print("Plane 1: " + p1.Normal);
                        // GD.Print("Plane 2: " + p2.Normal);
                        // GD.Print("Vertex 1: " + v1.point);
                        // GD.Print("Vertex 2: " + v2.point);
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

        foreach (Plane p in faces.Keys)
        {
            if (faces[p].Count < 3)//Generated an invalid polygon
                faces.Remove(p);//So we remove it
        }
        return faces;
    }

    /// <summary>
    /// Builds a coplanar loop of vertices that encircle a face from a list of vertexes and edges of a single given face
    /// </summary>
    /// <param name="face">The list of vertices and adjacents that make up the face</param>
    /// <returns>The list of vertices in order around a face</returns>
    private static List<Vector3> CreateFaceFromEdges(Dictionary<Vertex, AdjacentEdges> face)
    {
        // Debug stuff.
        // string s = "Face: ";
        // foreach (KeyValuePair<Vertex, AdjacentEdges> edge in face)
        // {
        //     s += edge.Key.point + " " + edge.Value.a.point + " " + edge.Value.b.point;
        // }
        // GD.Print(s);

        List<Vector3> edges = new();
        Vertex start = face.First().Key;
        Vertex here = face[start].a;
        Vertex next;
        Vertex previous = start;
        if (here == null)
            return edges;


        int count = 0;//To avoid infinite loops. Hopefully never needed.
                      //GD.Print("First:" + previous.point);
                      //GD.Print("Second:" + here.point);
        edges.Add(start.point);
        while (here.point != start.point)
        {
            edges.Add(here.point);
            next = face[here].GetNext(previous);
            previous = here;
            here = next;
            if (here == null)
            {
                return edges;
            }

            if (count++ > 20)
            {
                throw new Exception("Infinite loop in edge creation!");
            }
        }
        edges.Add(here.point);

        // s = "FaceFromEdges: ";
        // foreach (Vector3 point in edges)
        // {
        //     s += point + " ";
        // }
        // GD.Print(s);

        if (IsClockwise(edges[0], edges[1], edges[2]))
            edges.Reverse();
        return edges;
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
            if (a != null && a.point.IsEqualApprox(v.point))
            {
                throw new Exception("Was given a point twice!");
            }
            if (b != null && b.point.IsEqualApprox(v.point))
            {
                throw new Exception("Was given a point twice!");
            }
            if (b != null)
            {
                throw new Exception("Was given more than two vertices, this should not happen: " + a.point + " " + b.point + " " + v.point);
            }
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
}