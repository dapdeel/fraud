public class FrequencyCalculator
{
    private const double smoothingFactor = 0.1;
    private const double defaultInterval = 86400;

    public double Calculate(double CurrentEMEA, DateTime currentTransactionTime, DateTime lastTransactionTime)
    {
        var difference = lastTransactionTime - currentTransactionTime;
        return EMEA(CurrentEMEA, difference.Seconds);
    }
    public double Calculate()
    {
        return defaultInterval;
    }
    private double EMEA(double lastEmea, double interval)
    {
        double ema = smoothingFactor * interval + (1 - smoothingFactor) * lastEmea;
        return ema;
    }

}