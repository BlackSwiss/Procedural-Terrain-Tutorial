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

    //PERLIN NOISE ----------------------------
    public float perlinXScale = 0.01f;
    public float perlinYScale = 0.01f;
    //want offsets so they dont look so symmetrical
    public int perlinOffsetX = 0;
    public int perlinOffsetY = 0;

    public Terrain terrain;
    public TerrainData terrainData;

    public void Perlin()
    {
        //Get current height map
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

        //Go through the depth and size of the map, height is actually depth in 3d
        for(int y = 0; y < terrainData.heightmapHeight; y++)
        {
            for(int x =0; x < terrainData.heightmapWidth; x++)
            {
                //Assigning not adding, = replace, += add on to whats exists
                //Using the perlin noise function to create the terrain
                heightMap[x, y] = Mathf.PerlinNoise((x + perlinOffsetX) * perlinXScale, (y +perlinOffsetY) * perlinYScale);
            }
        }
        //Apply changes
        terrainData.SetHeights(0, 0, heightMap);
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
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

        //To loop around our height map, loop around x and y
        for(int x = 0; x < terrainData.heightmapWidth; x++)
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
        heightMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];

        //Loop thru the terrain
        for(int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for(int z = 0; z < terrainData.heightmapHeight; z++)
            {
                //Set height map to be the same as the color in the texture
                //Get pixel color at the same positions on the x and y plane
                //Grayscale converts the color (black and white) it is all we need
                //Multiply by height scale so height increases
                heightMap[x, z] = heightMapImage.GetPixel((int)(x * heightMapScale.x), (int)(z * heightMapScale.z)).grayscale * heightMapScale.y;
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
