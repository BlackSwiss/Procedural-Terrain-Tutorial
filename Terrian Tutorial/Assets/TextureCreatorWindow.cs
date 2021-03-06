﻿using UnityEditor;
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

    float brightness = 0.5f;
    float contrast = 0.5f;

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
        brightness = EditorGUILayout.Slider("Brightness", brightness, 0, 2);
        contrast = EditorGUILayout.Slider("Contrast", contrast, 0, 2);
        //Toggles
        alphaToggle = EditorGUILayout.Toggle("Alpha?", alphaToggle);
        mapToggle = EditorGUILayout.Toggle("Map?", mapToggle);
        seamlessToggle = EditorGUILayout.Toggle("Seamless", seamlessToggle);

        //Horizontal space lets you put things across screen
        //For a space with something in the middle
        GUILayout.BeginHorizontal();
        
        //Push to right size
        GUILayout.FlexibleSpace();

        float minColor = 1;
        float maxColor = 0;

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
                    if (seamlessToggle)
                    {
                        //Percentage of height and width
                        //how much we need to blend
                        float u = (float)x / (float)w;
                        float v = (float)y / (float)h;
                        //Perlin noise value at (x,y)
                        float noise00 = Utils.fBM((x + perlinOffsetX) * perlinXScale, (y + perlinOffsetY) * perlinYScale, perlinOctaves, perlinPersistance) * perlinYScale;
                        //Perlin noise value at (0,1)
                        float noise01 = Utils.fBM((x + perlinOffsetX) * perlinXScale, (y + perlinOffsetY + h) * perlinYScale, perlinOctaves, perlinPersistance) * perlinHeightScale;
                        //Perlin noise value at (1,0)
                        float noise10 = Utils.fBM((x + perlinOffsetX+w) * perlinXScale, (y + perlinOffsetY) * perlinYScale, perlinOctaves, perlinPersistance) * perlinHeightScale;
                        //Perlin noise value at (1,1)
                        float noise11 = Utils.fBM((x + perlinOffsetX+w) * perlinXScale, (y + perlinOffsetY+h) * perlinYScale, perlinOctaves, perlinPersistance) * perlinYScale;
                        //Get different blended amounts of perlin noise so it is seamless
                        float noiseTotal = u * v * noise00 + u * (1 - v) * noise01 + (1 - u) * v * noise10 + (1 - u) * (1 - v) * noise11;

                        //Split into rgb values then find gray value
                        float value = (int)(256 * noiseTotal) + 50;
                        //Clamp between 0 to 255 so it wont exceed, want to add up to a grayscale value
                        float r = Mathf.Clamp((int)noise00, 0, 255);
                        float g = Mathf.Clamp(value, 0, 255);
                        float b = Mathf.Clamp(value+50, 0, 255);
                        float a = Mathf.Clamp(value+100, 0, 255);

                        //Add and divide by 3 so it is grayscale
                        pValue = (r + g + b) / (3 * 255.0f);

                    }
                    else
                    {
                        //Set pvalue using fBM(fractual brownian motion) function
                        pValue = Utils.fBM((x + perlinOffsetX) * perlinXScale, (y + perlinOffsetY) * perlinYScale, perlinOctaves, perlinPersistance) * perlinHeightScale;
                    }
                    //using brightness equation
                    float colValue = contrast*(pValue - 0.5f)+0.5f*brightness;

                    //Will be extreme ranges once finished
                    if (minColor > colValue) minColor = colValue;
                    if (maxColor < colValue) maxColor = colValue;

                    //Pixel color set to color value to get grayscale, set alpha value around the toggle, 0 or 1
                    pixCol = new Color(colValue, colValue, colValue, alphaToggle ? colValue : 1);
                    //Set pixel
                    pTexture.SetPixel(x, y, pixCol);
                }
            }
            if (mapToggle)
            {
                
                for(int y = 0; y < h; y++)
                {
                    for(int x = 0; x < w; x++)
                    {
                        //pixel color and process pixel color
                        pixCol = pTexture.GetPixel(x, y);
                        //Setting pixel color as red as they are the same value and can use r g b 
                        float colValue = pixCol.r;
                        //Map color values
                        colValue = Utils.Map(colValue, minColor, maxColor, 0, 1);
                        //Put new values in all colors
                        pixCol.r = colValue;
                        pixCol.g = colValue;
                        pixCol.b = colValue;
                        pTexture.SetPixel(x, y, pixCol);

                    }
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
            //create array of bytes
            byte[] bytes = pTexture.EncodeToPNG();
            //Create directory to put them in, asset folder
            System.IO.Directory.CreateDirectory(Application.dataPath + "/SavedTextures");
            File.WriteAllBytes(Application.dataPath + "/SavedTextures/" + filename + ".png", bytes);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

    }

}
