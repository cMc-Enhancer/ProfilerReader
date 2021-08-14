using System;
using System.IO;
using UTJ.ProfilerReader.BinaryData;

namespace UTJ.ProfilerReader.Analyzer
{
    public abstract class AbstractTextBasedFileOutputAnalyzer : IAnalyzer
    {
        protected ProfilerLogFormat logFormat { get; private set; }
        protected uint logVersion { get; private set; }
        protected ushort logPlatform { get; private set; }

        protected abstract string FooterName { get; }
        protected string unityVersion { get; private set; }

        public void SetInfo(ProfilerLogFormat format, string unityVer, uint dataversion, ushort platform)
        {
            logFormat = format;
            logVersion = dataversion;
            logPlatform = platform;
            unityVersion = unityVer;
        }

        public abstract void CollectData(ProfilerFrameData frameData);

        protected abstract string GetResultText();

        public void WriteResultFile(string logfile, string outputpath)
        {
            try
            {
                string path = Path.Combine(outputpath, logfile.Replace(".", "_") + FooterName);
                string result = GetResultText();
                File.WriteAllText(path, result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ProfilerLogUtil.logErrorException(e);
            }
        }

        public void SetFileInfo(string logfile, string outputpath)
        {
        }
    }
}