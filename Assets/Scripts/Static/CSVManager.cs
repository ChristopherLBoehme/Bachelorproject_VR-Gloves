using System;
using UnityEngine;
using System.IO;



public static class CSVManager
{
    private static string reportDirectoryName = "Report";
    private static string reportFileName = "report.csv";
    private static string reportSeparator = ";";
    private static string[] reportHeaders = new string[29] //ändern wenn mehr spalten in Tabelle hinzugefügt werden
        {
            "endpointID",
            "deviceID",
            "wrist",
            "thumb",
            "joints1_2",
            "joints1_3",
            "joints1_4",
            "joints2_2",
            "joints2_3",
            "joints2_4",
            "joints3_2",
            "joints3_3",
            "joints3_4",
            "joints4_2",
            "joints4_3",
            "joints4_4",
            "joints5_2",
            "joints5_3",
            "joints5_4",
            "flex1_1",
            "flex1_2",
            "flex2_1",
            "flex2_2",
            "flex3_1",
            "flex3_2",
            "flex4_1",
            "flex4_2",
            "flex5_1",
            "flex5_2"
        };


    public static void ChangeReportHeaders(string[] newHeaders)
    {
        reportHeaders = newHeaders;
    }
    
    public static void ChangeReportFilename(string newFilename)
    {
        reportFileName = newFilename;
    }

    public static void ChangeDirectoryName(string newDirectoryname)
    {
        reportDirectoryName = newDirectoryname;
    }
    
    #region Interactions

    public static void AppendToReport(string[] strings)
    {
        VerifyDirectory();
        VerifyFile();
        using (StreamWriter sw = File.AppendText(GetFilePath()))
        {
            string finalString = "";
            for (int i = 0; i < strings.Length; i++)
            {
                if (finalString != "")
                {
                    finalString += reportSeparator;
                }

                finalString += strings[i];
                
            }

            finalString += reportSeparator;
            sw.WriteLine(finalString);
        }
    }

    public static void CreateReport()
    {
        VerifyDirectory();
        using (StreamWriter sw = File.CreateText(GetFilePath()))
        {
            string finalString = "";
            for (int i = 0; i < reportHeaders.Length; i++)
            {
                if (finalString != "")
                {
                    finalString += reportSeparator;
                }

                finalString += reportHeaders[i];
            }

            finalString += reportSeparator;
            sw.WriteLine(finalString);
        }
    }

    
    #endregion

    #region Operations
    static void VerifyDirectory()
    {
        string dir = GetDirectoryPath();
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
            
    }

    static void VerifyFile()
    {
        string file = GetFilePath();
        if (!File.Exists(file))
        {
            CreateReport();
        }
    }

    
    
    

    #endregion
    
    #region Queries

    static string GetDirectoryPath()
    {
        return Application.dataPath + "/" + reportDirectoryName;
    }

    static string GetFilePath()
    {
        return GetDirectoryPath() + "/" + reportFileName;
    }

    

    #endregion
    
}
