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
            bool exitFlag = true;
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

                if (args[i] == "-PH.exitcode")
                {
                    exitFlag = true;
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

            int code = 0;
            if (inputFile != null)
            {
                code = ProfilerToCsv(inputFile, outputDir, logFlag, isLegacyOutputDirPath, enableAllAnalyzers);
            }
            else
            {
                code = BatchProfilerToCsv(inputDir, outputDir, logFlag, isLegacyOutputDirPath, enableAllAnalyzers);
            }

            if (timeouted)
            {
                code = TimeoutCode;
            }

            if (exitFlag)
            {
                EditorApplication.Exit(code);
            }
        }

        public static int BatchProfilerToCsv(string inputDir, string outputDir, bool logFlag,
            bool isLegacyOutputDirPath, bool enableAllAnalyzers)
        {
            var files = Directory.GetFiles(inputDir, "*.raw");
            int code = 0;
            foreach (var file in files)
            {
                code = Math.Max(ProfilerToCsv(file, outputDir, logFlag, isLegacyOutputDirPath, enableAllAnalyzers),
                    code);
            }

            return NormalCode;
        }

        public static int ProfilerToCsv(string inputFile, string outputDir, bool logFlag, bool isLegacyOutputDirPath,
            bool enableAllAnalyzers)
        {
            int retCode = NormalCode;
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

            var logReader = ProfilerLogUtil.CreateLogReader(inputFile);
            currentReader = logReader;

            var analyzers = enableAllAnalyzers
                ? AnalyzerUtil.CreateAllAnalyzer()
                : AnalyzerUtil.CreateSourceTestAnalyzer();

            var frameData = logReader.ReadFrameData();
            SetAnalyzerInfo(analyzers, logReader, outputDir, inputFile);

            if (frameData == null)
            {
                Debug.LogError("No FrameDataFile " + inputFile);
            }

            // Loop and execute each frame
            while (frameData != null)
            {
                try
                {
                    frameData = logReader.ReadFrameData();
                    if (logFlag && frameData != null)
                    {
                        Console.WriteLine("ReadFrame:" + frameData.frameIndex);
                    }
                }
                catch (Exception e)
                {
                    retCode = ReadErrorCode;
                    Debug.LogError(e);
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

            return retCode;
        }

        private static void SetAnalyzerInfo(List<IAnalyzeFileWriter> analyzers,
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