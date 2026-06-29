namespace WinterRose.ProgressKeeping;

public class ProgressScope : IProgressScope
{
    private readonly Action<double, string?> reporter;
    private readonly double weight;
    private double offset;

    public ProgressScope(Action<double, string?> reporter, double weight = 1.0, double offset = 0)
    {
        this.reporter = reporter;
        this.weight = weight;
        this.offset = offset;
    }

    public void Report(double value, string? message = null)
    {
        double scaled = offset + (value * weight);
        reporter(scaled, message);
    }

    public IProgressScope CreateChild(double weight = 1.0)
    {
        return new ProgressScope(reporter, weight * this.weight, offset);
    }
}