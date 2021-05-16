using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ManusVR.Core.Apollo;
using ManusVR.Core.Hands;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class ExampleDat : MonoBehaviour
{
    public GameObject MenuSzeneControlReference;

    private string s;
    public string endpointID;
    public string deviceID;
    public string wrist;
    public string thumb;
    public string joints;
    public string flex;
    private string singleJoint;
    public Vector3[] jointArray = new Vector3[25];
    private string[] eachSensor;
    public float[] flexArray = new float[10];
    
    [Header("Collider Data")]
    public GameObject colIndexL;
    public GameObject colMiddleL;
    public GameObject colPinkyL;
    public GameObject colRingL;

    public Vector3 indexL;
    public Vector3 middleL;
    public Vector3 pinkyL;
    public Vector3 ringL;
    
    
    
    IEnumerator Start () 
    {
        // Rumble a glove after 3 seconds
        yield return new WaitForSeconds(3); // NICHT LOESCHEN
        // Rumble one of the gloves, 65000 is maximum power of rumbler
        //Apollo.rumble(GloveLaterality.GLOVE_LEFT, 1000, 65000);
        if (HandDataManager.CanGetHandData(1, device_type_t.GLOVE_LEFT))
        {
            Debug.Log("Linke Hand");
            Debug.Log(ApolloHandData.numJointsPerFinger);
            string[] delimiterStrings={"endpointID: ","deviceID: ","wrist (raw/processed): ","thumb: ","finger joints: ","flex sensor: "};
            s = HandDataManager.GetHandData(1, device_type_t.GLOVE_LEFT).ToString();
            string[] words = s.Split(delimiterStrings,StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                Debug.Log(word);
            }
            
            //erste stringteilung
            endpointID = words[0];
            deviceID = words[1];
            wrist = words[2];
            thumb = words[3];
            joints = words[4];
            flex = words[5];
            
            //aufteilen der Finger joint Daten
            string[] delChars = {" / "};
            string[] singleJoints = joints.Split(delChars,StringSplitOptions.RemoveEmptyEntries);
            foreach (var joint in singleJoints)
            {
                Debug.Log(joint);
            }

            for (int i = 0; i < 25; i++)
            {
                singleJoint = singleJoints[i];
                string[] delBrackets = {"(", ",",")"};
                string[] vectorJoints = singleJoint.Split(delBrackets,StringSplitOptions.RemoveEmptyEntries);
               jointArray[i] = new Vector3(float.Parse(vectorJoints[0],CultureInfo.InvariantCulture),float.Parse(vectorJoints[1],CultureInfo.InvariantCulture),float.Parse(vectorJoints[2],CultureInfo.InvariantCulture));
            }
            
            string sensors = flex;
            eachSensor = sensors.Split('/');
            
            for (int j = 0; j < 10; j++)
            {
                flexArray[j] =  float.Parse(eachSensor[j]);
            }

        }
        if (HandDataManager.CanGetHandData(1, device_type_t.GLOVE_RIGHT))
        {
            Debug.Log("Rechte Hand");
            Debug.Log(ApolloHandData.numJointsPerFinger);
            string[] delimiterStrings={"endpointID: ","deviceID: ","wrist (raw/processed): ","thumb: ","finger joints: ","flex sensor: "};
            s = HandDataManager.GetHandData(1, device_type_t.GLOVE_RIGHT).ToString();
            string[] words = s.Split(delimiterStrings,StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                Debug.Log(word);
            }
            
            //erste stringteilung
            endpointID = words[0];
            deviceID = words[1];
            wrist = words[2];
            thumb = words[3];
            joints = words[4];
            flex = words[5];
            
            //aufteilen der Finger joint Daten
            string[] delChars = {" / "};
            string[] singleJoints = joints.Split(delChars,StringSplitOptions.RemoveEmptyEntries);
            foreach (var joint in singleJoints)
            {
                Debug.Log(joint);
            }

            for (int i = 0; i < 25; i++)
            {
                singleJoint = singleJoints[i];
                string[] delBrackets = {"(", ",",")"};
                string[] vectorJoints = singleJoint.Split(delBrackets,StringSplitOptions.RemoveEmptyEntries);
               jointArray[i] = new Vector3(float.Parse(vectorJoints[0],CultureInfo.InvariantCulture),float.Parse(vectorJoints[1],CultureInfo.InvariantCulture),float.Parse(vectorJoints[2],CultureInfo.InvariantCulture));
            }
            
            string sensors = flex;
            eachSensor = sensors.Split('/');
            
            for (int j = 0; j < 10; j++)
            {
                flexArray[j] =  float.Parse(eachSensor[j]);
            }

        }
        
    }

    void Update()
    {
        if (HandDataManager.CanGetHandData(1, device_type_t.GLOVE_LEFT))
        {
            StartCoroutine(UpdateData());
            
        }
        if (HandDataManager.CanGetHandData(1, device_type_t.GLOVE_RIGHT))
        {
            StartCoroutine(UpdateData());
        }
    }

    private IEnumerator UpdateData()
    {
         yield return new WaitForSeconds(4); // NICHT LOESCHEN
        //TODO:SetupDatei Glove Links oder Glove Rechts.
        if (HandDataManager.CanGetHandData(1, device_type_t.GLOVE_LEFT))
        {
            
            string[] delimiterStrings={"endpointID: ","deviceID: ","wrist (raw/processed): ","thumb: ","finger joints: ","flex sensor: "};
            s = HandDataManager.GetHandData(1, device_type_t.GLOVE_LEFT).ToString();
            string[] words = s.Split(delimiterStrings,StringSplitOptions.RemoveEmptyEntries);
            
            //erste stringteilung
            endpointID = words[0];
            deviceID = words[1];
            wrist = words[2];
            thumb = words[3];
            joints = words[4];
            flex = words[5];
            
            //aufteilen der Finger joint Daten
            string[] delChars = {" / "};
            string[] singleJoints = joints.Split(delChars,StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < 25; i++)
            {
                singleJoint = singleJoints[i];
                string[] delBrackets = {"(", ",",")"};
                string[] vectorJoints = singleJoint.Split(delBrackets,StringSplitOptions.RemoveEmptyEntries);
               jointArray[i] = new Vector3(float.Parse(vectorJoints[0],CultureInfo.InvariantCulture),float.Parse(vectorJoints[1],CultureInfo.InvariantCulture),float.Parse(vectorJoints[2],CultureInfo.InvariantCulture));
            }
            
            string sensors = flex;
            eachSensor = sensors.Split('/');
            
            for (int j = 0; j < 10; j++)
            {
                flexArray[j] =  float.Parse(eachSensor[j]);
            }

        }
        if (HandDataManager.CanGetHandData(1, device_type_t.GLOVE_RIGHT))
        {
            
            string[] delimiterStrings={"endpointID: ","deviceID: ","wrist (raw/processed): ","thumb: ","finger joints: ","flex sensor: "};
            
            
            s = HandDataManager.GetHandData(1, device_type_t.GLOVE_RIGHT).ToString();
            
            
            string[] words = s.Split(delimiterStrings,StringSplitOptions.RemoveEmptyEntries);

            
            
            //erste stringteilung
            endpointID = words[0];
            deviceID = words[1];
            wrist = words[2];
            thumb = words[3];
            joints = words[4];
            flex = words[5];
            
            //aufteilen der Finger joint Daten
            string[] delChars = {" / "};
            string[] singleJoints = joints.Split(delChars,StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < 25; i++)
            {
                singleJoint = singleJoints[i];
                string[] delBrackets = {"(", ",",")"};
                string[] vectorJoints = singleJoint.Split(delBrackets,StringSplitOptions.RemoveEmptyEntries);
               jointArray[i] = new Vector3(float.Parse(vectorJoints[0],CultureInfo.InvariantCulture),float.Parse(vectorJoints[1],CultureInfo.InvariantCulture),float.Parse(vectorJoints[2],CultureInfo.InvariantCulture));
            }
            
            string sensors = flex;
            eachSensor = sensors.Split('/');
            
            for (int j = 0; j < 10; j++)
            {
                flexArray[j] =  float.Parse(eachSensor[j]);
            }

        }
        UpdateColliderPos(); // maybe remove

        
    }

    Vector3 GetWorldPos(GameObject gameObject)
    {
        return gameObject.transform.position;
    }

    private void UpdateColliderPos()
    {
        indexL = GetWorldPos(colIndexL);
        middleL = GetWorldPos(colMiddleL);
        ringL = GetWorldPos(colRingL);
        pinkyL = GetWorldPos(colPinkyL);
    }

    public float[] GetFlexArray()
    {
        return flexArray;
    }

    
 
    
    
    /* So sieht der Datenstring aus:
     
    endpointID: 11344931876436443136
    deviceID: 684906804
    wrist (raw/processed): (0.1, 0.1, 0.1, 1.0) (-0.1, 0.1, 0.1, 1.0) 
    thumb: (0.0, 0.0, 0.0, 1.0) 
    finger joints: (0.0, 0.0, 0.0) / (65.7, 260.1, 298.0) / (359.0, 350.1, 345.1) / (2.5, 0.1, 12.4) / (0.0, 0.0, 0.0) /
                   (0.0, 0.0, 0.0) / (14.1, 356.1, 343.2) / (1.4, 0.2, 356.4) / (1.3, 359.2, 7.8) / (0.0, 0.0, 0.0) /
                   (0.0, 0.0, 0.0) / (4.5, 5.8, 352.3) / (357.8, 359.3, 357.7) / (359.6, 4.4, 10.0) / (0.0, 0.0, 0.0) /
                   (0.0, 0.0, 0.0) / (352.0, 15.4, 349.2) / (0.7, 1.5, 355.7) / (0.2, 356.9, 11.9) / (0.0, 0.0, 0.0) /
                   (0.0, 0.0, 0.0) / (348.7, 24.0, 350.6) / (1.3, 0.9, 346.6) / (358.5, 355.2, 345.9) / (0.0, 0.0, 0.0) /  
    flex sensor: 0 / 0 / 0,124175824224949 / 0,0204395595937967 / 0 / 0 / 0,0345054939389229 / 0,0122344326227903 / 0 / 0,146007373929024 / 
    
    */
}
