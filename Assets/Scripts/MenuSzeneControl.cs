using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using ManusVR.Core.Apollo;
using UnityEngine;
using UnityEngine.UI;
using VRTK;

public class MenuSzeneControl : MonoBehaviour
{
 //Struct for the saved data of each test
    [System.Serializable]
    struct DataSet
    {
        [SerializeField]
       public  string winString;
       [SerializeField]
       public  string userInput;

       [SerializeField] 
       public string total;
       [SerializeField]
       public  string lapValues;

   
    public DataSet(string winString, string userInput, List<float> lapValues)
        {
            this.winString = winString;
            this.userInput = userInput;
            total = lapValues.Sum().ToString(CultureInfo.CurrentCulture);
            this.lapValues = string.Join(" ", lapValues);
        }
    }
    
    
    //SaveData Szenario 1 Hand
    List<DataSet> sets = new List<DataSet>();
    
    //SaveData Szenario1 Controller
    List<DataSet> controllerSets = new List<DataSet>();
    
    //SaveData Szenario2 Hand
    List<DataSet> setsSz2Hand = new List<DataSet>();
    
    //SaveData Szenario2 Controller
    List<DataSet> setsSz2Controller = new List<DataSet>();
    
    //SaveData Szenario3 Hand
    List<DataSet> setsSz3Hand = new List<DataSet>();
    
    //SaveData Szenario3 Controller
    List<DataSet> setsSz3Controller = new List<DataSet>();
    
    
    [Header("Object Reference")]
    public GameObject leftRightChoose;
    public GameObject Szen1Info;
    public GameObject Szen1Task;
    public GameObject Szen2Info;
    public GameObject Szen2Task;
    public GameObject Szen3Info;
    public GameObject Szen3Task;
    public GameObject ManusVrHands;
    public GameObject Szen1Manager;
    public Transform playerPos;

    public GameObject AfterTask3Hand;
    public GameObject AfterTask3Controller;

    public GameObject TestGesture;

    public bool handActive = true;
    private int rotMultiplier = -40;

   // public string Sz1DataLine1;
   // public  string Sz1DataLine2;
   // public List<float> Sz1DataLine3;

    #region Szenario Finished bools
    [Header("Finish Bools")]
    //with Hand
    public bool szenario1FinishedHn = false;
    public bool szenario2FinishedHn = false;
    public bool szenario3FinishedHn = false;
    //with controller
    public bool szenario1FinishedCr = false;
    public bool szenario2FinishedCr = false;
    public bool szenario3FinishedCr = false;
   
    #endregion


    //The player has to select which hand he uses 1.
    public bool handSelected = false;

    //Counter for The Time sz1 has been started
    public int szen1Count = 1;

    //Reference to  the Audioclips for the AudioFeedback
    public AudioClip audioClipCorrect;
    public AudioClip audioClipWrong;
    public AudioSource audioSource;
    
    //Select at Game Start which hand is used
    public bool leftHand =false; //1
    public bool rightHand =false; //2

    //Set which hand is used from ui
    public void setHand(int i)
    {
        if (handSelected == false)
        {
            if (i == 1)
            {
                leftHand = true;
                handSelected = true;
               // showLeftRightChoose = false;
                startSzenario(0);
            }

            if (i == 2)
            {
                rightHand = true;
                handSelected = true;
             //   showLeftRightChoose = false;
                startSzenario(0);
            }
        }
    }

    public void finishSzenario(int i)
    {
       /* if (i == 0)
        {
            Debug.Log("Start info Szen 1 Read finished");
            return;
        } */
    
       //szenario 1
        if (i == 1)
        {    //marks sz 1 as finished after running through the 10th time
            if (szen1Count == 10)
            {
                if (handActive)
                {
                    szenario1FinishedHn = true;
                }

                if (!handActive)
                {
                    szenario1FinishedCr = true;
                }

                hideMenu(Szen1Task);
                showMenu(Szen2Info);
                CheckHandFinish();
                CheckControllerFinish();
            }

            if (handActive)
            {
                SaveLastAbsolvedSzen1(szen1Count);
            }

            if (!handActive)
            {
                SaveLastAbsolvedSzen1C(szen1Count);
            }
            hideMenu(Szen1Task);
            
            //resets saving variables when not the last round
            if ( szen1Count <10)
            {
                szen1Count += 1;
                showMenu(Szen1Info);
                Szen1Manager.GetComponent<RandomNumberInput>().CreateWinningNumber();
                Szen1Manager.GetComponent<StopWatch>().Reset();
                Szen1Manager.GetComponent<RandomNumberInput>().szenFinished = false;
            }
            
            return;
        }
        
        //szenario 2
        if (i ==2)
        {
            if (handActive)
            {
                SaveSzen2Hand();
                szenario2FinishedHn = true;
            }

            if (!handActive)
            {
                SaveSzen2Controller();
                szenario2FinishedCr = true;
            }
            //Deactivate the gesture check for the gestures only used in szen 2
            TestGesture.GetComponent<TestGesture>().longPntr = false;
            TestGesture.GetComponent<TestGesture>().activateLngPntr = false;
            
            //reset the used pointers
            if (leftHand)
            {
                GetComponent<PointerControl>().SetPointers(1);
            }

            if (rightHand)
            {
                GetComponent<PointerControl>().SetPointers(2);
            }
            
            hideMenu(Szen2Task);
            showMenu(Szen3Info);
            CheckHandFinish();
            CheckControllerFinish();
            return;
        }
        
        //szenario 3
        if (i == 3)
        {
            if (handActive)
            {
                SaveSzen3Hand();
                szenario3FinishedHn = true;
                TestGesture.GetComponent<TestGesture>().redPntr = false;
                TestGesture.GetComponent<TestGesture>().bluePntr = false;
                TestGesture.GetComponent<TestGesture>().greenPntr = false;
                TestGesture.GetComponent<TestGesture>().yellowPntr = false;
                hideMenu(Szen3Task);
                //showMenu(Info to take off Vr Glove and use Controller)
                showMenu(AfterTask3Hand);
            }

            if (!handActive)
            {
                SaveSzen3Controller();
                szenario3FinishedCr = true;
                hideMenu(Szen3Task);
                showMenu(AfterTask3Controller);
            }
            
            CheckHandFinish();
            CheckControllerFinish();
            return;
        }
    }


    public void startSzenario(int i)
    {
        if (i == 0)
        {
            //ChooseLeftRight finished -> show Szen1Info UI
           hideMenu(leftRightChoose);
           showMenu(Szen1Info);
        }
        //Szenario 1 with Hand
        if (i == 1)
        {
            //Szenario 1 started from Szen1Info UI
            hideMenu(Szen1Info);
            
            //Position Task UI in Front of Player
            Szen1Task.transform.position = playerPos.position +new Vector3(0,-1.25f,0.58f);
            showMenu(Szen1Task);
        }
        //Szenario 2 with Hand
        if (i ==2)
        {
            //Szenario 2 started from Szen2Info UI
            hideMenu(Szen2Info);
            //position task in front of player
            Szen2Task.transform.position = playerPos.position +new Vector3(0,0,0.58f);

            //Activate general checking for gestures
            TestGesture.GetComponent<TestGesture>().checkForGestures = true;
            
            //Activate the gestures for szenario 2
            TestGesture.GetComponent<TestGesture>().longPntr = true;
            showMenu(Szen2Task);
            
        }
        //Szenario 3 with Hand
        if (i == 3)
        {
            //Szen 3 started from Szen3Info UI
            hideMenu(Szen3Info);
            //position task in front of player
            Szen3Task.transform.position = playerPos.position +new Vector3(0,0,0.58f);
            showMenu(Szen3Task);
            
            //Activate the gesture Check for Szen3
            TestGesture.GetComponent<TestGesture>().redPntr = true;
            TestGesture.GetComponent<TestGesture>().bluePntr = true;
            TestGesture.GetComponent<TestGesture>().greenPntr = true;
            TestGesture.GetComponent<TestGesture>().yellowPntr = true;
            
            
            
            
        }
    }

    private void CheckHandFinish()
    {
        if (szenario1FinishedHn && szenario2FinishedHn && szenario3FinishedHn)
        {
            handActive = false;
            //Deactivates the visibility of the ManusVRUE4Arms prefab
            ManusVrHands.SetActive(false);
            //Deactivate Checking for Gestures
            TestGesture.GetComponent<TestGesture>().checkForGestures = false;
        }
    }

    private void CheckControllerFinish()
    {
        if (szenario1FinishedCr && szenario2FinishedCr && szenario3FinishedCr)
        {
            //Place for code after all szenarios are finished with all input devices
            Debug.Log("Finished all Task Congratulations");
        }
    }

    private void showMenu(GameObject gameObject)
    {
        gameObject.SetActive(true);
    }

    private void hideMenu(GameObject gameObject)
    {
        gameObject.SetActive(false);
    }

    private void SaveLastAbsolvedSzen1(int i)
    {
        var horst = Szen1Manager.GetComponent<RandomNumberInput>();
        var gunter = Szen1Manager.GetComponent<StopWatch>();

        if (i == 1)
        {
            ChangeFilename(1);
            CSVManager.CreateReport();
        }
        
        
        DataSet d = new DataSet(horst.winningNumberString, horst.completeUserInput, gunter.lapValues);
        sets.Add(d);
        
        CSVManager.AppendToReport(new string[]{d.winString, d.userInput,d.total, d.lapValues});
    }
    
    private void SaveLastAbsolvedSzen1C(int i)
    {
        var horst = Szen1Manager.GetComponent<RandomNumberInput>();
        var gunter = Szen1Manager.GetComponent<StopWatch>();

        if (i == 1)
        {
            ChangeFilename(2);
            CSVManager.CreateReport();
        }
        
        DataSet f = new DataSet(horst.winningNumberString, horst.completeUserInput,gunter.lapValues);
        controllerSets.Add(f);
        
        CSVManager.AppendToReport(new string[]{f.winString,f.userInput,f.total,f.lapValues});
    }

    public void BeginnControllerTask()
    {
        if (handActive == false)
        {   //Reset szen1
            szen1Count = 1;
            hideMenu(AfterTask3Hand);
            showMenu(Szen1Info);
            Szen1Manager.GetComponent<RandomNumberInput>().CreateWinningNumber();
            Szen1Manager.GetComponent<StopWatch>().Reset();
            Szen1Manager.GetComponent<RandomNumberInput>().szenFinished = false;
            //reset szen2
            Szen2Task.GetComponentInChildren<S2Controll>().ResetSzenario();
            //reset szen3
            Szen3Task.GetComponentInChildren<S3Controll>().ResetSz3();
            
            
        }
    }

    public void SaveSzen2Hand()
    {
        var horst = Szen2Task.GetComponentInChildren<S2Controll>();
        var gunter = Szen2Task.GetComponentInChildren<StopWatch>();

        ChangeFilename(3);
        CSVManager.CreateReport();
        
        DataSet g = new DataSet("No given WinningString",horst.completeUserInput,gunter.lapValues);
        setsSz2Hand.Add(g);
        
        CSVManager.AppendToReport(new string[]{g.userInput,g.total,g.lapValues});
    }

    public void SaveSzen2Controller()
    {
        var horst = Szen2Task.GetComponentInChildren<S2Controll>();
        var gunter = Szen2Task.GetComponentInChildren<StopWatch>();

        ChangeFilename(4);
        CSVManager.CreateReport();
        
        DataSet h = new DataSet("No given WinningString", horst.completeUserInput, gunter.lapValues);
        setsSz2Controller.Add(h);
        
        CSVManager.AppendToReport(new string[]{h.userInput,h.total,h.lapValues});
    }

    public void SaveSzen3Hand()
    {
        var horst = Szen3Task.GetComponentInChildren<S3Controll>();
        var gunter = Szen3Task.GetComponentInChildren<StopWatch>();

        ChangeFilename(5);
        CSVManager.CreateReport();
        
        DataSet k = new DataSet(horst.completeTaskString,horst.completeUserColorInput,gunter.lapValues);
        setsSz3Hand.Add(k);
        
        
        CSVManager.AppendToReport(new string[]{k.winString,k.userInput,k.total,k.lapValues});


    }

    public void SaveSzen3Controller()
    {
        var horst = Szen3Task.GetComponentInChildren<S3Controll>();
        var gunter = Szen3Task.GetComponentInChildren<StopWatch>();

        ChangeFilename(6);
        CSVManager.CreateReport();
        
        DataSet l = new DataSet(horst.completeTaskString, horst.completeUserColorInput,gunter.lapValues);
        setsSz3Controller.Add(l);
        
        CSVManager.AppendToReport(new string[]{l.winString,l.userInput,l.total,l.lapValues});
    }

    public void ChangeDirectoryName()
    {
       var time = System.DateTime.UtcNow.Ticks.ToString();
        CSVManager.ChangeDirectoryName("Report"+time);
    }


    
    
    public void ChangeHeader(int i)
    {
        if (i ==1)
        {
            string[] reportHeadersSz1 = new string[4]
            {
                "Aufgabenstring",
                "Kompletter User-Input",
                "Gesamtzeit (Sek.)",
                "Zwischenzeiten (Sek.)"
            };
            CSVManager.ChangeReportHeaders(reportHeadersSz1);
            
        }

        if (i == 2)
        {
            string[] reportHeadersSz2 = new string[3]
            {
                "Kompletter User-Input",
                "Gesamtzeit (Sek.)",
                "Zwischenzeiten (Sek.)"
            };
            CSVManager.ChangeReportHeaders(reportHeadersSz2);
        }
    }

    public void ChangeFilename(int i)
    {
        if (i == 1)
        {
            string szen1Hand = "szen1H.csv";
            CSVManager.ChangeReportFilename(szen1Hand);
            ChangeHeader(1);
        }

        if (i == 2)
        {
            string szen1Controller = "szen1C.csv";
            CSVManager.ChangeReportFilename(szen1Controller);
            ChangeHeader(1);
        }

        if (i == 3)
        {
            string szen2Hand = "szen2H.csv";
            CSVManager.ChangeReportFilename(szen2Hand);
            ChangeHeader(2);
        }

        if (i == 4)
        {
            string szen2Controller = "szen2C.csv";
            CSVManager.ChangeReportFilename(szen2Controller);
            ChangeHeader(2);
        }

        if (i==5)
        {
            string szen3Hand = "szen3H.csv";
            CSVManager.ChangeReportFilename(szen3Hand);
            ChangeHeader(1);
        }

        if (i==6)
        {
            string szen3Controller = "szen3C.csv";
            CSVManager.ChangeReportFilename(szen3Controller);
            ChangeHeader(1);
        }
    }

    #region FeedBackMethods

    public void RumbleBoth()
    {
        GloveRumble();
        ControllerRumble();
    }

    public void GloveRumble()
    {
        if (leftHand)
        {
            Apollo.rumble(GloveLaterality.GLOVE_LEFT,250,65000);
        }

        if (rightHand)
        {
            Apollo.rumble(GloveLaterality.GLOVE_RIGHT,250,65000);
        }
    }

    public void ControllerRumble()
    {
        var leftControllerRef = VRTK_ControllerReference.GetControllerReference(SDK_BaseController.ControllerHand.Left);
        var rightControllerRef = VRTK_ControllerReference.GetControllerReference(SDK_BaseController.ControllerHand.Right);
        var strength = 1.0f;
        var duration = 0.00075f;
        var interval = 0.0001f;
        
        if ( VRTK_ControllerReference.IsValid(leftControllerRef))
        {
            VRTK_ControllerHaptics.TriggerHapticPulse(leftControllerRef,strength,duration,interval);
        }

        if (VRTK_ControllerReference.IsValid(rightControllerRef))
        {
            VRTK_ControllerHaptics.TriggerHapticPulse(rightControllerRef,strength,duration,interval);
        }
    }

    public void PlayAudioFeedback(int i)
    {
        if (i==1)
        {
            audioSource.PlayOneShot(audioClipCorrect,1);
        }

        if (i==2)
        {
            audioSource.PlayOneShot(audioClipWrong,1);
        }
        
    }

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        ChangeDirectoryName();   
        audioSource.GetComponent<AudioSource>();

    }

}
