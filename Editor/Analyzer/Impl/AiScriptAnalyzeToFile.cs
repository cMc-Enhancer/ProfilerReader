using System.Collections.Generic;
using UTJ.ProfilerReader.BinaryData;


namespace UTJ.ProfilerReader.Analyzer
{
    public class AiScriptAnalyzeToFile : AnalyzeToTextbaseFileBase
    {
        private class SampleData
        {
            public readonly string fullName;
            public readonly string sampleName;
            public readonly string categoryName;

            public float TotalSelfMsec = 0.0f;
            public float SelfMinMSec = float.MaxValue;
            public float SelfMaxMsec = 0.0f;

            public float TotalExecMsec = 0.0f;
            public float ExecMinMSec = float.MaxValue;
            public float ExecMaxMsec = 0.0f;

            public int CallNum = 0;

            public SampleData(string fullName, string sampleName, string categoryName)
            {
                this.sampleName = sampleName;
                this.fullName = fullName;
                this.categoryName = categoryName;
            }

            public void Called(float selfMsec, float execMsec)
            {
                SelfMinMSec = ProfilerLogUtil.Min(SelfMinMSec, selfMsec);
                SelfMaxMsec = ProfilerLogUtil.Max(SelfMaxMsec, selfMsec);
                TotalSelfMsec += selfMsec;

                ExecMinMSec = ProfilerLogUtil.Min(ExecMinMSec, execMsec);
                ExecMaxMsec = ProfilerLogUtil.Max(ExecMaxMsec, execMsec);
                TotalExecMsec += execMsec;
                ++CallNum;
            }
        }

        private readonly Dictionary<string, SampleData> _samples = new Dictionary<string, SampleData>();
        private int _frameNum = 0;

        public override void CollectData(ProfilerFrameData frameData)
        {
            Dictionary<string, int> threadNameCounter = new Dictionary<string, int>(8);
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

        private void AddSampleData(string fullName, string sampleName, string categoryName, float selfMsec,
            float execMsec)
        {
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

            sampleData.Called(selfMsec, execMsec);
        }

        protected override string GetResultText()
        {
            CsvStringGenerator csvStringGenerator = new CsvStringGenerator();
            csvStringGenerator.AppendColumn("name").AppendColumn("fullname").AppendColumn("category")
                .AppendColumn("callNum").AppendColumn("self").AppendColumn("sum(msec)").AppendColumn("perFrame(msec)")
                .AppendColumn("min(msec)").AppendColumn("max(msec)").AppendColumn("total").AppendColumn("sum(msec)")
                .AppendColumn("perFrame(msec)").AppendColumn("min(msec)").AppendColumn("max(msec)").NextRow();
            var sampleDataList = new List<SampleData>(_samples.Values);
            sampleDataList.Sort((a, b) =>
            {
                if (a.TotalSelfMsec > b.TotalSelfMsec)
                {
                    return -1;
                }

                if (a.TotalSelfMsec < b.TotalSelfMsec)
                {
                    return 1;
                }

                return 0;
            });
            foreach (var sampleData in sampleDataList)
            {
                csvStringGenerator.AppendColumn(sampleData.sampleName).AppendColumn(sampleData.fullName)
                    .AppendColumn(sampleData.categoryName).AppendColumn(sampleData.CallNum).AppendColumn("");

                csvStringGenerator.AppendColumn(sampleData.TotalSelfMsec)
                    .AppendColumn(sampleData.TotalSelfMsec / _frameNum).AppendColumn(sampleData.SelfMinMSec)
                    .AppendColumn(sampleData.SelfMaxMsec).AppendColumn("");


                csvStringGenerator.AppendColumn(sampleData.TotalExecMsec)
                    .AppendColumn(sampleData.TotalExecMsec / _frameNum).AppendColumn(sampleData.ExecMinMSec)
                    .AppendColumn(sampleData.ExecMaxMsec);
                csvStringGenerator.NextRow();
            }

            return csvStringGenerator.ToString();
        }

        protected override string FooterName
        {
            get { return "_main_self.csv"; }
        }
    }
}