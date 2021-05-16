using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;


public class RandomNumberInput : MonoBehaviour
{
    public GameObject MenuControl;
    
    
    [Header("Winning Number and length")]
    public int[] winningNumber;
    public string winningNumberString;
    public int numberLenght;

    [Header("User Input")]
    public int[] userNumber;
    public int input;
    public string completeUserInput= ""; //wird noch nicht nach create winning number resettet
    private int numberPos =0;
    private bool noWrongInput = true;
    private int lapCount = 0;

    [Header("Canvas Objects")]
    public Text winningText;
    public Text userText;

    public bool szenFinished = false;
    
    
    
    // Start is called before the first frame update
    void Start()
    {
        winningNumber = new int[numberLenght];
        userNumber = new int[numberLenght];
        CreateWinningNumber();
        
        
        
    }

    //Checks if the User Input is complete and the last number matches with the task
    private void WinningConditionCheck()
    {
        if (szenFinished == false)
        {
            if (winningNumber.Length == userNumber.Length && winningNumber[numberLenght-1]== userNumber[numberLenght-1]) 
            {
                        szenFinished = true;
                        GetComponent<StopWatch>().Pause();
                        MenuControl.GetComponent<MenuSzeneControl>().finishSzenario(1);
                        
            }
        }
        
    }

   /* private void OnDisable() // eventuell raus, weil gar kein OnEnable vorhanden, und szenFinished = false schon in der MSC?
    {
        szenFinished = false;
    }
    */


    public void CreateWinningNumber()
    {
        //last Created Number for winningNumber
        var lastEntry = 0;
        
          //Create Number  
        for (int i = 0; i < numberLenght; i++)
        {
            //Creates the First Number Entry
            winningNumber[i] = RandomNumber();
            //changes the number to set if the previous one is the same
            while (winningNumber[i]== lastEntry)
            {
                winningNumber[i] = RandomNumber();
            }
            lastEntry = winningNumber[i];
        }
        
        //Create String to show Number on Canvas
        string winString="";
        for (int j = 0; j < numberLenght; j++)
        {
            winString = winString + winningNumber[j].ToString();
        }
        winningText.text = winString;
        winningNumberString = winString;
        
        //Reset User InputField
        userText.text = "userNumber";
        userNumber = new int[numberLenght];
        numberPos = 0;
        completeUserInput = "";
    }

    //Creates Random Number and returns it as an int
    private int RandomNumber()
    { 
        var tempInt = Random.Range(1, 10);
        return tempInt;
    }

    public void GetUserInput()
    {
        var feedbackRef = MenuControl.GetComponent<MenuSzeneControl>();
        feedbackRef.ControllerRumble();
        feedbackRef.GloveRumble();
        //input variable called by the buttons pass the corresponding number?
        completeUserInput = completeUserInput + input.ToString();
        userNumber[numberPos] = input;
        UpdateUserTextField();
        CheckUserInput();
        if (noWrongInput)
        { //input is correct
            numberPos += 1;
            feedbackRef.PlayAudioFeedback(1);
            
        }
        else
        { //when input is incorrect
            feedbackRef.PlayAudioFeedback(2);
        }

        WinningConditionCheck();
        lapCount = GetComponent<StopWatch>().lapValues.Count;
    }

    public void SetInput(GameObject Button)
    {
      string tempinput = Button.GetComponentInChildren<Text>().text;
      input = int.Parse(tempinput);
    }

    public void CheckUserInput()
    {
        if (winningNumber[numberPos] == userNumber[numberPos])
        {
            noWrongInput = true;
        }
        else
        {
            noWrongInput = false;
        }
    }

    void UpdateUserTextField() //funktioniert noch nicht
    {
        string userString="";
        for (int i = 0; i <= numberPos; i++)
        {
            userString = userString + userNumber[i].ToString();
        }
        userText.text = userString;
    }

    
    
}


