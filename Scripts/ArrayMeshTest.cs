using Godot;
using System.Collections.Generic;

[Tool]
public partial class ArrayMeshTest : MeshInstance3D
{
	ArrayMesh mesh;
	// Called when the node enters the scene tree for the first time.
	[Export]
	int h = 1, k = 1, l = 1;
	[Export]
	bool Update { get => true; set { UpdateMesh(); } }
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void UpdateMesh()
	{
		List<Vector3> points = SymmetryOperations.CreateCrystalSymmetry(Vector3.One, SymmetryOperations.PointGroup.MBarThreeM);
		string s = "";
		foreach (Vector3 point in points)
			s += point + " ";
		GD.Print(s);
		//GD.Print(h + " " + k + " " + l);
		mesh = new ArrayMesh();
		List<Vector3> vertices = MillerToVertices();
		//GD.Print(vertices[0] + " " + vertices[1] + " " + vertices[2] + ((vertices.Count == 4) ? vertices[3] : ""));
		Godot.Collections.Array arrays = new();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();


		// Create the Mesh.
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.TriangleStrip, arrays);
		Mesh = mesh;
	}

	private List<Vector3> MillerToVertices()
	{
		List<Vector3> vertices = new();
		Stack<Vector3> skipped = new();
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
			int count = vertices.Count;//Do this before the for loop so we avoid extruding the v we added infinitely
			Vector3 axis = skipped.Pop();
			for (int i = 0; i < count; i++)
			{
				Vector3 vert = vertices[i];
				Vector3 newVert = vert + axis;
				vertices.Add(newVert);
			}
		}

		return vertices;
	}
}
