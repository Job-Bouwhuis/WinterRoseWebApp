namespace WinterRoseWebApp.Features.Dashboard.Models;

public class DashboardCardModel
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int Value { get; set; }
    public bool IsActive { get; set; }
}
