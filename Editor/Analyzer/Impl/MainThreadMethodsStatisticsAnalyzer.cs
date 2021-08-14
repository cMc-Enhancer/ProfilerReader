using System;
using System.Collections.Generic;
using System.Linq;
using UTJ.ProfilerReader.BinaryData;

namespace UTJ.ProfilerReader.Analyzer
{
    public class MainThreadMethodsStatisticsAnalyzer : AbstractTextBasedFileOutputAnalyzer
    {
        private class SampleData : IComparable
        {
            public readonly string fullName;
            public readonly string sampleName;
            public readonly string categoryName;

            // Method may be called multiple times in single frame
            private readonly List<List<float>> framesExecTimes = new List<List<float>>();
            private List<float> flattenedCallExecTimes;
            private List<float> flattenedFrameExecTimes;
            private List<int> frameCallCounts;

            private bool sorted = false;

            public SampleData(string fullName, string sampleName, string categoryName)
            {
                this.sampleName = sampleName;
                this.fullName = fullName;
                this.categoryName = categoryName;
            }

            public void NextFrame()
            {
                framesExecTimes.Add(new List<float>());
            }

            public void Called(float selfMsec)
            {
                if (framesExecTimes.Count == 0)
                {
                    NextFrame();
                }

                framesExecTimes[framesExecTimes.Count - 1].Add(selfMsec);
            }

            public float Sum()
            {
                float sum = 0.0f;
                foreach (List<float> frameExecTimes in framesExecTimes)
                {
                    sum += frameExecTimes.Sum();
                }

                return sum;
            }

            public float CallNinetyFifthPercentile()
            {
                EnsureFlattenedAndSorted();
                return flattenedCallExecTimes[(int) (flattenedCallExecTimes.Count * 0.95)];
            }

            public float CallNinetyNinthPercentile()
            {
                EnsureFlattenedAndSorted();
                return flattenedCallExecTimes[(int) (flattenedCallExecTimes.Count * 0.99)];
            }

            public float CallMax()
            {
                EnsureFlattenedAndSorted();
                return flattenedCallExecTimes[flattenedCallExecTimes.Count - 1];
            }

            public float CallAverage()
            {
                EnsureFlattenedAndSorted();

                return flattenedCallExecTimes.Average();
            }

            public float CallStandardDeviation()
            {
                EnsureFlattenedAndSorted();

                float avg = CallAverage();
                double sum = flattenedCallExecTimes.Sum(d => Math.Pow(d - avg, 2));
                double sumOfDerivationAverage = Math.Sqrt(sum / flattenedCallExecTimes.Count);
                return (float) sumOfDerivationAverage;
            }

            public float CallCount()
            {
                EnsureFlattenedAndSorted();
                return flattenedCallExecTimes.Count;
            }

            public float FrameCallNinetyFifthPercentile()
            {
                EnsureFlattenedAndSorted();
                return flattenedFrameExecTimes[(int) (flattenedFrameExecTimes.Count * 0.95)];
            }

            public float FrameCallNinetyNinthPercentile()
            {
                EnsureFlattenedAndSorted();
                return flattenedFrameExecTimes[(int) (flattenedFrameExecTimes.Count * 0.99)];
            }

            public float FrameCallMax()
            {
                EnsureFlattenedAndSorted();
                return flattenedFrameExecTimes[flattenedFrameExecTimes.Count - 1];
            }

            public float FrameCallAverage()
            {
                EnsureFlattenedAndSorted();

                return flattenedFrameExecTimes.Average();
            }

            public float FrameCallStandardDeviation()
            {
                EnsureFlattenedAndSorted();

                float avg = CallAverage();
                double sum = flattenedFrameExecTimes.Sum(d => Math.Pow(d - avg, 2));
                double sumOfDerivationAverage = Math.Sqrt(sum / flattenedFrameExecTimes.Count);
                return (float) sumOfDerivationAverage;
            }

            public float AverageFrameCallCount()
            {
                EnsureFlattenedAndSorted();
                return (float) frameCallCounts.Average();
            }

            public float NinetyFifthPercentileFrameCallCount()
            {
                EnsureFlattenedAndSorted();
                return frameCallCounts[(int) (frameCallCounts.Count * 0.95)];
            }

            public float NinetyNinthPercentileFrameCallCount()
            {
                EnsureFlattenedAndSorted();
                return frameCallCounts[(int) (frameCallCounts.Count * 0.99)];
            }

            public float MaxFrameCallCount()
            {
                EnsureFlattenedAndSorted();
                return frameCallCounts[frameCallCounts.Count - 1];
            }

            private void EnsureFlattenedAndSorted()
            {
                if (sorted)
                {
                    return;
                }

                flattenedCallExecTimes = new List<float>(framesExecTimes.Count);
                foreach (List<float> frameExecTimes in framesExecTimes)
                {
                    flattenedCallExecTimes.AddRange(frameExecTimes);
                }

                flattenedCallExecTimes.Sort();

                frameCallCounts = new List<int>(framesExecTimes.Count);
                flattenedFrameExecTimes = new List<float>(framesExecTimes.Count);
                foreach (List<float> frameExecTimes in framesExecTimes)
                {
                    flattenedFrameExecTimes.Add(frameExecTimes.Sum());
                    frameCallCounts.Add(frameExecTimes.Count);
                }

                frameCallCounts.Sort();
                flattenedFrameExecTimes.Sort();
            }

            public int CompareTo(object obj)
            {
                return -Sum().CompareTo(((SampleData) obj).Sum());
            }
        }

        private readonly Dictionary<string, SampleData> _samples = new Dictionary<string, SampleData>();
        private SampleData total;

        public MainThreadMethodsStatisticsAnalyzer()
        {
            total = new SampleData("Total", "Total", "Total");
            _samples.Add("Total", total);
        }

        public sealed override void CollectData(ProfilerFrameData frameData)
        {
            foreach (KeyValuePair<string, SampleData> pair in _samples)
            {
                pair.Value.NextFrame();
            }

            foreach (var thread in frameData.m_ThreadData)
            {
                if (thread.IsMainThread)
                {
                    CollectThread(thread);
                }
            }
        }

        private void CollectThread(ThreadData thread)
        {
            if (thread?.m_AllSamples == null)
            {
                return;
            }

            foreach (var sample in thread.m_AllSamples)
            {
                if (sample.parent == null)
                {
                    CollectFromNamedChildren(sample);
                }
            }
        }

        private void CollectFromNamedChildren(ProfilerSample sample)
        {
            if (!string.IsNullOrEmpty(sample.sampleName))
            {
                string category = ProtocolData.GetCategory(unityVersion, sample.group);
                AddSampleData(sample.fullSampleName, sample.sampleName, category, sample.selfTimeUs / 1000.0f,
                    sample.timeUS / 1000.0f);
            }

            if (sample.children != null)
            {
                foreach (var child in sample.children)
                {
                    CollectFromNamedChildren(child);
                }
            }
        }

        protected virtual bool IsSampleNameMatched(string name)
        {
            return true;
        }

        private void AddSampleData(string fullName, string sampleName, string categoryName, float selfMsec,
            float execMsec)
        {
            if (!IsSampleNameMatched(sampleName))
            {
                return;
            }

            if (selfMsec < 0.0f)
            {
                ProfilerLogUtil.logErrorString("minus Param " + sampleName + ":" + selfMsec + ":" + execMsec);
                return;
            }

            if (selfMsec > 1000.0f * 50.0f)
            {
                ProfilerLogUtil.logErrorString("minus Param " + sampleName + ":" + selfMsec + ":" + execMsec);
                return;
            }

            if (!_samples.TryGetValue(fullName, out var sampleData))
            {
                sampleData = new SampleData(fullName, sampleName, categoryName);
                _samples.Add(fullName, sampleData);
            }

            total.Called(selfMsec);
            sampleData.Called(selfMsec);
        }

        protected override string GetResultText()
        {
            var sampleDataList = new List<SampleData>(_samples.Values);
            sampleDataList.Sort();

            CsvStringGenerator csvStringGenerator = new CsvStringGenerator();
            csvStringGenerator.AppendColumn("name")
                .AppendColumn("fullname")
                .AppendColumn("category")
                .AppendColumn("callNum")
                .AppendColumn("perFrame(msec)")
                .AppendColumn("perCall(msec)")
                .AppendColumn("perCall95thPercentile(msec)")
                .AppendColumn("perCall99thPercentile(msec)")
                .AppendColumn("perCallMax(msec)")
                .AppendColumn("callStandardDeviation(msec)")
                .AppendColumn("perFrameCall95thPercentile(msec)")
                .AppendColumn("perFrameCall99thPercentile(msec)")
                .AppendColumn("perFrameCallMax(msec)")
                .AppendColumn("frameCallStandardDeviation(msec)")
                .AppendColumn("averageFrameCallCount")
                .AppendColumn("frameCallCount95thPercentile")
                .AppendColumn("frameCallCount99thPercentile")
                .AppendColumn("frameCallCountMax")
                .NextRow();

            foreach (var sampleData in sampleDataList)
            {
                csvStringGenerator.AppendColumn(sampleData.sampleName)
                    .AppendColumn(sampleData.fullName)
                    .AppendColumn(sampleData.categoryName)
                    .AppendColumn(sampleData.CallCount())
                    .AppendColumn(sampleData.FrameCallAverage())
                    .AppendColumn(sampleData.CallAverage())
                    .AppendColumn(sampleData.CallNinetyFifthPercentile())
                    .AppendColumn(sampleData.CallNinetyNinthPercentile())
                    .AppendColumn(sampleData.CallMax())
                    .AppendColumn(sampleData.CallStandardDeviation())
                    .AppendColumn(sampleData.FrameCallNinetyFifthPercentile())
                    .AppendColumn(sampleData.FrameCallNinetyNinthPercentile())
                    .AppendColumn(sampleData.FrameCallMax())
                    .AppendColumn(sampleData.FrameCallStandardDeviation())
                    .AppendColumn(sampleData.AverageFrameCallCount())
                    .AppendColumn(sampleData.NinetyFifthPercentileFrameCallCount())
                    .AppendColumn(sampleData.NinetyNinthPercentileFrameCallCount())
                    .AppendColumn(sampleData.MaxFrameCallCount())
                    .NextRow();
            }

            return csvStringGenerator.ToString();
        }

        protected override string FooterName => "_main_thread_methods_statistics.csv";
    }
}