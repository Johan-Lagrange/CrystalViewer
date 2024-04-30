# What is this?
Crystal viewer is a program to generate models of crystals from a few parameters: Namely symmetry group, axial lengths and angles, and miller indices.  
For information on what those are, look futher ahead in this.  
It also has display options like color, transparency, opacity, and roughness.  

# Special thanks and credits
I want to thank Mark Holtkamp specifically, whose help and direction are what made this project possible. Below are two resources linked from him that were invaluable in this project.
The project primarily uses this algorithm from ["Computing and drawing crystal shapes"(1979) by Eric Dowty](http://www.minsocam.org/ammin/AM65/AM65_465.pdf)
Functions for symmetry groups are from here: https://www.cryst.ehu.es/cryst/get_point_genpos.html

# How do I use it?
1. Select your crystal's symmetry group. This determines what the unit cell will look like, as well as what symmetry operations will be applied to the given face normals.
	1a (Optional) Modify the unit cell to the specific values that fit your crystal. Shape classes have built in values (a = b != c, for example) but the numbers I have chosen are arbitrary.
2. Input the initial miller indices / face normals of the crystal. This dictates what direction a crystal face is oriented towards. These initial faces will then be copied according to the selected shape class. 
	Note: Normally, the normal vector of a face only corresponds to its miller indices in a cubic crystal system (All axes at 90 degrees and same length), but as we transform the unit cell AFTER the calculations are done, we can use the miller indices as a normal directly.
# What are symmetry groups, axial lengths/interaxial angles, and miller indices?

## Symmetry groups
To start, there are different types of symmetry: Rotation, mirroring, and "retroinversion" which is like a mix of both.  
Different shapes have different types of rotation: An isosceles triangle has a mirror along its vertical axis, whereas an equilateral triangle has mirrors along all 3 of its axes, as well as threefold rotational symmetry  
//TODO explain symmetry groups, international notation, shape families

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
The 32 crystal systems can then be grouped into 6 more general crystal systems:
//TODO

## Miller indices
//TODO (Images?)

# TODO
	-Miller indices on faces
	-Saving and loading
	-Presets?
	-Resets
 	-Multiple crystals
	-Interpolate panel
