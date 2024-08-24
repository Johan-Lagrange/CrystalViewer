using Godot;
using System.Collections.Generic;

[Tool]
public partial class CrystalGameObject : MeshInstance3D
{
	ArrayMesh mesh;
	[ExportGroup("Crystal Parameters")]
	[Export]
	Vector3 AxisLengths
	{
		get => new(aLength, bLength, cLength);//Represents the axes' lengths as a vector 3 on the fly
		set
		{
			aLength = value.X;
			bLength = value.Y;
			cLength = value.Z;
			UpdateFromParameters();
		}
	}
	[Export]
	Vector3 AlphaBetaGamma
	{
		get => new(alpha, beta, gamma);
		set
		{
			alpha = value.X;
			beta = value.Y;
			gamma = value.Z;
			UpdateFromParameters();
		}
	}
	public float aLength = 1, bLength = 1, cLength = 1, alpha = 90, beta = 90, gamma = 90;
	private float sinAlpha, sinBeta, sinGamma, cosAlpha, cosBeta, cosGamma;//These are just to make calculations easier to read.

	[ExportGroup("Unit Vectors")]
	[Export]
	Vector3 aVector = Vector3.Right, bVector = Vector3.Up, cVector = Vector3.Back;//Back = 0 0 1
	[ExportGroup("")]
	[Export]
	public float UnitCellVolume { get => aVector.Dot(bVector.Cross(cVector)); set { } }
	[Export]
	bool UpdateTheMesh { get { return true; } set { UpdateMesh(); } }
	[Export]
	bool UseLatticeVectors { get => usingLatticeVectors; set { usingLatticeVectors = !usingLatticeVectors; UpdateLatticeVectors(); } }
	private bool usingLatticeVectors;
	// Called when the node enters the scene tree for the first time.
	[ExportGroup("Crystal Faces")]
	[Export]
	public SymmetryOperations.PointGroup PointGroup { get => _pointGroup; set { _pointGroup = value; UpdateAxes(value); } }//UpdateMesh(); } }
	[Export]
	public Vector3[] Normals { get => _normals; set { _normals = value; } }//UpdateMesh(); } }
	[Export]
	public float[] Distances { get => _distances; set { _distances = value; } }//UpdateMesh(); } }
	SymmetryOperations.PointGroup _pointGroup = SymmetryOperations.PointGroup.BarThreeRhombohedral;
	Vector3[] _normals = { new(3, 1, 0), new(0, 5, 1), new(-1, -1, -1), new(-2, 0, -1) };
	float[] _distances = { 1, .9f, 1, 1 };
	private Crystal crystal;

	private bool updatedThisFrame = false;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		updatedThisFrame = false;
	}

	public string GetShapeClass() => SymmetryOperations.GetNameOfSymmetryClass(PointGroup);
	public float GetSurfaceCount() => crystal.faces.Count;
	public float GetSurfaceArea()
	{
		return (float)Crystal.CalculateTotalSurfaceArea(crystal.faces, GodotCompatability.BasisToMatrix(Basis));
	}
	public float GetVolume()
	{
		return (float)Crystal.CalculateVolume(crystal.faces, GodotCompatability.BasisToMatrix(Basis));
	}

	private void UpdateLatticeVectors()
	{
		if (usingLatticeVectors)
			Basis = new Basis(aVector, bVector, cVector);
		else
			Basis = new Basis(Vector3.Right, Vector3.Up, Vector3.Forward);
	}
	public void ExportSTL(string filename)
	{
		crystal.ExportSTL(filename, GodotCompatability.BasisToMatrix(this.Basis));
	}
	public void ExportOBJ(string filename)
	{
		crystal.ExportOBJ(filename, GodotCompatability.BasisToMatrix(this.Basis));
	}
	private ArrayMesh CreateArrayMeshFromCrystal(Crystal c)
	{
		// Create the Mesh.
		mesh = new ArrayMesh();
		foreach (List<List<Vector3d>> faceGroup in c.faceGroups)
		{
			if (faceGroup.Count == 0)
				continue;

			Godot.Collections.Array arrays = new();//Array of surface data
			arrays.Resize((int)Mesh.ArrayType.Max);
			List<Vector3> meshVertices = new();
			List<Vector3> meshNormals = new();
			List<float> tangents = new();

			foreach (List<Vector3d> faced in faceGroup)
			{
				if (faced.Count == 0)
					continue;

				List<Vector3> face = new();
				foreach (Vector3d vd in faced)
					face.Add(GodotCompatability.DoubleToGD(vd));//Convert our bespoke double based vectors to GD's float ones

				Vector3 normal = GodotCompatability.CalculateNormal(face[0], face[1], face[2]);

				Vector3 tangentVector = (face[1] - face[0]).Normalized();
				float[] tangent = new float[] { tangentVector[0], tangentVector[1], tangentVector[2], 1 };

				for (int i = 1; i <= face.Count - 2; i++)//We work two vertices at a time. That's why we do face.count - 2
				{
					meshVertices.AddRange(new Vector3[] { face[0], face[i], face[i + 1] });
					meshNormals.AddRange(new Vector3[] { normal, normal, normal });
					tangents.AddRange(new float[] { tangent[0], tangent[1], tangent[2], tangent[3], tangent[0], tangent[1], tangent[2], tangent[3], tangent[0], tangent[1], tangent[2], tangent[3] });
				}
			}
			arrays[(int)Mesh.ArrayType.Vertex] = meshVertices.ToArray();
			arrays[(int)Mesh.ArrayType.Normal] = meshNormals.ToArray();
			arrays[(int)Mesh.ArrayType.Tangent] = tangents.ToArray();
			mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		}
		GD.Print(mesh);
		return mesh;
	}

	public void UpdateMesh()
	{
		if (updatedThisFrame)
			return;
		updatedThisFrame = true;

		if (Distances.Length != Normals.Length)
		{
			GD.Print("Resized distance array to match normals array");
			float[] newDistances = new float[Normals.Length];
			int i = 0;
			for (; i < Distances.Length && i < newDistances.Length; i++)
				newDistances[i] = Distances[i];
			for (; i < Normals.Length; i++)
				newDistances[i] = 1;
			Distances = newDistances;
		}

		List<Vector3d> normalsd = new List<Vector3d>();//List of our bespoke double vector type
		foreach (Vector3 n in Normals)
			normalsd.Add(GodotCompatability.GDToDouble(n));

		List<double> doubles = new List<double>();
		foreach (float f in Distances)
			doubles.Add((double)f);

		crystal = new Crystal(normalsd, doubles, _pointGroup);
		ArrayMesh mesh = CreateArrayMeshFromCrystal(crystal);
		Mesh = mesh;
	}
	public void UpdateFromParameters()
	{
		UpdateTrig();
		//http://www.gisaxs.com/index.php/Unit_cell
		aVector = new(aLength, 0, 0);

		bVector = new(bLength * cosGamma, bLength * sinGamma, 0);
		float cYComponent = (cosAlpha - cosBeta * cosGamma) / sinGamma;
		cVector = new(cLength * cosBeta,
			cLength * cYComponent,
			cLength * Mathf.Sqrt(1 - cosBeta * cosBeta - cYComponent * cYComponent));
		if (UseLatticeVectors == true)
			UpdateLatticeVectors();
	}
	public void UpdateFromVectors()
	{
		aLength = aVector.Length();
		bLength = bVector.Length();
		cLength = cVector.Length();
		alpha = Mathf.RadToDeg(bVector.AngleTo(cVector));
		beta = Mathf.RadToDeg(aVector.AngleTo(cVector));
		gamma = Mathf.RadToDeg(aVector.AngleTo(bVector));
		UpdateTrig();
		if (UseLatticeVectors == true)
			UpdateLatticeVectors();
	}
	private void UpdateTrig()
	{
		sinAlpha = Mathf.Sin(Mathf.DegToRad(alpha));
		cosAlpha = Mathf.Cos(Mathf.DegToRad(alpha));
		sinBeta = Mathf.Sin(Mathf.DegToRad(beta));
		cosBeta = Mathf.Cos(Mathf.DegToRad(beta));
		sinGamma = Mathf.Sin(Mathf.DegToRad(gamma));
		cosGamma = Mathf.Cos(Mathf.DegToRad(gamma));
	}
	private void UpdateAxes(SymmetryOperations.PointGroup pointGroup)
	{
		float[] axes = SymmetryOperations.GetParametersForPointGroup(pointGroup);
		aLength = axes[0];
		bLength = axes[1];
		cLength = axes[2];
		alpha = axes[3];
		beta = axes[4];
		gamma = axes[5];
		UpdateFromParameters();
	}
	public static Vector3 Integerize(Vector3 v)
	{
		const float threshold = .5f;
		Vector3 vScaled = v;
		for (int i = 0; i < 3; i++)
		{
			if (v[i] > 0.00001f)
				vScaled *= 1 / v[i];
		}
		float Deviation(float f) => (f - Mathf.Round(f)) * (f - Mathf.Round(f));

		if (Deviation(v.X) + Deviation(v.Y) + Deviation(v.Z) > threshold)
			return new(Mathf.Round(v.X), Mathf.Round(v.Y), Mathf.Round(v.Z));

		return vScaled;
	}
}
