using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace UTJ.ProfilerReader.Analyzer
{
    public class MainThreadAnalyzer : ISimpleAnalyzer
    {
        private class FrameSample
        {
            public int fileIndex;
            public int frameIndex;
            public string frameTime;
            public string frameFPS;
            public bool hasPlayerLoop;
            public List<Sample> samples = new List<Sample>();

            public FrameSample(int fileIndex, int frameIndex, RawFrameDataView rawFrameDataView)
            {
                this.fileIndex = fileIndex;
                this.frameIndex = frameIndex;
                this.frameTime = rawFrameDataView.frameTimeMs.ToString();
                this.frameFPS = rawFrameDataView.frameFps.ToString();
            }
        }

        private class Sample
        {
            public string propertyName;
            public string propertyPath;
            public string totalPercent;
            public string selfPercent;
            public string calls;
            public string totalTime;
            public string selfTime;

            public Sample(ProfilerProperty property)
            {
                propertyName = property.propertyName;
                propertyPath = property.propertyPath;
                totalPercent = property.GetColumn(HierarchyFrameDataView.columnTotalPercent);
                selfPercent = property.GetColumn(HierarchyFrameDataView.columnSelfPercent);
                calls = property.GetColumn(HierarchyFrameDataView.columnCalls);
                totalTime = property.GetColumn(HierarchyFrameDataView.columnTotalTime);
                selfTime = property.GetColumn(HierarchyFrameDataView.columnSelfTime);
            }
        }

        public void Analyze(string fileName)
        {
            int fileIndex;
            try
            {
                fileIndex = int.Parse(Regex.Replace(fileName.Replace(".raw", ""), @"^.*_", ""));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            try
            {
                ProfilerDriver.LoadProfile(fileName, false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            var firstFrameIndex = ProfilerDriver.firstFrameIndex;
            var lastFrameIndex = ProfilerDriver.lastFrameIndex;
            var profilerSortColumn = HierarchyFrameDataView.columnTotalTime;

            List<FrameSample> frames = new List<FrameSample>();
            for (int frameIndex = firstFrameIndex; frameIndex <= lastFrameIndex; ++frameIndex)
            {
                var rawFrameDataView = ProfilerDriver.GetRawFrameDataView(frameIndex, 0);
                FrameSample frameSample = new FrameSample(fileIndex, frameIndex, rawFrameDataView);

                var property = new ProfilerProperty();
                property.SetRoot(frameIndex, profilerSortColumn, 0);
                property.onlyShowGPUSamples = false;

                while (property.Next(true))
                {
                    Sample sample = new Sample(property);
                    if (sample.propertyName.Equals("PlayerLoop"))
                    {
                        frameSample.hasPlayerLoop = true;
                    }

                    frameSample.samples.Add(sample);
                }

                if (frameSample.hasPlayerLoop)
                {
                    frames.Add(frameSample);
                }

                property.Cleanup();
            }

            WriteText(fileName, frames);
        }

        private void WriteText(string fileName, List<FrameSample> frameSamples)
        {
            string outputDir = Path.Combine(Path.GetDirectoryName(fileName), fileName.Replace(".raw", "_")) +
                               "main_thread.csv";

            CsvStringGenerator csvStringGenerator = new CsvStringGenerator();
            csvStringGenerator
                .AppendColumn("fileIndex")
                .AppendColumn("frameIndex")
                .AppendColumn("frameTime")
                .AppendColumn("frameFPS")
                .AppendColumn("propertyName")
                .AppendColumn("propertyPath")
                .AppendColumn("totalPercent")
                .AppendColumn("selfPercent")
                .AppendColumn("calls")
                .AppendColumn("totalTime")
                .AppendColumn("selfTime")
                .NextRow();

            foreach (var frame in frameSamples)
            {
                foreach (var sample in frame.samples)
                {
                    csvStringGenerator
                        .AppendColumn(frame.fileIndex)
                        .AppendColumn(frame.frameIndex)
                        .AppendColumn(frame.frameTime)
                        .AppendColumn(frame.frameFPS)
                        .AppendColumn(sample.propertyName)
                        .AppendColumn(sample.propertyPath)
                        .AppendColumn(sample.totalPercent)
                        .AppendColumn(sample.selfPercent)
                        .AppendColumn(sample.calls)
                        .AppendColumn(sample.totalTime)
                        .AppendColumn(sample.selfTime)
                        .NextRow();
                }
            }

            File.WriteAllText(outputDir, csvStringGenerator.ToString());
        }
    }
}