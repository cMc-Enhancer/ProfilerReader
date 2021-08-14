namespace UTJ.ProfilerReader.Analyzer
{
    public class SoundMethodsStatisticsAnalyzer : MainThreadMethodsStatisticsAnalyzer
    {
        protected override bool IsSampleNameMatched(string name)
        {
            foreach (var method in SoundMethodPerFrameAnalyzer.MethodPatterns)
            {
                if (name.Contains(method))
                {
                    return true;
                }
            }

            return false;
        }

        protected override string FooterName => "_main_thread_sound_methods_statistics.csv";
    }
}