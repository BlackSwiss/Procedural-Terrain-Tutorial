using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

//Want this code to run in edit not when we press play
[ExecuteInEditMode]


public class CustomTerrain : MonoBehaviour {


    //Min and max height range
    //Can just use the name and not the value between scripts
    public Vector2 randomHeightRange = new Vector2(0, 0.1f);
    public Texture2D heightMapImage;
    //Scaling of heightmap
    public Vector3 heightMapScale = new Vector3(1, 1, 1);

    public bool reset = true;

    //PERLIN NOISE ----------------------------
    public float perlinXScale = 0.01f;
    public float perlinYScale = 0.01f;
    //want offsets so they dont look so symmetrical
    public int perlinOffsetX = 0;
    public int perlinOffsetY = 0;

    //MULTIPLE PERLIN ----------------------------
    //Create class that holds our values
    [System.Serializable]
    public class PerlinParameters
    {
        public float mPerlinXScale = 0.01f;
        public float mPerlinYScale = 0.01f;
        public int mPerlinOctaves = 3;
        public float mPerlinPersistance = 8;
        public float mPerlinHeightScale = 0.09f;
        public int mPerlinOffsetX = 0;
        public int mPerlinOffsetY = 0;
        //Allows to remove things from list
        public bool remove = false;
    }

    //VORONOI-------------------------
    public float voronoiFallOff = 0.2f;
    public float voronoiDropOff = 0.6f;
    public float voronoiMinHeight = 0.1f;
    public float voronoiMaxHeight = 0.5f;
    public int voronoiPeaks = 5;
    //Allow us to switch between these options
    public enum VoronoiType { Linear = 0, Power = 1, Combined  = 2, Challenge = 3}
    public VoronoiType voronoiType = VoronoiType.Linear;

    //Midpoint Displacement ----------------------
    public float MPDheightMin = -2f;
    public float MPDheightMax = 2f;
    public float MPDheightDampenerPower = 2.0f;
    public float MPDroughness = 2.0f;


    

    //Smooth
    public int smoothAmount = 1;

    //First empty list
    public List<PerlinParameters> perlinParameters = new List<PerlinParameters>()
    {
        new PerlinParameters()
    };

    //SPLATMAPS --------------------------------------
    [System.Serializable]
    public class SplatHeights
    {
        //Record texture and height we want that texture in
        public Texture2D texture = null;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        //Must have a slope
        public float minSlope = 0;
        public float maxSlope = 1.5f;
        //How far across tile you want to start before painting
        public Vector2 tileOffset = new Vector2(0, 0);
        //How big it will tile across surface
        public Vector2 tileSize = new Vector2(50, 50);
        public float noisex = 0.01f;
        public float noisey = 0.01f;
        public float noiseMultiplier = 0.1f;
        public float splatOffset = 0.1f;
        public bool remove = false;
    }

    //VEGETATION --------------------------------------------
    [System.Serializable]
    public class Vegetation
    {
        //More to add but some objects that affect trees
        public GameObject mesh;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 90;
        public float minScale = 0.5f;
        public float maxScale = 1.0f;
        public Color colour1 = Color.white;
        public Color colour2 = Color.white;
        public Color lightColour = Color.white;
        public float minRotation = 0;
        public float maxRotation = 360;
        //Control density within prototypes
        public float density = 0.5f;
        public bool remove = false;
    }
    public List<Vegetation> vegetation = new List<Vegetation>()
    {
        new Vegetation()
    };

    public int maxTrees = 5000;
    public int treeSpacing = 5;



    public List<SplatHeights> splatHeights = new List<SplatHeights>()
    {
        new SplatHeights()
    };

    //For our brownian motion
    public int perlinOctaves = 3;
    //Increasing amp each time going through octaves
    public float perlinPersistance = 8;
    public float perlinHeightScale = 0.09f;

    public Terrain terrain;
    public TerrainData terrainData;

    float[,] GetHeightMap()
    {
        //If not true use exisiting terrain
        if (!reset)
        {
            return terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        }
        //If false create new height map
        else
        {
            return new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
        }
    }

    //Vegetation methods for table
    //Method used for placing trees/rocks/etc onto terrain
    public void PlantVegetation()
    {
        //Create array of tree prototypes
        TreePrototype[] newTreePrototypes;
        //Set equal to length of table
        newTreePrototypes = new TreePrototype[vegetation.Count];
        //Keep track of index
        int tindex = 0;
        //Loop thru all
        foreach(Vegetation t in vegetation)
        {
            //Get new version of current tree
            newTreePrototypes[tindex] = new TreePrototype();
            newTreePrototypes[tindex].prefab = t.mesh;
            tindex++;
        }
        //Populates the table
        terrainData.treePrototypes = newTreePrototypes;

        //Create array of tree instances
        List<TreeInstance> allVegetation = new List<TreeInstance>();
        //First 2 for loops loop around the terrain x and z axis
        //We cant use heightmap so must use x and z
        for(int z = 0; z < terrainData.size.z;z += treeSpacing)
        {
            for(int x = 0; x < terrainData.size.x; x+= treeSpacing)
            {
                //For each tree, process where it will be on the terrain
                for(int tp = 0; tp < terrainData.treePrototypes.Length; tp++)
                {

                    //Density check
                    //If random value is greater than density value, dont plant
                    //Lower value = more space
                    if (UnityEngine.Random.Range(0.0f, 1.0f) > vegetation[tp].density) break;

                    //Get height and reduce to 0-1
                    float thisHeight = terrainData.GetHeight(x, z) / terrainData.size.y;
                    float thisHeightStart = vegetation[tp].minHeight;
                    float thisHeightEnd = vegetation[tp].maxHeight;

                    //Get steepness
                    //Values must be normalized so get x and z and divide by dimensions
                    float steepness = terrainData.GetSteepness(x / (float)terrainData.size.x, z / (float)terrainData.size.z);

                    //This if statement checks if the current tree can be placed at the specific height
                    if ((thisHeight >= thisHeightStart && thisHeight <= thisHeightEnd) && (steepness >= vegetation[tp].minSlope && steepness <= vegetation[tp].maxSlope))
                    {
                        TreeInstance instance = new TreeInstance();
                        //Set tree position between 0-1
                        //Randomize positions
                        instance.position = new Vector3((x + UnityEngine.Random.Range(-5.0f, 5.0f)) / terrainData.size.x, terrainData.GetHeight(x, z) / terrainData.size.y, (z +
                            UnityEngine.Random.Range(-5.0f, 5.0f)) / terrainData.size.z);

                        //Fix so only a small portion of tree is clipping thru floor
                        //Taking its value and finding out where it is in terms of the terrain
                        //
                        Vector3 treeWorldPos = new Vector3(instance.position.x * terrainData.size.x,
                            instance.position.y * terrainData.size.y,
                            instance.position.z * terrainData.size.z) + this.transform.position;

                        RaycastHit hit;
                        //Layer mask is layer terrain is on, on terrain layer
                        int layerMask = 1 << terrainLayer;
                        //Create array at (worldpos, cast at -vector3.up, wait for hit, casting 100 units, add in mask)
                        //Lift up by 10 units so can be placed on flat areas or down by 10 units
                        //Need to lift so it can hit terrain even if its flat
                        if (Physics.Raycast(treeWorldPos + new Vector3(0, 10, 0), -Vector3.up, out hit, 100, layerMask) || Physics.Raycast(treeWorldPos - new Vector3(0, 10, 0), Vector3.up, out hit, 100, layerMask))
                        {
                            //Find tree height using hit point from raycast
                            float treeHeight = (hit.point.y - this.transform.position.y) / terrainData.size.y;
                            //Use new x and y
                            instance.position = new Vector3(instance.position.x, treeHeight, instance.position.z);

                            //If tree cant raycast on the terrain then dont use it
                            //Set tree rotation
                            instance.rotation = UnityEngine.Random.Range(vegetation[tp].minRotation, vegetation[tp].minRotation);
                            //Type of tree in the list in order
                            instance.prototypeIndex = tp;
                            //Must have a color so trees arent invisible
                            //Now customizable
                            //Picks a random color out of 2 colors, Lerp blends both colors and chooses a color between them
                            instance.color = Color.Lerp(vegetation[tp].colour1, vegetation[tp].colour2, UnityEngine.Random.Range(0.0f, 1.0f));
                            instance.lightmapColor = vegetation[tp].lightColour;
                            //If we want to rescale instances we set here
                            //If we wanna keep consistent 
                            float s = instance.heightScale = UnityEngine.Random.Range(vegetation[tp].minScale, vegetation[tp].maxScale);
                            instance.heightScale = s;
                            instance.widthScale = s;

                            //If we want them different values
                            //instance.heightScale = UnityEngine.Random.Range(vegetation[tp].minScale,vegetation[tp].maxScale);
                            //instance.widthScale = UnityEngine.Random.Range(vegetation[tp].minScale,vegetation[tp].maxScale);

                            allVegetation.Add(instance);
                            //Check if we havent gone over the max trees
                            //Quit out and finish
                            if (allVegetation.Count >= maxTrees) goto TREESDONE;

                        }
                        //------------ADD THIS
                        instance.position = new Vector3(instance.position.x * terrainData.size.x / terrainData.alphamapWidth,
                                                 instance.position.y,
                                                     instance.position.z * terrainData.size.z / terrainData.alphamapHeight);
                        //------------ADD THIS

                        instance.rotation = UnityEngine.Random.Range(0, 360);
                        instance.prototypeIndex = tp;
                        instance.color = Color.white;
                        instance.lightmapColor = Color.white;
                    }
                }
            }
        }
        //All trees in array will be added onto terrain
        TREESDONE:
        terrainData.treeInstances = allVegetation.ToArray();
    }

    //Add and remove methods for table
    public void AddNewVegetation()
    {
        vegetation.Add(new Vegetation());
    }

    public void RemoveVegetation()
    {
        List<Vegetation> keptVegetation = new List<Vegetation>();
        for(int i = 0; i < vegetation.Count; i++)
        {
            if (!vegetation[i].remove){
                keptVegetation.Add(vegetation[i]);
            }
        }
        if(keptVegetation.Count == 0)
        {
            keptVegetation.Add(vegetation[0]);
        }
        vegetation = keptVegetation;
    }

    //Used for plus button on table
    //Add new height to list of splat heights
    public void AddnewSplatHeight()
    {
        //Add to list
        splatHeights.Add(new SplatHeights());
    }

    //Remove for minus sign
    public void RemoveSplatHeight()
    {
        List<SplatHeights> keptSplatHeights = new List<SplatHeights>();
        for(int i = 0; i < splatHeights.Count; i++)
        {
            if (!splatHeights[i].remove)
            {
                keptSplatHeights.Add(splatHeights[i]);
            }
        }
        //Must always have 1 in the list
        if(keptSplatHeights.Count == 0) //Dont want to keep any
        {
            keptSplatHeights.Add(splatHeights[0]); //add at least 1
        }
        splatHeights = keptSplatHeights;
    }

    //Return a float of the steepness value
    //Pass height map, current x and y, width and height(dont need to but makes more compact)
    float GetSteepness(float[,] heightmap, int x , int y , int width, int height)
    {
        //X and y position from height map
        float h = heightmap[x, y];
        //Get neighbour positions
        int nx = x + 1;
        int ny = y + 1;

        //if on the upper edge of the map find gradient by going backward
        //Tests if were on the edge of the heightmap
        //wrap around and get the pixel from the beginning as if it was seamless
        if (nx > width - 1) nx = x - 1;
        if (ny > height - 1) ny = y - 1;

        //heightmap value of neighbours - height
        float dx = heightmap[nx, y] - h;
        float dy = heightmap[x, ny] - h;
        //Store into a vector 2 so we can use .magnitude
        Vector2 gradient = new Vector2(dx, dy);

        //Or pythagorean theorem
        float steep = gradient.magnitude;

        return steep;

    }

    public void SplatMaps()
    {
        //New array
        SplatPrototype[] newSplatPrototypes;
        //Give count, for each texture in list we want that many spaces in prototype
        newSplatPrototypes = new SplatPrototype[splatHeights.Count];
        int spindex = 0;
        //Loops through all textures and adds them to prototypes with properties
        foreach(SplatHeights sh in splatHeights)
        {
            newSplatPrototypes[spindex] = new SplatPrototype();
            //Set texture to texture in heightmaps
            newSplatPrototypes[spindex].texture = sh.texture;
            newSplatPrototypes[spindex].tileOffset = sh.tileOffset;
            newSplatPrototypes[spindex].tileSize = sh.tileSize;
            //Apply texture to add pixel values
            newSplatPrototypes[spindex].texture.Apply(true);
            //Increment index
            spindex++;
        }
        //Like terrain.setHeights, applies textures into list
        terrainData.splatPrototypes = newSplatPrototypes;

        //Get our heights
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];
        
        //Loop through every position in splat map
        for(int y =0; y<terrainData.alphamapHeight; y++)
        {
            for(int x =0; x < terrainData.alphamapWidth; x++)
            {
                //Textures stored in array, layers = amount of textures we have
                float[] splat = new float[terrainData.alphamapLayers];
                //Loop thru textures
                for(int i = 0; i < splatHeights.Count; i++)
                {
                    //Will use perlin noise so it doesnt blend in a straight line
                    //Must be small so you dont notice wave
                    float noise = Mathf.PerlinNoise(x * splatHeights[i].noisex, y * splatHeights[i].noisey) * splatHeights[i].noiseMultiplier;
                    //offset used for overlapping textures making a blend effect
                    float offset = splatHeights[i].splatOffset + noise;
                    //Find start and stop height
                    float thisHeightStart = splatHeights[i].minHeight - offset;
                    float thisHeightStop = splatHeights[i].maxHeight + offset;
                    //Must test for steepness for blending
                    //Current heightmap position, width and height so we dont go outside of range
                    float steepness = terrainData.GetSteepness(y / (float)terrainData.alphamapHeight, x / (float)terrainData.alphamapWidth);
                    //float steepness = GetSteepness(heightMap, x, y, terrainData.heightmapWidth, terrainData.heightmapHeight);

                    //If values are in between start and stop
                    //Set value for that texture to 1
                    if((heightMap[x,y]>= thisHeightStart && heightMap[x,y]<= thisHeightStop) && (steepness >= splatHeights[i].minSlope && steepness <= splatHeights[i].maxSlope))
                    {
                        splat[i] = 1;
                    }
                }
                //Array is adding up to more than 1, we must normalize vectors
                //Normalize vector means dividing each value by its length, if 2 textures are set to 1, divide by 2
                NormalizeVector(splat);
                //Assign back into splatmap data
                for(int j = 0; j < splatHeights.Count; j++)
                {
                    splatmapData[x, y, j] = splat[j];
                }

            }
        }
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }

    //Take in array of floats
    //Used for splatmaps
    void NormalizeVector(float[] v)
    {
        float total = 0;
        //Gets total of every number in array
        for(int i = 0; i < v.Length; i++)
        {
            total += v[i];
        }
        //Divide each number by total
        for(int i = 0; i < v.Length; i++)
        {
            v[i] /= total;
        }
    }


    //A mathimatical function that will loop through all points in terrain
    public void Perlin()
    {
        //Get current height map
        float[,] heightMap = GetHeightMap();

        //Go through the depth and size of the map, height is actually depth in 3d
        for (int y = 0; y < terrainData.heightmapHeight; y++)
        {
            for(int x =0; x < terrainData.heightmapWidth; x++)
            {
                //Assigning not adding, = replace, += add on to whats exists
                //Using the perlin noise function to create the terrain
                //For just perlin noise: heightMap[x, y] = Mathf.PerlinNoise((x + perlinOffsetX) * perlinXScale, (y +perlinOffsetY) * perlinYScale);

                //For perlin noise and brownian motion
                heightMap[x, y] += Utils.fBM((x+perlinOffsetX) * perlinXScale, (y+perlinOffsetY) * perlinYScale, perlinOctaves, perlinPersistance) * perlinHeightScale;
            }
        }
        //Apply changes
        terrainData.SetHeights(0, 0, heightMap);
    }

    //Allows for multiple perlin functions in one terrain
    public void MultiplePerlinTerrain()
    {
        float[,] heightMap = GetHeightMap();
        for(int y = 0; y< terrainData.heightmapHeight; y++)
        {
            for(int x = 0; x < terrainData.heightmapWidth; x++)
            {
                //Loop around our list
                //For each set of parameters
                foreach(PerlinParameters p in perlinParameters)
                {
                    //Use the brownian motion equation
                    //Use += to add multiple perlin noise curves
                    heightMap[x,y] += Utils.fBM((x + p.mPerlinOffsetX) * p.mPerlinXScale, (y + p.mPerlinOffsetY) * p.mPerlinYScale, p.mPerlinOctaves, p.mPerlinPersistance) * p.mPerlinHeightScale;
                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    //Create new item in list
    public void AddNewPerlin()
    {
        //GUI table will add a row for us
        perlinParameters.Add(new PerlinParameters());
    }

    //remove tiem from list
    public void RemovePerlin()
    {
        //Create another list
        List<PerlinParameters> keptPerlinParameters = new List<PerlinParameters>();

        //Loop around current list
        for(int i = 0; i < perlinParameters.Count; i++)
        {
            //Any that wont be removed will go into new list
            if (!perlinParameters[i].remove)
            {
                keptPerlinParameters.Add(perlinParameters[i]);
            }
        }
        //Make sure we have one thing in the list at all times
        if (keptPerlinParameters.Count == 0) // dont want to keep any

        {
            keptPerlinParameters.Add(perlinParameters[0]);
        }
        perlinParameters = keptPerlinParameters;
    }

    public void Voronoi()
    {
        //Create height map
        float[,] heightMap = GetHeightMap();

        for (int p = 0; p < voronoiPeaks; p++)
        {
            Vector3 peak = new Vector3(UnityEngine.Random.Range(0, terrainData.heightmapWidth),
                                       UnityEngine.Random.Range(voronoiMinHeight, voronoiMaxHeight),
                                       UnityEngine.Random.Range(0, terrainData.heightmapHeight));


            //Get a random range that is within the terrain, raise it by a random amount
            //Y value is the height of the peak, (x,y,z)

            //Vector3 peak = new Vector3(UnityEngine.Random.Range(0, terrainData.heightmapWidth), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0, terrainData.heightmapHeight));

            //at the x and z value, we set the peak to whatever our y is
            if (heightMap[(int)peak.x, (int)peak.z] < peak.y)
                heightMap[(int)peak.x, (int)peak.z] = peak.y;
            else
                continue;

            //Get a peak location on height map
            Vector2 peakLocation = new Vector2(peak.x, peak.z);
            //Max distance the farthest point could be on the heightmap
            //Looking at completle opposite conrner of the peak location
            float maxDistance = Vector2.Distance(new Vector2(0, 0), new Vector2(terrainData.heightmapWidth, terrainData.heightmapHeight));

            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                for (int x = 0; x < terrainData.heightmapWidth; x++)
                {
                    //Stop us from processing the peak, if its the peak then were at the peak and dont want to do anything with its height
                    if (!(x == peak.x && y == peak.z))
                    {
                        //Get distance from the peak location to current location
                        //float distanceToPeak = Vector2.Distance(peakLocation, new Vector2(x, y)) *fallOff;
                        //each location on the heightmap will be equal to the height of the peak - (distance to peak / max distance so its under 1)
                        //no matter how far, you are going to get shorter peaks
                        //heightMap[x, y] = peak.y - (distanceToPeak / maxDistance);

                        //Using equations and trying to get a more round mountain
                        //More control and have value from 0 to 1
                        float distanceToPeak = Vector2.Distance(peakLocation, new Vector2(x, y)) / maxDistance;
                        float h;

                        //Different functiosn to be used with voronoi variables
                        if (voronoiType == VoronoiType.Combined)
                        {
                            h = peak.y - distanceToPeak * voronoiFallOff - Mathf.Pow(distanceToPeak, voronoiDropOff);
                        }
                        else if (voronoiType == VoronoiType.Power)
                        {
                            h = peak.y - Mathf.Pow(distanceToPeak, voronoiDropOff) * voronoiFallOff;
                        }
                        else if (voronoiType == VoronoiType.Challenge)
                        {
                            h = peak.y - Mathf.Pow(distanceToPeak * 3, voronoiFallOff) -  Mathf.Sin((float)(distanceToPeak * 2 * Math.PI)) / voronoiDropOff;
                        }
                        else
                        {
                            h = peak.y - distanceToPeak * voronoiFallOff;
                        }

                        /*//Height = equation were trying, can use different functions
                        //float h = peak.y - distanceToPeak * voronoiFallOff - Mathf.Pow(distanceToPeak, voronoiDropOff); //combined

                        //float h = peak.y - Math.Pow(distanceToPeak, voronoiDropOff)*voronoiFallOff; //power

                        float h = peak.y - distanceToPeak * voronoiFallOff - Mathf.Pow(distanceToPeak, voronoiDropOff); //linear
                        */

                        //Plug equation into heightmap
                        if(heightMap[x,y] < h)
                            heightMap[x, y] = h;

                    }
                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);

    }
    //Code for midpoint displacement
    public void MidpointDisplacement()
    {
        //Get height map
        float[,] heightMap = GetHeightMap();
        //Get width
        int width = terrainData.heightmapWidth - 1;
        //Set up square size to be the width
        int squareSize = width;
        float heightMin = MPDheightMin;
        float heightMax = MPDheightMax;
        float heightDampener = (float)Mathf.Pow(MPDheightDampenerPower, -1 * MPDroughness);

        //Specify where each vertex is during the function
        int cornerX, cornerY;
        int midX, midY;
        int pmidXL, pmidXR, pmidYU, pmidYD;

        //Set terrain to random height
        //Need a starting value to add to our average
        /*heightMap[0, 0] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[0,terrainData.heightmapHeight - 2] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[terrainData.heightmapWidth - 2, 0] = UnityEngine.Random.Range(0f, 0.2f);
        heightMap[terrainData.heightmapWidth - 2, terrainData.heightmapHeight - 2] = UnityEngine.Random.Range(0f, 0.2f);*/

        while (squareSize > 0)
        {
            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    //Midpoint will be all 4 corners added together /4
                    heightMap[midX, midY] = (float)((heightMap[x, y] + heightMap[cornerX, y] + heightMap[x, cornerY] + heightMap[cornerX, cornerY]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));
                }
            }
            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    pmidXR = (int)(midX + squareSize);
                    pmidYU = (int)(midY + squareSize);
                    pmidXL = (int)(midX - squareSize);
                    pmidYD = (int)(midY - squareSize);

                    if (pmidXL <= 0 || pmidYD <= 0 || pmidXR >= width - 1 || pmidYU >= width - 1) continue;

                    //Calculate square value for bottom side
                    heightMap[midX, y] = (float)((heightMap[midX,midY] + heightMap[x, y] + heightMap[midX,pmidYD]+heightMap[cornerX,y]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));
                    //Calculate square value for top side
                    heightMap[midX, cornerY] = (float)((heightMap[x, cornerY] + heightMap[midX, midY] + heightMap[cornerX, cornerY] + heightMap[midX, pmidYU]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));
                    //Calculate square value for left side
                    heightMap[x, midY] = (float)((heightMap[x, y] + heightMap[pmidXL, midY] + heightMap[x, cornerY] + heightMap[midX, midY]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));
                    //Calculate square value for right side
                    heightMap[cornerX, midY] = (float)((heightMap[cornerX, y] + heightMap[midX, midY] + heightMap[cornerX, cornerY] + heightMap[pmidXR, midY]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));

                }
            }
                    //First time we loop we wil have whole terrain, then half, etc.
                    squareSize = (int)(squareSize / 2.0f);
            heightMin *= heightDampener;
            heightMax *= heightDampener;
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    List<Vector2> GenerateNeighbours(Vector2 pos, int width, int height)
    {
        List<Vector2> neighbours = new List<Vector2>();
        for(int y = -1; y < 2; y++)
        {
            for(int x = -1; x < 2; x++)
            {
                if(!(x==0 && y == 0))
                {
                    Vector2 nPos = new Vector2(Mathf.Clamp(pos.x + x, 0, width - 1), Mathf.Clamp(pos.y + y, 0, height - 1));

                    if (!neighbours.Contains(nPos))
                        neighbours.Add(nPos);
                }
            }
        }
        return neighbours;
    }

    public void Smooth()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        //Display bar/ progress bar
        float smoothProgress = 0;
        //Title, text under bar, value of progress bar 0-1
        EditorUtility.DisplayProgressBar("Smoothing Terrain", "Progress", smoothProgress);

        for (int i = 0; i < smoothAmount; i++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                for (int x = 0; x < terrainData.heightmapWidth; x++)
                {
                    float avgHeight = heightMap[x, y];
                    //Loop around and grab all neighbours
                    List<Vector2> neighbours = GenerateNeighbours(new Vector2(x, y), terrainData.heightmapWidth, terrainData.heightmapHeight);
                    //Loop around each neighbour, add height
                    foreach (Vector2 n in neighbours)
                    {
                        avgHeight += heightMap[(int)n.x, (int)n.y];
                    }

                    //Put into heightmap where it is /count+1
                    heightMap[x, y] = avgHeight / ((float)neighbours.Count + 1);
                }
            }
            smoothProgress++;
            //Will give a value between 0-1
            EditorUtility.DisplayProgressBar("Smoothing Terrain", "Progress", smoothProgress/smoothAmount);
        }

        terrainData.SetHeights(0, 0, heightMap);
        //Gets rid of window
        EditorUtility.ClearProgressBar();
    }



    //Processing for height value
    public void RandomTerrain()
    {
        //WILL USE THIS IF WE WANTED A WHOLE NEW TERRAIN EVERYTIME WE HIT CREATE RANDOM TERRAIN
        /*Create new height map, 2d array of floats
        float[,] heightMap;
        //Set size of height map based on the terrain data, set dimension
        heightMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
        */

        //Get height starting from 0,0 for entire width and height and put into height map
        float[,] heightMap = GetHeightMap();

        //To loop around our height map, loop around x and y
        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            //Y in 2d plane would be z not y
            for(int z =0; z< terrainData.heightmapHeight; z++)
            {
                //To add to the height map use +=, to replace do =
                heightMap[x, z] += UnityEngine.Random.Range(randomHeightRange.x, randomHeightRange.y);

            }
        }
        //Apply all new height in array starting from 0,0
        //We can replace the 0,0 to start it at a different point if wanted
        //Since we are doing the whole map we use 0,0
        terrainData.SetHeights(0, 0, heightMap);

    }

    //Use image to set heights
    public void LoadTexture()
    {
        //Create empty height map
        float[,] heightMap;
        heightMap = GetHeightMap();

        //Loop thru the terrain
        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for(int z = 0; z < terrainData.heightmapHeight; z++)
            {
                //Set height map to be the same as the color in the texture
                //Get pixel color at the same positions on the x and y plane
                //Grayscale converts the color (black and white) it is all we need
                //Multiply by height scale so height increases
                heightMap[x, z] += heightMapImage.GetPixel((int)(x * heightMapScale.x), (int)(z * heightMapScale.z)).grayscale * heightMapScale.y;
            }
        }
        //Apply changes
        terrainData.SetHeights(0, 0, heightMap);
    }
    
    public void resetTerrain()
    {
        float[,] heightMap;
        heightMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];

        terrainData.SetHeights(0, 0, heightMap);
    }

    //Everytime you go back to editor after editing script
    private void OnEnable()
    {
        Debug.Log("Initialising Terrain Data");
        //Grab terrain
        terrain = this.GetComponent<Terrain>();
        //Grab all terrain Data
        //Also use Terrain.terrainData if you have more than 1 terrain on scene
        terrainData = Terrain.activeTerrain.terrainData;
    }

    public enum TagType { Tag = 0, Layer = 1 }
    [SerializeField]
    int terrainLayer = -1;

    void Awake()
    {
        //Will be used to create custom tags and automatically assign those tags onto objects

        //Serialized allow to sync values between editor and code

        //Tag manager given path to pick up default tags and other tags made
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        AddTag(tagsProp, "Terrain", TagType.Tag);
        AddTag(tagsProp, "Cloud", TagType.Tag);
        AddTag(tagsProp, "Shore", TagType.Tag);

        //Apply tag changes to tag database
        SerializedProperty layerProp = tagManager.FindProperty("layers");
        terrainLayer = AddTag(layerProp, "Terrain", TagType.Layer);
        tagManager.ApplyModifiedProperties();

        //Take this object
        this.gameObject.tag = "Terrain";
        this.gameObject.layer = terrainLayer;
    }

    int AddTag(SerializedProperty tagsProp, string newTag, TagType tType)
    {
        //Check if we found tag or layer
        bool found = false;
        //ensure the tag doesnt already exist
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(newTag)) { found = true; return i; }
        }
        //add your new tag
        if (!found && tType == TagType.Tag)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
            newTagProp.stringValue = newTag;
        }
        //add new layer
        else if (!found && tType == TagType.Layer)
        {
            for (int j = 8; j < tagsProp.arraySize; j++)
            {
                SerializedProperty newLayer = tagsProp.GetArrayElementAtIndex(j);
                //add layer in next empty slot
                if (newLayer.stringValue == "")
                {
                    Debug.Log("Adding New Layer: " + newTag);
                    newLayer.stringValue = newTag;
                    return j;
                }
            }
        }
        return -1;
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
