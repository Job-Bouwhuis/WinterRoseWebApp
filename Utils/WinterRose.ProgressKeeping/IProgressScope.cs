namespace WinterRose.ProgressKeeping;

public interface IProgressScope
{
    void Report(double value, string? message = null);
    IProgressScope CreateChild(double weight = 1.0);
}