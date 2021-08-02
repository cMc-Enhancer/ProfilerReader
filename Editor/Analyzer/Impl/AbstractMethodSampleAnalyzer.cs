using System.Collections.Generic;
using System.Linq;
using UTJ.ProfilerReader.BinaryData;

namespace UTJ.ProfilerReader.Analyzer
{
    public abstract class AbstractMethodSampleAnalyzer : AnalyzeToTextbaseFileBase
    {
        private class FrameSampleData
        {
            public Dictionary<string, float> Samples { get; } = new Dictionary<string, float>();
        }

        private readonly List<FrameSampleData> frameSampleData = new List<FrameSampleData>();
        private readonly HashSet<string> distinctSampleNames = new HashSet<string>();

        public sealed override void CollectData(ProfilerFrameData frameData)
        {
            FrameSampleData data = new FrameSampleData();

            var samples = from thread in frameData.m_ThreadData
                where thread.IsMainThread && thread.m_AllSamples != null
                from sample in thread.m_AllSamples
                where sample.parent == null
                select sample;
            foreach (var sample in samples)
            {
                CollectSample(sample, data);
            }

            frameSampleData.Add(data);
        }

        private void CollectSample(ProfilerSample sample, FrameSampleData data)
        {
            string sampleName = sample.sampleName;
            var isAiScript = !string.IsNullOrEmpty(sampleName) && IsSampleNameMatched(sampleName);
            if (isAiScript)
            {
                distinctSampleNames.Add(sampleName);
                float execMsec = sample.timeUS / 1000.0f;
                data.Samples.TryGetValue(sampleName, out float defaultValue);
                data.Samples[sampleName] = defaultValue + execMsec;
            }

            if (sample.children != null)
            {
                foreach (var child in sample.children)
                {
                    CollectSample(child, data);
                }
            }
        }

        protected abstract bool IsSampleNameMatched(string name);

        protected sealed override string GetResultText()
        {
            CsvStringGenerator csvStringGenerator = new CsvStringGenerator();
            csvStringGenerator.AppendColumn("frame");

            foreach (var name in distinctSampleNames)
            {
                csvStringGenerator.AppendColumn(name);
            }

            csvStringGenerator.AppendColumn("total");

            csvStringGenerator.NextRow();

            int frameNum = 1;
            foreach (FrameSampleData sampleData in frameSampleData)
            {
                float total = 0.0f;
                csvStringGenerator.AppendColumn(frameNum);
                foreach (var sampleName in distinctSampleNames)
                {
                    sampleData.Samples.TryGetValue(sampleName, out float execMsec);
                    csvStringGenerator.AppendColumn(execMsec);
                    total += execMsec;
                }

                csvStringGenerator.AppendColumn(total);

                frameNum++;
                csvStringGenerator.NextRow();
            }

            return csvStringGenerator.ToString();
        }
    }
}