using System.Net;
using Microsoft.Extensions.Localization;

namespace Kododo.Showcase;

/// <summary>The shop's home page: its name from ConfigWay, its texts from CultureWay, buttons that schedule RunWay jobs.</summary>
internal sealed class HomePage
{
    private record Tea(string Name, string Origin, string NoteKey, decimal Price, string Colour);

    private static readonly Tea[] Teas =
    [
        new("Sencha Kagoshima", "Japan", "Sencha", 14.50m, "#5f8f3e"),
        new("Darjeeling First Flush", "India", "Darjeeling", 18.00m, "#b8862f"),
        new("Earl Grey Classic", "Sri Lanka", "EarlGrey", 11.00m, "#7a4a2e"),
    ];

    public static string Render(string basePath, ShopOptions shop, IStringLocalizer<HomePage> text, string culture)
    {
        string T(string key, params object[] args) => WebUtility.HtmlEncode(text[key, args].Value);
        var name = WebUtility.HtmlEncode(shop.Name);
        var languages = string.Join("", new[] { "en", "pl", "de" }.Select(c =>
            c == culture ? $"""<strong aria-current="true">{c}</strong>""" : $"""<a href="?culture={c}">{c}</a>"""));
        string Price(decimal price) => culture == "en" ? $"€{price:0.00}" : $"{price.ToString("0.00").Replace('.', ',')} €";
        var teas = string.Concat(Teas.Select(t => $"""
                <article class="tea">
                  <div class="tea-art" style="--tea:{t.Colour}">{Leaf}</div>
                  <div class="tea-body">
                    <p class="tea-origin">{t.Origin}</p>
                    <h3>{t.Name}</h3>
                    <p class="tea-note">{T(t.NoteKey)}</p>
                    <p class="tea-price">{Price(t.Price)} <span>/ 100 g</span></p>
                  </div>
                </article>
            """));
        return $$"""
            <!doctype html>
            <html lang="{{culture}}">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <base href="{{WebUtility.HtmlEncode(basePath)}}">
              <title>{{name}}</title>
              <style>
                :root{--bg:#faf7f2;--card:#fff;--ink:#1f1d1a;--muted:#6b665e;--line:#e8e2d8;--accent:#3f6b3a;--accent-ink:#fff;--demo:#1f2a1e}
                *{box-sizing:border-box}
                html{-webkit-text-size-adjust:100%}
                body{margin:0;font:16px/1.6 system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;background:var(--bg);color:var(--ink)}
                a{color:inherit}
                .wrap{max-width:1080px;margin:0 auto;padding:0 20px}
                .serif{font-family:'Iowan Old Style','Palatino Linotype',Palatino,Georgia,serif}

                .demo-bar{background:var(--demo);color:#dfe8dc;font-size:.875rem}
                .demo-bar .wrap{display:flex;align-items:center;justify-content:space-between;gap:16px;padding-top:10px;padding-bottom:10px}
                .demo-bar p{margin:0}
                .demo-bar .badge{display:inline-block;font-size:.7rem;font-weight:700;letter-spacing:.08em;text-transform:uppercase;background:#c9e3b4;color:var(--demo);border-radius:4px;padding:2px 6px;margin-right:8px}
                .panel{display:inline-flex;align-items:center;gap:6px;white-space:nowrap;background:#c9e3b4;color:var(--demo);font-weight:600;text-decoration:none;padding:7px 14px;border-radius:999px}
                .panel:hover{background:#fff}

                header.site{border-bottom:1px solid var(--line);background:var(--bg)}
                header.site .wrap{display:flex;align-items:center;justify-content:space-between;height:72px}
                .brand{display:flex;align-items:center;gap:10px;font-size:1.5rem;font-weight:600;margin:0;letter-spacing:-.01em}
                .brand svg{width:28px;height:28px;color:var(--accent)}
                .languages{display:flex;gap:4px;font-size:.8rem;text-transform:uppercase;letter-spacing:.05em}
                .languages a,.languages strong{padding:4px 8px;border-radius:6px;text-decoration:none}
                .languages a{color:var(--muted)}
                .languages a:hover{background:var(--line);color:var(--ink)}
                .languages strong{background:var(--ink);color:var(--bg)}

                .hero{padding:72px 0 56px}
                .hero h2{font-size:clamp(2.2rem,5vw,3.6rem);line-height:1.1;font-weight:500;margin:0 0 16px;max-width:14em;letter-spacing:-.02em}
                .hero p{color:var(--muted);font-size:1.125rem;max-width:36em;margin:0}

                h2.section{font-size:1.75rem;font-weight:500;margin:0 0 24px;letter-spacing:-.01em}
                .teas{display:grid;grid-template-columns:repeat(3,1fr);gap:20px;margin-bottom:80px}
                .tea{background:var(--card);border:1px solid var(--line);border-radius:14px;overflow:hidden}
                .tea-art{height:150px;display:grid;place-items:center;background:color-mix(in srgb,var(--tea) 14%,#fff)}
                .tea-art svg{width:64px;height:64px;color:var(--tea)}
                .tea-body{padding:18px 20px 20px}
                .tea-origin{margin:0;font-size:.75rem;text-transform:uppercase;letter-spacing:.08em;color:var(--muted)}
                .tea h3{margin:2px 0 6px;font-size:1.125rem}
                .tea-note{margin:0 0 14px;color:var(--muted);font-size:.95rem}
                .tea-price{margin:0;font-weight:600}
                .tea-price span{font-weight:400;color:var(--muted);font-size:.875rem}

                .behind{background:#f0ebe2;border-top:1px solid var(--line);border-bottom:1px solid var(--line);padding:64px 0}
                .behind .lead{color:var(--muted);margin:-12px 0 28px;max-width:40em}
                .ways{display:grid;grid-template-columns:repeat(3,1fr);gap:20px}
                .way{background:var(--card);border:1px solid var(--line);border-radius:14px;padding:22px;display:flex;flex-direction:column}
                .way h3{margin:0 0 4px;font-size:1rem}
                .way .pkg{font:.75rem ui-monospace,'Cascadia Mono',Consolas,monospace;color:var(--muted);margin:0 0 10px}
                .way p.desc{margin:0 0 16px;color:var(--muted);font-size:.95rem;flex:1}
                .way a.open{font-weight:600;color:var(--accent);text-decoration:none}
                .way a.open:hover{text-decoration:underline}
                .jobs{display:flex;flex-wrap:wrap;gap:8px;margin-bottom:12px}
                button{font:inherit;font-size:.875rem;padding:7px 12px;border-radius:8px;border:1px solid var(--line);background:var(--bg);color:var(--ink);cursor:pointer}
                button:hover{border-color:var(--accent);color:var(--accent)}
                #status{min-height:1.5em;margin:0 0 8px;color:var(--accent);font-size:.875rem;font-weight:600}

                footer.site{padding:32px 0 48px;color:var(--muted);font-size:.875rem}
                footer.site .wrap{display:flex;justify-content:space-between;gap:16px;flex-wrap:wrap}
                footer.site p{margin:0}

                @media (max-width:820px){.teas,.ways{grid-template-columns:1fr}.hero{padding:48px 0 40px} }
                @media (max-width:560px){.demo-bar .wrap{flex-direction:column;align-items:flex-start} }
              </style>
            </head>
            <body>
              <div class="demo-bar">
                <div class="wrap">
                  <p><span class="badge">Demo</span>{{T("Intro")}}</p>
                  <a class="panel" href="admin/">{{T("OpenPanel")}} →</a>
                </div>
              </div>

              <header class="site">
                <div class="wrap">
                  <h1 class="brand serif">{{Leaf}}<span data-testid="shop-name">{{name}}</span></h1>
                  <nav class="languages" aria-label="Language">{{languages}}</nav>
                </div>
              </header>

              <main>
                <section class="hero">
                  <div class="wrap">
                    <h2 class="serif" data-testid="tagline">{{T("Tagline")}}</h2>
                    <p>{{T("HeroText")}}</p>
                  </div>
                </section>

                <section class="wrap" aria-labelledby="collection">
                  <h2 class="section serif" id="collection">{{T("Collection")}}</h2>
                  <div class="teas">
            {{teas}}
                  </div>
                </section>

                <section class="behind" aria-labelledby="behind">
                  <div class="wrap">
                    <h2 class="section serif" id="behind">{{T("Behind")}}</h2>
                    <p class="lead">{{T("BehindText")}}</p>
                    <div class="ways">
                      <div class="way">
                        <h3>Configuration</h3>
                        <p class="pkg">Kododo.ConfigWay</p>
                        <p class="desc">{{T("ConfigWay")}}</p>
                        <a class="open" href="config/">Configuration →</a>
                      </div>
                      <div class="way">
                        <h3>Translations</h3>
                        <p class="pkg">Kododo.CultureWay</p>
                        <p class="desc">{{T("CultureWay")}}</p>
                        <a class="open" href="translations/">Translations →</a>
                      </div>
                      <div class="way">
                        <h3>{{T("Jobs")}}</h3>
                        <p class="pkg">Kododo.RunWay</p>
                        <div class="jobs">
                          <button data-job="newsletter">{{T("Newsletter")}}</button>
                          <button data-job="reindex">{{T("Reindex")}}</button>
                        </div>
                        <p id="status" role="status"></p>
                        <a class="open" href="scheduler/">Scheduler →</a>
                      </div>
                    </div>
                  </div>
                </section>
              </main>

              <footer class="site">
                <div class="wrap">
                  <p data-testid="support">{{T("Support", shop.SupportEmail)}}</p>
                  <p>Built with <a href="https://github.com/kododo-dev">Kododo Ways</a></p>
                </div>
              </footer>

              <script>
                for (const button of document.querySelectorAll('button[data-job]'))
                  button.addEventListener('click', async () => {
                    const response = await fetch('jobs/' + button.dataset.job, { method: 'POST' });
                    document.getElementById('status').textContent = response.ok ? {{System.Text.Json.JsonSerializer.Serialize(text["Scheduled"].Value)}} : response.statusText;
                  });
              </script>
            </body>
            </html>
            """;
    }

    private const string Leaf = """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M11 20A7 7 0 0 1 9.8 6.1C15.5 5 17 4.48 19 2c1 2 2 4.18 2 8 0 5.5-4.78 10-10 10Z"/><path d="M2 21c0-3 1.85-5.36 5.08-6C9.5 14.52 12 13 13 12"/></svg>""";
}
