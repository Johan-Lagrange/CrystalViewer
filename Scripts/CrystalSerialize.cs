public partial class Crystal
{
  public class CrystalSerialize
  {
    public string Name { get; set; }
    public string SpaceGroup { get; set; }
    public Vector3d[] Normals { get; set; }
    public double[] Distances { get; set; }
    public CrystalMaterial[] Materials { get; set; }
    public Vector3d AxisAngles { get; set; }
    public Vector3d AxisLengths { get; set; }
  }
}