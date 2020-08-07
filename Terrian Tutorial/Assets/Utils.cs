 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils{

    //Fractcal bornwian motion function
    public static float fBM(float x , float y , int oct, float persistance)
    {
        //Total height value
        float total = 0;
        //How close waves are together, changes with every octave
        float frequency = 1;
        //Scale with every new wave
        float amplitude = 1;
        //Max value, addition of each amp with each octave, used to bring range back to 0 and 1
        float maxValue = 0;
        for(int i = 0; i < oct; i++)
        {
            //Amp will get small with each wave
            total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistance;
            //Can change this around to get different results
            frequency *= 2;
        }
        return total / maxValue;
    }

    //Rescaling image
    public static float Map(float value, float originalMin, float originalMax, float targetMin,float targetMax)
    {
        return (value - originalMin) * (targetMax - targetMin) / (originalMax - originalMin) + targetMin;
    }

    public static System.Random r = new System.Random();
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        //Loops thru the list
        //swaps current value with random value
        while (n > 1)
        {
            //Go to next position
            n--;
            //gets random
            int k = r.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

}
