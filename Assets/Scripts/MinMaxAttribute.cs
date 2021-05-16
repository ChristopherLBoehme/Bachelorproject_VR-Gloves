using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinMaxAttribute : PropertyAttribute
{
    public string minName = "$Min";
    public float? max;
    public float min;
    
    

    public MinMaxAttribute(string minName = null)
    {
        if (minName != null)
            this.minName = minName;
    }
    public MinMaxAttribute(float max, string minName = null) : this(0, max, minName) { }
    public MinMaxAttribute(float min, float max, string minName = null) : this(minName)
    {
        this.max = max;
        this.min = min;
    }
}
