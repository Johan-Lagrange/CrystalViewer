using System;
using System.Collections.Generic;
public static class SymmetryOperations
{
	public enum PointGroup
	{
		None,//This is just done as good practice.
		One, BarOne, //Triclinic indices 1-2
		Two, M, TwoSlashM, //Monoclinic indices 3-5
		TwoTwoTwo, MMTwo, MMM, //Orthorhombic indices 6-8
		Four, BarFour, FourSlashM, FourTwoTwo, FourMM, BarFourTwoM, FourSlashMMM, //Tetragonal indices 9-15
		ThreeRhombohedral, BarThreeRhombohedral, ThreeTwoRhombohedral, ThreeMRhombohedral, BarThreeMRhombohedral,//Rhombohedral indices 16-20
		ThreeHexagonal, BarThreeHexagonal, ThreeOneTwoHexagonal, ThreeTwoOneHexagonal, ThreeMOneHexagonal, ThreeOneMHexagonal, BarThreeOneMHexagonal, BarThreeMOneHexagonal, //Trigonal? indices 21-28
		Six, BarSix, SixSlashM, SixTwoTwo, SixMM, BarSixMTwo, SixSlashMMM, //Hexagonal indices 29-35
		TwoThree, MBarThree, FourThreeTwo, BarFourThreeM, MBarThreeM //Cubic indices 36-40
	};

	/// <summary>
	/// Gets the unit cell perameters from each point group. Numbers are arbitrary, but nice.
	/// </summary>
	/// <param name="pointGroup">The point group for which we are getting the matching unit cell parameters</param>
	/// <returns>A float[6]. [0],[1],[2], are the lengths of a, b, c, and [3],[4],[5] are the interaxial angles alpha, beta, gamma</returns>
	public static float[] GetParametersForPointGroup(PointGroup pointGroup)
	{
		switch ((int)pointGroup)
		{
			case 0 or 1 or 2:
				return new float[] { 1, 1.5f, 2, 30, 60, 80 };//Triclinic
			case 3 or 4 or 5:
				return new float[] { 1, 2, 1.5f, 90, 60, 90 };//Monoclinic. Again monoclinic has b as the wacky axis
			case 6 or 7 or 8:
				return new float[] { 1, 1.5f, 2, 90, 90, 90 };//Orthorhombic
			case >= 9 and <= 15:
				return new float[] { 1, 1, 1.5f, 90, 90, 90 }; //Tetragonal
			case >= 16 and <= 20:
				return new float[] { 1, 1, 1, 60, 60, 60 };
			case >= 21 and <= 35:
				return new float[] { 1, 1, 1.5f, 90, 90, 120 }; //Trigonal and Hexagonal
			case >= 36 and <= 40:
				return new float[] { 1, 1, 1, 90, 90, 90 };
			default:
				return new float[] { 1, 1, 1, 90, 90, 90 };
		}
	}

	public static string GetNameOfSymmetryClass(PointGroup group)
	{
		switch (group)
		{
			//Just good practice
			case PointGroup.None: return "Pedial";

			//Triclinic
			case PointGroup.One: return "Pedial";
			case PointGroup.BarOne: return "Pinacoidal";

			//Monoclinic
			case PointGroup.Two: return "Sphenoidal";
			case PointGroup.M: return "Domatic";
			case PointGroup.TwoSlashM: return "Prismatic";

			//Orthorhombic
			case PointGroup.TwoTwoTwo: return "Rhombic-disphenoidal";
			case PointGroup.MMTwo: return "Rhombic-pyramidal";
			case PointGroup.MMM: return "Rhombic-dipyramidal";

			//Tetragonal
			case PointGroup.Four: return "Tetragonal-Pyramidal";
			case PointGroup.BarFour: return "Tetragonal-disphenoidal";
			case PointGroup.FourSlashM: return "Tetragonal-dipyramidal";
			case PointGroup.FourTwoTwo: return "Tetragonal-trapezohedral";
			case PointGroup.FourMM: return "Ditetragonal-pyramidal";
			case PointGroup.BarFourTwoM: return "Tetragonal-scalenohedral";
			case PointGroup.FourSlashMMM: return "Ditetragonal-dipyramidal";

			//Hexagonal with rhombohedral axes
			case PointGroup.ThreeRhombohedral: return "Trigonal-pyramidal";
			case PointGroup.BarThreeRhombohedral: return "Rhombohedral";
			case PointGroup.ThreeTwoRhombohedral: return "Trigonal-trapezohedral";
			case PointGroup.ThreeMRhombohedral: return "Ditrigonal-pyramidal";
			case PointGroup.BarThreeMRhombohedral: return "Hexagonal-scalenohedral";

			//Hexagonal with hexagonal axes
			case PointGroup.ThreeHexagonal: return "Trigonal-pyramidal";
			case PointGroup.BarThreeHexagonal: return "Rhombohedral";
			case PointGroup.ThreeOneTwoHexagonal: return "Trigonal-trapezohedral";
			case PointGroup.ThreeTwoOneHexagonal: return "Trigonal-trapezohedral";
			case PointGroup.ThreeMOneHexagonal: return "Ditrigonal-pyramidal";
			case PointGroup.ThreeOneMHexagonal: return "Ditrigonal-pyramidal";
			case PointGroup.BarThreeOneMHexagonal: return "Hexagonal-scalenohedral";
			case PointGroup.BarThreeMOneHexagonal: return "Hexagonal-scalenohedral";

			//Hexagonal
			case PointGroup.Six: return "Hexagonal-pyramidal";
			case PointGroup.BarSix: return "Trigonal-dipyramidal";
			case PointGroup.SixSlashM: return "Hexagonal-dipyramidal";
			case PointGroup.SixTwoTwo: return "Hexagonal-trapezohedral";
			case PointGroup.SixMM: return "Dihexagonal-pyramidal";
			case PointGroup.BarSixMTwo: return "Ditrigonal-dipyramidal";
			case PointGroup.SixSlashMMM: return "Dihexagonal-dipyramidal";

			//Cubic
			case PointGroup.TwoThree: return "Tetaroidal";
			case PointGroup.MBarThree: return "Diploidal";
			case PointGroup.FourThreeTwo: return "Gyroidal";
			case PointGroup.BarFourThreeM: return "Hextetrahedral";
			case PointGroup.MBarThreeM: return "Hexoctahedral";

			//More good practice
			default: return "";
		}
	}

	/// <summary>
	/// The list of EVERY POSSIBLE OPERATION within a point group that will yield the same shape back.
	/// All are callable as functions
	/// </summary>
	/// <value>The list of symmetry operation functions to run to generate the given point group</value>
	public static readonly Func<Vector3d, Vector3d>[][] PointGroupPositions =
	{
		new[] {Identity}, //Null

		new[] {Identity}, //One
		new[] {Inv}, //BarOne

		new[] {DiY},//Monoclinic has "Y" as its unique axis because monoclinic is A STUPID CRYSTAL SYSTEM //Two
		new[] {MirY}, //M
		new[] {DiY, Inv, MirY}, //TwoSlashM

		new[] {DiZ, DiY, DiX}, //TwoTwoTwo
		new[] {DiZ, MirY, MirX}, //MMTwo
		new[] {DiZ, DiY, DiX, Inv, MirZ, MirY, MirX}, //MMM

		new[] {DiZ, TetZPos, TetZNeg}, //Four
		new[] {DiZ, InvTetZPos, InvTetZNeg}, //BarFour
		new[] {DiZ, TetZPos, TetZNeg, Inv, MirZ, InvTetZPos, InvTetZNeg}, //FourSlashM
		new[] {DiZ, TetZPos, TetZNeg, DiY, DiX}, //FourTwoTwo
		new[] {DiZ, TetZPos, TetZNeg, MirY, MirX, MirXY, MirX_Y, MirZ}, //FourMM
		new[] {DiZ, InvTetZPos, InvTetZNeg, DiY, DiX, MirXY, MirX_Y}, //BarFourTwoM
		new[] {DiZ, TetZPos, TetZNeg, DiY, DiX, DiXY, DiX_Y, Inv, MirZ, InvTetZPos, InvTetZNeg, MirY, MirX, MirXY, MirX_Y}, //FourSlashMMM

		new[] {TriXYZPos, TriXYZNeg}, //Three Rhombohedral
		new[] {TriXYZPos, TriXYZNeg, Inv, InvTriXYZPos, InvTriXYZNeg}, //BarThree Rhombohedral
		new[] {TriXYZPos, TriXYZNeg, Di_XZ, DiX_Y, DiY_Z},//ThreeTwo Rhombohedral
		new[] {TriXYZPos, TriXYZNeg, Mir_XZ, MirX_Y, MirY_Z},//ThreeM Rhombohedral
		new[] {TriXYZPos, TriXYZNeg, Di_XZ, DiX_Y, DiY_Z, Inv, InvTriXYZPos, InvTriXYZNeg, Mir_XZ, MirX_Y, MirY_Z},//BarThreeM Rhombohedral

		//Trigonal?
		new[] {TriZPos, TriZNeg}, //Three Hexagonal
		new[] {TriZPos, TriZNeg, Inv, InvTriZPos, InvTriZNeg}, //BarThree Hexagonal
		new[] {TriZPos, TriZNeg, DiX_Y, DiXYY, DiXXY},//ThreeOneTwo Hexagonal
		new[] {TriZPos, TriZNeg, DiXY, DiX, DiY},//ThreeTwoOne Hexagonal
		new[] {TriZPos, TriZNeg, MirXY, MirX, MirY},//ThreeMOne Hexagonal
		new[] {TriZPos, TriZNeg, MirX_Y, MirXYY, MirXXY},//ThreeOneM Hexagonal
		new[] {TriZPos, TriZNeg, DiX_Y, DiXYY, DiXXY, Inv, InvTriZPos, InvTriZNeg, MirX_Y, MirXYY, MirXXY},//BarThreeOneM Hexagonal
		new[] {TriZPos, TriZNeg, DiXY, DiX, DiY, Inv, InvTriZPos, InvTriZNeg, MirXY, MirX, MirY},//BarThreeMOne Hexagonal

		new[] {TriZPos, TriZNeg, DiZ, HexZNeg, HexZPos}, //Six
		new[] {TriZPos, TriZNeg, MirZ, InvHexZNeg, InvHexZPos}, //BarSix
		new[] {TriZPos, TriZNeg, DiZ, HexZNeg, HexZPos, Inv, InvTriZPos, InvTriZNeg, MirZ, InvHexZNeg, InvHexZPos}, //SixSlashM
		new[] {TriZPos, TriZNeg, DiZ, HexZNeg, HexZPos, DiXY, DiX, DiY, DiX_Y, DiXYY, DiXXY}, //SixTwoTwo
		new[] {TriZPos, TriZNeg, DiZ, HexZNeg, HexZPos, MirXY, MirX, MirY, MirX_Y, MirXYY, MirXXY}, //SixMM
		new[] {TriZPos, TriZNeg, MirZ, InvHexZNeg, InvHexZPos, MirXY, MirX, MirY, DiX_Y, DiXYY, DiXXY}, //BarSixMTwo
		new[] {TriZPos, TriZNeg, DiZ, HexZNeg, HexZPos, DiXY, DiX, DiX_Y, DiXYY, DiXXY, Inv, InvTriZPos, InvTriZNeg, MirZ, InvHexZNeg, InvHexZPos, MirXY, MirX, MirY, MirX_Y, MirXYY, MirXXY}, //SixSlashMMM

		new[] {DiZ, DiY, DiX, TriXYZPos, Tri_XY_ZPos, TriX_Y_ZPos, Tri_X_YZPos, TriXYZNeg, TriX_Y_ZNeg, Tri_X_YZNeg, Tri_XY_ZNeg}, //TwoThree
		new[] {DiZ, DiY, DiX, TriXYZPos, Tri_XY_ZPos, TriX_Y_ZPos, Tri_X_YZPos, TriXYZNeg, TriX_Y_ZNeg, Tri_X_YZNeg, Tri_XY_ZNeg, Inv, MirZ, MirY, MirX, InvTriXYZPos, InvTri_XY_ZPos, InvTriX_Y_ZPos, InvTri_X_YZPos, InvTriXYZNeg, InvTriX_Y_ZNeg, InvTri_X_YZNeg, InvTri_XY_ZNeg}, //MBarThree
		new[] {DiZ, DiY, DiX, TriXYZPos, Tri_XY_ZPos, TriX_Y_ZPos, Tri_X_YZPos, TriXYZNeg, TriX_Y_ZNeg, Tri_X_YZNeg, Tri_XY_ZNeg, DiXY, DiX_Y, TetZNeg, TetZPos, TetXNeg, TetXPos, DiYZ, DiY_Z, TetXPos, TetYPos, DiXZ, TetYNeg, Di_XZ}, //FourThreeTwo
		new[] {DiZ, DiY, DiX, TriXYZPos, Tri_XY_ZPos, TriX_Y_ZPos, Tri_X_YZPos, TriXYZNeg, TriX_Y_ZNeg, Tri_X_YZNeg, Tri_XY_ZNeg, MirX_Y, MirXY, InvTetZPos, InvTetZNeg, MirY_Z, InvTetXPos, InvTetXNeg, MirYZ, Mir_XZ, InvTetYNeg, MirXZ, InvTetYPos}, //BarFourThreeM
		new[] {DiZ, DiY, DiX, TriXYZPos, Tri_XY_ZPos, TriX_Y_ZPos, Tri_X_YZPos, TriXYZNeg, TriX_Y_ZNeg, Tri_X_YZNeg, Tri_XY_ZNeg, DiXY, DiX_Y, TetZNeg, TetZPos, TetXNeg, DiYZ, DiY_Z, TetXPos, TetYPos, DiXZ, TetYNeg, Di_XZ, Inv, MirZ, MirY, MirX, InvTriXYZPos, InvTri_XY_ZPos, InvTriX_Y_ZPos, InvTri_X_YZPos, InvTriXYZNeg, InvTriX_Y_ZNeg, InvTri_X_YZNeg, InvTri_XY_ZNeg, MirXY, MirX_Y, InvTetZNeg, InvTetZPos, InvTetXNeg, MirYZ, MirY_Z, InvTetXPos, InvTetYPos, MirXZ, InvTetYNeg, Mir_XZ}, //MBarThreeM
	};

	/// <summary>
	/// The list of callable operations that will generate a point group. 
	/// Yields the same result as PointGroupPositions with far fewer operations, as most can be redundant.
	/// An extreme example is 47 operations vs 5 for MBarThreeM
	/// </summary>
	/// <value>The list of symmetry operation functions to run to generate the given point group</value>
	public static readonly Func<Vector3d, Vector3d>[][] PointGroupOperations =
	{
		new[] {Identity},//0

		new[] {Identity}, //1 One
		new[] {Inv}, //2 BarOne

		new[] {DiY},//3 Monoclinic has "Y" as its unique axis because monoclinic is A STUPID CRYSTAL SYSTEM //Two
		new[] {MirY}, //4 M
		new[] {DiY, Inv}, //5 TwoSlashM

		new[] {DiZ, DiY}, //6 TwoTwoTwo
		new[] {DiZ, MirY}, //7 MMTwo
		new[] {DiZ, DiY, Inv}, //8 MMM

		new[] {DiZ, TetZPos}, //9 Four
		new[] {DiZ, InvTetZPos}, //10 BarFour
		new[] {DiZ, TetZPos, Inv}, //11 FourSlashM
		new[] {DiZ, TetZPos, DiY}, //12 FourTwoTwo
		new[] {DiZ, TetZPos, MirY}, //13 FourMM
		new[] {DiZ, InvTetZPos, DiY}, //14 BarFourTwoM
		new[] {DiZ, TetZPos, DiY, Inv}, //15 FourSlashMMM

		new[] {TriXYZPos, TriXYZNeg}, //16 Three Rhombohedral
		new[] {TriXYZPos, TriXYZNeg, Inv }, //17 BarThree Rhombohedral
		new[] {TriXYZPos, TriXYZNeg, DiX_Y}, //18 ThreeTwo Rhombohedral
		new[] {TriXYZPos, TriXYZNeg, MirX_Y},//19 ThreeM Rhombohedral
		new[] {TriXYZPos, TriXYZNeg, DiX_Y, Inv},//20 BarThreeM Rhombohedral

		new[] {TriZPos, TriZNeg}, //21 Three Hexagonal
		new[] {TriZPos, TriZNeg, Inv}, //22 BarThree Hexagonal
		new[] {TriZPos, TriZNeg, DiX_Y}, //23 ThreeOneTwo Hexagonal
		new[] {TriZPos, TriZNeg, DiXY}, //24 ThreeTwoOne Hexagonal
		new[] {TriZPos, TriZNeg, MirXY},//25 ThreeMOne Hexagonal
		new[] {TriZPos, TriZNeg, MirX_Y},//26 ThreeOneM Hexagonal
		new[] {TriZPos, TriZNeg, DiX_Y, Inv},//27 BarThreeOneM Hexagonal
		new[] {TriZPos, TriZNeg, DiXY, Inv},//28 BarThreeMOne Hexagonal

		new[] {TriZPos, TriZNeg, DiZ}, //29 Six
		new[] {TriZPos, TriZNeg, MirZ}, //30 BarSix
		new[] {TriZPos, TriZNeg, DiZ, Inv}, //31 SixSlashM
		new[] {TriZPos, TriZNeg, DiZ, DiXY}, //32 SixTwoTwo
		new[] {TriZPos, TriZNeg, DiZ, MirXY}, //33 SixMM
		new[] {TriZPos, TriZNeg, MirZ, MirXY}, //34 BarSixMTwo
		new[] {TriZPos, TriZNeg, DiZ, DiXY, Inv}, //35 SixSlashMMM

		new[] {DiZ, DiY, TriXYZPos, TriXYZNeg}, //36 TwoThree
		new[] {DiZ, DiY, TriXYZPos, TriXYZNeg, Inv}, //37 MBarThree
		new[] {DiZ, DiY, TriXYZPos, TriXYZNeg, DiXY}, //38 FourThreeTwo
		new[] {DiZ, DiY, TriXYZPos, TriXYZNeg, MirX_Y}, //39 BarFourThreeM
		new[] {DiZ, DiY, TriXYZPos, TriXYZNeg, DiXY, Inv}, //40 MBarThreeM
	};

	//#region allows you to specify a collapsable area of code in most editors.
	#region SymmetryOperations
	public static Vector3d Identity(Vector3d v) => new(v.X, v.Y, v.Z);//Why would you call this..?
	public static Vector3d DiX(Vector3d v) => new(v.X, -v.Y, -v.Z);//Diad along X Axis
	public static Vector3d DiY(Vector3d v) => new(-v.X, v.Y, -v.Z);
	public static Vector3d DiZ(Vector3d v) => new(-v.X, -v.Y, v.Z);
	//(1, 1, 1), (-1, 0, 1), (0, -1, 1): (-1, -1, 1), (1, 0, 1), (0, 1, 1)
	public static Vector3d DiXY(Vector3d v) => new(v.Y, v.X, -v.Z);
	//(1, 1, 1), (-1, 0, 1), (0, 1, 1), (-1, -1, 1), (1, 0, 1), (0, -1, 1): (1, 1, -1), (0, -1, -1), (1, 0, -1), (-1, -1, -1), (0, 1, -1), (-1, 0, -1)
	public static Vector3d DiX_Y(Vector3d v) => new(-v.Y, -v.X, -v.Z);
	public static Vector3d DiXZ(Vector3d v) => new(v.Z, -v.Y, v.X);//Seitz 2(1 0 1)
	public static Vector3d Di_XZ(Vector3d v) => new(-v.Z, -v.Y, -v.X);//Seitz 2(-1 0 1)
	public static Vector3d DiYZ(Vector3d v) => new(-v.X, v.Z, v.Y);
	public static Vector3d DiY_Z(Vector3d v) => new(-v.X, -v.Z, -v.Y);
	public static Vector3d DiXXY(Vector3d v) => new(v.X, v.X - v.Y, -v.Z);//Seitz 2(2 1 0)
	public static Vector3d DiXYY(Vector3d v) => new(-v.X + v.Y, v.Y, -v.Z);//Seitz 2(1 2 0)

	// public static Vector3d TriXPos(Vector3d v) => new(v.X, -v.Z, v.Y - v.Z);
	// public static Vector3d TriXNeg(Vector3d v) => new(v.X, -v.Y + v.Z, -v.Y);

	// public static Vector3d TriYPos(Vector3d v) => new(v.X - v.Z, v.Y, -v.Z);
	// public static Vector3d TriYNeg(Vector3d v) => new(-v.X, v.Y, -v.X + v.Y);

	//TODO I'm using a rotation matrix, but is that much better? It only works with gamma=90 but our target is gamma=120. However the supplied versions don't seem to keep magnitude, whereas these do.
	// GD.Print(Math.Cos(2 * Math.PI / 3)); //= -.5
	//GD.Print(Math.Sin(2 * Math.PI / 3)); //= sqrt3/2 = 0.8660254037844387	
	public static Vector3d TriZPos(Vector3d v) => new(v.X * 0.8660254037844387 + v.Y * -.5, v.X * -.5 + v.Y * -0.8660254037844387, v.Z);
	public static Vector3d TriZNeg(Vector3d v) => new(v.X * -0.8660254037844387 + v.Y * -.5, v.X * -.5 + v.Y * 0.8660254037844387, v.Z);
	// public static Vector3d TriZPos(Vector3d v) => new(-v.Y, v.X - v.Y, v.Z);
	// public static Vector3d TriZNeg(Vector3d v) => new(-v.X + v.Y, -v.X, v.Z);

	public static Vector3d TriXYZPos(Vector3d v) => new(v.Z, v.X, v.Y);//Tri along 1, 1, 1
	public static Vector3d TriXYZNeg(Vector3d v) => new(v.Y, v.Z, v.X);

	public static Vector3d TriX_Y_ZPos(Vector3d v) => new(-v.Z, -v.X, v.Y);//Tri along 1, -1, -1
	public static Vector3d TriX_Y_ZNeg(Vector3d v) => new(-v.Y, v.Z, -v.X);

	public static Vector3d Tri_XY_ZPos(Vector3d v) => new(v.Z, -v.X, -v.Y);//Tri along -1, 1, -1
	public static Vector3d Tri_XY_ZNeg(Vector3d v) => new(-v.Y, -v.Z, v.X);

	public static Vector3d Tri_X_YZPos(Vector3d v) => new(-v.Z, v.X, -v.Y);//Tri along -1, -1, 1
	public static Vector3d Tri_X_YZNeg(Vector3d v) => new(v.Y, -v.Z, -v.X);


	public static Vector3d TetXPos(Vector3d v) => new(v.X, -v.Z, v.Y);
	public static Vector3d TetXNeg(Vector3d v) => new(v.X, v.Z, -v.Y);
	public static Vector3d TetYPos(Vector3d v) => new(v.Z, v.Y, -v.X);
	public static Vector3d TetYNeg(Vector3d v) => new(-v.Z, v.Y, v.X);
	public static Vector3d TetZPos(Vector3d v) => new(-v.Y, v.X, v.Z);
	public static Vector3d TetZNeg(Vector3d v) => new(v.Y, -v.X, v.Z);

	public static Vector3d HexZPos(Vector3d v) => new(v.X - v.Y, v.X, v.Z);
	public static Vector3d HexZNeg(Vector3d v) => new(v.Y, -v.X + v.Y, v.Z);

	public static Vector3d MirX(Vector3d v) => new(-v.X, v.Y, v.Z);
	public static Vector3d MirY(Vector3d v) => new(v.X, -v.Y, v.Z);
	public static Vector3d MirZ(Vector3d v) => new(v.X, v.Y, -v.Z);
	public static Vector3d MirXY(Vector3d v) => new(-v.Y, -v.X, v.Z);
	public static Vector3d MirX_Y(Vector3d v) => new(v.Y, v.X, v.Z);
	public static Vector3d MirXZ(Vector3d v) => new(-v.Z, v.Y, -v.X);
	public static Vector3d Mir_XZ(Vector3d v) => new(v.Z, v.Y, v.X);
	public static Vector3d MirYZ(Vector3d v) => new(v.X, -v.Z, -v.Y);
	public static Vector3d MirY_Z(Vector3d v) => new(v.X, v.Z, v.Y);
	public static Vector3d MirXXY(Vector3d v) => new(v.X - v.Y, -v.Y, v.Z);//Seitz m(2 1 0)
	public static Vector3d MirXYY(Vector3d v) => new(-v.X, -v.X + v.Y, v.Z);//Seitz m(1 2 0)

	public static Vector3d Inv(Vector3d v) => new(-v.X, -v.Y, -v.Z);
	// public static Vector3d InvTriXPos(Vector3d v) => new(-v.X, v.Z, -v.Y + v.Z); Unused
	// public static Vector3d InvTriXNeg(Vector3d v) => new(-v.X, v.Y - v.Z, v.Y);
	// public static Vector3d InvTriYPos(Vector3d v) => new(-v.X + v.Z, -v.Y, v.Z);
	// public static Vector3d InvTriYNeg(Vector3d v) => new(v.X, -v.Y, v.X - v.Z);
	public static Vector3d InvTriZPos(Vector3d v) => new(v.Y, -v.X + v.Y, -v.Z);
	public static Vector3d InvTriZNeg(Vector3d v) => new(v.X - v.Y, v.X, -v.Z);

	public static Vector3d InvTriXYZPos(Vector3d v) => new(-v.Z, -v.X, -v.Y);//Tri along 1, 1, 1
	public static Vector3d InvTriXYZNeg(Vector3d v) => new(-v.Y, -v.Z, -v.X);

	public static Vector3d InvTriX_Y_ZPos(Vector3d v) => new(v.Z, v.X, -v.Y);//Tri along 1, -1, -1
	public static Vector3d InvTriX_Y_ZNeg(Vector3d v) => new(v.Y, -v.Z, v.X);

	public static Vector3d InvTri_XY_ZPos(Vector3d v) => new(-v.Z, v.X, v.Y);//Tri along -1, 1, -1
	public static Vector3d InvTri_XY_ZNeg(Vector3d v) => new(v.Y, v.Z, -v.X);

	public static Vector3d InvTri_X_YZPos(Vector3d v) => new(v.Z, -v.X, v.Y);//Tri along -1, -1, 1
	public static Vector3d InvTri_X_YZNeg(Vector3d v) => new(-v.Y, v.Z, v.X);


	public static Vector3d InvTetXPos(Vector3d v) => new(-v.X, v.Z, -v.Y);
	public static Vector3d InvTetXNeg(Vector3d v) => new(-v.X, -v.Z, v.Y);
	public static Vector3d InvTetYPos(Vector3d v) => new(-v.Z, -v.Y, v.X);
	public static Vector3d InvTetYNeg(Vector3d v) => new(v.Z, -v.Y, -v.X);
	public static Vector3d InvTetZPos(Vector3d v) => new(v.Y, -v.X, -v.Z);
	public static Vector3d InvTetZNeg(Vector3d v) => new(-v.Y, v.X, -v.Z);

	public static Vector3d InvHexZPos(Vector3d v) => new(-v.Y, v.X - v.Y, -v.Z);
	public static Vector3d InvHexZNeg(Vector3d v) => new(-v.X + v.Y, -v.X, -v.Z);
	#endregion
}