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
            string inputDir = null;
            string outputDir = null;

            for (int i = 0; i < args.Length; ++i)
            {
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
            }

            BatchProfilerToCsv(inputDir, outputDir);

            EditorApplication.Exit(0);
        }

        public static void BatchProfilerToCsv(string inputDir, string outputDir)
        {
            var files = Directory.GetFiles(inputDir, "*.raw");
            foreach (var file in files)
            {
                ProfilerToCsv(file, outputDir);
            }
        }

        public static void ProfilerToCsv(string inputFile, string outputDir)
        {
            if (string.IsNullOrEmpty(outputDir))
            {
                outputDir = Path.GetDirectoryName(inputFile);
            }

            String[] files = Directory.GetFiles(outputDir, Path.GetFileName(inputFile).Replace(".raw", "_*.csv"));
            if (files.Length > 0)
            {
                Console.WriteLine("Output file found for raw file " + inputFile + ", skip");
                return;
            }

            // UtjAnalyzer(inputFile, outputDir);

            SimpleAnalyzer(inputFile);
        }

        private static void UtjAnalyzer(string inputFile, string outputDir)
        {
            currentReader = ProfilerLogUtil.CreateLogReader(inputFile);

            var analyzers = AnalyzerUtil.CreateSourceTestAnalyzer();

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
                    if (frameData != null)
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