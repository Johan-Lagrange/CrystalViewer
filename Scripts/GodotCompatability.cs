using System.Collections.Generic;
using Godot;

public static class GodotCompatability
{
    public static Vector3 DoubleToGD(Vector3d v) => new Vector3((float)v.x, (float)v.y, (float)v.z);
    public static Vector3d GDToDouble(Vector3 v) => new Vector3d(v.X, v.Y, v.Z);
    public static Basis MatrixToBasis(Vector3d[] m) => new Basis(DoubleToGD(m[0]), DoubleToGD(m[1]), DoubleToGD(m[2]));
    public static Vector3d[] BasisToMatrix(Basis b) => new Vector3d[] { GDToDouble(b.X), GDToDouble(b.Y), GDToDouble(b.Z) };



    public static Vector3 CalculateNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 normal = (c - a).Cross(b - a).Normalized();
        if (normal.Dot(a) < 0)
            normal = -normal;
        return normal.Normalized();
    }


    /// <summary>
    /// Saves the mesh as an STL
    /// </summary>
    /// <param name="fileName">Name of the file to save to</param>
    /// <param name="mesh">Crystal mesh, not transformed by any crystal parameters</param>
    /// <param name="m">Crystal parameters to transform the mesh by</param>
    public static void ExportArrayMeshAsSTL(string fileName, ArrayMesh mesh, Basis m)
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

        Vector3[] faces = mesh.GetFaces();//Every 3 vertices is a new tri

        for (int i = 0; i < faces.Length; i++)
            faces[i] = m * faces[i];


        file.StoreLine("solid " + fileName.Substring(0, fileName.Length - 4));//Trim out .stl tag
        for (int i = 0; i < faces.Length - 2; i += 3)
        {
            Vector3 v1 = faces[i];
            Vector3 v2 = faces[i + 1];
            Vector3 v3 = faces[i + 2];//these 3 are one tri in the mesh

            Vector3 normal = CalculateNormal(v1, v2, v3);

            file.StoreLine("facet normal " + normal.X + " " + normal.Y + " " + normal.Z);
            file.StoreLine("\touter loop");
            file.StoreLine("\t\t vertex " + v1.X + " " + v1.Y + " " + v1.Z);
            file.StoreLine("\t\t vertex " + v2.X + " " + v2.Y + " " + v2.Z);
            file.StoreLine("\t\t vertex " + v3.X + " " + v3.Y + " " + v3.Z);
            file.StoreLine("\tendloop");
            file.StoreLine("endfacet");
        }
        file.StoreLine("endsolid " + fileName);
    }

    /// <summary>
    /// Saves the mesh as an OBJ
    /// </summary>
    /// <param name="fileName">Name of the file to save to</param>
    /// <param name="mesh">Crystal mesh, not transformed by any crystal parameters</param>
    /// <param name="m">Crystal parameters to transform the mesh by</param>
    public static void ExportArrayMeshAsOBJ(string fileName, ArrayMesh mesh, Basis m)
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

        using FileAccess file = FileAccess.Open(fileName, FileAccess.ModeFlags.Write);

        Vector3[] faces = mesh.GetFaces();//Every 3 vertices is a new tri

        for (int i = 0; i < faces.Length; i++)
            faces[i] = m * faces[i];

        //Index each vertex and normal
        int vertexIndex = 1, normalIndex = 1;
        Dictionary<Vector3, int> vertexDict = new Dictionary<Vector3, int>();
        Dictionary<Vector3, int> normalDict = new Dictionary<Vector3, int>();
        foreach (Vector3 v in faces)
        {
            if (vertexDict.ContainsKey(v) == false)
                vertexDict.Add(v, vertexIndex++);
        }
        //Creates an alias for each normal. We go by 3 since every 3 vertices is a surface triangle
        for (int i = 0; i < faces.Length - 2; i += 3)
        {
            Vector3 normal = CalculateNormal(faces[i], faces[i + 1], faces[i + 2]);

            if (normalDict.ContainsKey(normal) == false)
                normalDict.Add(normal, normalIndex++);
        }


        file.StoreLine("o " + fileName.Substring(0, fileName.Length - 4));//Trim out .obj tag

        //Create list of vertices and normals in file
        foreach (Vector3 v in vertexDict.Keys)
            file.StoreLine($"v {v.X} {v.Y} {v.Z} #v{vertexDict[v]}");
        foreach (Vector3 n in normalDict.Keys)
            file.StoreLine($"vn {n.X} {n.Y} {n.Z} #v{normalDict[n]}");

        file.StoreLine("s " + 0);//Surface number 0

        for (int i = 0; i < faces.Length - 2; i += 3)
        {
            //Yes this is redundant. But we have to create an alias for each vertex before we use it
            Vector3 v1 = faces[i];
            Vector3 v2 = faces[i + 1];
            Vector3 v3 = faces[i + 2];//these 3 are one tri in the mesh

            Vector3 normal = CalculateNormal(v1, v2, v3);
            if (normal == Vector3.Zero)
                GD.Print($"v1: {v1} v2: {v2} v3: {v3} norm: {normal} i: {i} count: {faces.Length}");
            int n = normalDict[normal];

            //A .obj face is structured like this:
            //f vertex1/texturecoords1/normal1 vertex2...
            //We skip texturecoords by doing vertex1//normal1
            //we COULD add more vertices per face instead of just building tris
            //But "mesh.GetFaces" only returns tris and I dont want to find like surfaces.
            file.StoreLine($"f {vertexDict[v1]}//{n} {vertexDict[v2]}//{n} {vertexDict[v3]}//{n}");
        }
    }
}