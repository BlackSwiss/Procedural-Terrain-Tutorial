using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EditorGUITable;

//Links the 2 scripts together
[CustomEditor(typeof(CustomTerrain))]

//Multiple objects will be edited
[CanEditMultipleObjects]

public class CustomTerrainEditor : Editor
{
    //Properties ----------------------------
    //Sync with height range in other script
    SerializedProperty randomHeightRange;
    SerializedProperty heightMapScale;
    SerializedProperty heightMapImage;
    SerializedProperty perlinXScale;
    SerializedProperty perlinYScale;
    SerializedProperty perlinOffsetX;
    SerializedProperty perlinOffsetY;
    SerializedProperty perlinOctaves;
    SerializedProperty perlinPersistance;
    SerializedProperty perlinHeightScale;
    SerializedProperty reset;
    SerializedProperty voronoiFallOff;
    SerializedProperty voronoiDropOff;
    SerializedProperty voronoiMinHeight;
    SerializedProperty voronoiMaxHeight;
    SerializedProperty voronoiPeaks;
    SerializedProperty voronoiType;
    SerializedProperty MPDheightMin;
    SerializedProperty MPDheightMax;
    SerializedProperty MPDheightDampenerPower;
    SerializedProperty MPDroughness;
    SerializedProperty smoothAmount;
   /* SerializedProperty noisex;
    SerializedProperty noisey;
    SerializedProperty noiseMultiplier;
    SerializedProperty splatOffset;
    */

    //Perlin noise
    GUITableState perlinParameterTable;
    SerializedProperty perlinParameters;

    //Splatmap
    GUITableState splatMapTable;
    SerializedProperty splatHeights;

    //Vegetation
    GUITableState vegetationTable;
    SerializedProperty vegetation;
    SerializedProperty maxTrees;
    SerializedProperty treeSpacing;

    //Details
    GUITableState detailTable;
    SerializedProperty details;
    SerializedProperty maxDetails;
    SerializedProperty detailSpacing;

    //Water
    SerializedProperty waterHeight;
    SerializedProperty waterGO;
    SerializedProperty shoreLineMaterial;

    //Erosion
    SerializedProperty erosionType;
    SerializedProperty erosionStrength;
    SerializedProperty springsPerRiver;
    SerializedProperty solubility;
    SerializedProperty droplets;
    SerializedProperty erosionSmoothAmount;
    SerializedProperty erosionAmount;

    //fold outs -----------------------
    bool showRandom = false;
    bool showLoadHeights = false;
    bool showPerlinNoise = false;
    bool showMultiplePerlin = false;
    bool showVoronoi = false;
    bool showMidpoint = false;
    bool showSmooth = false;
    bool showSplatMaps = false;
    bool showHeights = false;
    bool showVegetation = false;
    bool showDetail = false;
    bool showWater = false;
    bool showErosion = false;

    Texture2D hmTexture;

    //everytime we add something new in editor, terrain will renable and rerun initialization
    //Dont need to press play everytime to see changes
    void OnEnable()
    {
        //Links the values together, sync with the inspector
        randomHeightRange = serializedObject.FindProperty("randomHeightRange");
        heightMapScale = serializedObject.FindProperty("heightMapScale");
        heightMapImage = serializedObject.FindProperty("heightMapImage");
        perlinXScale = serializedObject.FindProperty("perlinXScale");
        perlinYScale = serializedObject.FindProperty("perlinYScale");
        perlinOffsetX = serializedObject.FindProperty("perlinOffsetX");
        perlinOffsetY = serializedObject.FindProperty("perlinOffsetY");
        perlinOctaves = serializedObject.FindProperty("perlinOctaves");
        perlinPersistance = serializedObject.FindProperty("perlinPersistance");
        perlinHeightScale = serializedObject.FindProperty("perlinHeightScale");
        reset = serializedObject.FindProperty("reset");
        perlinParameterTable = new GUITableState("perlinParameterTable");
        perlinParameters = serializedObject.FindProperty("perlinParameters");
        voronoiDropOff = serializedObject.FindProperty("voronoiDropOff");
        voronoiFallOff = serializedObject.FindProperty("voronoiFallOff");
        voronoiPeaks = serializedObject.FindProperty("voronoiPeaks");
        voronoiMinHeight = serializedObject.FindProperty("voronoiMinHeight");
        voronoiMaxHeight = serializedObject.FindProperty("voronoiMaxHeight");
        voronoiType = serializedObject.FindProperty("voronoiType");
        MPDheightMin = serializedObject.FindProperty("MPDheightMin");
        MPDheightMax = serializedObject.FindProperty("MPDheightMax");
        MPDheightDampenerPower = serializedObject.FindProperty("MPDheightDampenerPower");
        MPDroughness = serializedObject.FindProperty("MPDroughness");
        smoothAmount = serializedObject.FindProperty("smoothAmount");

        splatMapTable = new GUITableState("splatMapTable");
        splatHeights = serializedObject.FindProperty("splatHeights");

        vegetationTable = new GUITableState("vegetationTable");
        vegetation = serializedObject.FindProperty("vegetation");
        maxTrees = serializedObject.FindProperty("maxTrees");
        treeSpacing = serializedObject.FindProperty("treeSpacing");

        detailTable = new GUITableState("detailTable");
        details = serializedObject.FindProperty("details");
        maxDetails = serializedObject.FindProperty("maxDetails");
        detailSpacing = serializedObject.FindProperty("detailSpacing");

        waterHeight = serializedObject.FindProperty("waterHeight");
        waterGO = serializedObject.FindProperty("waterGO");
        shoreLineMaterial = serializedObject.FindProperty("shoreLineMaterial");
        
        erosionType = serializedObject.FindProperty("erosionType");
        droplets = serializedObject.FindProperty("droplets");
        erosionStrength = serializedObject.FindProperty("erosionStrength");
        springsPerRiver = serializedObject.FindProperty("springsPerRiver");
        solubility = serializedObject.FindProperty("solubility");
        erosionSmoothAmount = serializedObject.FindProperty("erosionSmoothAmount");
        erosionAmount = serializedObject.FindProperty("erosionAmount");

        hmTexture = new Texture2D(513, 513, TextureFormat.ARGB32, false);
       /* noisex = serializedObject.FindProperty("noisex");
        noisey = serializedObject.FindProperty("noisey");
        noiseMultiplier = serializedObject.FindProperty("noiseMultiplier");
        splatOffset = serializedObject.FindProperty("splatOffset");
        */
    }


    //Keeps track of where you scroll
    Vector2 scrollPos;
    //Graphical user interface we will see in inspector for custom terrain editor
    //Will get long
    //Display loop for GUI inspector
    public override void OnInspectorGUI()
    {
        //Must always be on top, updates everything between this script and customterrain
        //Sync values for GUI
        serializedObject.Update();
        //Do any changes in here

        //Link to script on terrain not terrain itself
        CustomTerrain terrain = (CustomTerrain)target;

        //Scrollbar starting code
        Rect r = EditorGUILayout.BeginVertical();
        //If it exceeds height or width it will give scroll bar
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(r.width), GUILayout.Height(r.height));
        //Increase indent level in inspector, how far from left hand side, used so foldout arrows dont get covered up
        EditorGUI.indentLevel++;

        //If ticked will reset everytime you generate random
        EditorGUILayout.PropertyField(reset);

        //Little button to fold out or not
        showRandom = EditorGUILayout.Foldout(showRandom, "Random");

        //If they click the button it will show whats inside the field
        //Everything in fold out needs to be here
        if (showRandom)
        {
            //Makes a horizontal line, empty slider that looks like a line
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            //Puts text onto the screen, in bold
            GUILayout.Label("Set Heights Between Random Values", EditorStyles.boldLabel);

            //Display value we are interested in
            EditorGUILayout.PropertyField(randomHeightRange);

            //Puts a button on the screen and allows you to click it, takes a string, if statement executes when clicked
            if (GUILayout.Button("Random Heights"))
            {
                terrain.RandomTerrain();
            }


        }

        //Make a fold out called load heights
        showLoadHeights = EditorGUILayout.Foldout(showLoadHeights, "Load Heights");
        //If they press the fold out button
        if (showLoadHeights)
        {
            //Line for organization
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Load Heights From Texture", EditorStyles.boldLabel);
            //2 Properties fields to put an image and a (x y z) coords 
            EditorGUILayout.PropertyField(heightMapImage);
            EditorGUILayout.PropertyField(heightMapScale);
            //If they press the load Texture button
            if (GUILayout.Button("Load Texture"))
            {
                terrain.LoadTexture();
            }
        }


        showPerlinNoise = EditorGUILayout.Foldout(showPerlinNoise, "Single Perlin Noise");
        //Perlin noise menu
        if (showPerlinNoise)
        {
            //Bar and header
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Perlin Noise", EditorStyles.boldLabel);

            //Slider for both y and x values
            EditorGUILayout.Slider(perlinXScale, 0, 1, new GUIContent("X Scale"));
            EditorGUILayout.Slider(perlinYScale, 0, 1, new GUIContent("Y Scale"));

            //Sliders for perlin noise stuff
            EditorGUILayout.IntSlider(perlinOffsetX, 0, 10000, new GUIContent("Offset X"));
            EditorGUILayout.IntSlider(perlinOffsetY, 0, 10000, new GUIContent("Offset Y"));
            EditorGUILayout.IntSlider(perlinOctaves, 1, 10, new GUIContent("Octaves"));
            EditorGUILayout.Slider(perlinPersistance, 0.1f, 10, new GUIContent("Persistance"));
            EditorGUILayout.Slider(perlinHeightScale, 0, 1, new GUIContent("Height Scale"));

            //Button for perlin noise
            if (GUILayout.Button("Perlin"))
            {
                terrain.Perlin();
            }
        }

        showMultiplePerlin = EditorGUILayout.Foldout(showMultiplePerlin, "Multiple Perlin Noise");
        //Multiple perlin noise menu
        if (showMultiplePerlin)
        {
            //Labels
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Multiple Perlin Noise", EditorStyles.boldLabel);
            //Create table
            perlinParameterTable = GUITableLayout.DrawTable(perlinParameterTable, serializedObject.FindProperty("perlinParameters"));
            GUILayout.Space(20);
            //Add plus and minus button, horizontally next to each other
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddNewPerlin();
            }
            if (GUILayout.Button("-"))
            {
                terrain.RemovePerlin();
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Apply Multiple Perlin"))
            {
                terrain.MultiplePerlinTerrain();
            }
        }
        showVoronoi = EditorGUILayout.Foldout(showVoronoi, "Voronoi");
        //Vornoi dropout menu
        if (showVoronoi)
        {
            //Vornoi variables and min and max values
            EditorGUILayout.IntSlider(voronoiPeaks, 1, 10, new GUIContent("Peak Count"));
            EditorGUILayout.Slider(voronoiFallOff, 0, 10, new GUIContent("Falloff"));
            EditorGUILayout.Slider(voronoiDropOff, 0, 10, new GUIContent("Dropoff"));
            EditorGUILayout.Slider(voronoiMinHeight, 0, 1, new GUIContent("Min Height"));
            EditorGUILayout.Slider(voronoiMaxHeight, 0, 1, new GUIContent("Max Height"));
            EditorGUILayout.PropertyField(voronoiType);
            if (GUILayout.Button("Voronoi"))
            {
                terrain.Voronoi();
            }
        }

        showMidpoint = EditorGUILayout.Foldout(showMidpoint, "Midpoint");
        if (showMidpoint)
        {
            EditorGUILayout.PropertyField(MPDheightMin);
            EditorGUILayout.PropertyField(MPDheightMax);
            EditorGUILayout.PropertyField(MPDheightDampenerPower);
            EditorGUILayout.PropertyField(MPDroughness);
            if (GUILayout.Button("MPD"))
            {
                terrain.MidpointDisplacement();
            }
        }
        showSplatMaps = EditorGUILayout.Foldout(showSplatMaps, "Splat Maps");
        if (showSplatMaps)
        {

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Splat Maps", EditorStyles.boldLabel);

            //Sliders for modifying blend, for all the same modifications
            /*EditorGUILayout.Slider(noisex, 0, 0.1f, new GUIContent("Noise x value"));
            EditorGUILayout.Slider(noisey, 0.001f, 1, new GUIContent("Noise y value"));
            EditorGUILayout.Slider(noiseMultiplier, 0.001f, 1, new GUIContent("Noise Multiplier"));
            EditorGUILayout.Slider(splatOffset, 0, 1, new GUIContent("Offset/Blend value "));
            */

            splatMapTable = GUITableLayout.DrawTable(splatMapTable, serializedObject.FindProperty("splatHeights"));
            GUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddnewSplatHeight();
            }
            if (GUILayout.Button("-"))
            {
                terrain.RemoveSplatHeight();
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Apply SplatMaps"))
            {
                terrain.SplatMaps();
            }
        }
        showVegetation = EditorGUILayout.Foldout(showVegetation, "Vegetation");
        if (showVegetation)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Vegetation", EditorStyles.boldLabel);

            EditorGUILayout.IntSlider(maxTrees, 0, 10000, new GUIContent("Maximum Trees"));
            EditorGUILayout.IntSlider(treeSpacing, 2, 20, new GUIContent("Tree Spacing"));

            vegetationTable = GUITableLayout.DrawTable(vegetationTable, serializedObject.FindProperty("vegetation"));
            GUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddNewVegetation();
            }
            if (GUILayout.Button("-")){
                terrain.RemoveVegetation();
            }
            EditorGUILayout.EndHorizontal();
            if(GUILayout.Button("Apply Vegetation"))
            {
                terrain.PlantVegetation();
            }
        }
        //Foldout tab for detail
        showDetail = EditorGUILayout.Foldout(showDetail, "Details");
        if (showDetail)
        {
            //Label
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Details", EditorStyles.boldLabel);

            //Sliders
            EditorGUILayout.IntSlider(maxDetails, 0, 10000, new GUIContent("Maximum Detail"));
            EditorGUILayout.IntSlider(detailSpacing, 1, 20, new GUIContent("Detail Spacing"));

            //Table
            detailTable = GUITableLayout.DrawTable(detailTable, serializedObject.FindProperty("details"));
            //Allows you to see the details based on the max details slider
            terrain.GetComponent<Terrain>().detailObjectDistance = maxDetails.intValue;
            GUILayout.Space(20);

            //Add and remove buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddNewDetails();
            }
            if (GUILayout.Button("-"))
            {
                terrain.RemoveDetails();
            }
            EditorGUILayout.EndHorizontal();

            //Apply
            if(GUILayout.Button("Apply Details"))
            {
                terrain.AddDetails();
            }
        }

        showWater = EditorGUILayout.Foldout(showWater, "Water");
        if (showWater)
        {
            //Labels
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Water", EditorStyles.boldLabel);

            //Slider and game object holder
            EditorGUILayout.Slider(waterHeight, 0, 1, new GUIContent("Water Height"));
            EditorGUILayout.PropertyField(waterGO);

            //Button to add water
            if(GUILayout.Button("Add Water"))
            {
                terrain.AddWater();
            }

            //Material holder
            EditorGUILayout.PropertyField(shoreLineMaterial);
            if(GUILayout.Button("Add Shoreline"))
            {
                terrain.DrawShoreLine();
            }

        }
        showErosion = EditorGUILayout.Foldout(showErosion, "Erosion");
        if (showErosion)
        {
            //Labels
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Water", EditorStyles.boldLabel);

            //Sliders and stuff
            EditorGUILayout.PropertyField(erosionType);
            EditorGUILayout.Slider(erosionStrength, 0, 1, new GUIContent("Erosion Strength"));
            EditorGUILayout.IntSlider(droplets, 0, 500, new GUIContent("Droplets"));
            EditorGUILayout.Slider(erosionAmount, 0, 1, new GUIContent("Erosion Amount"));
            EditorGUILayout.Slider(solubility, 0.001f, 1, new GUIContent("Solubility"));
            EditorGUILayout.IntSlider(springsPerRiver, 0, 20, new GUIContent("Springs Per River"));
            EditorGUILayout.IntSlider(erosionSmoothAmount, 0, 10, new GUIContent("Smooth Amount"));

            if (GUILayout.Button("Erode"))
            {
                terrain.Erode();
            }


        }

        showSmooth = EditorGUILayout.Foldout(showSmooth, "Smooth");
        if (showSmooth)
        {
            EditorGUILayout.IntSlider(smoothAmount, 1, 10, new GUIContent("smoothAmount"));
            if (GUILayout.Button("Smooth"))
            {
                terrain.Smooth();
            }
        }

        //Code for reset button
        if (GUILayout.Button("Reset Heights"))
        {
            terrain.resetTerrain();
        }

        //Display height map
        showHeights = EditorGUILayout.Foldout(showHeights, "Height Map");
        if (showHeights)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            int hmtSize = (int)(EditorGUIUtility.currentViewWidth - 100);
            //Texture being displayed as a label
            GUILayout.Label(hmTexture, GUILayout.Width(hmtSize), GUILayout.Height(hmtSize));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            //Center button
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            //Button, will be same width as texture
            if (GUILayout.Button("Refresh", GUILayout.Width(hmtSize)))
            {
                //Get heightmap
                float[,] heightMap = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight);

                //go around heightmap
                for (int y = 0; y < terrain.terrainData.heightmapHeight; y++)
                {
                    for (int x = 0; x < terrain.terrainData.heightmapWidth; x++)
                    {
                        //set pixel color based on heightmap value
                        hmTexture.SetPixel(x, y, new Color(heightMap[x, y], heightMap[x, y], heightMap[x, y], 1));
                    }
                }
                hmTexture.Apply();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
            //Scrollbar ending code
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            //Must end with this, apply new changes
            serializedObject.ApplyModifiedProperties();
        }
    

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
