using Godot;
using System.Collections.Generic;
using System.Linq;
public partial class GemGUI : Control
{
	[Export]
	WorldEnvironment environment;
	[Export]
	Camera3D camera;
	[Export]
	Node3D crystalParent, axes;
	[Export]
	CrystalGameObject crystal;
	[Export]
	StandardMaterial3D baseMaterial;
	[Export]
	Label dataText;
	[Export]
	OptionButton crystalSystem;
	[Export]
	SpinBox[] crystalParams = new SpinBox[6];//a, b, c lengths, alpha, beta, gamma in degrees
	[Export]
	Range scaleSlider;
	private Crystal loadedCrystal;

	private Color baseColor;
	private bool rotate = false;
	private bool autoUpdate = true;
	private bool updatedParamsThisFrame = false;
	private bool updatedNormsThisFrame = false;
	float mouseSens = 1;
	float distance = 4;
	[Export]
	Container vectorList;
	[Export]
	public PackedScene spinBox;
	private List<VectorListItem> listItems = new List<VectorListItem>();

	//TODO controller support
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//Set defaults
		baseColor = baseMaterial.AlbedoColor;
		AddNewNormal();
		AddNewNormal();
		AddNewNormal();
		AddNewNormal();
		listItems[0].SetValues(new(3, 1, 0), 1, baseColor);
		listItems[1].SetValues(new(0, 5, 1), .9f, baseColor);
		listItems[2].SetValues(new(-1, -1, -1), 1, baseColor);
		listItems[3].SetValues(new(-2, 0, -1), 1, baseColor);

		foreach (VectorListItem item in listItems)
		{
			SetNormals(item.index, item.vector, item.distance);
		}
		crystalSystem.Select(22);//-3rhomb
		SetCrystalSystem(22);
		crystal.OnGenerationFinished += FirstUpdate;
		crystal.OnGenerationFinished += UpdateCrystalStatistics;
		crystal.CallDeferred("StartMeshUpdate");

		void FirstUpdate()
		{
			crystal.OnGenerationFinished -= FirstUpdate;
			crystal.UpdateAxes(crystal.PointGroup);
			UpdateCrystalStatistics(skipWidgetVisibleCheck: true);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (rotate)
			crystalParent.RotateY(.1f * (float)delta);
		updatedParamsThisFrame = false;
		updatedNormsThisFrame = false;
	}

	public void SaveCrystal(string path)
	{
		crystal.SaveCrystal(path);
	}

	/// <summary>
	/// Loads a crystal in json format into the viewer
	/// </summary>
	/// <param name="path">The file path of the .json crystal to load</param>
	public void LoadCrystal(string path)
	{
		void UpdateAfterLoad()
		{
			crystal.OnGenerationFinished -= UpdateAfterLoad;
			crystal.UpdateFromParameters();//Reapply unit cell transformation in case it is different
			UpdateCrystalStatistics(skipWidgetVisibleCheck: true);
		}
		//Make sure we don't prematurely update
		updatedNormsThisFrame = true;
		updatedParamsThisFrame = true;

		//Do all the initial loading and generate crystal
		loadedCrystal = crystal.LoadCrystal(path);
		//Duplicate the loaded material list as the crystal's one will be overwritten
		List<StandardMaterial3D> loadedMaterialList = new List<StandardMaterial3D>(crystal.materialList);

		//Remove old normals
		RemoveAllNormals();
		//Replace old normals with new ones
		for (int i = 0; i < loadedCrystal.initialNormals.Count; i++)
		{
			Vector3 normal = GodotCompatability.DoubleToGD(loadedCrystal.
				initialNormals[i]);
			//GD.Print("Loaded Normal: " + normal);
			float distance = (float)loadedCrystal.initialDistances[i];
			//GD.Print("MatlistLoadCrystal " + loadedMaterialList.Count);
			AddNewNormal(normal, distance, loadedMaterialList[i].AlbedoColor);
			crystal.Normals[i] = normal;
			crystal.Distances[i] = distance;

		}
		crystal.materialList = loadedMaterialList;
		crystal.PointGroup = loadedCrystal.pointGroup;
		crystalSystem.Select(SpaceGroupToSpinnerIndex(loadedCrystal.pointGroup));
		crystalParams[0].SetValueNoSignal(crystal.aLength);
		crystalParams[1].SetValueNoSignal(crystal.bLength);
		crystalParams[2].SetValueNoSignal(crystal.cLength);
		crystalParams[3].SetValueNoSignal(crystal.alpha);
		crystalParams[4].SetValueNoSignal(crystal.beta);
		crystalParams[5].SetValueNoSignal(crystal.gamma);

		crystal.OnGenerationFinished += UpdateAfterLoad;
		crystal.StartMeshUpdate();
		//^ this is the line that actually updates the crystal
	}


	public void ExportSTL(string path)
	{
		crystal.ExportSTL(path);
	}
	public void ExportOBJ(string path)
	{
		crystal.ExportOBJ(path);
	}
	public void GetDragInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseEvent && Input.IsMouseButtonPressed(MouseButton.Left))
		{
			Vector2 mouseDir = mouseEvent.Relative * mouseSens;
			mouseDir *= Mathf.Pi / 360f;
			crystalParent.RotateY(mouseDir.X);//Left/right
			crystalParent.RotateX(-mouseDir.Y);//Up/Down
		}
		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
		{
			float dif = 0;
			if (mouseButton.ButtonIndex == MouseButton.WheelUp)
				dif = .1f;
			if (mouseButton.ButtonIndex == MouseButton.WheelDown)
				dif = -.1f;
			scaleSlider.Value += dif;
		}
	}

	public void SetAutoUpdate(bool update)
	{
		autoUpdate = update;
	}
	public void SetAutoRotate(bool rotate)
	{
		this.rotate = rotate;
	}
	public void SetCameraOrthogonal(bool ortho)
	{
		camera.Projection = ortho ? Camera3D.ProjectionType.Orthogonal : Camera3D.ProjectionType.Perspective;
		if (ortho == true)
		{
			camera.SetOrthogonal(distance, .001f, distance * 2);
		}//TODO figure out rotation for orthogonal or maybe just move it
	}
	public void SetCameraRotation(string axis)
	{
		switch (axis)
		{
			case "a":
				crystalParent.LookAt(-crystal.Basis.X, crystal.Basis.Y);
				break;
			case "b":
				crystalParent.LookAt(-crystal.Basis.Y, crystal.Basis.Z);
				break;
			case "c":
				crystalParent.LookAt(crystal.Basis.Z, crystal.Basis.Y);
				break;
			case "abc"://TODO this doesn't work correctly
				crystalParent.LookAt(-(crystal.Basis.X + crystal.Basis.Y - crystal.Basis.Z), crystal.Basis.Y);
				break;
		}
	}

	public void SetScale(float scale) { crystalParent.Scale = Vector3.One * scale; }
	public void SetCameraDistance(float distance)//Unused- We set scale now.
	{//3-20
		distance = (1 - distance) * (1 - distance) * 13 + 3;
		this.distance = distance;
		camera.Far = distance * 2;
		camera.Position = new(0, 0, -distance);
		if (camera.Projection == Camera3D.ProjectionType.Orthogonal)
			camera.SetOrthogonal(distance, .001f, distance * 2);
	}
	public void SetAxisVisibility(bool visible) { axes.Visible = visible; }
	public void SetAxisScale(float scale) { axes.Scale = Vector3.One * scale; }
	public void SetCrystalSystem(int num)
	{
		//Because group separators take an index, we go by 10s
		crystal.PointGroup = (SymmetryOperations.PointGroup)(crystalSystem.GetItemId(num) / 10);

		float[] parameters = SymmetryOperations.GetParametersForPointGroup(crystal.PointGroup);
		for (int i = 0; i < 6; i++)
			crystalParams[i].SetValueNoSignal(parameters[i]);
		void UpdateParamsOnce()
		{
			crystal.OnGenerationFinished -= UpdateParamsOnce;
			crystal.UpdateAxes(crystal.PointGroup);
			CheckParamUpdate();
		}
		crystal.OnGenerationFinished += UpdateParamsOnce;
		crystal.CallDeferred("StartMeshUpdate");
	}
	public void SetCullBackface(bool cull)
	{
		StandardMaterial3D mat = (StandardMaterial3D)crystal.MaterialOverride;
		mat.CullMode = cull ? BaseMaterial3D.CullModeEnum.Back : BaseMaterial3D.CullModeEnum.Disabled;
	}
	public void SetColor(Color color, int index = -1)
	{
		if (index == -1)
		{
			baseColor = color;
			for (int i = 0; i < crystal.materialList.Count; i++)
				SetColor(color, i);
			foreach (VectorListItem item in listItems)
			{
				item.colorButton.Color = color;
			}
		}
		else
		{
			StandardMaterial3D material = crystal.materialList[index];
			material.AlbedoColor = color;
		}
	}
	public void SetRefraction(double refraction, int index = -1)
	{
		if (index == -1)
		{
			for (int i = 0; i < crystal.materialList.Count; i++)
				SetRefraction(refraction, i);

		}
		else
		{
			StandardMaterial3D material = crystal.materialList[index];
			material.RefractionEnabled = refraction != 0;
			material.RefractionScale = (float)refraction;
		}
	}
	public void SetRoughness(double roughness, int index = -1)//NOTE: Floats in godot signals are doubles in C#
	{
		if (index == -1)
		{
			for (int i = 0; i < crystal.materialList.Count; i++)
				SetRoughness(roughness, i);
		}
		else
		{
			StandardMaterial3D material = crystal.materialList[index];
			material.Roughness = (float)roughness;
		}
	}
	public void SetBackgroundImage(string imagePath)
	{
		if (imagePath == "" || imagePath == null)
		{
			((PanoramaSkyMaterial)environment.Environment.Sky.SkyMaterial).Panorama = ResourceLoader.Load<CompressedTexture2D>("res://Assets/1008231038_HDR.jpg");
			//We use resourceloader for internal files. 
			//Image.LoadFromFile WILL work in editor for res:// files, 
			//but once it's exported it won't work.
			return;
		}
		try
		{
			((PanoramaSkyMaterial)environment.Environment.Sky.SkyMaterial).Panorama = ImageTexture.CreateFromImage(Image.LoadFromFile(imagePath));
		}
		catch (System.Exception)
		{
		}
	}
	public void UpdateCrystalStatistics() => UpdateCrystalStatistics(skipWidgetVisibleCheck: false);//For easier signal calling
	public void UpdateCrystalStatistics(bool skipWidgetVisibleCheck = false)
	{
		if (dataText.IsVisibleInTree() == false && skipWidgetVisibleCheck == false)//These calculations can be expensive so we don't do it if we don't need to.
		{
			return;//We call this method manually when the data tab is switched onto so that the data is always fresh when visible. After that we auto update.
		}
		string areaString = "";
		double[] areas = crystal.GetSurfaceAreaGroups();
		for (int i = 0; i < areas.Length; i++)
			areaString += $"Group {i + 1} area: {System.Math.Round(areas[i], 4)}\n";

		dataText.Text = "Shape class: " + crystal.GetShapeClass() + "\n" +
						"Volume: " + System.Math.Round(crystal.GetVolume(), 4) + "\n" +
						"Surface Area: " + System.Math.Round(crystal.GetSurfaceArea(), 4) + "\n" +
						"Number of surfaces: " + crystal.GetSurfaceCount() + "\n"
						+ areaString;

	}
	private void CheckParamUpdate()
	{
		if (autoUpdate == false || updatedParamsThisFrame)
			return;
		updatedParamsThisFrame = true;

		crystal.CallDeferred("UpdateFromParameters");
	}
	private void CheckNormUpdate()
	{
		if (autoUpdate == false || updatedNormsThisFrame)
			return;
		updatedNormsThisFrame = true;

		crystal.CallDeferred("StartMeshUpdate");
	}
	public void SetA(float a) { crystal.aLength = a; CheckParamUpdate(); }
	public void SetB(float b) { crystal.bLength = b; CheckParamUpdate(); }
	public void SetC(float c) { crystal.cLength = c; CheckParamUpdate(); }
	public void SetAlpha(float alpha) { crystal.alpha = alpha; CheckParamUpdate(); }
	public void SetBeta(float beta) { crystal.beta = beta; CheckParamUpdate(); }
	public void SetGamma(float gamma) { crystal.gamma = gamma; CheckParamUpdate(); }

	/// <summary>
	/// Creates a new normal with default values
	/// </summary>
	public void AddNewNormal()
	{
		AddNewNormal(Vector3.One, 1, baseColor);
	}

	/// <summary>
	/// Creates a new normal and sets values after creation 
	/// without triggering any signals
	/// </summary>
	/// <param name="normal">Direction of the normal</param>
	/// <param name="distance">Distance of the normal from the center</param>
	/// <param name="color">Color of this normal and all reflected faces</param>
	public void AddNewNormal(Vector3 normal, float distance, Color color)
	{
		VectorListItem listItem = new VectorListItem();
		listItem.index = listItems.Count;
		listItems.Add(listItem);

		SpinBox h = (SpinBox)spinBox.Instantiate();
		SpinBox k = (SpinBox)spinBox.Instantiate();
		SpinBox l = (SpinBox)spinBox.Instantiate();
		SpinBox d = (SpinBox)spinBox.Instantiate();
		d.MinValue = 0.01f;
		d.MaxValue = 2;
		ColorPickerButton c = new ColorPickerButton();
		Button x = new Button();
		x.Text = "X";

		listItem.boxes[0] = h;
		listItem.boxes[1] = k;
		listItem.boxes[2] = l;
		listItem.boxes[3] = d;
		listItem.colorButton = c;
		listItem.button = x;

		//Material already exists and may hold color we added previously
		//We want to reflect that in the GUI so we set the color of the element we add to that color instead of the default color.
		if (listItem.index < crystal.materialList.Count)
			color = crystal.materialList[listItem.index].AlbedoColor;

		listItem.SetValues(normal, distance, color);

		SetNormals(listItem.index, listItem.vector, listItem.distance);

		h.ValueChanged += listItem.SetX;//Callback methods to update when number is changed
		k.ValueChanged += listItem.SetY;
		l.ValueChanged += listItem.SetZ;
		d.ValueChanged += listItem.SetDistance;
		c.ColorChanged += listItem.SetColor;

		x.Pressed += listItem.Remove;

		listItem.Update += SetNormals;
		listItem.UpdateColor += SetColor;
		listItem.Delet += RemoveNormals;

		vectorList.AddChild(h);
		vectorList.AddChild(k);
		vectorList.AddChild(l);
		vectorList.AddChild(d);
		vectorList.AddChild(c);
		vectorList.AddChild(x);
	}

	/// <summary>
	/// Sets the values of a face
	/// </summary>
	/// <param name="idx">Index of the normal to set</param>
	/// <param name="normal">Direction the normal is facing</param>
	/// <param name="distance">Distance of the face from the center</param>
	public void SetNormals(int idx, Vector3 normal, float distance)
	{
		if (normal.IsZeroApprox() || distance == 0)
			return;
		if (crystal.Normals.Length <= idx || crystal.Distances.Length <= idx)
		{
			//No more space in crystal's arrays, so we add more slots for a new one and copy the arrays
			Vector3[] newNormals = new Vector3[idx + 2];//TODO check why I add two here
			float[] newDistances = new float[idx + 2];
			crystal.Normals.CopyTo(newNormals, 0);
			crystal.Distances.CopyTo(newDistances, 0);
			crystal.Normals = newNormals;
			crystal.Distances = newDistances;
		}
		crystal.Normals[idx] = normal;
		crystal.Distances[idx] = distance;
		CheckNormUpdate();
	}

	/// <summary>
	/// Remove a normal from the list. Shift all later normals back one.
	/// </summary>
	/// <param name="idx">Index of the normal to remove</param>
	public void RemoveNormals(int idx)
	{
		listItems[idx].boxes[0].QueueFree();
		listItems[idx].boxes[1].QueueFree();
		listItems[idx].boxes[2].QueueFree();
		listItems[idx].boxes[3].QueueFree();
		listItems[idx].colorButton.QueueFree();
		listItems[idx].button.QueueFree();
		listItems[idx].QueueFree();

		for (int i = idx + 1; i < listItems.Count; i++)
			listItems[i].index--;
		listItems.RemoveAt(idx);

		List<Vector3> normals = crystal.Normals.ToList<Vector3>();
		List<float> distances = crystal.Distances.ToList<float>();
		normals.RemoveAt(idx);
		distances.RemoveAt(idx);
		crystal.Normals = normals.ToArray();
		crystal.Distances = distances.ToArray();

		if (crystal.materialList.Count > idx)
			crystal.materialList.RemoveAt(idx);

		CheckNormUpdate();
	}

	/// <summary>
	/// Removes all normals from the list of faces
	/// </summary>
	public void RemoveAllNormals()
	{
		while (listItems.Count > 0)
			RemoveNormals(listItems.Count - 1);
	}

	/// <summary>
	/// Converts between space group numbers to its corresponding spot in the UI spinner, which has gaps for the names of each group.
	/// </summary>
	/// <param name="group">Initial group enum</param>
	/// <returns>The group's index in the UI spinner</returns>
	private int SpaceGroupToSpinnerIndex(SymmetryOperations.PointGroup group)
	{
		int number = (int)group;
		switch (number)
		{
			case 0:
				return number;
			case 1 or 2:
				return number + 1;//Triclinic
			case 3 or 4 or 5:
				return number + 2;//Monoclinic. Again monoclinic has b as the wacky axis
			case 6 or 7 or 8:
				return number + 3;//Orthorhombic
			case >= 9 and <= 15:
				return number + 4; //Tetragonal
			case >= 16 and <= 20:
				return number + 5;
			case >= 21 and <= 28:
				return number + 6; //Trigonal
			case >= 29 and <= 35:
				return number + 7; //Hexagonal
			case >= 36 and <= 40:
				return number + 8;
			default:
				return number;
		}
	}
}
