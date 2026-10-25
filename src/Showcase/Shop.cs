using System.ComponentModel.DataAnnotations;
using Kododo.CultureWay.Core.Model;
using Kododo.RunWay.Core.Handlers;
using Microsoft.Extensions.Options;

namespace Kododo.Showcase;

// A small made-up shop that uses every Way: ConfigWay holds its settings and a feature flag, CultureWay the texts of
// its home page, RunWay its background jobs, which read the settings, and MonitorWay shows what it all does. The
// settings and the flag start from appsettings.json.

[Display(Name = "Shop", Description = "The shop's public details, shown on the home page and used by the jobs.")]
public class ShopOptions
{
    [Display(Name = "Name", Description = "Shown at the top of the home page.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Support Email", Description = "Shown in the footer of the home page.")]
    [EmailAddress]
    public string SupportEmail { get; set; } = string.Empty;

    [Display(Name = "Newsletter", Description = "When off, the newsletter job skips sending.")]
    public bool NewsletterEnabled { get; set; }
}

[Display(Name = "Search", Description = "The shop's product search, rebuilt by the reindex job.")]
public class SearchOptions
{
    [Display(Name = "Batch Size", Description = "Products indexed per batch.")]
    [Range(10, 10_000)]
    public int BatchSize { get; set; }
}

internal class NewsletterJob
{
    public required string Issue { get; set; }
}

internal class NewsletterJobHandler(IOptionsMonitor<ShopOptions> shop, ILogger<NewsletterJobHandler> logger) : IJobHandler<NewsletterJob>
{
    public async Task HandleAsync(NewsletterJob data, CancellationToken stoppingToken)
    {
        // Read when the job runs: a change saved in Configuration applies to the next run.
        var options = shop.CurrentValue;
        if (!options.NewsletterEnabled)
        {
            logger.LogInformation("Newsletter {Issue} skipped: turned off in the {Shop} settings", data.Issue, options.Name);
            return;
        }
        logger.LogInformation("Sending newsletter {Issue} of {Shop}", data.Issue, options.Name);
        await Task.Delay(TimeSpan.FromSeconds(4), stoppingToken);
    }
}

internal class ReindexJob
{
    public required int Products { get; set; }
}

internal class ReindexJobHandler(IOptionsMonitor<SearchOptions> search, ILogger<ReindexJobHandler> logger) : IJobHandler<ReindexJob>
{
    public async Task HandleAsync(ReindexJob data, CancellationToken stoppingToken)
    {
        var batch = search.CurrentValue.BatchSize;
        for (var done = 0; done < data.Products; done += batch)
        {
            logger.LogInformation("Indexed {Done} of {Products} products", Math.Min(done + batch, data.Products), data.Products);
            await Task.Delay(TimeSpan.FromMilliseconds(300), stoppingToken);
        }
    }
}

internal class CleanupJob
{
    public required string What { get; set; }
}

internal class CleanupJobHandler(ILogger<CleanupJobHandler> logger) : IJobHandler<CleanupJob>
{
    public Task HandleAsync(CleanupJob data, CancellationToken stoppingToken)
    {
        logger.LogInformation("Cleaned up {What}", data.What);
        return Task.CompletedTask;
    }
}

/// <summary>Texts of the home page, scoped to <see cref="HomePage"/>; those missing from the database are written at startup.</summary>
internal static class ShopTranslations
{
    public static Translation[] Seed() =>
    [
        T("Tagline", "en", "Loose-leaf tea, shipped fresh."),
        T("Tagline", "pl", "Herbata liściasta, wysyłana świeża."),
        T("Tagline", "de", "Loser Tee, frisch versandt."),
        T("Intro", "en", "This page is the shop. Its settings, texts and background jobs are managed in one admin panel."),
        T("Intro", "pl", "Ta strona to sklep. Jego ustawienia, teksty i zadania w tle są zarządzane w jednym panelu."),
        T("Intro", "de", "Diese Seite ist der Shop. Einstellungen, Texte und Hintergrundjobs werden in einem Admin-Panel verwaltet."),
        T("HeroText", "en", "Single-estate teas from Japan, India and Sri Lanka, picked by people who drink them every day and packed the week they ship."),
        T("HeroText", "pl", "Herbaty z pojedynczych plantacji w Japonii, Indiach i na Sri Lance, wybierane przez ludzi, którzy piją je codziennie, i pakowane w tygodniu wysyłki."),
        T("HeroText", "de", "Tees von einzelnen Gärten in Japan, Indien und Sri Lanka, ausgewählt von Menschen, die sie täglich trinken, und in der Versandwoche verpackt."),
        T("Collection", "en", "This season's teas"),
        T("Collection", "pl", "Herbaty tego sezonu"),
        T("Collection", "de", "Tees dieser Saison"),
        T("Sencha", "en", "Grassy and bright, with a sweet, lingering finish."),
        T("Sencha", "pl", "Trawiasta i świeża, ze słodkim, długim finiszem."),
        T("Sencha", "de", "Grasig und frisch, mit süßem, langem Abgang."),
        T("Darjeeling", "en", "Floral, light and muscatel: the first pick of spring."),
        T("Darjeeling", "pl", "Kwiatowy, lekki, muszkatołowy: pierwszy zbiór wiosny."),
        T("Darjeeling", "de", "Blumig, leicht und muskatig: die erste Pflückung des Frühlings."),
        T("EarlGrey", "en", "Ceylon black tea with real bergamot oil."),
        T("EarlGrey", "pl", "Czarna herbata cejlońska z prawdziwym olejkiem bergamotki."),
        T("EarlGrey", "de", "Schwarzer Ceylon-Tee mit echtem Bergamotteöl."),
        T("Behind", "en", "Behind the scenes"),
        T("Behind", "pl", "Za kulisami"),
        T("Behind", "de", "Hinter den Kulissen"),
        T("BehindText", "en", "Four Kododo Ways run this shop. Change something in the panel and reload this page: no restart, no deploy."),
        T("BehindText", "pl", "Ten sklep obsługują cztery Kododo Ways. Zmień coś w panelu i odśwież tę stronę: bez restartu, bez wdrożenia."),
        T("BehindText", "de", "Vier Kododo Ways betreiben diesen Shop. Ändern Sie etwas im Panel und laden Sie die Seite neu: ohne Neustart, ohne Deployment."),
        T("ConfigWay", "en", "The shop's name, support email and job settings, and the free shipping flag. Rename the shop or turn the flag off and the page changes."),
        T("ConfigWay", "pl", "Nazwa sklepu, e-mail pomocy, ustawienia zadań i flaga darmowej dostawy. Zmień nazwę albo wyłącz flagę, a strona się zmieni."),
        T("ConfigWay", "de", "Name des Shops, Support-E-Mail, Job-Einstellungen und das Flag für kostenlosen Versand. Benennen Sie den Shop um oder schalten Sie das Flag aus, und die Seite ändert sich."),
        T("MonitorWay", "en", "Logs, traces, errors and metrics of this shop. Load this page or run a job, then find it in the logs and the traces."),
        T("MonitorWay", "pl", "Logi, ślady, błędy i metryki tego sklepu. Odśwież tę stronę albo uruchom zadanie, a potem znajdź je w logach i śladach."),
        T("MonitorWay", "de", "Logs, Traces, Fehler und Metriken dieses Shops. Laden Sie diese Seite oder starten Sie einen Job und finden Sie ihn dann in den Logs und Traces."),
        T("FreeShipping", "en", "Free shipping on orders over €40."),
        T("FreeShipping", "pl", "Darmowa dostawa przy zamówieniach powyżej 40 €."),
        T("FreeShipping", "de", "Kostenloser Versand ab 40 € Bestellwert."),
        T("CultureWay", "en", "Every text on this page in English, Polish and German. Edit one and switch the language."),
        T("CultureWay", "pl", "Każdy tekst tej strony po angielsku, polsku i niemiecku. Zmień jeden i przełącz język."),
        T("CultureWay", "de", "Jeder Text dieser Seite auf Englisch, Polnisch und Deutsch. Bearbeiten Sie einen und wechseln Sie die Sprache."),
        T("OpenPanel", "en", "Open the admin panel"),
        T("OpenPanel", "pl", "Otwórz panel"),
        T("OpenPanel", "de", "Admin-Panel öffnen"),
        T("Jobs", "en", "Run a job"),
        T("Jobs", "pl", "Uruchom zadanie"),
        T("Jobs", "de", "Job starten"),
        T("Newsletter", "en", "Send the newsletter"),
        T("Newsletter", "pl", "Wyślij newsletter"),
        T("Newsletter", "de", "Newsletter senden"),
        T("Reindex", "en", "Rebuild the search index"),
        T("Reindex", "pl", "Przebuduj indeks wyszukiwania"),
        T("Reindex", "de", "Suchindex neu aufbauen"),
        T("Scheduled", "en", "Scheduled. See it in the Scheduler."),
        T("Scheduled", "pl", "Zaplanowane. Zobacz w Harmonogramie."),
        T("Scheduled", "de", "Geplant. Siehe Scheduler."),
        T("Support", "en", "Questions? Write to {0}."),
        T("Support", "pl", "Pytania? Napisz na {0}."),
        T("Support", "de", "Fragen? Schreiben Sie an {0}."),
    ];

    private static Translation T(string key, string culture, string value) => new($"{nameof(HomePage)}.{key}", culture, value);
}
