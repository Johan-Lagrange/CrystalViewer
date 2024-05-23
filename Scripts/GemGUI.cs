using Godot;
using System.Collections.Generic;
using System.Linq;
public partial class GemGUI : Control
{
	[Export]
	Camera3D camera;
	[Export]
	Node3D crystalParent, axes;
	[Export]
	CrystalGameObject crystal;
	[Export]
	Label dataText;
	[Export]
	OptionButton crystalSystem;
	[Export]
	SpinBox[] crystalParams = new SpinBox[6];
	[Export]
	Range scaleSlider;
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
		AddNewNormal();
		AddNewNormal();
		AddNewNormal();
		AddNewNormal();
		listItems[0].SetValues(new(3, 1, 0), 1);//Defaults
		listItems[1].SetValues(new(0, 5, 1), .9f);
		listItems[2].SetValues(new(-1, -1, -1), 1);
		listItems[3].SetValues(new(-2, 0, -1), 1);
		foreach (VectorListItem item in listItems)
		{
			SetNormals(item.index, item.vector, item.distance);
		}
		crystalSystem.Select(22);//-3rhomb
		SetCrystalSystem(22);
		crystal.CallDeferred("UpdateFromParameters");
		crystal.CallDeferred("UpdateMesh");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (rotate)
			crystalParent.RotateY(.1f * (float)delta);
		updatedParamsThisFrame = false;
		updatedNormsThisFrame = false;
	}

	public void ExportSTL(string output)
	{
		crystal.ExportSTL(output);
	}
	public void ExportOBJ(string output)
	{
		crystal.ExportOBJ(output);
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
		crystal.PointGroup = (SymmetryOperations.PointGroup)(crystalSystem.GetItemId(num) / 10);
		//GD.Print(crystal.PointGroup.ToString());
		float[] parameters = SymmetryOperations.GetParametersForPointGroup(crystal.PointGroup);
		for (int i = 0; i < 6; i++)
			crystalParams[i].SetValueNoSignal(parameters[i]);
		CheckParamUpdate();
		crystal.CallDeferred("UpdateMesh");
	}
	public void SetCullBackface(bool cull)
	{
		StandardMaterial3D mat = (StandardMaterial3D)crystal.MaterialOverride;
		mat.CullMode = cull ? BaseMaterial3D.CullModeEnum.Back : BaseMaterial3D.CullModeEnum.Disabled;
	}
	public void SetColor(Color color)
	{
		StandardMaterial3D material = (StandardMaterial3D)crystal.MaterialOverride;
		material.AlbedoColor = color;
	}
	public void SetRefraction(double refraction)
	{
		StandardMaterial3D material = (StandardMaterial3D)crystal.MaterialOverride;
		material.RefractionEnabled = refraction != 0;
		material.RefractionScale = (float)refraction;
	}
	public void SetRoughness(double roughness)//NOTE: Floats in godot signals are doubles in C#
	{
		StandardMaterial3D material = (StandardMaterial3D)crystal.MaterialOverride;
		material.Roughness = (float)roughness;
	}
	public void UpdateCrystalData(int param = -1)//1 = data tab. We call this from the tab menu sometimes
	{
		if (param != 1 && dataText.IsVisibleInTree() == false)//These calculations can be expensive so we don't do it if we don't need to.
			return;//We call this method manually when the data tab is switched onto so that the data is always fresh when visible. After that we auto update.
		dataText.Text = "Shape class: " + crystal.GetShapeClass() + "\n" +
						"Volume: " + crystal.GetVolume() + "\n" +
						"Surface Area: " + crystal.GetSurfaceArea() + "\n" +
						"Number of surfaces: " + crystal.GetSurfaceCount();
	}
	private void CheckParamUpdate()
	{
		if (autoUpdate == false || updatedParamsThisFrame)
			return;
		updatedParamsThisFrame = true;

		crystal.CallDeferred("UpdateFromParameters");
		CallDeferred("UpdateCrystalData", 0);
	}
	private void CheckNormUpdate()
	{
		if (autoUpdate == false || updatedNormsThisFrame)
			return;
		updatedNormsThisFrame = true;

		crystal.CallDeferred("UpdateMesh");
		CallDeferred("UpdateCrystalData", 0);
	}
	public void SetA(float a) { crystal.aLength = a; CheckParamUpdate(); }
	public void SetB(float b) { crystal.bLength = b; CheckParamUpdate(); }
	public void SetC(float c) { crystal.cLength = c; CheckParamUpdate(); }
	public void SetAlpha(float alpha) { crystal.alpha = alpha; CheckParamUpdate(); }
	public void SetBeta(float beta) { crystal.beta = beta; CheckParamUpdate(); }
	public void SetGamma(float gamma) { crystal.gamma = gamma; CheckParamUpdate(); }
	public void AddNewNormal()
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
		Button x = new Button();
		x.Text = "X";

		listItem.boxes[0] = h;
		listItem.boxes[1] = k;
		listItem.boxes[2] = l;
		listItem.boxes[3] = d;
		listItem.button = x;

		listItem.SetValues(Vector3.One, 1);
		SetNormals(listItem.index, listItem.vector, listItem.distance);

		h.ValueChanged += listItem.SetX;//Callback methods to update when number is changed
		k.ValueChanged += listItem.SetY;
		l.ValueChanged += listItem.SetZ;
		d.ValueChanged += listItem.SetDistance;
		x.Pressed += listItem.Remove;

		listItem.Update += SetNormals;
		listItem.Delet += RemoveNormals;

		vectorList.AddChild(h);
		vectorList.AddChild(k);
		vectorList.AddChild(l);
		vectorList.AddChild(d);
		vectorList.AddChild(x);
	}
	public void SetNormals(int idx, Vector3 normal, float distance)
	{
		if (normal.IsZeroApprox() || distance == 0)
			return;
		if (crystal.Normals.Length <= idx || crystal.Distances.Length <= idx)
		{
			Vector3[] newNormals = new Vector3[idx + 2];
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
	public void RemoveNormals(int idx)
	{
		listItems[idx].boxes[0].QueueFree();
		listItems[idx].boxes[1].QueueFree();
		listItems[idx].boxes[2].QueueFree();
		listItems[idx].boxes[3].QueueFree();
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

		CheckNormUpdate();
	}
}
