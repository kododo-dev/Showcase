using Kododo.ConfigWay;
using Kododo.ConfigWay.PostgreSQL;
using Kododo.ConfigWay.UI;
using Kododo.CultureWay;
using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.PostgreSQL;
using Kododo.CultureWay.UI;
using Kododo.RunWay;
using Kododo.RunWay.Dashboard;
using Kododo.RunWay.PostgreSQL;
using Kododo.RunWay.Runner;
using Kododo.RunWay.Schedule;
using Kododo.Showcase;
using Kododo.Way.Shell;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Npgsql;

// ConfigWay, CultureWay and RunWay in one app, each on its own page and together in the Way.Shell panel at /admin.
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ShowcaseDB")
    ?? throw new InvalidOperationException("Set ConnectionStrings:ShowcaseDB to a PostgreSQL database.");
string[] cultures = ["en", "pl", "de"];

// One shell for every Way: the panel and each Way's own page share the title, links and theme.
builder.Services.AddWayShell(o =>
{
    o.Title = "Kododo Ways";
    o.HomeUrl = "~/";
    o.Links.Add(new ShellLink("Shop", "~/") { Icon = "home" });
    o.Links.Add(new ShellLink("GitHub", "https://github.com/kododo-dev"));
});

builder.AddConfigWay(x =>
{
    x.AddOptions<ShopOptions>("Shop");
    x.AddOptions<SearchOptions>("Search");
    x.AddUiEditor();
    x.UsePostgreSql(connectionString);
});

builder.Services.AddLocalization();
builder.Services.AddCultureWay(x =>
{
    x.Options.SupportedCultures = cultures;
    x.Options.DefaultCulture = "en";
    x.AddEditor();
    x.UsePostgreSQL(connectionString);
});
builder.Services.AddRequestLocalization(o =>
{
    o.SetDefaultCulture("en").AddSupportedCultures(cultures).AddSupportedUICultures(cultures);
    o.RequestCultureProviders = [new QueryStringRequestCultureProvider(), new AcceptLanguageHeaderRequestCultureProvider()];
});

// RunWay's DbContext gets a connection per scope; the scope disposes it.
builder.Services.AddScoped(_ => new NpgsqlConnection(connectionString));
builder.Services.AddRunWay(x =>
{
    x.AddDashboard()
        .UsePostgreSQL(p => p.GetRequiredService<NpgsqlConnection>())
        .AddRunner(r =>
        {
            r.AddHandlersFromAssembly(typeof(Program).Assembly);
            r.DeleteSucceededAfterTimeSpan = TimeSpan.FromMinutes(30);
        });
});

var app = builder.Build();
if (app.Configuration["PathBase"] is { Length: > 0 } pathBase)
{
    app.UsePathBase(pathBase);
    // Routing has to run after the path base is removed; otherwise POSTs under the prefix get 405.
    app.UseRouting();
}
app.UseRequestLocalization();

await app.InitializeRunWayDatabaseAsync();
await app.InitializeCultureWayAsync();
await SeedTranslationsAsync(app);
await app.SetRecurrenceAsync("cleanup", "*/5 * * * *", new CleanupJob { What = "abandoned carts" });

app.MapGet("/", (HttpContext context, IOptionsSnapshot<ShopOptions> shop, IStringLocalizer<HomePage> text) =>
{
    var culture = context.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture.Name ?? "en";
    return Results.Content(HomePage.Render($"{context.Request.PathBase}/", shop.Value, text, culture), "text/html");
});
app.MapPost("/jobs/newsletter", async (IScheduler runway) =>
{
    await runway.Job(new NewsletterJob { Issue = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") }).ScheduleAsync(CancellationToken.None);
    return Results.NoContent();
});
app.MapPost("/jobs/reindex", async (IScheduler runway) =>
{
    await runway.Job(new ReindexJob { Products = 5_000 }).ScheduleAsync(CancellationToken.None);
    return Results.NoContent();
});

// A public demo: anyone may open every Way and the panel. A real app protects them, e.g.
// app.UseConfigWay().RequireAuthorization("Admin") and app.UseWayShell("/admin").RequireAuthorization().
app.UseConfigWay().AllowAnonymous();
app.UseCultureWay().AllowAnonymous();
app.UseRunWayDashboard().AllowAnonymous();
app.UseWayShell("/admin").AllowAnonymous();

app.Run();

static async Task SeedTranslationsAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var store = scope.ServiceProvider.GetRequiredService<IStore>();
    if ((await store.GetAllAsync()).Count > 0) return;
    await store.SetAsync(ShopTranslations.Seed());
    await app.InitializeCultureWayAsync();
}
