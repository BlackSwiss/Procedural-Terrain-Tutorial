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

    GUITableState perlinParameterTable;
    SerializedProperty perlinParameters;

    //fold outs -----------------------
    bool showRandom = false;
    bool showLoadHeights = false;
    bool showPerlinNoise = false;
    bool showMultiplePerlin = false;
    bool showVoronoi = false;

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
    }

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
            if(GUILayout.Button("Random Heights"))
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
            if(GUILayout.Button("Apply Multiple Perlin"))
            {
                terrain.MultiplePerlinTerrain();
            }
        }
        showVoronoi = EditorGUILayout.Foldout(showVoronoi, "Voronoi");
        if (showVoronoi)
        {
            EditorGUILayout.IntSlider(voronoiPeaks, 1, 10, new GUIContent("Peak Count"));
            EditorGUILayout.Slider(voronoiFallOff, 0, 10, new GUIContent("Falloff"));
            EditorGUILayout.Slider(voronoiDropOff, 0, 10, new GUIContent("Dropoff"));
            EditorGUILayout.Slider(voronoiMinHeight, 0, 1, new GUIContent("Min Height"));
            EditorGUILayout.Slider(voronoiMaxHeight, 0, 1, new GUIContent("Max Height"));
            if (GUILayout.Button("Voronoi"))
            {
                terrain.Voronoi();
            }
        }

        //Code for reset button
        if (GUILayout.Button("Reset Heights"))
        {
            terrain.resetTerrain();
        }

        

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
