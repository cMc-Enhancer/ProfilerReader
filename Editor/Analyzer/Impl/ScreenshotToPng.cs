using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.Experimental.Rendering;
using UTJ.ProfilerReader.BinaryData;
using UTJ.ProfilerReader.RawData.Protocol;
#if UNITY_EDITOR
using UnityEngine;
#endif

namespace UTJ.ProfilerReader.Analyzer
{
    public class ScreenShotToProfiler : IAnalyzer
    {

        public static readonly Guid MetadataGuid = new Guid("4389DCEB-F9B3-4D49-940B-E98482F3A3F8");
        public static readonly int InfoTag = -1;

        private string outputPath;
        private string logFile;
        private bool createDir;
        private StringBuilder stringBuilder = new StringBuilder();

        private class CaptureData
        {
            public int profilerFrameIndex;
            public int idx;
            public int width;
            public int height;
            public int originWidth;
            public int originHeight;

            public CaptureData(int frameIdx ,byte[] data)
            {
                profilerFrameIndex = frameIdx;
                idx = GetIntValue(data, 0);
                width = GetShortValue(data, 4);
                height = GetShortValue(data, 6);
                originWidth = GetShortValue(data, 8);
                originHeight = GetShortValue(data, 10);
            }

            public static int GetIntValue(byte[] bin, int offset)
            {
                return (bin[offset + 0] << 0) +
                    (bin[offset + 1] << 8) +
                    (bin[offset + 2] << 16) +
                    (bin[offset + 3] << 24);
            }
            public static int GetShortValue(byte[] bin, int offset)
            {
                return (bin[offset + 0] << 0) +
                    (bin[offset + 1] << 8);
            }
        }
        private Dictionary<int, CaptureData> captureFrameData = new Dictionary<int, CaptureData>();


        public void CollectData(ProfilerFrameData frameData)
        {
            foreach( var thread in frameData.m_ThreadData)
            {
                if (thread.IsMainThread)
                {
                    ExecuteThreadData(frameData.frameIndex,thread);
                }
            }
        }
        private void ExecuteThreadData(int frameIdx,ThreadData thread)
        {
            if(thread == null || thread.m_AllSamples == null) { return; }
            foreach( var sample in thread.m_AllSamples)
            {
                if( sample == null || sample.sampleName != RawDataDefines.EmitFramemetataSample)
                {
                    continue;
                }
                if( sample.metaDatas == null || 
                    sample.metaDatas.metadatas == null ) { 
                    continue; 
                }
                ExecuteFrameMetadata(frameIdx,sample);
            }
        }

        private void ExecuteFrameMetadata(int frameIdx,ProfilerSample sample)
        {
            var metadatas = sample.metaDatas.metadatas;
            if (metadatas.Count < 2)
            {
                return;
            }
            var guidBin = metadatas[0].convertedObject as byte[];
            var tagId = (int)metadatas[1].convertedObject;
            var valueBin = metadatas[2].convertedObject as byte[];
            if (guidBin == null || valueBin == null)
            {
                return;
            }
            Guid guid = new Guid(guidBin);

            CaptureData captureData = null;
            if (guid != MetadataGuid)
            {
                return;
            }
            if (tagId == InfoTag)
            {
                captureData = new CaptureData(frameIdx,valueBin);
                captureFrameData.Add(captureData.idx, captureData);
                return;
            }
            if( captureFrameData.TryGetValue(tagId,out captureData)){
                ExecuteBinData(captureData, valueBin);
            }
        }

        private void InitDirectory()
        {
            if (!createDir)
            {
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }
                createDir = true;
            }
        }

        // execute data
        private void ExecuteBinData(CaptureData captureData,byte[] binData)
        {
            InitDirectory();

            string file = GetFilePath(captureData);
            byte[] pngBin = null;
#if UNITY_EDITOR
            pngBin = ImageConversion.EncodeArrayToPNG(binData,
                GraphicsFormat.R8G8B8A8_SRGB,
                (uint)captureData.width, (uint)captureData.height);

            // debug!
//            File.WriteAllBytes(file.Replace("png", "bin"), binData);
#endif
            if ( pngBin != null)
            {
                File.WriteAllBytes(file, pngBin);
            }
        }

        private string GetFilePath(CaptureData captureData)
        {
            stringBuilder.Length = 0;
            stringBuilder.Append(outputPath).Append("/ss-");
            stringBuilder.Append(string.Format("{0:D5}", captureData.idx));
            stringBuilder.Append(".png");
            return stringBuilder.ToString();
        }


        public void SetFileInfo(string logfilename, string outputpath)
        {
            outputPath = Path.Combine(outputpath, "screenshots");
            logFile = logfilename;
        }

        public void WriteResultFile(string logfilaneme, string outputpath)
        {
        }

        // nothing todo...
        public void SetInfo(ProfilerLogFormat logformat, string unityVersion, uint dataversion, ushort platform)
        {
        }

    }

}