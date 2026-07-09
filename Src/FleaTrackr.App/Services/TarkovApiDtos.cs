using System.Text.Json.Serialization;
using FleaTrackr.Core.Models;

namespace FleaTrackr.App.Services;

// Wire-format DTOs for the tarkov.dev GraphQL responses. Kept internal and separate from the Core
// models so the domain never depends on the API's exact JSON shape; the ToModel helpers below do
// the one-way mapping. Property names use [JsonPropertyName] to match the GraphQL fields exactly
// (notably "priceRUB", whose casing is not a simple camelCase of the C# name).

internal sealed class GqlResponse<T>
{
    [JsonPropertyName("data")] public T? Data { get; set; }
    [JsonPropertyName("errors")] public List<GqlError>? Errors { get; set; }
}

internal sealed class GqlError
{
    [JsonPropertyName("message")] public string? Message { get; set; }
}

internal sealed class ItemsData
{
    [JsonPropertyName("items")] public List<ItemDto>? Items { get; set; }
}

internal sealed class HistoryData
{
    [JsonPropertyName("historicalItemPrices")] public List<HistPointDto>? HistoricalItemPrices { get; set; }
}

internal sealed class ItemDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("shortName")] public string? ShortName { get; set; }
    [JsonPropertyName("basePrice")] public int BasePrice { get; set; }
    [JsonPropertyName("avg24hPrice")] public int? Avg24hPrice { get; set; }
    [JsonPropertyName("lastLowPrice")] public int? LastLowPrice { get; set; }
    [JsonPropertyName("low24hPrice")] public int? Low24hPrice { get; set; }
    [JsonPropertyName("high24hPrice")] public int? High24hPrice { get; set; }
    [JsonPropertyName("changeLast48hPercent")] public double? ChangeLast48hPercent { get; set; }
    [JsonPropertyName("lastOfferCount")] public int? LastOfferCount { get; set; }
    [JsonPropertyName("minLevelForFlea")] public int? MinLevelForFlea { get; set; }
    [JsonPropertyName("updated")] public string? Updated { get; set; }
    [JsonPropertyName("iconLink")] public string? IconLink { get; set; }
    [JsonPropertyName("wikiLink")] public string? WikiLink { get; set; }
    [JsonPropertyName("buyFor")] public List<VendorPriceDto>? BuyFor { get; set; }
    [JsonPropertyName("sellFor")] public List<VendorPriceDto>? SellFor { get; set; }
    [JsonPropertyName("bartersFor")] public List<BarterDto>? BartersFor { get; set; }
    [JsonPropertyName("craftsFor")] public List<CraftDto>? CraftsFor { get; set; }
    [JsonPropertyName("bartersUsing")] public List<BarterDto>? BartersUsing { get; set; }
    [JsonPropertyName("craftsUsing")] public List<CraftDto>? CraftsUsing { get; set; }

    public Item ToModel() => new()
    {
        Id = Id,
        Name = Name ?? "",
        ShortName = ShortName ?? "",
        BasePrice = BasePrice,
        Avg24hPrice = Avg24hPrice,
        LastLowPrice = LastLowPrice,
        Low24hPrice = Low24hPrice,
        High24hPrice = High24hPrice,
        ChangeLast48hPercent = ChangeLast48hPercent,
        LastOfferCount = LastOfferCount,
        MinLevelForFlea = MinLevelForFlea,
        Updated = TarkovApiParse.Timestamp(Updated),
        IconLink = IconLink,
        WikiLink = WikiLink,
        BuyFor = VendorPriceDto.ToModels(BuyFor),
        SellFor = VendorPriceDto.ToModels(SellFor),
    };
}

internal sealed class VendorPriceDto
{
    [JsonPropertyName("price")] public int Price { get; set; }
    [JsonPropertyName("currency")] public string? Currency { get; set; }
    [JsonPropertyName("priceRUB")] public int PriceRub { get; set; }
    [JsonPropertyName("vendor")] public VendorDto? Vendor { get; set; }

    public static IReadOnlyList<VendorPrice> ToModels(List<VendorPriceDto>? dtos) =>
        dtos is null ? [] : dtos.Select(d => new VendorPrice(
            d.Vendor?.Name ?? "", d.Price, d.Currency ?? "RUB", d.PriceRub)).ToList();
}

internal sealed class VendorDto
{
    [JsonPropertyName("name")] public string? Name { get; set; }
}

internal sealed class NamedDto
{
    [JsonPropertyName("name")] public string? Name { get; set; }
}

internal sealed class StackDto
{
    [JsonPropertyName("item")] public ItemDto? Item { get; set; }
    [JsonPropertyName("count")] public double Count { get; set; }

    public static IReadOnlyList<ItemStack> ToModels(List<StackDto>? dtos) =>
        dtos is null ? [] : dtos.Where(d => d.Item is not null)
            .Select(d => new ItemStack(d.Item!.ToModel(), d.Count)).ToList();
}

internal sealed class BarterDto
{
    [JsonPropertyName("trader")] public NamedDto? Trader { get; set; }
    [JsonPropertyName("level")] public int Level { get; set; }
    [JsonPropertyName("requiredItems")] public List<StackDto>? RequiredItems { get; set; }
    [JsonPropertyName("rewardItems")] public List<StackDto>? RewardItems { get; set; }

    public Barter ToModel() => new()
    {
        TraderName = Trader?.Name ?? "",
        TraderLevel = Level,
        RequiredItems = StackDto.ToModels(RequiredItems),
        RewardItems = StackDto.ToModels(RewardItems),
    };
}

internal sealed class CraftDto
{
    [JsonPropertyName("station")] public NamedDto? Station { get; set; }
    [JsonPropertyName("level")] public int Level { get; set; }
    [JsonPropertyName("duration")] public int Duration { get; set; }
    [JsonPropertyName("requiredItems")] public List<StackDto>? RequiredItems { get; set; }
    [JsonPropertyName("rewardItems")] public List<StackDto>? RewardItems { get; set; }

    public Craft ToModel() => new()
    {
        StationName = Station?.Name ?? "",
        StationLevel = Level,
        Duration = TimeSpan.FromSeconds(Duration),
        RequiredItems = StackDto.ToModels(RequiredItems),
        RewardItems = StackDto.ToModels(RewardItems),
    };
}

internal sealed class HistPointDto
{
    [JsonPropertyName("price")] public int Price { get; set; }
    [JsonPropertyName("priceMin")] public int PriceMin { get; set; }
    [JsonPropertyName("offerCount")] public int OfferCount { get; set; }
    [JsonPropertyName("timestamp")] public string? Timestamp { get; set; }

    public HistoricalPricePoint ToModel() =>
        new(TarkovApiParse.Timestamp(Timestamp) ?? default, Price, PriceMin, OfferCount);
}

/// <summary>Shared parsing helpers for the loosely-typed timestamp strings the API returns.</summary>
internal static class TarkovApiParse
{
    /// <summary>
    /// The API returns timestamps either as ISO-8601 (item <c>updated</c>) or as Unix-millisecond
    /// strings (history <c>timestamp</c>). Handle both; return null on anything unparseable.
    /// </summary>
    public static DateTimeOffset? Timestamp(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (long.TryParse(raw, out long unixMs))
            return DateTimeOffset.FromUnixTimeMilliseconds(unixMs);
        return DateTimeOffset.TryParse(raw, out DateTimeOffset dto) ? dto : null;
    }
}
