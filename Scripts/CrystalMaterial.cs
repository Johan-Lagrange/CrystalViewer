public partial class Crystal
{
  public struct CrystalMaterial
  {
    //All values between 0 and 1 except refraction which is -1 to 1
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; }
    public float Roughness { get; set; }
    public float Refraction { get; set; }
  }
}