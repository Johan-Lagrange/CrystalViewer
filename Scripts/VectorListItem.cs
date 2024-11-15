using Godot;

/// <summary>
/// Used to keep track of face values in a list where entries can be added or removed.
/// Has h, j, k values for x, y, z, and distance for magnitude.
/// Has spin boxes for each value that have a callback when changed, as well as an x to delete the entry.
/// </summary>
public partial class VectorListItem : Node
{
	[Signal]
	public delegate void UpdateEventHandler(int idx, Vector3 normal, float distance); [Signal]
	public delegate void UpdateColorEventHandler(Color c, int idx);

	[Signal]
	public delegate void DeletEventHandler(int idx);
	public Vector3 vector;
	public float distance;
	public int index;
	public SpinBox[] boxes = new SpinBox[4];
	//TODO color: Handle when face doesn't correspond to color.
	public ColorPickerButton colorButton;
	public Button button;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	private void EmitUpdateSignal()
	{
		EmitSignal("Update", index, vector, distance);
	}
	private void EmitUpdateColorSignal(Color color)
	{
		EmitSignal("UpdateColor", color, index);
	}
	public void SetValues(Vector3 v, float d, Color c)
	{
		vector = v; distance = d;
		boxes[0].SetValueNoSignal(v.X);
		boxes[1].SetValueNoSignal(v.Y);
		boxes[2].SetValueNoSignal(v.Z);
		boxes[3].SetValueNoSignal(d);
		colorButton.Color = c;
	}
	public void SetX(double x) { vector.X = (float)x; EmitUpdateSignal(); }
	public void SetY(double y) { vector.Y = (float)y; EmitUpdateSignal(); }
	public void SetZ(double z) { vector.Z = (float)z; EmitUpdateSignal(); }
	public void SetDistance(double d) { this.distance = (float)d; EmitUpdateSignal(); }
	public void SetColor(Color c) { EmitUpdateColorSignal(c); }
	public void Remove() { EmitSignal("Delet", index); }
}
