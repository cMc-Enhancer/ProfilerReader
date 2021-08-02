namespace UTJ.ProfilerReader.Analyzer
{
    public class AiScriptAnalyzeToFile : AbstractMethodSampleAnalyzer
    {
        protected override bool IsSampleNameMatched(string name)
        {
            return name.Contains("AiPlayer") || name.Contains("NeoPath");
        }

        protected override string FooterName
        {
            get { return "_ai_method_per_frame.csv"; }
        }
    }
}