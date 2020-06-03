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

    //Processing for height value
    public void RandomTerrain()
    {

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
