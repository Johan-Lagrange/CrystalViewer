using Godot;

public partial class VectorListItem : Node
{
	[Signal]
	public delegate void UpdateEventHandler(int idx, Vector3 normal, float distance);
	[Signal]
	public delegate void DeletEventHandler(int idx);
	public Vector3 vector = Vector3.One;
	public float distance = 1;
	public int index = 0;
	public SpinBox[] boxes = new SpinBox[4];
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

	// private void EmitUpdate()
	// {
	// 	;
	// }
	public void SetValues(Vector3 v, float d)
	{
		vector = v; distance = d;
		boxes[0].SetValueNoSignal(v.X);
		boxes[1].SetValueNoSignal(v.Y);
		boxes[2].SetValueNoSignal(v.Z);
		boxes[3].SetValueNoSignal(d);
	}
	public void SetX(double x) { vector.X = (float)x; EmitUpdateSignal(); }
	public void SetY(double y) { vector.Y = (float)y; EmitUpdateSignal(); }
	public void SetZ(double z) { vector.Z = (float)z; EmitUpdateSignal(); }
	public void SetDistance(double d) { this.distance = (float)d; EmitUpdateSignal(); }
	public void Remove() { EmitSignal("Delet", index); }
}
