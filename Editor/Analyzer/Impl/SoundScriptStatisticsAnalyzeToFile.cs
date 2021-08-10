namespace UTJ.ProfilerReader.Analyzer
{
    public class SoundScriptStatisticsAnalyzeToFile : MainThreadAnalyzeToFile
    {
        protected override bool IsSampleNameMatched(string name)
        {
            foreach (var method in SoundScriptAnalyzeToFile.MethodPatterns)
            {
                if (name.Contains(method))
                {
                    return true;
                }
            }

            return false;
        }

        protected override string FooterName
        {
            get { return "_main_thread_sound_methods_statistics.csv"; }
        }
    }
}