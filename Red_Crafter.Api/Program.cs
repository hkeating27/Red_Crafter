using System.Net.Http.Json;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

var app = builder.Build();

// ---- If you run http-only, you can comment this out to remove warnings ----
// app.UseHttpsRedirection();

// --------------------
// Load recipes.json
// --------------------
var recipesPath = Path.Combine(app.Environment.ContentRootPath, "Data", "recipes.json");

var recipesList = File.Exists(recipesPath)
    ? JsonSerializer.Deserialize<List<Recipe>>(File.ReadAllText(recipesPath),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new()
    : new();

var recipesById = recipesList.ToDictionary(r => r.itemId, r => r);

app.Logger.LogInformation("Loaded {Count} recipes from {Path}", recipesList.Count, recipesPath);

// --------------------
// Endpoints
// --------------------
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", app = "Red Crafter" }));

// Debug endpoint: see the computed recursive cost for a single item
app.MapGet("/api/cost", async (
    string world,
    int itemId,
    IHttpClientFactory factory,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(world))
        return Results.BadRequest(new { error = "world is required" });

    var client = factory.CreateClient();

    var priceCache = new Dictionary<int, int>(); // market sane price cache
    var costMemo = new Dictionary<int, int>();  // recursive cost memo

    var (cost, detail) = await ComputeCostAsync(
        world,
        itemId,
        client,
        recipesById,
        priceCache,
        costMemo,
        new HashSet<int>(),
        ct);

    return Results.Ok(new
    {
        world,
        itemId,
        computedCostGil = cost,
        note = detail
    });
});

// Top 20 profits using recursive material cost
app.MapGet("/api/profits/top", async (
    string world,
    IHttpClientFactory factory,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(world))
        return Results.BadRequest(new { error = "world is required" });

    var client = factory.CreateClient();

    // Per-request caches
    var priceCache = new Dictionary<int, int>(); // itemId -> sane market price
    var costMemo = new Dictionary<int, int>();  // itemId -> computed recursive cost

    const decimal taxRate = 0.05m;

    var results = new List<ProfitRow>();
    var skipped = new List<object>();

    foreach (var r in recipesList)
    {
        // Sell price (sane)
        var sellPrice = await GetSaneMarketPriceAsync(world, r.itemId, client, priceCache, ct);
        if (sellPrice <= 0)
        {
            skipped.Add(new { r.itemId, r.name, error = "No sell listings" });
            continue;
        }

        // Recursive cost for this craft
        var (unitCost, costNote) = await ComputeCostAsync(
            world,
            r.itemId,
            client,
            recipesById,
            priceCache,
            costMemo,
            new HashSet<int>(),
            ct);

        if (unitCost <= 0)
        {
            skipped.Add(new { r.itemId, r.name, error = "Could not compute cost", note = costNote });
            continue;
        }

        // Outlier filter: ignore extreme garbage prices
        if (sellPrice > unitCost * 1000)
        {
            skipped.Add(new { r.itemId, r.name, error = "Outlier filtered", sellPrice, unitCost });
            continue;
        }

        // Profit (per recipe output)
        var gross = (decimal)sellPrice * r.outputQty;
        var tax = Math.Round(gross * taxRate, 0, MidpointRounding.AwayFromZero);
        var profit = gross - tax - unitCost * r.outputQty;

        // Profit percent based on cost
        var profitPct = Math.Round((profit / (unitCost * r.outputQty)) * 100m, 2);

        results.Add(new ProfitRow(
            r.itemId,
            r.name,
            sellPrice,
            unitCost,
            r.outputQty,
            (int)tax,
            (int)profit,
            profitPct
        ));
    }

    var top = results
        .OrderByDescending(x => x.ProfitGil)
        .ThenByDescending(x => x.ProfitPercent)
        .Take(20)
        .ToList();

    return Results.Ok(new
    {
        world,
        items = top,
        skippedCount = skipped.Count,
        skipped
    });
});

app.Run();

// --------------------
// Core logic
// --------------------

// Sane market price = median of the lowest 10 listings (reduces “troll min”)
async Task<int> GetSaneMarketPriceAsync(
    string world,
    int itemId,
    HttpClient client,
    Dictionary<int, int> cache,
    CancellationToken ct)
{
    if (cache.TryGetValue(itemId, out var cached))
        return cached;

    var url =
        $"https://universalis.app/api/v2/{Uri.EscapeDataString(world)}/{itemId}" +
        $"?listings=1000&fields=listings.pricePerUnit";

    using var resp = await client.GetAsync(url, ct);
    if (!resp.IsSuccessStatusCode)
    {
        cache[itemId] = 0;
        return 0;
    }

    var uni = await resp.Content.ReadFromJsonAsync<UniversalisResponse>(cancellationToken: ct);
    if (uni?.listings == null || uni.listings.Length == 0)
    {
        cache[itemId] = 0;
        return 0;
    }

    var sorted = uni.listings
        .Select(l => l.pricePerUnit)
        .Where(p => p > 0)
        .OrderBy(p => p)
        .ToArray();

    if (sorted.Length == 0)
    {
        cache[itemId] = 0;
        return 0;
    }

    // take the lowest 10 (or fewer)
    var low = sorted.Take(Math.Min(10, sorted.Length)).ToArray();

    // median
    int median = low[low.Length / 2];

    cache[itemId] = median;
    return median;
}

// Computes the cheapest way to obtain 1 unit of itemId:
// - if craftable: craftCost (recursive)
// - if buyable: marketCost (sane)
// returns min(craftCost, marketCost) when both exist
async Task<(int unitCost, string note)> ComputeCostAsync(
    string world,
    int itemId,
    HttpClient client,
    Dictionary<int, Recipe> recipesById,
    Dictionary<int, int> marketPriceCache,
    Dictionary<int, int> memo,
    HashSet<int> visiting,
    CancellationToken ct)
{
    if (memo.TryGetValue(itemId, out var cachedCost))
        return (cachedCost, "memo");

    if (visiting.Contains(itemId))
        return (0, "Cycle detected in recipes.json");

    // Market price is always a fallback
    var market = await GetSaneMarketPriceAsync(world, itemId, client, marketPriceCache, ct);

    // If not craftable, use market
    if (!recipesById.TryGetValue(itemId, out var recipe))
    {
        memo[itemId] = market;
        return (market, "market-only");
    }

    // Craft cost
    visiting.Add(itemId);

    long sum = 0;

    foreach (var ing in recipe.ingredients)
    {
        if (ing.qty <= 0) continue;

        var (ingUnitCost, ingNote) = await ComputeCostAsync(
            world,
            ing.itemId,
            client,
            recipesById,
            marketPriceCache,
            memo,
            visiting,
            ct);

        if (ingUnitCost <= 0)
        {
            visiting.Remove(itemId);
            return (0, $"Missing ingredient cost for {ing.name} ({ing.itemId}) [{ingNote}]");
        }

        sum += (long)ingUnitCost * ing.qty;
    }

    visiting.Remove(itemId);

    // Normalize cost per 1 output unit if outputQty > 1
    var craftUnitCost = recipe.outputQty > 0
        ? (int)Math.Ceiling(sum / (decimal)recipe.outputQty)
        : (int)sum;

    // Choose cheapest of craft vs market (if market exists)
    int chosen;
    string note;

    if (market > 0)
    {
        chosen = Math.Min(craftUnitCost, market);
        note = chosen == craftUnitCost ? "craft-cheaper" : "market-cheaper";
    }
    else
    {
        chosen = craftUnitCost;
        note = "craft-only";
    }

    memo[itemId] = chosen;
    return (chosen, note);
}

// --------------------
// Records
// --------------------
record UniversalisResponse(Listing[]? listings);
record Listing(int pricePerUnit);

record Recipe(int itemId, string name, int outputQty, RecipeIngredient[] ingredients);
record RecipeIngredient(int itemId, string name, int qty);

record ProfitRow(
    int ItemId,
    string Name,
    int SellPricePerUnit,
    int CostPerUnit,
    int OutputQty,
    int EstimatedTaxGil,
    int ProfitGil,
    decimal ProfitPercent
);
