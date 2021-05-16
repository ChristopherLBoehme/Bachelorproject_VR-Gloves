using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class CustomGesture
{
    public GameObject scriptManager;

    //bools for each flex value to check from outside if correct for this gesture
    public bool finger1flex1 = false; 
    public bool finger1flex2 = false;
    public bool finger2flex1 = false;
    public bool finger2flex2 = false;
    public bool finger3flex1 = false;
    public bool finger3flex2 = false;
    public bool finger4flex1 = false;
    public bool finger4flex2 = false;
    public bool finger5flex1 = false;
    public bool finger5flex2 = false;

    
    //bool if the gesture is correct
    public bool gestureComplete = false;
  
    
    //Border Values

    #region Border Values
    [Header("Thumb ")]
    [MinMax(1, "lowerSensor0")] public float upperSensor0 = 0.6f;
    public float lowerSensor0 = 0.5f;
    
    [MinMax(1, "lowerSensor1")] public float upperSensor1 = 0.1f;
    public float lowerSensor1 = 0.7f;
    
    [Header("Index (Red)")]
    [MinMax(1, "lowerSensor2")] public float upperSensor2 = 0.7f;
    public float lowerSensor2 = 0.1f;
    
    [MinMax(1, "lowerSensor3")] public float upperSensor3 = 0.7f;
    public float lowerSensor3 = 0.1f;
    
    [Header("Middle (Blue)")]
    [MinMax(1, "lowerSensor4")] public float upperSensor4 = 0.7f;
    public float lowerSensor4 = 0.1f;
    
    [MinMax(1, "lowerSensor5")] public float upperSensor5 = 0.7f;
    public float lowerSensor5 = 0.1f;
    
    [Header("Ring (Green)")]
    [MinMax(1, "lowerSensor6")] public float upperSensor6 = 0.7f;
    public float lowerSensor6 = 0.1f;
    
    [MinMax(1, "lowerSensor7")] public float upperSensor7 = 0.7f;
    public float lowerSensor7 = 0.1f;
    
    [Header("Pinky (Yellow")]
    [MinMax(1, "lowerSensor8")] public float upperSensor8 = 0.7f;
    public float lowerSensor8 = 0.1f;
    
    [MinMax(1, "lowerSensor9")] public float upperSensor9 = 0.7f;
    public float lowerSensor9 = 0.1f;
    #endregion


    //Current Flex Values from the Game Manager
    public float[] flexValueArray = new float [10];
    private void GetFlexValues()
    {
       flexValueArray = scriptManager.GetComponent<ExampleDat>().GetFlexArray();

    }

    public void GestureComplete()
    {
        if (finger1flex1 && finger1flex2 && finger2flex1 && finger2flex2 && finger3flex1 && finger3flex2 && finger4flex1 && finger4flex2 && finger5flex1 && finger5flex2)
        {
            gestureComplete = true;
            
        }
        else
        {
            gestureComplete = false;
        }
    }

    public void Evaluate()
    {
        GetFlexValues();
        if (flexValueArray[0]>=lowerSensor0 && flexValueArray[0]<= upperSensor0)
        {
            finger1flex1 = true;
            
        }
        else
        {
            finger1flex1 = false;
        }

        if (flexValueArray[1]>=lowerSensor1 && flexValueArray[1]<=upperSensor1)
        {
            finger1flex2 = true;
        }
        else
        {
            finger1flex2 = false;
        }
        
        if (flexValueArray[2]>=lowerSensor2 && flexValueArray[2]<=upperSensor2)
        {
            finger2flex1 = true;
        }
        else
        {
            finger2flex1 = false;
        }
        
        if (flexValueArray[3]>=lowerSensor3 && flexValueArray[3]<=upperSensor3)
        {
           finger2flex2 = true;
        }
        else
        {
            finger2flex2 = false;
        }
        
        if (flexValueArray[4]>=lowerSensor4 && flexValueArray[4]<=upperSensor4)
        {
            finger3flex1 = true; 
        }
        else
        {
            finger3flex1 = false;
        }
        
        if (flexValueArray[5]>=lowerSensor5 && flexValueArray[5]<=upperSensor5)
        {
            finger3flex2 = true;
        }
        else
        {
            finger3flex2 = false;
        }
        
        if (flexValueArray[6]>=lowerSensor6 && flexValueArray[6]<=upperSensor6)
        {
            finger4flex1 = true;
        }
        else
        {
            finger4flex1 = false;
        }
        
        if (flexValueArray[7]>=lowerSensor7 && flexValueArray[7]<=upperSensor7)
        {
            finger4flex2 = true;
        }
        else
        {
            finger4flex2 = false;
        }
        
        if (flexValueArray[8]>=lowerSensor8 && flexValueArray[8]<=upperSensor8)
        {
            finger5flex1 = true;
        }
        else
        {
            finger5flex1 = false;
        }
        
        if (flexValueArray[9]>=lowerSensor9 && flexValueArray[9]<=upperSensor9)
        {
            finger5flex2 = true;
        }
        else
        {
            finger5flex2 = false;
        }
        GestureComplete();
    }
}
