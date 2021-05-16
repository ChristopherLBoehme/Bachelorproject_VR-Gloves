using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class S2Controll : MonoBehaviour
{
    
    public bool szenarioFinished = false;
    public Slider slider1;
    public GameObject MenuControl;
    public GameObject Hover;

    public GameObject Content;
    public Vector3 contentTransformPos;

    public String completeUserInput = "";

    #region TaskChecklist
    
    private bool toggleButtonActivated = false;
    private bool sliderValueChanged = false;
    private bool scrollViewButtonClicked = false;
    private bool dwellTimeSelectionUsed = false;
    
    #endregion

    public Text ToggleTask;
    public Text SliderTask;
    public Text ScrollViewTask;
    public Text DwellTimeSelectionTask;
    public Text DwellTimeTimer;

    public int time = 10;
    public bool dwellRoutineStarted = false;

    #region Bools for Slider

    private bool bottomValueSet = false;
    private bool topValueSet = false;

    #endregion

    

    public void ResetSzenario()
    {
        szenarioFinished = false;
        toggleButtonActivated = false;
        sliderValueChanged = false;
        dwellTimeSelectionUsed = false;
        scrollViewButtonClicked = false;

        bottomValueSet = false;
        topValueSet = false;
        slider1.value = 0.5f;
        completeUserInput = "";

        ActivateTextGameObject(ToggleTask);
        ActivateTextGameObject(SliderTask);
        ActivateTextGameObject(ScrollViewTask);
        ActivateTextGameObject(DwellTimeSelectionTask);
        ActivateTextGameObject(DwellTimeTimer);
        ResetRectTransform();
        
        SetRoutineStartedFalse();
        GetComponent<StopWatch>().Reset();
        
        
    }

    public void CheckSzenFinished()
    {
        if (toggleButtonActivated && sliderValueChanged && scrollViewButtonClicked && dwellTimeSelectionUsed)
        {
            szenarioFinished = true;
            GetComponent<StopWatch>().Pause();
            MenuControl.GetComponent<MenuSzeneControl>().finishSzenario(2);
        }
        
    }
    public void Toggle(bool state)
    {
        if (!toggleButtonActivated)
        {
            completeUserInput = completeUserInput + "Toggle/";
            toggleButtonActivated = true;
            MenuControl.GetComponent<MenuSzeneControl>().RumbleBoth();
            MenuControl.GetComponent<MenuSzeneControl>().PlayAudioFeedback(1);
            GetComponentInParent<StopWatch>().NextLap();
            CheckSzenFinished();
        }
        
    }

    public void Slider()
    {
        if (!sliderValueChanged)
        {
            float currentSliderVal = slider1.value;

            if (currentSliderVal == 0.0f)
            {
                bottomValueSet = true;
                MenuControl.GetComponent<MenuSzeneControl>().RumbleBoth();
            
            }
            if (currentSliderVal == 1.0f)
            {
                topValueSet = true;
                MenuControl.GetComponent<MenuSzeneControl>().RumbleBoth();
            
            }

            if (bottomValueSet && topValueSet)
            {
                sliderValueChanged = true;
                MenuControl.GetComponent<MenuSzeneControl>().PlayAudioFeedback(1);
                completeUserInput = completeUserInput + "Slider/";
                GetComponentInParent<StopWatch>().NextLap();
            }
            CheckSzenFinished();
            
        }
        

    }

    public void ScrollView(bool lastEntry)
    {
        if (!scrollViewButtonClicked)
        {
            scrollViewButtonClicked = true;
            MenuControl.GetComponent<MenuSzeneControl>().RumbleBoth();
            MenuControl.GetComponent<MenuSzeneControl>().PlayAudioFeedback(1);
            completeUserInput = completeUserInput +"ScrollView/";
            GetComponentInParent<StopWatch>().NextLap();
            CheckSzenFinished();
        }
        
    }

    public void DwellTime()
    {
        if (!dwellRoutineStarted)
        {
           StartCoroutine(DwellTimeRoutine());
           dwellRoutineStarted = true;
        }
    }

    

    IEnumerator DwellTimeRoutine()
    {
        //dwellRoutineStarted = true;
        MenuControl.GetComponent<MenuSzeneControl>().RumbleBoth();
        for (var j = 0; j <= time; j++)
        {
            DwellTimeTimer.text =  (10-j).ToString();
            yield return new WaitForSeconds(1);
        }
        completeUserInput = completeUserInput +"DwellTime/";
        dwellTimeSelectionUsed = true;
        GetComponentInParent<StopWatch>().NextLap();
        MenuControl.GetComponent<MenuSzeneControl>().PlayAudioFeedback(1);
        CheckSzenFinished();
        
    }

    public void SetRoutineStartedFalse()
    {
        if (!dwellTimeSelectionUsed)
        {
            dwellRoutineStarted = false;
            DwellTimeTimer.text = 10.ToString();
        }
    }

    private void DeactivateTextGameObject(Text text)
    {
        var tempObject = text.gameObject;
        tempObject.SetActive(false);
    }

    private void ActivateTextGameObject(Text text)
    {
        var tempObject = text.gameObject;
        tempObject.SetActive(true);
    }

    //Resets the position of the content of the scroll view
    private void ResetRectTransform()
    {
        Content.GetComponent<RectTransform>().anchoredPosition = contentTransformPos;
    }

    //Saves the original position of the content of the scroll view
    private void Start()
    {
        contentTransformPos = Content.GetComponent<RectTransform>().anchoredPosition;
         
    }

    private void Update()
    {
        if (dwellTimeSelectionUsed)
        {
            DeactivateTextGameObject(DwellTimeSelectionTask);
            DeactivateTextGameObject(DwellTimeTimer);
        }

        if (toggleButtonActivated)
        {
            DeactivateTextGameObject(ToggleTask);
        }

        if (scrollViewButtonClicked)
        {
            DeactivateTextGameObject(ScrollViewTask);
        }

        if (sliderValueChanged)
        {
            DeactivateTextGameObject(SliderTask);
        }
        
    }
}
