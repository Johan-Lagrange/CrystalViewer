# What is this?
Crystal viewer is a program to generate models of crystals from a few parameters: Namely symmetry group, axial lengths and angles, and miller indices.  
For information on what those are, look futher ahead in this.  
It also has display options like color, transparency, opacity, and roughness.  

# Special thanks and credits
I want to thank Mark Holtkamp (who runs a site with similar function, [smorf.nl](https://smorf.nl)) specifically, whose help and direction are what made this project possible. Below are two resources linked from him that were invaluable in this project. His viewer has more features than this, and runs in the browser.
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
4a. A vertex on a face should only have neighboring vertices on that face. If there are more, we know the face is malformed and can disregard it
    This tends to happen with a face of zero area (Think of an octagon where the corners shrink to a square. The diagonals, that are now the corners, are still extant, but with zero length)
5. Create a clockwise-ordered array of vertices that make up the face by traversing the linked list, making sure we don't backtrack.
5a. Check the first couple vertices to see if they are clockwise, and if not, reverse the array to make it clockwise.
6. Generate normals, tangents, and tris for each trio of vertices, and use Godot's mesh builder to create a mesh from there.

# TODO
	-Miller indices on faces
	-Saving and loading
	-Presets?
	-Resets
 	-Multiple crystals
	-Interpolate panel
