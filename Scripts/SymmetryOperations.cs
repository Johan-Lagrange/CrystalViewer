using Godot;
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
	/// The list of EVERY POSSIBLE OPERATION within a point group that will yield the same shape back.
	/// All are callable as functions
	/// </summary>
	/// <value></value>
	public static readonly Func<Vector3, Vector3>[][] PointGroupPositions =
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
	/// </summary>
	/// <value></value>
	public static readonly Func<Vector3, Vector3>[][] PointGroupOperations =
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

		new[] {TriXYZPos}, //16 Three Rhombohedral
		new[] {TriXYZPos, Inv}, //17 BarThree Rhombohedral
		new[] {TriXYZPos, DiX_Y}, //18 ThreeTwo Rhombohedral
		new[] {TriXYZPos, MirX_Y},//19 ThreeM Rhombohedral
		new[] {TriXYZPos, DiX_Y, Inv},//20 BarThreeM Rhombohedral

		new[] {TriZPos}, //21 Three Hexagonal
		new[] {TriZPos, Inv}, //22 BarThree Hexagonal
		new[] {TriZPos, DiX_Y}, //23 ThreeOneTwo Hexagonal
		new[] {TriZPos, DiXY}, //24 ThreeTwoOne Hexagonal
		new[] {TriZPos, MirXY},//25 ThreeMOne Hexagonal
		new[] {TriZPos, MirX_Y},//26 ThreeOneM Hexagonal
		new[] {TriZPos, DiX_Y, Inv},//27 BarThreeOneM Hexagonal
		new[] {TriZPos, DiXY, Inv},//28 BarThreeMOne Hexagonal

		new[] {TriZPos, DiZ}, //29 Six
		new[] {TriZPos, MirZ}, //30 BarSix
		new[] {TriZPos, DiZ, Inv}, //31 SixSlashM
		new[] {TriZPos, DiZ, DiXY}, //32 SixTwoTwo
		new[] {TriZPos, DiZ, MirXY}, //33 SixMM
		new[] {TriZPos, MirZ, MirXY}, //34 BarSixMTwo
		new[] {TriZPos, DiZ, DiXY, Inv}, //35 SixSlashMMM

		new[] {DiZ, DiY, TriXYZPos}, //36 TwoThree
		new[] {DiZ, DiY, TriXYZPos, Inv}, //37 MBarThree
		new[] {DiZ, DiY, TriXYZPos, DiXY}, //38 FourThreeTwo
		new[] {DiZ, DiY, TriXYZPos, MirX_Y}, //39 BarFourThreeM
		new[] {DiZ, DiY, TriXYZPos, DiXY, Inv}, //40 MBarThreeM
	};


	/// <summary>
	/// Takes an initial vector and applies all point group operations on it, 
	/// returning a list of every vector therein, including the original. (Identity is an operation after all)
	/// </summary>
	/// <param name="v">The initial vector to do symmetry stuff on</param>
	/// <param name="group">The crystal's point group. Used to get a list of operations</param>
	/// <returns>A list of vectors, including the original, that are made from the symmetry operations</returns>
	public static List<Vector3> CreateCrystalSymmetry(Vector3 v, PointGroup group)
	{
		List<Vector3> vectorList = new() { v };

		foreach (Func<Vector3, Vector3> Operation in PointGroupPositions[(int)group])
		{
			ApplyOperation(vectorList, Operation);
		}
		return vectorList;
	}
	/// <summary>
	/// Applies a symmetry operation to every vector in a list, and adds every result to the list.
	/// </summary>
	/// <param name="vectorList">List of vectors to operate upon and expand</param>
	/// <param name="symmetryOperation">The symmetry operation to do</param>
	public static void ApplyOperation(List<Vector3> vectorList, Func<Vector3, Vector3> symmetryOperation)
	{
		int count = vectorList.Count;//Get the count before we add to the list so we aren't in an infinite loop
		for (int i = 0; i < count; i++)
		{
			Vector3 v = symmetryOperation(vectorList[i]);
			v = FormatVector3(v);

			bool valid = true;
			foreach (Vector3 vl in vectorList)
			{
				if (v.IsEqualApprox(vl))
				{
					valid = false;
					break;
				}
			}
			if (valid)
				vectorList.Add(v);
		}
	}

	/// <summary>
	/// Gets the unit cell perameters from each point group. Numbers are arbitrary, but nice.
	/// </summary>
	/// <param name="pointGroup">The point group for which we are getting the matching unit cell parameters</param>
	/// <returns>A float[6]. [0],[1],[2], are the lengths of a, b, c, and [3],[4],[5] are the interaxial angles alpha, beta, gamma</returns>
	public static float[] GetParametersForPointGroup(PointGroup pointGroup)
	{
		switch ((int)pointGroup)
		{
			case 1 or 2:
				return new float[] { 1, 1.5f, 2, 30, 60, 80 };//Triclinic
			case 3 or 4 or 5:
				return new float[] { 1, 2, 1.5f, 90, 60, 90 };//Monoclinic. Again monoclinic as b as the wacky axis
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
	private static Vector3 FormatVector3(Vector3 v)
	{
		float x = v.X * v.X < 0.00001f ? 0 : v.X;
		float y = v.Y * v.Y < 0.00001f ? 0 : v.Y;
		float z = v.Z * v.Z < 0.00001f ? 0 : v.Z;
		return new Vector3(x, y, z);
	}
	//#region allows you to specify a collapsable area of code in most editors.
	#region SymmetryOperations
	public static Vector3 Identity(Vector3 v) => new(v.X, v.Y, v.Z);//Why would you call this..?
	public static Vector3 DiX(Vector3 v) => new(v.X, -v.Y, -v.Z);//Diad along X Axis
	public static Vector3 DiY(Vector3 v) => new(-v.X, v.Y, -v.Z);
	public static Vector3 DiZ(Vector3 v) => new(-v.X, -v.Y, v.Z);
	public static Vector3 DiXY(Vector3 v) => new(v.Y, v.X, -v.Z);
	public static Vector3 DiX_Y(Vector3 v) => new(-v.Y, -v.X, -v.Z);
	public static Vector3 DiXZ(Vector3 v) => new(v.Z, -v.Y, v.X);//Seitz 2(1 0 1)
	public static Vector3 Di_XZ(Vector3 v) => new(-v.Z, -v.Y, -v.X);//Seitz 2(-1 0 1)
	public static Vector3 DiYZ(Vector3 v) => new(-v.X, v.Z, v.Y);
	public static Vector3 DiY_Z(Vector3 v) => new(-v.X, -v.Z, -v.Y);
	public static Vector3 DiXXY(Vector3 v) => new(v.X, v.X - v.Y, -v.Z);//Seitz 2(2 1 0)
	public static Vector3 DiXYY(Vector3 v) => new(-v.X + v.Y, v.Y, -v.Z);//Seitz 2(1 2 0)

	// public static Vector3 TriXPos(Vector3 v) => new(v.X, -v.Z, v.Y - v.Z);
	// public static Vector3 TriXNeg(Vector3 v) => new(v.X, -v.Y + v.Z, -v.Y);

	// public static Vector3 TriYPos(Vector3 v) => new(v.X - v.Z, v.Y, -v.Z);
	// public static Vector3 TriYNeg(Vector3 v) => new(-v.X, v.Y, -v.X + v.Y);

	public static Vector3 TriZPos(Vector3 v) => new(-v.Y, v.X - v.Y, v.Z);
	public static Vector3 TriZNeg(Vector3 v) => new(-v.X + v.Y, -v.X, v.Z);

	public static Vector3 TriXYZPos(Vector3 v) => new(v.Z, v.X, v.Y);//Tri along 1, 1, 1
	public static Vector3 TriXYZNeg(Vector3 v) => new(v.Y, v.Z, v.X);

	public static Vector3 TriX_Y_ZPos(Vector3 v) => new(-v.Z, -v.X, v.Y);//Tri along 1, -1, -1
	public static Vector3 TriX_Y_ZNeg(Vector3 v) => new(-v.Y, v.Z, -v.X);

	public static Vector3 Tri_XY_ZPos(Vector3 v) => new(v.Z, -v.X, -v.Y);//Tri along -1, 1, -1
	public static Vector3 Tri_XY_ZNeg(Vector3 v) => new(-v.Y, -v.Z, v.X);

	public static Vector3 Tri_X_YZPos(Vector3 v) => new(-v.Z, v.X, -v.Y);//Tri along -1, -1, 1
	public static Vector3 Tri_X_YZNeg(Vector3 v) => new(v.Y, -v.Z, -v.X);


	public static Vector3 TetXPos(Vector3 v) => new(v.X, -v.Z, v.Y);
	public static Vector3 TetXNeg(Vector3 v) => new(v.X, v.Z, -v.Y);
	public static Vector3 TetYPos(Vector3 v) => new(v.Z, v.Y, -v.X);
	public static Vector3 TetYNeg(Vector3 v) => new(-v.Z, v.Y, v.X);
	public static Vector3 TetZPos(Vector3 v) => new(-v.Y, v.X, v.Z);
	public static Vector3 TetZNeg(Vector3 v) => new(v.Y, -v.X, v.Z);

	public static Vector3 HexZPos(Vector3 v) => new(v.X - v.Y, v.X, v.Z);
	public static Vector3 HexZNeg(Vector3 v) => new(v.Y, -v.X + v.Y, v.Z);

	public static Vector3 MirX(Vector3 v) => new(-v.X, v.Y, v.Z);
	public static Vector3 MirY(Vector3 v) => new(v.X, -v.Y, v.Z);
	public static Vector3 MirZ(Vector3 v) => new(v.X, v.Y, -v.Z);
	public static Vector3 MirXY(Vector3 v) => new(-v.Y, -v.X, v.Z);
	public static Vector3 MirX_Y(Vector3 v) => new(v.Y, v.X, v.Z);
	public static Vector3 MirXZ(Vector3 v) => new(-v.Z, v.Y, -v.X);
	public static Vector3 Mir_XZ(Vector3 v) => new(v.Z, v.Y, v.X);
	public static Vector3 MirYZ(Vector3 v) => new(v.X, -v.Z, -v.Y);
	public static Vector3 MirY_Z(Vector3 v) => new(v.X, v.Z, v.Y);
	public static Vector3 MirXXY(Vector3 v) => new(v.X - v.Y, -v.Y, v.Z);//Seitz m(2 1 0)
	public static Vector3 MirXYY(Vector3 v) => new(-v.X, -v.X + v.Y, v.Z);//Seitz m(1 2 0)

	public static Vector3 Inv(Vector3 v) => new(-v.X, -v.Y, -v.Z);
	// public static Vector3 InvTriXPos(Vector3 v) => new(-v.X, v.Z, -v.Y + v.Z); Unused
	// public static Vector3 InvTriXNeg(Vector3 v) => new(-v.X, v.Y - v.Z, v.Y);
	// public static Vector3 InvTriYPos(Vector3 v) => new(-v.X + v.Z, -v.Y, v.Z);
	// public static Vector3 InvTriYNeg(Vector3 v) => new(v.X, -v.Y, v.X - v.Z);
	public static Vector3 InvTriZPos(Vector3 v) => new(v.Y, -v.X + v.Y, -v.Z);
	public static Vector3 InvTriZNeg(Vector3 v) => new(v.X - v.Y, v.X, -v.Z);

	public static Vector3 InvTriXYZPos(Vector3 v) => new(-v.Z, -v.X, -v.Y);//Tri along 1, 1, 1
	public static Vector3 InvTriXYZNeg(Vector3 v) => new(-v.Y, -v.Z, -v.X);

	public static Vector3 InvTriX_Y_ZPos(Vector3 v) => new(v.Z, v.X, -v.Y);//Tri along 1, -1, -1
	public static Vector3 InvTriX_Y_ZNeg(Vector3 v) => new(v.Y, -v.Z, v.X);

	public static Vector3 InvTri_XY_ZPos(Vector3 v) => new(-v.Z, v.X, v.Y);//Tri along -1, 1, -1
	public static Vector3 InvTri_XY_ZNeg(Vector3 v) => new(v.Y, v.Z, -v.X);

	public static Vector3 InvTri_X_YZPos(Vector3 v) => new(v.Z, -v.X, v.Y);//Tri along -1, -1, 1
	public static Vector3 InvTri_X_YZNeg(Vector3 v) => new(-v.Y, v.Z, v.X);


	public static Vector3 InvTetXPos(Vector3 v) => new(-v.X, v.Z, -v.Y);
	public static Vector3 InvTetXNeg(Vector3 v) => new(-v.X, -v.Z, v.Y);
	public static Vector3 InvTetYPos(Vector3 v) => new(-v.Z, -v.Y, v.X);
	public static Vector3 InvTetYNeg(Vector3 v) => new(v.Z, -v.Y, -v.X);
	public static Vector3 InvTetZPos(Vector3 v) => new(v.Y, -v.X, -v.Z);
	public static Vector3 InvTetZNeg(Vector3 v) => new(-v.Y, v.X, -v.Z);

	public static Vector3 InvHexZPos(Vector3 v) => new(-v.Y, v.X - v.Y, -v.Z);
	public static Vector3 InvHexZNeg(Vector3 v) => new(-v.X + v.Y, -v.X, -v.Z);
	#endregion
}