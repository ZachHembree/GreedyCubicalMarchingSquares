# Greedy Cubical Marching Squares
This is an implimentation of the Cubical Marching Squares algorithm for use with Unity Engine. As of yet it lacks lacks the amgiguous case resolution or sharp feature preservation features described in the original [National Taiwan University paper](https://graphics.cmlab.csie.ntu.edu.tw/CMS/), but it does support a WIP version of a voxel simplification feature that is designed to combine voxels with planar surfaces and minimize the number of polygons needed to represent flat areas while retaining a high level of detail.


![Image of unreduced and unreduced cabinet meshes](https://i.imgur.com/3Gkreem.png)


## Usage
In order to use this, you'll need to create an instance of one of the classes inheriting from GreedyCms.Volume, HeightMapVolume or MeshVolume, with the correct input, an IList<float>[][] heightmap or a UnityEngine.Mesh respectively, and use that to create an instance of the GreedyCms.Surface class and call GetMeshData to get the information needed to instantiate a new mesh.
