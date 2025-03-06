using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

/// <summary>
/// This class interfaces crystal generation code with godot
/// </summary>
public partial class CrystalGameObject : MeshInstance3D
{
	[Signal]
	public delegate void OnGenerationFinishedEventHandler();
	[Export]
	StandardMaterial3D baseMaterial;

	/// <summary>
	/// List of materials for each face group. Keeps track of colors for groups that don't end up being generated as well.
	/// </summary>
	public List<StandardMaterial3D> materialList = new List<StandardMaterial3D>();
	ArrayMesh mesh;
	Thread crystalGenerationThread;
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
	bool UpdateTheMesh { get { return true; } set { StartMeshUpdate(); } }
	[Export]
	bool UseLatticeVectors { get => usingLatticeVectors; set { usingLatticeVectors = !usingLatticeVectors; UpdateLatticeVectors(); } }
	private bool usingLatticeVectors = true;
	// Called when the node enters the scene tree for the first time.
	[ExportGroup("Crystal Faces")]
	[Export]
	public SymmetryOperations.PointGroup PointGroup { get => _pointGroup; set => _pointGroup = value; }
	[Export]
	public Vector3[] Normals { get => _normals; set { _normals = value; } }
	[Export]
	public float[] Distances { get => _distances; set { _distances = value; } }
	SymmetryOperations.PointGroup _pointGroup = SymmetryOperations.PointGroup.BarThreeRhombohedral;
	/// <summary>
	/// Arbitrary sample normals
	/// </summary>
	Vector3[] _normals = { new(3, 1, 0), new(0, 5, 1), new(-1, -1, -1), new(-2, 0, -1) };
	/// <summary>
	/// Arbitrary sample distances
	/// </summary>
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
		return (float)CrystalMath.CalculateTotalSurfaceArea(crystal.faces, GodotCompatability.BasisToMatrix(Basis));
	}
	public double[] GetSurfaceAreaGroups()
	{
		double[] areas = new double[crystal.initialNormals.Count];
		for (int i = 0; i < crystal.initialNormals.Count; i++)
		{
			int faceGroupIndex = crystal.FindSurfaceMadeByIndex(i);
			if (faceGroupIndex == -1)
				areas[i] = 0;
			else
				areas[i] = CrystalMath.CalculateTotalSurfaceArea(crystal.faceGroups[faceGroupIndex], GodotCompatability.BasisToMatrix(Basis));
		}
		return areas;
	}
	public float GetVolume()
	{
		return (float)CrystalMath.CalculateVolume(crystal.faces, GodotCompatability.BasisToMatrix(Basis));
	}

	public int FindSurfaceMadeByIndex(int initialIndex)
	{
		return crystal.FindSurfaceMadeByIndex(initialIndex);
	}

	private void UpdateLatticeVectors()
	{
		if (usingLatticeVectors)
		{
			Basis = new Basis(aVector, bVector, cVector);
			crystal.UnitCell = GodotCompatability.BasisToMatrix(this.Basis);
		}
		else
			Basis = new Basis(Vector3.Right, Vector3.Up, Vector3.Forward);
	}

	public void SaveCrystal(string path)
	{
		Crystal.CrystalMaterial[] exportMaterials = materialList.Select(mat => GodotMaterialToCrystalMaterial(mat)).ToArray();
		Vector3d axisAngles = GodotCompatability.GDToDouble(AlphaBetaGamma);
		Vector3d axisLengths = GodotCompatability.GDToDouble(AxisLengths);
		crystal.SaveCrystal(path, exportMaterials, axisAngles, axisLengths);
	}
	public Crystal LoadCrystal(string path)
	{
		Crystal.CrystalSerialize importMaterials;
		crystal = Crystal.LoadCrystal(path, out importMaterials);

		materialList = new List<StandardMaterial3D>();
		foreach (Crystal.CrystalMaterial mat in importMaterials.Materials)
		{
			materialList.Add(CrystalMaterialToGodotMaterial(mat));
		}
		Vector3 angles = GodotCompatability.DoubleToGD(importMaterials.AxisAngles);
		alpha = angles.X;
		beta = angles.Y;
		gamma = angles.Z;
		Vector3 lengths = GodotCompatability.DoubleToGD(importMaterials.AxisLengths);
		aLength = lengths.X;
		bLength = lengths.Y;
		cLength = lengths.Z;
		// materialList = importMaterials.Materials.Select(mat => CrystalMaterialToGodotMaterial(mat)).ToList();
		GD.Print(materialList.Count);
		return crystal;
	}

	public void ExportSTL(string filename)
	{
		crystal.ExportSTL(filename);
	}
	public void ExportOBJ(string filename)
	{
		Crystal.CrystalMaterial[] exportMaterials = materialList.Select(mat => GodotMaterialToCrystalMaterial(mat)).ToArray();
		crystal.ExportOBJ(filename, exportMaterials.ToArray());
	}

	public Crystal.CrystalMaterial GodotMaterialToCrystalMaterial(StandardMaterial3D mat)
	{
		Crystal.CrystalMaterial crystalMaterial = new Crystal.CrystalMaterial();
		crystalMaterial.R = mat.AlbedoColor.R;
		crystalMaterial.G = mat.AlbedoColor.G;
		crystalMaterial.B = mat.AlbedoColor.B;
		crystalMaterial.A = mat.AlbedoColor.A;
		crystalMaterial.Roughness = mat.Roughness;
		crystalMaterial.Refraction = mat.RefractionScale;
		return crystalMaterial;
	}
	public StandardMaterial3D CrystalMaterialToGodotMaterial(Crystal.CrystalMaterial mat)
	{
		StandardMaterial3D output = (StandardMaterial3D)baseMaterial.Duplicate(true);
		output.AlbedoColor = new Color(mat.R, mat.G, mat.B, mat.A);
		output.Roughness = mat.Roughness;
		output.RefractionScale = mat.Refraction;
		return output;
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
		return mesh;
	}

	public void StartMeshUpdate()
	{
		if (updatedThisFrame)
			return;
		updatedThisFrame = true;
		if (crystalGenerationThread != null && crystalGenerationThread.IsAlive)
		{
			//Got new data to calculate, but busy with old data
			//So add method to generate new crystal once old one is done
			void CalculateNewest()
			{
				//note: OnGenerationFinished is only called after crystalGenerationThread is done
				OnGenerationFinished -= CalculateNewest;
				StartMeshUpdate();
			}
			OnGenerationFinished += CalculateNewest;
			return;
		}
		crystalGenerationThread = new Thread(GenerateCrystal);
		crystalGenerationThread.Start();
	}

	public void GenerateCrystal()
	{
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

		try
		{
			crystal = new Crystal(normalsd, doubles, _pointGroup);
		}
		catch (Exception e)//if we don't catch within this thread, 
		{//then it goes unhandled and crashes the entire application
		 //With normal exceptions in godot, we don't need to do this
			GD.PrintErr(e.Message + " " + e.StackTrace);
		}
		CallDeferred("FinishMeshUpdate");//Since we change gd stuff here, gd requires we do this in a deferred call
	}
	public void FinishMeshUpdate()
	{
		EmitSignal("OnGenerationFinished");

		ArrayMesh mesh = CreateArrayMeshFromCrystal(crystal);
		Mesh = mesh;

		UpdateMaterials();
	}

	public void UpdateMaterials()
	{
		if (crystal.initialNormals.Count > materialList.Count)
		{
			for (int i = materialList.Count; i < crystal.initialNormals.Count; i++)
			{
				materialList.Add((StandardMaterial3D)baseMaterial.Duplicate(true));
			}
		}
		for (int materialIndex = 0; materialIndex < materialList.Count; materialIndex++)
		{
			int surfaceOverrideIndex = crystal.FindSurfaceMadeByIndex(materialIndex);

			if (surfaceOverrideIndex != -1 && surfaceOverrideIndex < mesh.GetSurfaceCount())
			{
				SetSurfaceOverrideMaterial(surfaceOverrideIndex, materialList[materialIndex]);
			}
		}

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
	public void UpdateAxes(SymmetryOperations.PointGroup pointGroup)
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
