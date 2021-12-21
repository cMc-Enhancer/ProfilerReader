using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UTJ.ProfilerReader.Analyzer;
using UTJ.ProfilerReader.RawData;

namespace UTJ.ProfilerReader
{
    public class CUIInterface
    {
        const int NormalCode = 0;
        const int TimeoutCode = 10;
        const int ReadErrorCode = 11;

        private static int timeoutSec = 0;
        private static ILogReaderPerFrameData currentReader = null;
        private static bool timeouted = false;

        private static string overrideUnityVersion = null;


        public static void SetTimeout(int sec)
        {
            Debug.Log("SetTimeout " + sec);
            timeoutSec = sec;
            Thread th = new Thread(TimeOutExecute);
            th.Start();
        }

        public static void TimeOutExecute()
        {
            Thread.Sleep(timeoutSec * 1000);
            Debug.Log("Timeout!!!");
            currentReader.ForceExit();
            timeouted = true;
        }

        public static void ProfilerToCsv()
        {
            var args = Environment.GetCommandLineArgs();
            string inputFile = null;
            string inputDir = null;
            string outputDir = null;
            bool logFlag = false;
            bool isLegacyOutputDirPath = false;
            bool enableAllAnalyzers = false;

            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] == "-PH.inputFile")
                {
                    inputFile = args[i + 1];
                    i += 1;
                }

                if (args[i] == "-PH.inputDir")
                {
                    inputDir = args[i + 1];
                    i += 1;
                }

                if (args[i] == "-PH.outputDir")
                {
                    outputDir = args[i + 1];
                    i += 1;
                }

                if (args[i] == "-PH.timeout")
                {
                    SetTimeout(int.Parse(args[i + 1]));
                    i += 1;
                }

                if (args[i] == "-PH.overrideUnityVersion")
                {
                    overrideUnityVersion = args[i + 1];
                    i += 1;
                }

                if (args[i] == "-PH.log")
                {
                    logFlag = true;
                }

                if (args[i] == "-PH.dirLegacy") ;
                {
                    isLegacyOutputDirPath = true;
                }

                if (args[i] == "-PH.enableAllAnalyzers")
                {
                    enableAllAnalyzers = true;
                }
            }

            if (inputFile != null)
            {
                ProfilerToCsv(inputFile, outputDir, logFlag, isLegacyOutputDirPath, enableAllAnalyzers);
            }
            else
            {
                BatchProfilerToCsv(inputDir, outputDir, logFlag, isLegacyOutputDirPath, enableAllAnalyzers);
            }
        }

        public static void BatchProfilerToCsv(string inputDir, string outputDir, bool logFlag,
            bool isLegacyOutputDirPath, bool enableAllAnalyzers)
        {
            var files = Directory.GetFiles(inputDir, "*.raw");
            foreach (var file in files)
            {
                ProfilerToCsv(file, outputDir, logFlag, isLegacyOutputDirPath, enableAllAnalyzers);
            }
        }

        public static void ProfilerToCsv(string inputFile, string outputDir, bool logFlag, bool isLegacyOutputDirPath,
            bool enableAllAnalyzers)
        {
            if (string.IsNullOrEmpty(outputDir))
            {
                if (isLegacyOutputDirPath)
                {
                    outputDir = Path.GetDirectoryName(inputFile);
                }
                else
                {
                    string file = Path.GetFileName(inputFile);
                    outputDir = Path.Combine(Path.GetDirectoryName(inputFile), file.Replace('.', '_'));
                }
            }

            UtjAnalyzer(inputFile, outputDir, logFlag, enableAllAnalyzers);

            SimpleAnalyzer(inputFile);
        }

        private static void UtjAnalyzer(string inputFile, string outputDir, bool logFlag, bool enableAllAnalyzers)
        {
            currentReader = ProfilerLogUtil.CreateLogReader(inputFile);

            var analyzers = enableAllAnalyzers
                ? AnalyzerUtil.CreateAllAnalyzer()
                : AnalyzerUtil.CreateSourceTestAnalyzer();

            var frameData = currentReader.ReadFrameData();
            SetAnalyzerInfo(analyzers, currentReader, outputDir, inputFile);

            if (frameData == null)
            {
                Debug.LogError("No FrameDataFile " + inputFile);
            }

            // Loop and execute each frame
            while (frameData != null)
            {
                try
                {
                    frameData = currentReader.ReadFrameData();
                    if (logFlag && frameData != null)
                    {
                        Console.WriteLine("ReadFrame:" + frameData.frameIndex);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return;
                }

                foreach (var analyzer in analyzers)
                {
                    try
                    {
                        if (frameData != null)
                        {
                            analyzer.CollectData(frameData);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }

                GC.Collect();
            }

            foreach (var analyzer in analyzers)
            {
                analyzer.WriteResultFile(Path.GetFileName(inputFile), outputDir);
            }
        }

        private static void SimpleAnalyzer(string inputFile)
        {
            List<ISimpleAnalyzer> analyzers = AnalyzerUtil.CreateSimpleAnalyzers();
            
            Debug.Log("Simple analyzer processing file " + inputFile);

            foreach (var analyzer in analyzers)
            {
                analyzer.Analyze(inputFile);
            }
            
            GC.Collect();
        }

        private static void SetAnalyzerInfo(List<IAnalyzer> analyzers,
            ILogReaderPerFrameData logReader, string outDir, string inFile)
        {
            ProfilerLogFormat format = ProfilerLogFormat.TypeData;
            if (logReader.GetType() == typeof(ProfilerRawLogReader))
            {
                format = ProfilerLogFormat.TypeRaw;
            }

            string unityVersion = Application.unityVersion;
            if (!string.IsNullOrEmpty(overrideUnityVersion))
            {
                unityVersion = overrideUnityVersion;
            }

            foreach (var analyzer in analyzers)
            {
                analyzer.SetInfo(format, unityVersion, logReader.GetLogFileVersion(), logReader.GetLogFilePlatform());
                analyzer.SetFileInfo(inFile, outDir);
            }
        }
    }
}