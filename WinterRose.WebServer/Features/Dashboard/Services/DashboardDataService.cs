using WinterRose.WebServer.Features.Dashboard.Models;

namespace WinterRose.WebServer.Features.Dashboard.Services;

public class DashboardDataService
{
    private static readonly Random RANDOM = new();

    public Task<List<DashboardCardModel>> GetCardsAsync()
    {
        var cards = new List<DashboardCardModel>
        {
            CreateCard("Auth System"),
            CreateCard("Inventory System"),
            CreateCard("World Simulation"),
            CreateCard("UI Renderer"),
            CreateCard("Audio Engine"),
        };

        return Task.FromResult(cards);
    }

    private DashboardCardModel CreateCard(string title)
    {
        return new DashboardCardModel
        {
            Title = title,
            Description = $"{title} subsystem status overview",
            Value = RANDOM.Next(0, 100),
            IsActive = RANDOM.NextDouble() > 0.3
        };
    }
}
