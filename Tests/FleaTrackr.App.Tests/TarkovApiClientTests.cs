using System.Net;
using FleaTrackr.App.Services;
using FleaTrackr.Core.Models;
using FluentAssertions;

namespace FleaTrackr.App.Tests;

/// <summary>
/// Offline tests for <see cref="TarkovApiClient"/>: JSON -> model mapping (flea vs trader prices),
/// the PVP/PVE game-mode variable, and the 429 -> retry -> success path. No live API.
/// </summary>
public class TarkovApiClientTests
{
    private const string GpuItemsData = """
        {"items":[{
          "id":"gpu1","name":"Graphics card","shortName":"GPU","basePrice":42000,
          "avg24hPrice":150000,"lastLowPrice":148000,"low24hPrice":140000,"high24hPrice":160000,
          "changeLast48hPercent":-2.5,"lastOfferCount":312,"minLevelForFlea":15,
          "updated":"2026-01-02T03:04:05.000Z","iconLink":"http://x/i.png","wikiLink":"http://wiki/gpu",
          "buyFor":[{"price":160000,"currency":"RUB","priceRUB":160000,"vendor":{"name":"Flea Market"}}],
          "sellFor":[
            {"price":150000,"currency":"RUB","priceRUB":150000,"vendor":{"name":"Flea Market"}},
            {"price":110000,"currency":"RUB","priceRUB":110000,"vendor":{"name":"Mechanic"}}
          ]
        }]}
        """;

    private static TarkovApiClient ClientWith(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler));

    [Fact]
    public async Task SearchItems_maps_flea_and_trader_prices()
    {
        var handler = new StubHttpMessageHandler().EnqueueData(GpuItemsData);
        using var client = ClientWith(handler);

        IReadOnlyList<Item> items = await client.SearchItemsAsync("gpu", GameMode.Pvp, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        Item gpu = items[0];
        gpu.Name.Should().Be("Graphics card");
        gpu.MinLevelForFlea.Should().Be(15);
        gpu.Updated.Should().Be(DateTimeOffset.Parse("2026-01-02T03:04:05.000Z"));
        gpu.FleaSell!.PriceRub.Should().Be(150000);
        gpu.FleaBuy!.PriceRub.Should().Be(160000);
        gpu.BestTraderSell!.Vendor.Should().Be("Mechanic");
        gpu.BestTraderSell!.PriceRub.Should().Be(110000);
    }

    [Fact]
    public async Task Passes_the_selected_game_mode_as_a_graphql_variable()
    {
        var handler = new StubHttpMessageHandler().EnqueueData(GpuItemsData);
        using var client = ClientWith(handler);

        await client.SearchItemsAsync("gpu", GameMode.Pve, ct: TestContext.Current.CancellationToken);

        handler.RequestBodies.Should().ContainSingle()
            .Which.Should().Contain("\"mode\":\"pve\"");
    }

    [Fact]
    public async Task Retries_on_429_then_succeeds()
    {
        var handler = new StubHttpMessageHandler()
            .Enqueue(HttpStatusCode.TooManyRequests, "rate limited")
            .EnqueueData(GpuItemsData);
        using var client = ClientWith(handler);

        IReadOnlyList<Item> items = await client.SearchItemsAsync("gpu", GameMode.Pvp, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        handler.RequestBodies.Should().HaveCount(2, "the first 429 should have been retried");
    }

    [Fact]
    public async Task Empty_query_short_circuits_without_calling_the_api()
    {
        var handler = new StubHttpMessageHandler();
        using var client = ClientWith(handler);

        IReadOnlyList<Item> items = await client.SearchItemsAsync("   ", GameMode.Pvp, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        handler.RequestBodies.Should().BeEmpty();
    }
}
