These are notes and copied segments from my learning on crystallography.
Some of it is copied from linked sources.

Miller indices:
https://www.doitpoms.ac.uk/tlplib/miller_indices/vector_plane.php
	a is RECIPROCAL of intercept along x axis
	b is RECIPROCAL of intercept along y axis
	c is RECIPROCAL of intercept along z axis

a b and c can be skewed by a linear transformation!
UVW are used as the eigenvectors instead of i^ and j^

Interaxial angles:
	alpha (α) is between b and c
	beta (β) is between a and c
	gamma (γ) is between a and b
basically it's the angle that the axis you're measuring isn't on.

(1 0 0) is x intercept of 1, y and z of 0
(-1 0 0) (called "bar one" the - sign is a bar above the number) start from (1, 0, 0) instead and then intercept -1, 0, 0.
Same goes for any negative index of a, b, or c. Note that the b and c intercept start from the NEW origin.

(1 0 0), (-1 0 0), (0 1 0), (0 -1 0), (0 0 1), (0 0 -1)
all look the same. They can all be referenced with {1 0 0}. 
It's every face that is equivalent in the symmetry of the crystal. 
Triclinic has no mirror or rotational symmetry, so {1 0 0} there is ONLY (1 0 0)

(1 0 0), (2 0 0), (3 0 0), etc are all the same direction
so you can write [1 0 0] or "[U V W]" to refer to all of those

[1 0 0], [0 1 0]... are all symmetrical
so you can do <1 0 0> for THAT. It's {} AND [].


Weiss Zone Law

The Weiss zone law states that:
If the direction [UVW] lies in the plane (hkl), then:
hU + kV + lW = 0
(Note: This is just the dot product)

In a cubic system this is exactly analogous to taking the scalar product of the direction and the plane normal, so that if they are perpendicular, 
the angle between them, θ, is 90° , then cosθ = 0, and the direction lies in the plane. 
Indeed, in a cubic system, the scalar product can be used to determine the angle between a direction and a plane.
However, the Weiss zone law is more general, and can be shown to work for all crystal systems, to determine if a direction lies in a plane.

From the Weiss zone law the following rule can be derived:
The direction, [UVW], of the intersection of (h1k1l1) and (h2k2l2) is given by:
	U = k1l2 − k2l1
	V = l1h2 − l2h1
	W = h1k2 − h2k1
(Note: This is just the cross product)

As it is derived from the Weiss zone law, this relation applies to all crystal systems, including those that are not orthogonal. 


Cell types
within crystal lattices you can choose different unit cells. 
A primitive cell (smallest possible with no extra points) is usually preferable
but a non-primitive one can be chosen if it shows the symmetry better.

There are different types of unit cells:
	(P)Primitive: ONLY 1 point per cell. All others are the corresponding point in another cell.
	(I)Body Centered: One extra point dead center
	(F)Face Centered: An extra point at the center of EVERY face
	(C/S)C Face Centered: An extra point at the center of the C faces. Also can be A Face for A center and B face for that
	
Crystal systems/lattices (And their unit cells! And symmetries!!!):
	(c)Cubic (P, I, F)           4x3(each diag) : a  = b  = c, 90  = α  = β  = γ       //Cube(duh)
	(t)Tetragonal (P, I)           4[001]       : a  = b != c, 90  = α  = β  = γ       //Tall cube
	(o)Orthorhombic (P, I, F, C) 3x2(1 per axis): a != b != c, 90  = α  = β  = γ       //Rectangle cube
	(h)Trigonal/Rhombohedral (P/R)   3[001]     : a  = b  = c, 90 != α  = β  = γ  < 120//Cube but axes closed in towards 1,1,1
	(h)Trigonal                    3[001]       : a  = b != c, 90  = α  = β != γ  = 120//Like hexagonal but extra points in middle
	(h)Hexagonal (P)               6[001]       : a  = b != c, 90  = α  = β != γ  = 120//Like 2 equi tri mashed together and then extruded.
	(m)Monoclinic (P, C)           2[001]       : a != b != c, 90  = α  = β != γ != 90 //Extruded parallelogram
	(a)Triclinic/Anorthic (P)      0 :(         : a != b != c, 90 != α != β != γ       //the wacky one

Hexagonal can be represented "Open c axis" with origin at center of hexagon as (a1 a2 a3 c) with a1-3 all 120 deg and equal length. 
http://www.gisaxs.com/index.php/Unit_cell

Graphite has 2D Hexagonal layers which can be stacked ABA or ABC. 
ABA             ABC
<8==8>o--o<     8>o--o<8==
	|    |        |    |
--o<8==8>o-     --o<8==8>o-
	|    |          |    |
<8==8>o--o<     <8==8>o--o<
ABA is sturdier, Everything lines up
ABA is also called Bernal or Hexagonal Close Packing(HCP). Hexagonal lattice type with extra atom at 1/3?, 1/3?, 1/2
ABC is also called rhombohedral packing or Cubic Closed Packed(CCP) Unit cell is face centered cubic

	
Crystal structure

The structure of a crystal can be described by combining the following elements: 
	lattice type:  location of the lattice points within the unit cell.
	lattice parameters: size and shape of the unit cell.
	motif: list of the atoms associated with each lattice POINT, along with their fractional coordinates relative to the lattice point
Adding the motif to every lattice point will recreate the crystal.
NOTE: This includes the extra points added in different unit cells. 

Types of symmetries:
Diad is 2 fold rotational
Triad is 3 fold
Tetrad is 4 fold
Hexad is 6 fold
An object is centrosymmetric if it has a point at both (x, y, z) and -(x, y, z)
There's only 32 combinations of mirror planes, rotation axes, centres of symmetry, and inversion axes. Anything more will make a previous one.
An n-fold inversion axis is a rotation of 360/n along a given axis, then flipping around the center. Do that until we're back at the start.
https://www.doitpoms.ac.uk/tlplib/crystallography3/combining_symmetry.php

Crystal Classes:


Hermann-Mauguin symbols: (AKA International symbols)
https://www2.tulane.edu/~sanelson/eens211/32crystalclass.htm

Write a number for each UNIQUE rotation axis that is not produced by any other operation.
Write an m for each unique mirror plane
If any of the rotational axes are perpindicular to the mirror plane, add a slash between the number and m
If there are any retroinversion axes, use a bar or a negative sign

System 		Class 		Symmetry 			Name of Class
Triclinic 	 
			1 			none 				Pedial
			-1			i 					Pinacoidal
Monoclinic 	
			2 			1A2 				Sphenoidal
			m 			1m 					Domatic
			2/m 		i, 1A2, 1m 			Prismatic
Orthorhombic 	
			222 		3A2 				Rhombic-disphenoidal
			mm2 (2mm) 	1A2, 2m 			Rhombic-pyramidal //Is normally written mm2 for some reason
			2/m2/m2/m 	i, 3A2, 3m 			Rhombic-dipyramidal
Tetragonal 	
			4 			1A4 				Tetragonal-Pyramidal
			-4			-A4 				Tetragonal-disphenoidal
			4/m 		i, 1A4, 1m 			Tetragonal-dipyramidal
			422 		1A4, 4A2 			Tetragonal-trapezohedral
			4mm 		1A4, 4m 			Ditetragonal-pyramidal
			-42m 		1-A4, 2A2, 2m 		Tetragonal-scalenohedral
			4/m2/m2/m 	i, 1A4, 4A2, 5m 	Ditetragonal-dipyramidal
Hexagonal 	
			3 			1A3 				Trigonal-pyramidal
			-3			1-A3 				Rhombohedral
			32 			1A3, 3A2 			Trigonal-trapezohedral
			3m 			1A3, 3m 			Ditrigonal-pyramidal
			-32/m 		1-A3, 3A2, 3m 		Hexagonal-scalenohedral
			6 			1A6 				Hexagonal-pyramidal
			-6			1-A6 				Trigonal-dipyramidal
			6/m 		i, 1A6, 1m 			Hexagonal-dipyramidal
			622 		1A6, 6A2 			Hexagonal-trapezohedral
			6mm 		1A6, 6m 			Dihexagonal-pyramidal
			-6m2 		1-A6, 3A2, 3m 		Ditrigonal-dipyramidal
			6/m2/m2/m 	i, 1A6, 6A2, 7m 	Dihexagonal-dipyramidal
Isometric 	
			23 			3A2, 4A3  			Tetaroidal
			2/m-3 		3A2, 3m, 4-A3  		Diploidal
			432 		3A4, 4A3, 6A2 		Gyroidal
			-43m 		3-A4, 4A3, 6m 		Hextetrahedral
			4/m32/m 	3A4, 4-A3, 6A2, 9m 	Hexoctahedral

 
Note that the 32 crystal classes are divided into 6 crystal systems.

	The Triclinic System has only 1-fold or 1-fold rotoinversion axes.
	The Monoclinic System has only mirror plane(s) or a single 2-fold axis.
	The Orthorhombic System has only two fold axes or a 2-fold axis and 2 mirror planes.
	The Tetragonal System has either a single 4-fold or 4-fold rotoinversion axis.
	The Hexagonal System has no 4-fold axes, but has at least 1 6-fold or 3-fold axis.
	The Isometric System has either 4 3-fold axes or 4 3-fold rotoinversion axes.

https://chem.libretexts.org/Bookshelves/General_Chemistry/Chem1_(Lower)/07%3A_Solids_and_Liquids/7.06%3A_Introduction_to_Crystals

Faces having a lower density of lattice points (as in the (31) face shown above) can acquire new layers more rapidly, 
and thus grow more rapidly than faces having a high lattice-point density. 
The faces that can potentially develop in a crystal are determined entirely by the symmetry properties of the underlying lattice. 
But the faces that actually develop under specific conditions — 
and thus the overall shape of the crystal — 
is determined by the relative rates of growth of the various faces. 
The slower the growth rate, the larger the face.

This relation can be understood by noting that faces that grow normal to shorter unit cell axes 
(as in the needle-shaped crystal shown above) present a larger density of lattice points to the surface 
(that is, more points per unit surface area.) 
This means that more time is required for diffusion of enough new particles to build out a new layer on such a surface. 

Unique Axis: The one parallel to symmetry axis and/or normal of the symmetry plane.
In monoclinic this is normally the b axis. In tetragonal, trigonal, and hexagonal, it is conventionally the c axis.
