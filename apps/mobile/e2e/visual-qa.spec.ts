import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page, type TestInfo } from '@playwright/test';

const categories = [
  { id: 'hair', name: 'Hair & braids' },
  { id: 'nails', name: 'Nails' },
  { id: 'makeup', name: 'Makeup' },
];

const businesses = {
  page: 1,
  pageSize: 20,
  totalCount: 2,
  items: [
    {
      id: '11111111-aaaa-aaaa-aaaa-111111111111',
      tradingName: 'Luna Glow Studio',
      description: 'Braids, natural hair and protective styling.',
      city: 'Gqeberha',
      suburb: 'Walmer',
      province: 'Eastern Cape',
      fromPrice: 280,
      averageRating: 4.8,
      reviewCount: 42,
      categories: ['Hair & braids'],
      portfolioImageUrl: 'https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?auto=format&fit=crop&w=900&q=80',
    },
    {
      id: '22222222-aaaa-aaaa-aaaa-222222222222',
      tradingName: 'The Nail Garden',
      description: 'Modern manicures and detailed nail art.',
      city: 'Gqeberha',
      suburb: 'Summerstrand',
      province: 'Eastern Cape',
      fromPrice: 190,
      averageRating: 4.6,
      reviewCount: 27,
      categories: ['Nails'],
      portfolioImageUrl: 'https://images.unsplash.com/photo-1604654894610-df63bc536371?auto=format&fit=crop&w=900&q=80',
    },
  ],
};

async function mockDiscovery(page: Page, delay = 0) {
  await page.route('**/api/v1/categories', (route) => route.fulfill({ json: categories }));
  await page.route('**/api/v1/businesses?**', async (route) => {
    if (delay) await new Promise((resolve) => setTimeout(resolve, delay));
    await route.fulfill({ json: businesses });
  });
  await page.route('https://images.unsplash.com/**', (route) => route.abort());
}

async function attachPageScreenshot(page: Page, testInfo: TestInfo, name: string) {
  await testInfo.attach(name, {
    body: await page.screenshot({ fullPage: true, animations: 'disabled' }),
    contentType: 'image/png',
  });
}

test('@visual renders deterministic discovery at the configured viewport', async ({ page }, testInfo) => {
  await mockDiscovery(page);
  await page.goto('/home');
  await expect(page.getByRole('heading', { name: 'Find your glow.' })).toBeVisible();
  await expect(page.getByText('Luna Glow Studio')).toBeVisible();
  await expect(page.locator('body')).not.toHaveCSS('overflow-x', 'scroll');

  const viewport = page.viewportSize();
  expect(viewport?.width).toBeGreaterThanOrEqual(390);
  await attachPageScreenshot(page, testInfo, `discovery-${testInfo.project.name.toLowerCase().replaceAll(' ', '-')}`);
});

test('@visual honours reduced motion without changing content', async ({ page }, testInfo) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await mockDiscovery(page);
  await page.goto('/home');

  await expect(page.locator('.chip-row--discover .chip-track')).toHaveCSS('animation-name', 'none');
  await expect(page.getByText('Luna Glow Studio')).toBeVisible();
  await attachPageScreenshot(
    page,
    testInfo,
    `reduced-motion-${testInfo.project.name.toLowerCase().replaceAll(' ', '-')}`,
  );
});

test('@visual supports keyboard and screen-reader navigation', async ({ page }) => {
  await mockDiscovery(page);
  await page.goto('/home');

  const focused = page.getByRole('navigation', { name: 'Main navigation' }).getByRole('link', { name: 'Discover' });
  await focused.focus();
  await expect(focused).toBeVisible();
  await expect.poll(() => focused.evaluate((element) => getComputedStyle(element).outlineStyle)).not.toBe('none');

  const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  expect(results.violations.filter((item) => ['serious', 'critical'].includes(item.impact ?? ''))).toEqual([]);
  await expect(page.getByRole('navigation', { name: 'Main navigation' })).toBeVisible();
  await expect(page.locator('main')).toBeVisible();
});

test('@visual remains usable while a slow response is loading', async ({ page }) => {
  await mockDiscovery(page, 900);
  await page.goto('/home');

  await expect(page.locator('.skeleton-card').first()).toBeVisible();
  await expect(page.getByRole('navigation', { name: 'Main navigation' })).toBeVisible();
  await expect(page.getByText('Luna Glow Studio')).toBeVisible({ timeout: 5_000 });
  await expect(page.locator('.skeleton-card')).toHaveCount(0);
});
