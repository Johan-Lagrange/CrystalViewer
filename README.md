# What is this?
Crystal viewer is a program to generate models of crystals from a few parameters: Namely symmetry group, axial lengths and angles, and miller indices.  
For information on what those are, look futher ahead in this readme.  
The models can also be exported as .obj and .stl files as well, for 3d printing.
(You have my advance permission to 3d print models you make in this program)
It also has display options like color, transparency, opacity, and roughness.  

# Special thanks and credits
I want to thank Mark Holtkamp (who runs a site with similar function, [smorf.nl](https://smorf.nl)) specifically, whose help and direction are what made this project possible. Below are two resources linked from him that were invaluable in this project. His viewer has more features than this, and runs in the browser.
The project primarily uses this algorithm from ["Computing and drawing crystal shapes"(1979) by Eric Dowty](http://www.minsocam.org/ammin/AM65/AM65_465.pdf)

Functions for symmetry groups are from here: https://www.cryst.ehu.es/cryst/get_point_genpos.html
This project uses the [godot-dockable-container](https://github.com/gilzoide/godot-dockable-container) plugin by gilzoide for its GUI

# How do I use it?
1. Select your crystal's symmetry group. This determines what the unit cell will look like, as well as what symmetry operations will be applied to the given face normals.
	1a (Optional) Modify the unit cell to the specific values that fit your crystal. Shape classes have built in requirements (a = b != c, for example) but the numbers I have chosen are arbitrary.

2. Input the initial miller indices / face normals of the crystal. This dictates what direction a crystal face is oriented towards. These initial faces will then be copied according to the selected shape class. 
	
 	Note: Normally, the normal vector of a face only corresponds to its miller indices in a cubic crystal system (All axes at 90 degrees and same length), but as we transform the unit cell AFTER the calculations are done, we can use the miller indices as a normal directly.
# What are symmetry groups, axial lengths/interaxial angles, and miller indices?

## Symmetry groups
To start, there are different types of symmetry: Rotation, mirroring, and "retroinversion" which is like a mix of both.  
Different shapes have different types of rotation: An isosceles triangle has a mirror along its vertical axis, whereas an equilateral triangle has mirrors along all 3 of its axes, as well as threefold rotational symmetry  
Each crystal system is made of a combination of these types of symmetry.
Using this symmetry, we can generate a full crystal from just a few initial faces by mirroring/rotating them according to the symmetry group.

## Axial lengths and interaxial angles
This is basically a fancy word for the eigenvectors of a crystal system.  
a, b, and c correspond to x, y, and z
Interaxial angles are the angles (commonly in degrees) between two axes:
	
 	alpha (α) is between b and c  
	beta (β) is between a and c  
	gamma (γ) is between a and b  
Basically, its the angle that DOES NOT contain the axis it's named after. 

## Crystal systems
Every UNIQUE combination of symmetry operations is its own crystal class.  
Any extra symmetry operation done to one of those 32 will just create another of the 32.  
The 32 crystal systems can then be grouped into 6 more general crystal systems.
Some hexagonal crystals can be described in more than one system, so there are more than 32 here.
See the crystal notes file for more info on these.
Below are all the symmetry operations in each crystal class:
	
	Triclinic
	Triclinic has little to no symmetry, with all axes of different length, and all inter axial angles differing from one another.
		One: Identity
		BarOne: Inv

	Monoclinic 
	Monoclinic's unit cell is like an extruded parallelogram
	Note: Monoclinic has "Y" as its unique axis instead of Z 
		Two: DiY 
		M: MirY
		TwoSlashM: DiY, Inv, MirY

	Orthorhombic
	Orthorhombic's unit cell is like a rectangular cube
		TwoTwoTwo: DiZ, DiY, DiX
		MMTwo: DiZ, MirY, MirX
		MMM: DiZ, DiY, DiX, Inv, MirZ, MirY, MirX

  Tetragonal
	Tetragonal's unit cell is like a tall cube, square on the bottom
		Four: DiZ, TetZPos, TetZNeg
		BarFour: DiZ, InvTetZPos, InvTetZNeg
		FourSlashM: DiZ, TetZPos, TetZNeg, Inv, MirZ, InvTetZPos, InvTetZNeg
		FourTwoTwo: DiZ, TetZPos, TetZNeg, DiY, DiX
		FourMM: DiZ, TetZPos, TetZNeg, MirY, MirX, MirXY, MirX_Y, MirZ
		BarFourTwoM: DiZ, InvTetZPos, InvTetZNeg, DiY, DiX, MirXY, MirX_Y
		FourSlashMMM: DiZ, TetZPos, TetZNeg, DiY, DiX, DiXY, DiX_Y, Inv, MirZ, InvTetZPos, InvTetZNeg, MirY, MirX, MirXY, MirX_Y

	Rhombohedral
	Trigonal, Hexagonal, and Rhombohedral are all related
		Three Rhombohedral: TriXYZPos, TriXYZNeg
		BarThree Rhombohedral: TriXYZPos, TriXYZNeg, Inv, InvTriXYZPos, InvTriXYZNeg
		ThreeTwo Rhombohedral: TriXYZPos, TriXYZNeg, Di_XZ, DiX_Y, DiY_Z
		ThreeM Rhombohedral: TriXYZPos, TriXYZNeg, Mir_XZ, MirX_Y, MirY_Z
		BarThreeM Rhombohedral: TriXYZPos, TriXYZNeg, Di_XZ, DiX_Y, DiY_Z, Inv, InvTriXYZPos, InvTriXYZNeg, Mir_XZ, MirX_Y, MirY_Z

	Trigonal
		Three Hexagonal: TriZPos, TriZNeg
		BarThree Hexagonal: TriZPos, TriZNeg, Inv, InvTriZPos, InvTriZNeg
		ThreeOneTwo Hexagonal: TriZPos, TriZNeg, DiX_Y, DiXYY, DiXXY
		ThreeTwoOne Hexagonal: TriZPos, TriZNeg, DiXY, DiX, DiY
		ThreeMOne Hexagonal: TriZPos, TriZNeg, MirXY, MirX, MirY
		ThreeOneM Hexagonal: TriZPos, TriZNeg, MirX_Y, MirXYY, MirXXY
		BarThreeOneM Hexagonal: TriZPos, TriZNeg, DiX_Y, DiXYY, DiXXY, Inv, InvTriZPos, InvTriZNeg, MirX_Y, MirXYY, MirXXY
		BarThreeMOne Hexagonal: TriZPos, TriZNeg, DiXY, DiX, DiY, Inv, InvTriZPos, InvTriZNeg, MirXY, MirX, MirY

	Hexagonal
		Six: TriZPos, TriZNeg, DiZ, HexZNeg, HexZPos
		BarSix: TriZPos, TriZNeg, MirZ, InvHexZNeg, InvHexZPos
		SixSlashM: TriZPos, TriZNeg, DiZ, HexZNeg, HexZPos, Inv, InvTriZPos, InvTriZNeg, MirZ, InvHexZNeg, InvHexZPos
		SixTwoTwo: TriZPos, TriZNeg, DiZ, HexZNeg, HexZPos, DiXY, DiX, DiY, DiX_Y, DiXYY, DiXXY
		SixMM: TriZPos, TriZNeg, DiZ, HexZNeg, HexZPos, MirXY, MirX, MirY, MirX_Y, MirXYY, MirXXY
		BarSixMTwo: TriZPos, TriZNeg, MirZ, InvHexZNeg, InvHexZPos, MirXY, MirX, MirY, DiX_Y, DiXYY, DiXXY
		SixSlashMMM: TriZPos, TriZNeg, DiZ, HexZNeg, HexZPos, DiXY, DiX, DiX_Y, DiXYY, DiXXY, Inv, InvTriZPos, InvTriZNeg, MirZ, InvHexZNeg, InvHexZPos, MirXY, MirX, MirY, MirX_Y, MirXYY, MirXXY

	Cubic
	Cubic has the most symmetry, and is also the coolest.
		TwoThree: DiZ, DiY, DiX, TriXYZPos, Tri_XY_ZPos, TriX_Y_ZPos, Tri_X_YZPos, TriXYZNeg, TriX_Y_ZNeg, Tri_X_YZNeg, Tri_XY_ZNeg
		MBarThree: DiZ, DiY, DiX, TriXYZPos, Tri_XY_ZPos, TriX_Y_ZPos, Tri_X_YZPos, TriXYZNeg, TriX_Y_ZNeg, Tri_X_YZNeg, Tri_XY_ZNeg, Inv, MirZ, MirY, MirX, InvTriXYZPos, InvTri_XY_ZPos, InvTriX_Y_ZPos, InvTri_X_YZPos, InvTriXYZNeg, InvTriX_Y_ZNeg, InvTri_X_YZNeg, InvTri_XY_ZNeg
		FourThreeTwo: DiZ, DiY, DiX, TriXYZPos, Tri_XY_ZPos, TriX_Y_ZPos, Tri_X_YZPos, TriXYZNeg, TriX_Y_ZNeg, Tri_X_YZNeg, Tri_XY_ZNeg, DiXY, DiX_Y, TetZNeg, TetZPos, TetXNeg, TetXPos, DiYZ, DiY_Z, TetXPos, TetYPos, DiXZ, TetYNeg, Di_XZ
		BarFourThreeM: DiZ, DiY, DiX, TriXYZPos, Tri_XY_ZPos, TriX_Y_ZPos, Tri_X_YZPos, TriXYZNeg, TriX_Y_ZNeg, Tri_X_YZNeg, Tri_XY_ZNeg, MirX_Y, MirXY, InvTetZPos, InvTetZNeg, MirY_Z, InvTetXPos, InvTetXNeg, MirYZ, Mir_XZ, InvTetYNeg, MirXZ, InvTetYPos
		MBarThreeM: DiZ, DiY, DiX, TriXYZPos, Tri_XY_ZPos, TriX_Y_ZPos, Tri_X_YZPos, TriXYZNeg, TriX_Y_ZNeg, Tri_X_YZNeg, Tri_XY_ZNeg, DiXY, DiX_Y, TetZNeg, TetZPos, TetXNeg, DiYZ, DiY_Z, TetXPos, TetYPos, DiXZ, TetYNeg, Di_XZ, Inv, MirZ, MirY, MirX, InvTriXYZPos, InvTri_XY_ZPos, InvTriX_Y_ZPos, InvTri_X_YZPos, InvTriXYZNeg, InvTriX_Y_ZNeg, InvTri_X_YZNeg, InvTri_XY_ZNeg, MirXY, MirX_Y, InvTetZNeg, InvTetZPos, InvTetXNeg, MirYZ, MirY_Z, InvTetXPos, InvTetYPos, MirXZ, InvTetYNeg, Mir_XZ
[The list of operations are from this link](https://www.cryst.ehu.es/cryst/get_point_genpos.html)

## Miller indices
Because crystals grow in different shapes as their unit cells are different shapes, (being longer along one or two axes, or skewed), we need a notation to describe faces that accounts for that. 
This application uses miller indices for that. 
A 2D miller index has 2 numbers. A 3d miller index has 3 numbers.
For a basic explanation, I'll be using 2d miller indices.
The number in a miller index describes reciprocal of the axis intercept.
A miller index of (1 0) would be a vertical line that intersects x at 1, and does not intersect y.
A miller index of (1 1) would intersect x and y at 1.
A miller index of (2 1) would intersect x at 0.5, and y at 1.
A miller index of (2 0) is the same as one of (1, 0), since they reduce like fractions. 
Miller indices can also be negative, (-1 1) intersects x at -1 and y at +1.
For negatives, we don't say "negative one", we say "bar one".
For some crystals, like cubic, (1 0 0), (-1 0 0), (0 1 0), (0 -1 0), (0 0 1), (0 0 -1) are all functionally equivalent due to the crystal's symmetry.
Because of that, {1 0 0} can be used to refer to all the different versions of (1 0 0).
In fact, the only crystal system where this doesn't apply at all is in the triclinic system, which doesn't have any symmetry.
If the axes are orthonormal (90 degrees and length 1), the miller index also describes the "normal" of the line, which is the line that is perpendicular to the index.
In the program, we do the initial calculations with orthonormal axes, and then skew it after, so we just take the miller index as the normal for the planes we are building. It's pretty convenient.

## How does this generate a crystal shape?
The way this algorithm generates a crystal is:
1. Acquire a list of symmetry operations (reflections, rotations, retroinversions) from the given point group
2. For every given face vector, apply every combination of symmetry operations in the list to it.

   As an example, to make a 2D square with starting vector (1, 0), operation 1 would be mirror over y,

   after which the list is (1, 0) and (-1, 0), and operation 2 would rotate 90 degrees, which we apply to both items

   for a result of (1, 0), (-1, 0), (0, -1), (0, 1).

	2a. if a plane is in front of another (parallel and has a greater or equal distance) it is redundant and can be removed.
3. Generate a list of vertices by intersecting every trio of planes.

    Vertices keep track of which planes they are a part of. This is imporant.

	3a. Remove any vertex that is in front of a plane, since that lies outside of the crystal.

	3b. Combine any two vertices that are in the same spot, otherwise vertices would only ever keep track of 3 faces
4. Create an unordered linked list of every edge on the face by comparing every pair of vertices creating an edge if they share 2 faces

   	The AdjacentEdges type is there to keep track of which vertices are adjacent to which edges. We use this to traverse a face's edges
	
 	4a. A vertex on a face should only have neighboring vertices on that face. If there are more, we know the face is malformed and can disregard it.

   This tends to happen with a face of zero area (Think of an octagon where the corners shrink to a square. The diagonals, that are now the corners, are still extant, but with zero length)
6. Create a clockwise-ordered array of vertices that make up the face by traversing the linked list, making sure we don't backtrack.

	5a. Check the first couple vertices to see if they are clockwise, and if not, reverse the array to make it clockwise.
8. Generate normals, tangents, and tris for each trio of vertices, and use Godot's mesh builder to create a mesh from there.

# TODO
	-Miller indices on faces
 	-Multiple crystals
	-Interpolate panel
