using System.Collections.Generic;
using System.Linq;
using UTJ.ProfilerReader.BinaryData;

namespace UTJ.ProfilerReader.Analyzer
{
    public class MainThreadMethodPerFrameSampleAnalyzer : AbstractTextBasedFileOutputAnalyzer
    {
        private class FrameMethodSampleData
        {
            public readonly string sampleName;
            public readonly string fullName;
            private readonly List<float> execTimes = new List<float>();
            private readonly List<float> selfTimes = new List<float>();

            public FrameMethodSampleData(string sampleName, string fullName)
            {
                this.sampleName = sampleName;
                this.fullName = fullName;
            }

            public void Call(float execTime, float selfTime)
            {
                execTimes.Add(execTime);
                selfTimes.Add(selfTime);
            }

            public float ExecTime()
            {
                return execTimes.Sum();
            }

            public float SelfTime()
            {
                return selfTimes.Sum();
            }

            public int CallCount()
            {
                return execTimes.Count;
            }
        }

        private class FrameSampleData
        {
            public readonly int frameIndex;

            public Dictionary<string, FrameMethodSampleData> Samples { get; } =
                new Dictionary<string, FrameMethodSampleData>();

            public FrameSampleData(int frameIndex)
            {
                this.frameIndex = frameIndex;
            }
        }

        private readonly List<FrameSampleData> allFrameSampleData = new List<FrameSampleData>();

        public sealed override void CollectData(ProfilerFrameData frameData)
        {
            FrameSampleData data = new FrameSampleData(frameData.frameIndex);

            var samples = from thread in frameData.m_ThreadData
                where thread.IsMainThread && thread.m_AllSamples != null
                from sample in thread.m_AllSamples
                where sample.parent == null
                select sample;
            foreach (var sample in samples)
            {
                CollectSample(sample, data);
            }

            allFrameSampleData.Add(data);
        }

        private void CollectSample(ProfilerSample sample, FrameSampleData data)
        {
            string sampleName = sample.sampleName;
            var methodShouldSample = !string.IsNullOrEmpty(sampleName) && IsSampleNameMatched(sampleName);
            if (methodShouldSample)
            {
                float execTime = sample.timeUS / 1000.0f;
                float selfTime = sample.selfTimeUs / 1000.0f;
                if (!data.Samples.ContainsKey(sampleName))
                {
                    data.Samples[sampleName] = new FrameMethodSampleData(sampleName, sample.fullSampleName);
                }

                FrameMethodSampleData frameMethodSampleData = data.Samples[sampleName];
                frameMethodSampleData.Call(execTime, selfTime);
            }

            if (sample.children != null)
            {
                foreach (var child in sample.children)
                {
                    CollectSample(child, data);
                }
            }
        }

        protected virtual bool IsSampleNameMatched(string name)
        {
            return true;
        }

        protected sealed override string GetResultText()
        {
            CsvStringGenerator csvStringGenerator = new CsvStringGenerator();
            csvStringGenerator.AppendColumn("frame")
                .AppendColumn("name")
                .AppendColumn("fullName")
                .AppendColumn("execTime")
                .AppendColumn("selfTime")
                .AppendColumn("callCount")
                .NextRow();

            foreach (FrameSampleData frameData in allFrameSampleData)
            {
                float total = 0.0f;
                foreach (FrameMethodSampleData sampleData in frameData.Samples.Values)
                {
                    var execTime = sampleData.ExecTime();
                    csvStringGenerator.AppendColumn(frameData.frameIndex)
                        .AppendColumn(sampleData.sampleName)
                        .AppendColumn(sampleData.fullName)
                        .AppendColumn(execTime)
                        .AppendColumn(sampleData.SelfTime())
                        .AppendColumn(sampleData.CallCount())
                        .NextRow();
                    total += execTime;
                }

                csvStringGenerator.AppendColumn(frameData.frameIndex)
                    .AppendColumn("Total")
                    .AppendColumn("Total")
                    .AppendColumn(total)
                    .AppendColumn("/")
                    .AppendColumn("/")
                    .NextRow();
            }

            return csvStringGenerator.ToString();
        }

        protected override string FooterName => "_main_thread_method_per_frame.csv";
    }
}