namespace WinterRose.ProgressKeeping;

public interface IProgressScope
{
    Task ReportAsync(double value, string? message, ReportStatus status);
    IProgressScope CreateChild(double weight = 1.0);
}