namespace WinterRose.ProgressKeeping;

public struct ProgressScope : IProgressScope
{
    private readonly Func<double, string?, ReportStatus, Task>? reporter;
    private readonly double weight;
    private double offset;

    public ProgressScope() : this(null, 0, 0) {}
    
    public ProgressScope(Func<double, string?, ReportStatus, Task>? reporter, double weight = 1.0, double offset = 0)
    {
        this.reporter = reporter;
        this.weight = weight;
        this.offset = offset;
    }

    public async Task ReportAsync(double value, string? message, ReportStatus status)
    {
        double scaled = offset + (value * weight);
        if(reporter is not null)
            await reporter.Invoke(scaled, message, status);
    }

    public IProgressScope CreateChild(double weight = 1.0)
    {
        return new ProgressScope(reporter, weight * this.weight, offset);
    }
}