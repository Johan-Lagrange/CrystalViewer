# What is this?
Crystal viewer is a program to generate models of crystals from a few parameters: Namely symmetry group, axial lengths and angles, and miller indices.  
For information on what those are, look futher ahead in this.  
It also has display option like color, transparency, opacity, and roughness.  

# How do I use it?
-Select symmetry group  
-Input miller indices  

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
	-Downloadable builds
	-Miller indices on faces
	-OBJ export
	-Saving and loading
	-Presets?
	-Resets
 	-Multiple crystals
