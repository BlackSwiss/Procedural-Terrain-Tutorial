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
        //How far across tile you want to start before painting
        public Vector2 tileOffset = new Vector2(0, 0);
        //How big it will tile across surface
        public Vector2 tileSize = new Vector2(50, 50);
        public bool remove = false;
    }

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
    void Awake()
    {
        //Will be used to create custom tags and automatically assign those tags onto objects

        //Serialized allow to sync values between editor and code

        //Tag manager given path to pick up default tags and other tags made
        SerializedObject tagManager = new SerializedObject(
                                                            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        AddTag(tagsProp, "Terrain");
        AddTag(tagsProp, "Cloud");
        AddTag(tagsProp, "Shore");

        //Apply tag changes to tag database
        tagManager.ApplyModifiedProperties();

        //Take this object
        this.gameObject.tag = "Terrain";
    }

    void AddTag(SerializedProperty tagsProp, string newTag)
    {
        bool found = false;

        //ensure the tag doesn't already exist
        for(int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            //if tag exists, set found to true
            if (t.stringValue.Equals(newTag)) { found = true; break; }
        }

        //add your new tag if tag does not exist
        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
            newTagProp.stringValue = newTag;
        }
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
