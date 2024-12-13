namespace Api.DTOs
{
    public class FraudAnalysisResult
    {
        public double FraudScore { get; set; }
        public double InfluentialScore { get; set; }
        public double BlacklistScore { get; set; }
        public double AnomalyScore { get; set; }
    }
}
