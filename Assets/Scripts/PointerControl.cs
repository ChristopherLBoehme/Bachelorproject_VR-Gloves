using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class PointerControl : MonoBehaviour
{
   
    #region Pointer Left Hand
    [Header("Pointer Left Hand")]
    public GameObject indexLStandard;
    public GameObject indexLLong;
    public GameObject middleLStandard;
    public GameObject pinkyLStandard;
    public GameObject ringLStandard;

    #endregion

    #region Pointer Righ Hand

    [Header("Pointer Right Hand")]
    public GameObject indexRStandard;
    public GameObject indexRLong;
    public GameObject middleRStandard;
    public GameObject pinkyRStandard;
    public GameObject ringRStandard;

    #endregion

    #region Szen3 Pointer Objects

    [Header("Szen3 Pointer Objects")]
    public GameObject redPointer;
    public GameObject bluePointer;
    public GameObject yellowPointer;
    public GameObject greenPointer;
    
    

    #endregion

    #region Szen3 MeshRender Referenzes
    [Header("Mesh Renderer References")]
    public MeshRenderer indexL ;
    public MeshRenderer middleL;
    public MeshRenderer ringL;
    public MeshRenderer pinkyL;
    
    public MeshRenderer indexR ;
    public MeshRenderer middleR;
    public MeshRenderer ringR;
    public MeshRenderer pinkyR;
    
    

    #endregion

    private VRTK_Pointer pointerLeft;
    private VRTK_UIPointer uiPointerLeft;

    private VRTK_Pointer pointerRight;
    private VRTK_UIPointer uiPointerRight;

    public void Start()
    {
        //deactivate pointer for szen 3 to make sure they are not active
        ResetColorPointers();
        SetMeshRendererReferences();
        ToggleMeshRenderer(1);
        ToggleMeshRenderer(2);
        
        pointerLeft = indexLLong.GetComponentInChildren<VRTK_Pointer>();
        uiPointerLeft = indexLLong.GetComponentInChildren<VRTK_UIPointer>();

        pointerRight = indexRLong.GetComponentInChildren<VRTK_Pointer>();
        uiPointerRight = indexRLong.GetComponentInChildren<VRTK_UIPointer>();
    }

    public void ResetColorPointers()
    {
        DeactivatePointer(redPointer);
        DeactivatePointer(bluePointer);
        DeactivatePointer(yellowPointer);
        DeactivatePointer(greenPointer);
    }

    public void SetPointers(int i)
    {
        /*Zu beginn Standard Pointer an beiden Händen aktiviert, nach auswahl welche Hand deaktivieren Pointer anderer Hand
         * wenn long pointer aktiviert wird andere pointer deaktivieren.         
         */
        
        //Left Hand Standard Pointer
        if (i == 1)
        {
            
            ActivatePointer(indexLStandard);
            ActivatePointer(middleLStandard);
            ActivatePointer(pinkyLStandard);
            ActivatePointer(ringLStandard);
            
            DeactivatePointer(indexLLong);
            DeactivatePointer(indexRLong);
            DeactivatePointer(indexRStandard);
            DeactivatePointer(middleRStandard);
            DeactivatePointer(pinkyRStandard);
            DeactivatePointer(ringRStandard);
        }
        //Right Hand Standard Pointer
        if (i == 2)
        {
            ActivatePointer(indexRStandard);
            ActivatePointer(middleRStandard);
            ActivatePointer(pinkyRStandard);
            ActivatePointer(ringRStandard);
            
            DeactivatePointer(indexLLong);
            DeactivatePointer(indexRLong);
            DeactivatePointer(indexLStandard);
            DeactivatePointer(middleLStandard);
            DeactivatePointer(pinkyLStandard);
            DeactivatePointer(ringLStandard);
        }
        
        
        //Left Hand Long Pointer

        if (i ==3)
        {
            
            DeactivatePointer(indexLStandard);
            DeactivatePointer(middleLStandard);
            DeactivatePointer(pinkyLStandard);
            DeactivatePointer(ringLStandard);
            
            ActivatePointer(indexLLong);
        }
        
        //Right Hand Long Pointer
        if (i == 4)
        {
            DeactivatePointer(indexRStandard);
            DeactivatePointer(middleRStandard);
            DeactivatePointer(pinkyRStandard);
            DeactivatePointer(ringRStandard);
        
            ActivatePointer(indexRLong);

            
        }
        
        
        //Szen3 Red
        if (i == 5)
        {
            ActivatePointer(redPointer);
        }
        
        //Szen3 Blue
        if (i == 6)
        {
            ActivatePointer(bluePointer);
        }
        
        //Szen3 Green
        if (i== 7)
        {
            ActivatePointer(greenPointer);
        }
        
        //Szen3 Yellow
        if (i == 8)
        {
            ActivatePointer(yellowPointer);
        }

        if (i == 9)
        {
            ResetColorPointers();
        }
    }

    
    private void ActivatePointer(GameObject gameObject)
    {
        gameObject.SetActive(true);
    }

    private void DeactivatePointer(GameObject gameObject)
    {
        gameObject.SetActive(false);
    }

    public void ToggleVrtkPointer(int i)
    {
        if (i == 1)
        {
            pointerLeft.enabled = !pointerLeft.enabled;
        }

        if (i == 2)
        {
            pointerRight.enabled = !pointerRight.enabled;
        }
        
    }
    
    public void ToggleVrtkUiPointer(int i)
    {
        if (i ==1)
        {
            uiPointerLeft.enabled = !uiPointerLeft.enabled;
        }

        if (i==2)
        {
            uiPointerRight.enabled = !uiPointerRight.enabled;
        }
    }

    public void SetMeshRendererReferences()
    {
        indexL = indexLStandard.GetComponentInChildren<MeshRenderer>();
        middleL = middleLStandard.GetComponentInChildren<MeshRenderer>();
        ringL = ringLStandard.GetComponentInChildren<MeshRenderer>();
        pinkyL = pinkyLStandard.GetComponentInChildren<MeshRenderer>();

        indexR = indexRStandard.GetComponentInChildren<MeshRenderer>();
        middleR = middleRStandard.GetComponentInChildren<MeshRenderer>();
        ringR = ringRStandard.GetComponentInChildren<MeshRenderer>();
        pinkyR = pinkyRStandard.GetComponentInChildren<MeshRenderer>();
    }

    public void ToggleMeshRenderer(int i)
    {
        if (i == 1) //left Hand
        {
           
            indexL.enabled = !indexL.enabled;
            middleL.enabled = !middleL.enabled;
            ringL.enabled = !ringL.enabled;
            pinkyL.enabled = !pinkyL.enabled;
        }

        if (i ==2) // Right Hand
        {
            
            indexR.enabled = !indexR.enabled;
            middleR.enabled = !middleR.enabled;
            ringR.enabled = !ringR.enabled;
            pinkyR.enabled = !pinkyR.enabled;
        }
    }
    
    }
    
    

