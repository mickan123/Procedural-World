# Procedural-World

This repository is a Unity tool for procedural terrain generation that I have been working on. 


## Usage

To use this tool simply create a game object in Unity and attach the "Terrain Generator" script. From here you will need to configure the Terrain Settings and various Biomes. Each Biome is mainly configured via a node based GUI which allows you to construct height maps, place objects in the biome, and configure roads.

Below is an example of the node based editor. The top row connected by blue lines define the height map of the biome. In this case it is simplex noise that is scaled by 100 and then run through a Hydraulic Erosion simulation. Below this we can see several rows connected by pink lines representing objects to spawn in the biome. They are spawned either randomly or according to poisson disk sampling. They are then scaled, rotated randomly before being filtered by various conditions such as not spawning on roads, not spawning on terrain that is too steep or too high. On the far right we define the road settings used to spawn roads in this biome. 

![image](https://user-images.githubusercontent.com/20761702/158009828-67e9f69e-9158-4912-8a68-75624b458eed.png)

Below is an example of a single chunk of the above Biome.

![image](https://user-images.githubusercontent.com/20761702/158010066-341c1984-2096-46ab-94bc-f485eb26d1c6.png)

