using UnityEditor;
using UnityEngine;
using System.IO;

public class TextureCreatorWindow : EditorWindow {

    //Using perlin noise to generate texture
    //Saves as this file
    string filename = "myProceduralTexture";

    float perlinXScale;
    float perlinYScale;
    int perlinOctaves;
    float perlinPersistance;
    float perlinHeightScale;
    int perlinOffsetX;
    int perlinOffsetY;
    //Tick boxes
    //Opacity
    bool alphaToggle = false;
    //Seamless textures
    bool seamlessToggle = false;
    //Remap all values
    bool mapToggle = false;

    Texture2D pTexture;

    [MenuItem("Window/TextureCreatorWindow")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(TextureCreatorWindow));
    }

    private void OnEnable()
    {
        pTexture = new Texture2D(513, 513, TextureFormat.ARGB32, false);
    }

    private void OnGUI()
    {
        //Top of window
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        //file name will be what we enter
        filename = EditorGUILayout.TextField("Texture Name", filename);

        int wSize = (int)(EditorGUIUtility.currentViewWidth - 100);

        //Perlin noise settings
        perlinXScale = EditorGUILayout.Slider("X Scale", perlinXScale, 0, 0.1f);
        perlinYScale = EditorGUILayout.Slider("X Scale", perlinYScale, 0, 0.1f);
        perlinOctaves = EditorGUILayout.IntSlider("Octaves", perlinOctaves, 1, 10);
        perlinPersistance = EditorGUILayout.Slider("Persistance", perlinPersistance, 1, 10);
        perlinHeightScale = EditorGUILayout.Slider("Height Scale", perlinHeightScale, 0, 1);
        perlinOffsetX = EditorGUILayout.IntSlider("Offset X", perlinOffsetX, 0, 10000);
        perlinOffsetY = EditorGUILayout.IntSlider("Offset Y", perlinOffsetY, 0, 10000);
        //Toggles
        alphaToggle = EditorGUILayout.Toggle("Alpha?", alphaToggle);
        mapToggle = EditorGUILayout.Toggle("Map?", mapToggle);
        seamlessToggle = EditorGUILayout.Toggle("Seamless", seamlessToggle);

        //Horizontal space lets you put things across screen
        //For a space with something in the middle
        GUILayout.BeginHorizontal();
        
        //Push to right size
        GUILayout.FlexibleSpace();

        //Button same size as the image
        //Allows for more
        if (GUILayout.Button("Generate", GUILayout.Width(wSize)))
        {
            int w = 513;
            int h = 513;
            float pValue;
            //Initally set to white
            Color pixCol = Color.white;
            //Loop around width and height of image
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    //Set pvalue using fBM(fractual brownian motion) function
                    pValue = Utils.fBM((x + perlinOffsetX) * perlinXScale, (y + perlinOffsetY) * perlinYScale, perlinOctaves, perlinPersistance) * perlinHeightScale;

                    //Set color value to pvalue
                    float colValue = pValue;
                    //Pixel color set to color value to get grayscale, set alpha value around the toggle, 0 or 1
                    pixCol = new Color(colValue, colValue, colValue, alphaToggle ? colValue : 1);
                    //Set pixel
                    pTexture.SetPixel(x, y, pixCol);
                }
            }
            //Apply to texture
            pTexture.Apply(false, false);
        }
        
        //More space
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        //Our texture that displays through a label with a width and height
        GUILayout.Label(pTexture, GUILayout.Width(wSize), GUILayout.Height(wSize));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        //Another button
        if (GUILayout.Button("Save", GUILayout.Width(wSize)))
        {

        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

    }

}
