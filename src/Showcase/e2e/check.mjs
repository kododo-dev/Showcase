// Browser scenarios against a running showcase (Production environment, fresh database):
//   node check.mjs http://localhost:5095 [chrome|msedge|firefox|webkit]
// Needs the playwright package (npm i --no-save playwright); firefox and webkit need `npx playwright install`.
import { chromium, firefox, webkit } from 'playwright';

const base = process.argv[2].replace(/\/$/, '');
const which = process.argv[3] ?? 'chrome';
const browser = which === 'firefox' ? await firefox.launch() : which === 'webkit' ? await webkit.launch() : await chromium.launch({ channel: which });

const results = [];
const record = (name, ok, detail = '') => results.push({ name, ok, detail });
const visible = (locator) => locator.first().waitFor({ timeout: 10000 }).then(() => true, () => false);
const hash = (page) => new URL(page.url()).hash;

async function open(contextOptions = {}) {
  // English and light theme to start with, whatever the machine's settings.
  const context = await browser.newContext({ locale: 'en-US', colorScheme: 'light', ...contextOptions });
  const page = await context.newPage();
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  page.on('console', (m) => { if (m.type() === 'error' && !m.text().startsWith('Failed to load resource')) errors.push(m.text()); });
  page.on('response', (r) => { if (r.status() >= 400 && !r.url().endsWith('/favicon.ico')) errors.push(`${r.status()} ${r.url()}`); });
  return { context, page, errors };
}

/** The textarea of the Way on screen holding `value`, or null. */
async function fieldWithValue(page, value) {
  await page.locator('main textarea').first().waitFor();
  const index = await page.locator('main textarea').evaluateAll((els, v) => els.findIndex((e) => e.value === v), value);
  return index >= 0 ? page.locator('main textarea').nth(index) : null;
}

async function save(page, api) {
  const [response] = await Promise.all([
    page.waitForResponse((r) => r.url().endsWith(api) && r.request().method() === 'POST'),
    page.getByRole('button', { name: 'Save' }).click(),
  ]);
  return response.status();
}

const fontOf = (page) => page.locator('main h1').first().evaluate((e) => getComputedStyle(e).fontFamily);
const shellMenu = (page) => page.locator('nav .MuiListItemText-primary').allTextContents();

/** Sets a field of a Way in the panel, found by its current value, and saves it. */
async function change(page, url, api, from, to) {
  const [response] = await Promise.all([page.waitForResponse((r) => r.url().endsWith(api.replace('Update', 'Get'))), page.goto(url)]);
  if (!response.ok()) return;
  const field = await fieldWithValue(page, from);
  if (!field) return;
  await field.fill(to);
  await Promise.all([page.waitForResponse((r) => r.url().endsWith(api)), page.getByRole('button', { name: /^(Save|Zapisz)$/ }).click()]);
}

const NAME = 'Acme Tea';
const TAGLINE = 'Loose-leaf tea, shipped fresh.';

{
  const { context, page, errors } = await open();
  try {
    await page.goto(`${base}/`);
    record('home page: name from ConfigWay', await visible(page.getByTestId('shop-name').filter({ hasText: NAME })));
    record('home page: text from CultureWay', await visible(page.getByTestId('tagline').filter({ hasText: 'Loose-leaf tea' })));
    await page.getByRole('link', { name: 'pl', exact: true }).click();
    record('home page in Polish', await visible(page.getByTestId('tagline').filter({ hasText: 'Herbata liściasta' })));
    await page.getByRole('link', { name: 'en', exact: true }).click();

    await page.getByRole('link', { name: 'Open the admin panel' }).click();
    record('panel opens the first Way', (await visible(page.getByRole('heading', { name: 'All Settings' }))) && hash(page) === '#/config/', page.url());
    record('panel lists the three Ways', (await shellMenu(page)).join(',') === 'Configuration,Translations,Scheduler', (await shellMenu(page)).join(','));
    record('title of the host', (await page.title()) === 'Kododo Ways', await page.title());
    const menu = page.getByTestId('way-menu');
    record("ConfigWay's sections in the shell menu", await visible(menu.getByText('Shop')));
    record("ConfigWay's monospace theme in the panel", /IBM Plex Mono/.test(await fontOf(page)), await fontOf(page));

    // ConfigWay → the home page and the jobs read the settings.
    await menu.getByText('Shop').click();
    await page.getByRole('heading', { name: 'Shop' }).waitFor();
    const name = await fieldWithValue(page, NAME);
    record('ConfigWay field in the panel', !!name);
    if (name) {
      await name.fill('Acme Tea & Co');
      record('ConfigWay saves from the panel', (await save(page, '/config/api/UpdateConfiguration')) < 300);
    }

    // CultureWay: the Way's menu changes with it, the state of the previous one does not linger.
    await page.locator('nav').getByText('Translations').click();
    record('switch to CultureWay', (await visible(page.getByRole('heading', { name: 'All' }))) && hash(page) === '#/translations/', page.url());
    record("ConfigWay's menu replaced by CultureWay's", (await menu.getByText('Shop').count()) === 0 && (await visible(menu.getByText('HomePage'))));
    await menu.getByText('HomePage').click();
    await page.getByRole('heading', { name: 'HomePage' }).waitFor();
    record('CultureWay namespace in the panel', hash(page) === '#/translations/ns/HomePage', page.url());
    const tagline = await fieldWithValue(page, TAGLINE);
    record('CultureWay translation in the panel', !!tagline);
    if (tagline) {
      await tagline.fill('Loose-leaf tea, shipped the same day.');
      record('CultureWay saves from the panel', (await save(page, '/translations/api/UpdateTranslations')) === 204);
    }

    // RunWay, then theme and language across the Ways.
    await page.locator('nav').getByText('Scheduler').click();
    record('switch to RunWay', (await visible(page.getByRole('heading', { name: 'Dashboard' }))) && hash(page) === '#/scheduler/', page.url());
    record("RunWay's menu in the shell menu", await visible(menu.getByText('Recurrences')));
    await menu.getByText('Recurrences').click();
    record('recurrence registered by the host', (await visible(page.getByText('cleanup'))) && hash(page) === '#/scheduler/recurrences', page.url());

    await page.getByRole('button', { name: 'Switch to dark theme' }).click();
    await page.getByRole('button', { name: 'Language' }).click();
    await page.getByRole('menuitem', { name: 'Polski' }).click();
    await page.locator('nav').getByText('Configuration').click();
    record('language carries over to the next Way', await visible(page.getByRole('heading', { name: 'Wszystkie ustawienia' })));
    const header = await page.locator('main header').first().evaluate((e) => getComputedStyle(e).backgroundColor);
    record('dark theme carries over to the next Way', header === 'rgb(36, 36, 36)', header);
    record('browser back returns to the previous Way', await page.goBack().then(() => visible(page.getByRole('heading', { name: /Cykliczne|Recurrences/ }))), page.url());

    // Back on the home page: what was saved in the panel is live.
    await page.goto(`${base}/?culture=en`);
    record('home page shows the name saved in ConfigWay', await visible(page.getByTestId('shop-name').filter({ hasText: 'Acme Tea & Co' })));
    record('home page shows the text saved in CultureWay', await visible(page.getByTestId('tagline').filter({ hasText: 'the same day' })));
    await page.getByRole('button', { name: 'Send the newsletter' }).click();
    record('home page schedules a RunWay job', await visible(page.getByRole('status').filter({ hasText: 'Scheduled' })));

    await page.goto(`${base}/admin/#/scheduler/`);
    record('deep link into a Way of the panel', await visible(page.getByRole('heading', { name: /Dashboard|Pulpit/ })));
    record('no errors', errors.length === 0, errors.join(' | '));

  } finally {
    // Put the demo back as it was, also after a failed step.
    await change(page, `${base}/admin/#/config/section/Shop`, '/config/api/UpdateConfiguration', 'Acme Tea & Co', NAME).catch(() => {});
    await change(page, `${base}/admin/#/translations/ns/HomePage`, '/translations/api/UpdateTranslations', 'Loose-leaf tea, shipped the same day.', TAGLINE).catch(() => {});
  }
  await context.close();
}

// Each Way also on its own page, with the host's shell settings and without the list of Ways.
{
  const { context, page, errors } = await open();
  for (const [path, heading] of [['config', 'All Settings'], ['translations', 'All'], ['scheduler', 'Dashboard']]) {
    await page.goto(`${base}/${path}/`);
    record(`${path} on its own page`, (await visible(page.getByRole('heading', { name: heading }))) && (await page.locator('nav').count()) === 0);
  }
  record('host link back to the shop', await visible(page.getByRole('link', { name: 'Shop' })));
  record('no errors (own pages)', errors.length === 0, errors.join(' | '));
  await context.close();
}

// Phone: the panel's menu slides in and closes after choosing a Way.
{
  const { context, page } = await open({ viewport: { width: 390, height: 800 } });
  await page.goto(`${base}/admin/`);
  await visible(page.getByRole('heading', { name: 'All Settings' }));
  await page.getByRole('button', { name: 'Open menu' }).click();
  await page.locator('nav').getByText('Scheduler').click();
  const closed = await page.locator('nav').waitFor({ state: 'hidden', timeout: 5000 }).then(() => true, () => false);
  record('phone: choosing a Way closes the menu', closed && (await visible(page.getByRole('heading', { name: 'Dashboard' }))), page.url());
  await context.close();
}

await browser.close();
const failed = results.filter((r) => !r.ok);
for (const r of results) console.log(`${r.ok ? 'ok  ' : 'FAIL'} ${r.name}${r.detail ? ` — ${r.detail}` : ''}`);
console.log(`\n${which} ${base}: ${results.length - failed.length}/${results.length} ok`);
process.exit(failed.length ? 1 : 0);
