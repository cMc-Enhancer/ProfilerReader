﻿using System.Collections.Generic;
using UTJ.ProfilerReader.BinaryData;

namespace UTJ.ProfilerReader.Analyzer
{
    public class MainThreadCategoryAnalyzer : AbstractTextBasedFileOutputAnalyzer
    {
        private class FrameByCategory
        {
            public Dictionary<string, float> frameData;
            public int frameIdx;

            public void AddData(string category, float msec)
            {
                if (frameData == null)
                {
                    frameData = new Dictionary<string, float>();
                }

                float val;
                if (frameData.TryGetValue(category, out val))
                {
                    frameData[category] = val + msec;
                }
                else
                {
                    frameData.Add(category, msec);
                }
            }

            public void AppendCsv(CsvStringGenerator csvStringGenerator, ICollection<string> categoriesStr)
            {
                foreach (var category in categoriesStr)
                {
                    float val;

                    if (!frameData.TryGetValue(category, out val))
                    {
                        val = 0.0f;
                    }

                    csvStringGenerator.AppendColumn(val);
                }
            }
        }

        private string[] categories;
        private SortedDictionary<string, int> categoryDictionary;
        private List<FrameByCategory> frames = new List<FrameByCategory>();

        public override void CollectData(ProfilerFrameData frameData)
        {
            SetupCategories();
            FrameByCategory frameByCategory = new FrameByCategory {frameIdx = frameData.frameIndex};
            foreach (var thread in frameData.m_ThreadData)
            {
                if (thread.IsMainThread)
                {
                    CollectThread(thread, frameByCategory);
                }
            }

            frames.Add(frameByCategory);
        }

        private void SetupCategories()
        {
            if (categoryDictionary != null)
            {
                return;
            }

            categoryDictionary = new SortedDictionary<string, int>();
            categories = ProtocolData.GetCategories(unityVersion);

            int idx = 0;
            foreach (var item in categories)
            {
                if (!categoryDictionary.ContainsKey(item))
                {
                    categoryDictionary.Add(item, idx++);
                }
            }
        }

        private void CollectThread(ThreadData thread, FrameByCategory frameByCategory)
        {
            if (thread.m_AllSamples == null)
            {
                return;
            }

            foreach (var sample in thread.m_AllSamples)
            {
                if (0 <= sample.group && sample.group < categories.Length)
                    frameByCategory.AddData(categories[sample.group], sample.selfTimeUs * 0.001f);
            }
        }

        protected override string GetResultText()
        {
            CsvStringGenerator csvStringGenerator = new CsvStringGenerator();
            csvStringGenerator.AppendColumn("frameIdx");
            foreach (var str in categoryDictionary.Keys)
            {
                csvStringGenerator.AppendColumn(str + "(msec)");
            }

            csvStringGenerator.NextRow();

            foreach (var frame in frames)
            {
                csvStringGenerator.AppendColumn(frame.frameIdx);
                frame.AppendCsv(csvStringGenerator, categoryDictionary.Keys);
                csvStringGenerator.NextRow();
            }

            return csvStringGenerator.ToString();
        }

        protected override string FooterName => "_main_thread_category_per_frame.csv";
    }
}