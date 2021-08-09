using System;
using System.Collections.Generic;
using System.Linq;
using UTJ.ProfilerReader.BinaryData;

namespace UTJ.ProfilerReader.Analyzer
{
    public class MainThreadAnalyzeToFile : AnalyzeToTextbaseFileBase
    {
        private class SampleData : IComparable
        {
            public readonly string fullName;
            public readonly string sampleName;
            public readonly string categoryName;

            private readonly List<float> selfMsecs = new List<float>();
            private bool sorted = false;
            private float avg = -1f;
            private float stdv = -1f;

            public SampleData(string fullName, string sampleName, string categoryName)
            {
                this.sampleName = sampleName;
                this.fullName = fullName;
                this.categoryName = categoryName;
            }

            public void Called(float selfMsec)
            {
                selfMsecs.Add(selfMsec);
            }

            public float Sum()
            {
                return selfMsecs.Sum();
            }

            public float NinetyFifthPercentile()
            {
                EnsureSorted();
                return selfMsecs[(int) (selfMsecs.Count * 0.95)];
            }

            public float NinetyNinthPercentile()
            {
                EnsureSorted();
                return selfMsecs[(int) (selfMsecs.Count * 0.99)];
            }

            public float Max()
            {
                EnsureSorted();
                return selfMsecs[selfMsecs.Count - 1];
            }

            public float Average()
            {
                if (avg < 0)
                {
                    avg = selfMsecs.Average();
                }

                return avg;
            }

            public float StandardDeviation()
            {
                EnsureSorted();

                if (stdv < 0)
                {
                    Average();
                    double sum = selfMsecs.Sum(d => Math.Pow(d - avg, 2));
                    double sumOfDerivationAverage = Math.Sqrt(sum / selfMsecs.Count);
                    stdv = (float) sumOfDerivationAverage;
                }

                return stdv;
            }

            public float CallCount()
            {
                return selfMsecs.Count;
            }

            private void EnsureSorted()
            {
                if (sorted)
                {
                    return;
                }

                selfMsecs.Sort();
            }

            public int CompareTo(object obj)
            {
                return -Sum().CompareTo(((SampleData) obj).Sum());
            }
        }

        private readonly Dictionary<string, SampleData> _samples = new Dictionary<string, SampleData>();
        private int _frameNum = 0;
        private SampleData total;

        public MainThreadAnalyzeToFile()
        {
            total = new SampleData("Total", "Total", "Total");
            _samples.Add("Total", total);
        }

        public sealed override void CollectData(ProfilerFrameData frameData)
        {
            foreach (var thread in frameData.m_ThreadData)
            {
                if (thread.IsMainThread)
                {
                    CollectThread(thread);
                }
            }

            ++_frameNum;
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
                .AppendColumn("standardDeviation(msec)")
                .NextRow();

            foreach (var sampleData in sampleDataList)
            {
                csvStringGenerator.AppendColumn(sampleData.sampleName)
                    .AppendColumn(sampleData.fullName)
                    .AppendColumn(sampleData.categoryName)
                    .AppendColumn(sampleData.CallCount())
                    .AppendColumn(sampleData.Sum() / _frameNum)
                    .AppendColumn(sampleData.Average())
                    .AppendColumn(sampleData.NinetyFifthPercentile())
                    .AppendColumn(sampleData.NinetyNinthPercentile())
                    .AppendColumn(sampleData.Max())
                    .AppendColumn(sampleData.StandardDeviation())
                    .NextRow();
            }

            return csvStringGenerator.ToString();
        }

        protected override string FooterName
        {
            get { return "_main_thread_methods_statistics.csv"; }
        }
    }
}