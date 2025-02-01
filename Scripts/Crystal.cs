using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>
/// This class generates crystals given face vectors and symmetry
/// It also has some extra methods for calculating things like surface area
/// </summary>
public partial class Crystal
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
    normalGroups = CrystalGeneration.GenerateSymmetryGroups(initialNormals, pointGroup);

    planeGroups = CrystalGeneration.GeneratePlanes(normalGroups, distances);//Create a plane with distance from center for every generated normal

    //Associate each plane with the face group it belongs to, so we keep things sorted when adding the vertices
    Dictionary<Planed, int> planesToFaceGroups = new Dictionary<Planed, int>();
    for (int group = 0; group < planeGroups.Count; group++)
    {
      foreach (Planed plane in planeGroups[group])
      {
        planesToFaceGroups.Add(plane, group);
      }
    }

    List<Vertex> vertices = CrystalGeneration.GenerateVertices(planeGroups);

    CrystalGeneration.RemoveInvalidPlanes(vertices);

    //Create a dictionary that will take a plane and give an unordered list of each edge that makes up the plane's face
    Dictionary<Planed, Dictionary<Vertex, AdjacentEdges>> unorderedEdges = CrystalGeneration.GenerateEdges(vertices);

    faceGroups = new List<List<List<Vector3d>>>();
    foreach (List<Planed> list in planeGroups)
      faceGroups.Add(new List<List<Vector3d>>());

    faces = new();//The final mesh that we are building. Contains the vertices of each face in order.
    foreach (Planed plane in unorderedEdges.Keys)
    {
      List<Vector3d> face = CrystalGeneration.CreateFaceFromEdges(unorderedEdges[plane]);
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

      Vector3d normal = CrystalMath.CalculateNormal(v1, v2, v3);

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
        Vector3d normal = CrystalMath.CalculateNormal(face[0], face[1], face[2]);

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
        Vector3d normal = CrystalMath.CalculateNormal(face[0], face[1], face[2]);

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