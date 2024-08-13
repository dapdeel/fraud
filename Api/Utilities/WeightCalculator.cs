public class WeightCalculator
{
    private const double DecayRate = 0.0001;

    public float Calculate(double EMEA, float Amount, DateTime LastTransactionDate)
    {
        double BaseWeight = Amount / EMEA;
        double TimeDifference = (DateTime.Now - LastTransactionDate).TotalMinutes;
        float DecayedWeight = (float)(BaseWeight * Math.Exp(-DecayRate * TimeDifference));
        return DecayedWeight;
    }
      public float Calculate( double EMEA, float Amount)
    {
        float BaseWeight = (float)(Amount / EMEA);
        return BaseWeight;
    }
}