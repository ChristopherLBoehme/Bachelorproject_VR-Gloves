using System;
using System.Collections;
using System.Collections.Generic;
using ManusVR.Core.Apollo;
using UnityEngine;
using UnityEngine.UI;
using VRTK;
using Random = UnityEngine.Random;

public class S3Controll : MonoBehaviour
{

   public GameObject MenuSzeneControl;
   
   private int usedHand = 0;

   #region ReferenceControllerObjects

   public GameObject BlueController;
   public GameObject RedController;
   public GameObject GreenController;
   public GameObject YellowController;
   
   #endregion

   #region Variables for Task Generation

   public int[] task;
   public int taskLength = 20;
   public int currentTaskPos=0;

   public string completeTaskString;
   public string completeUserColorInput = "";
   public int lastUserInput = 0;

   public Text taskInfo;
   public bool szenFinished = false;
   
   #endregion

   public void HandVar()
   {
      if (MenuSzeneControl.GetComponent<MenuSzeneControl>().leftHand)
      {
         usedHand = 1; // left Hand
      }

      if (MenuSzeneControl.GetComponent<MenuSzeneControl>().rightHand)
      {
         usedHand = 2; // right Hand
      }
   }

   public void OnEnable()
   {
      HandVar();
      if (usedHand == 1)
      {
         //Activate the colored spheres as indicator which finger to use
         MenuSzeneControl.GetComponent<PointerControl>().ToggleMeshRenderer(1);
      }

      if (usedHand == 2)
      {
         //Activate the colored spheres as indicator which finger to use
         MenuSzeneControl.GetComponent<PointerControl>().ToggleMeshRenderer(2);
      }
      
   }

   public void RedButton()
   {
      GetComponent<StopWatch>().NextLap();
      //lastUserInput = 1;
      UserInput(1);
   }

   public void BlueButton()
   {
      GetComponent<StopWatch>().NextLap();
      //lastUserInput = 2;
      UserInput(2);
   }

   public void GreenButton()
   {
      GetComponent<StopWatch>().NextLap();
      //lastUserInput = 3;
      UserInput(3);
   }

   public void YellowButton()
   {
      GetComponent<StopWatch>().NextLap();
      //lastUserInput = 4;
      UserInput(4);
   }

   public void CreateTask()
   {
      var lastEntry = 0;
      for (int i = 0; i < taskLength; i++)
      {
         task[i] = RandomNumber();
         while (task[i]==lastEntry)
         {
            task[i] = RandomNumber();
         }

         lastEntry = task[i];
      }

      string taskString = "";
      for (int j = 0; j < taskLength; j++)
      {
         taskString = taskString + task[j].ToString();
      }
      completeTaskString = taskString;
   }

   private int RandomNumber()
   {
      var tempInt = Random.Range(1, 5);
      return tempInt;
   }

   public void Start()
   {
      task = new int[taskLength];
      CreateTask();
      ChangeTaskColor();
   }

   public void ResetSz3()
   {
      szenFinished = false;
      currentTaskPos = 0;
      lastUserInput = 0;
      completeTaskString = "";
      completeUserColorInput = "";
      task = new int[taskLength];
      CreateTask();
      ChangeTaskColor();
      GetComponent<StopWatch>().Reset();
   }

   private void UserInput(int i)
   {
      lastUserInput = i;
      completeUserColorInput = completeUserColorInput + i.ToString();
      CheckWithTask(i);
   }

   private void CheckWithTask(int i)
   {
      //winning condition
      if (currentTaskPos == taskLength-1 && lastUserInput ==task[currentTaskPos])
      {
         
         szenFinished = true;
         MenuSzeneControl.GetComponent<MenuSzeneControl>().RumbleBoth();
         MenuSzeneControl.GetComponent<MenuSzeneControl>().PlayAudioFeedback(1);
         GetComponent<StopWatch>().Pause();
         MenuSzeneControl.GetComponent<MenuSzeneControl>().finishSzenario(3);
         return;
      }

      if (task[currentTaskPos]== i && !szenFinished ) 
      {
         if (currentTaskPos < 9)
         {
            currentTaskPos += 1;
         }

         MenuSzeneControl.GetComponent<MenuSzeneControl>().RumbleBoth();
         MenuSzeneControl.GetComponent<MenuSzeneControl>().PlayAudioFeedback(1);
         
         //Change Task Color
         ChangeTaskColor();
      }
      else
      {
         MenuSzeneControl.GetComponent<MenuSzeneControl>().PlayAudioFeedback(2);
      }
   } 
 

   private void ChangeTaskColor()
   {
      if (task[currentTaskPos] == 1)
      {
         //Red
         taskInfo.text = "Activate Red";
      }
      if (task[currentTaskPos] == 2)
      {
         //blue
         taskInfo.text = "Activate Blue";
      }
      if (task[currentTaskPos] == 3)
      {
         //green
         taskInfo.text = "Activate Green";
      }
      if (task[currentTaskPos] == 4)
      {
         //yellow
         taskInfo.text = "Activate Yellow";
      }
   }
}
