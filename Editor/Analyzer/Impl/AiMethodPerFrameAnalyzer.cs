namespace UTJ.ProfilerReader.Analyzer
{
    public class AiMethodPerFrameAnalyzer : AbstractMethodPerFrameSampleAnalyzer
    {
        protected override bool IsSampleNameMatched(string name)
        {
            return name.Contains("AiPlayer") || name.Contains("NeoPath");
        }

        protected override string FooterName => "_ai_method_per_frame.csv";
    }
}