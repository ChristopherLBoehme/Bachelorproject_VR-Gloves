using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGesture : MonoBehaviour
{
    public GameObject menuSzeneControl;
    public GameObject s3Control;
    
    public CustomGesture myGesture1; // Test

    public CustomGesture longPointer;
    public CustomGesture activateLongPointer;
    
    //Szenario3
    public CustomGesture redGesture;
    public CustomGesture blueGesture;
    public CustomGesture yellowGesture;
    public CustomGesture greenGesture;

    #region Bools to Activate Gesture Check
    [Header("Activate the Gestures to Check")]
    public bool longPntr = false;
    public bool activateLngPntr = false;
    public bool redPntr = false;
    public bool bluePntr = false;
    public bool yellowPntr = false;
    public bool greenPntr = false;

    public bool checkForGestures = false;

    private bool valueChanged = false;
    private bool pointerToggled = false;

    private bool redToggled = false;
    private bool blueToggled = false;
    private bool greenToggled = false;
    private bool yellowToggled = false;
    

    #endregion
    
    //if set  true then evaluate the gesture
    private void ActiveGestures()
    {
        #region Szenario3

        if (redPntr)
        {
            redGesture.Evaluate();
        }

        if (bluePntr)
        {
            blueGesture.Evaluate();
        }

        if (yellowPntr)
        {
            yellowGesture.Evaluate();
        }

        if (greenPntr)
        {
            greenGesture.Evaluate();
        }
        #endregion

        #region Szen2

        if (longPntr)
        {
            longPointer.Evaluate();
        }

        if (longPntr && activateLngPntr )
        {
            activateLongPointer.Evaluate();
        }

        

        #endregion
    }
    
    

    private void Update()
    {
        if (checkForGestures)
        {
            ActiveGestures();
        }

        #region Szen2
        if (menuSzeneControl.GetComponent<MenuSzeneControl>().leftHand) 
        {
                if (longPointer.gestureComplete && (valueChanged ==false))
                {
                    menuSzeneControl.GetComponent<PointerControl>().SetPointers(3);
                    valueChanged = true;
                }
                else
                {
                    if (valueChanged && !longPointer.gestureComplete)
                    {
                        menuSzeneControl.GetComponent<PointerControl>().SetPointers(1);
                        valueChanged = false;
                    }
                }

                if (activateLongPointer.gestureComplete && longPointer.gestureComplete)
                {
                    if (!pointerToggled)
                    {
                        menuSzeneControl.GetComponent<PointerControl>().ToggleVrtkUiPointer(1);
                        pointerToggled = true;
                    }
                }
                else
                {
                    if (pointerToggled)
                    {
                        menuSzeneControl.GetComponent<PointerControl>().ToggleVrtkUiPointer(2);
                    }
                    pointerToggled = false;
                }
        }
        
        if (menuSzeneControl.GetComponent<MenuSzeneControl>().rightHand)
        {
            if (longPointer.gestureComplete && (valueChanged == false))
            {
                menuSzeneControl.GetComponent<PointerControl>().SetPointers(4);
                valueChanged = true;
            }
            else
            {
                if (valueChanged && !longPointer.gestureComplete)
                {
                    menuSzeneControl.GetComponent<PointerControl>().SetPointers(2);
                    valueChanged = false;
                }
            }
            if (activateLongPointer.gestureComplete && longPointer.gestureComplete)
            {
                if (!pointerToggled)
                {
                    menuSzeneControl.GetComponent<PointerControl>().ToggleVrtkUiPointer(2);
                    pointerToggled = true;
                    
                    

                }
            }
            else
            {
                if (pointerToggled)
                {
                    menuSzeneControl.GetComponent<PointerControl>().ToggleVrtkUiPointer(2);
                }
                pointerToggled = false;
            }
        }

        #endregion

        #region Szen3

        if (redPntr && greenPntr && yellowPntr && bluePntr)
        {
            var buttonref = s3Control.GetComponent<S3Controll>();
            if (redGesture.gestureComplete && !redToggled)
            {
                buttonref.RedButton();
                menuSzeneControl.GetComponent<PointerControl>().SetPointers(5);
                redToggled = true;
            }
            else
            {
                if (!redGesture.gestureComplete)
                {
                    redToggled = false;
                    menuSzeneControl.GetComponent<PointerControl>().SetPointers(9);
                    
                }
            }

            if (blueGesture.gestureComplete && !blueToggled)
            {
                buttonref.BlueButton();
                menuSzeneControl.GetComponent<PointerControl>().SetPointers(6);
                blueToggled = true;
            }
            else
            {
                if (!blueGesture.gestureComplete)
                {
                    blueToggled = false;
                    menuSzeneControl.GetComponent<PointerControl>().SetPointers(9);
                }
            }

            if (greenGesture.gestureComplete && !greenToggled)
            {
                buttonref.GreenButton();
                menuSzeneControl.GetComponent<PointerControl>().SetPointers(7);
                greenToggled = true;
            }
            else
            {
                if (!greenGesture.gestureComplete)
                {
                    greenToggled = false;
                    menuSzeneControl.GetComponent<PointerControl>().SetPointers(9);
                }
            }

            if (yellowGesture.gestureComplete && !yellowToggled)
            {
                buttonref.YellowButton();
                menuSzeneControl.GetComponent<PointerControl>().SetPointers(8);
                yellowToggled = true;
            }
            else
            {
                if (!yellowGesture.gestureComplete)
                {
                    yellowToggled = false;
                    menuSzeneControl.GetComponent<PointerControl>().SetPointers(9);
                }
            }
        }

        #endregion
    }
}
