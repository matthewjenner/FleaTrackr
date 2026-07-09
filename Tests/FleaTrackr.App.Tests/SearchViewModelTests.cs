using FleaTrackr.App.Services;
using FleaTrackr.App.ViewModels;
using FleaTrackr.Core.Models;
using FluentAssertions;

namespace FleaTrackr.App.Tests;

/// <summary>
/// Drives <see cref="SearchViewModel"/> through the real <see cref="TarkovApiClient"/> backed by a
/// stub HTTP handler (full VM -> client -> HTTP path), verifying results populate, the empty state
/// is set, and the active game mode reaches the API.
/// </summary>
public class SearchViewModelTests
{
    private const string TwoItems = """
        {"items":[
          {"id":"gpu1","name":"Graphics card","shortName":"GPU","basePrice":42000,
           "avg24hPrice":150000,"lastLowPrice":148000,
           "sellFor":[{"price":150000,"currency":"RUB","priceRUB":150000,"vendor":{"name":"Flea Market"}}]},
          {"id":"gpu2","name":"Graphics card (used)","shortName":"GPU","basePrice":30000,
           "sellFor":[{"price":90000,"currency":"RUB","priceRUB":90000,"vendor":{"name":"Mechanic"}}]}
        ]}
        """;

    private static SearchViewModel MakeVm(StubHttpMessageHandler handler, GameMode mode)
    {
        var client = new TarkovApiClient(new HttpClient(handler));
        return new SearchViewModel(client, () => mode);
    }

    [Fact]
    public async Task Search_populates_results()
    {
        var handler = new StubHttpMessageHandler().EnqueueData(TwoItems);
        var vm = MakeVm(handler, GameMode.Pvp) ;
        vm.Query = "gpu";

        await vm.SearchAsync(TestContext.Current.CancellationToken);

        vm.Results.Should().HaveCount(2);
        vm.Results[0].Name.Should().Be("Graphics card");
        vm.Results[0].FleaSellText.Should().Be("150,000");
        vm.IsEmptyResult.Should().BeFalse();
    }

    [Fact]
    public async Task No_matches_sets_the_empty_state()
    {
        var handler = new StubHttpMessageHandler().EnqueueData("""{"items":[]}""");
        var vm = MakeVm(handler, GameMode.Pvp);
        vm.Query = "zzz";

        await vm.SearchAsync(TestContext.Current.CancellationToken);

        vm.Results.Should().BeEmpty();
        vm.IsEmptyResult.Should().BeTrue();
    }

    private static Item ItemWithFlea(string name, int fleaSell) => new()
    {
        Id = name, Name = name,
        SellFor = [new VendorPrice(VendorPrice.FleaMarketVendorName, fleaSell, "RUB", fleaSell)],
    };

    [Fact]
    public async Task Results_sort_by_flea_price_high_to_low()
    {
        var api = new FakeApi()
            .SetItem(ItemWithFlea("gpu-a", 50_000))
            .SetItem(ItemWithFlea("gpu-b", 150_000))
            .SetItem(ItemWithFlea("gpu-c", 90_000));
        var vm = new SearchViewModel(api, () => GameMode.Pvp) { Query = "gpu" };

        await vm.SearchAsync(TestContext.Current.CancellationToken);
        vm.SelectedSort = SearchSortOption.All.First(o => o.Sort == SearchSort.PriceHighToLow);

        vm.Results.Select(r => r.Name).Should().ContainInOrder("gpu-b", "gpu-c", "gpu-a");
    }

    [Fact]
    public async Task Search_sends_the_active_game_mode()
    {
        var handler = new StubHttpMessageHandler().EnqueueData(TwoItems);
        var vm = MakeVm(handler, GameMode.Pve);
        vm.Query = "gpu";

        await vm.SearchAsync(TestContext.Current.CancellationToken);

        handler.RequestBodies.Should().ContainSingle().Which.Should().Contain("\"mode\":\"pve\"");
    }
}
