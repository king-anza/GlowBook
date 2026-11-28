import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

test('has no serious accessibility violations on public entry screens', async ({ page }) => {
  await page.route('**/api/v1/categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/businesses?**', (route) =>
    route.fulfill({ json: { page: 1, pageSize: 20, totalCount: 0, items: [] } }),
  );
  await page.goto('/home');
  const discovery = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  expect(discovery.violations.filter((item) => ['serious', 'critical'].includes(item.impact ?? ''))).toEqual([]);

  await page.goto('/auth');
  const authentication = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  expect(authentication.violations.filter((item) => ['serious', 'critical'].includes(item.impact ?? ''))).toEqual([]);
});

test('shows the discovery page', async ({ page }) => {
  await page.route('**/api/v1/categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/businesses?**', (route) =>
    route.fulfill({ json: { page: 1, pageSize: 20, totalCount: 0, items: [] } }),
  );
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Find your glow.' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Find a pro' })).toBeVisible();
});

test('offers an in-context retry after discovery fails', async ({ page }) => {
  let attempts = 0;
  await page.route('**/api/v1/categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/businesses?**', (route) => {
    attempts += 1;
    if (attempts <= 2) return route.fulfill({ status: 503, json: { title: 'Unavailable' } });
    return route.fulfill({ json: { page: 1, pageSize: 20, totalCount: 0, items: [] } });
  });

  await page.goto('/home');
  const alert = page.getByRole('alert');
  await expect(alert).toContainText('We could not load professionals');
  await alert.getByRole('button', { name: 'Try again' }).click();
  await expect(page.getByRole('heading', { name: 'No professionals match this search' })).toBeVisible();
});

test('counts, clears and persists discovery filters in the URL', async ({ page }) => {
  await page.route('**/api/v1/categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/businesses?**', (route) =>
    route.fulfill({ json: { page: 1, pageSize: 20, totalCount: 0, items: [] } }),
  );
  await page.goto('/home');
  await page.getByLabel('Service or style').fill('braids');
  await page.getByLabel('Area').fill('Gqeberha');
  await page.getByRole('button', { name: 'Find a pro' }).click();
  await expect(page).toHaveURL(/search=braids/);
  await expect(page).toHaveURL(/city=Gqeberha/);
  await expect(page.getByLabel('2 active filters')).toBeVisible();
  await page.getByRole('button', { name: 'Clear all' }).first().click();
  await expect(page).not.toHaveURL(/search=/);
  await expect(page.getByLabel(/active filters/)).toHaveCount(0);
});

test('keeps form controls readable when the browser prefers dark mode', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'dark' });
  await page.goto('/auth');
  await expect(page.getByLabel('Email')).toBeVisible();
  await expect
    .poll(() => page.locator('html').evaluate((element) => getComputedStyle(element).colorScheme))
    .toBe('light');
  await expect
    .poll(() => page.getByLabel('Email').evaluate((element) => getComputedStyle(element).color))
    .toBe('rgb(39, 33, 62)');
});

test('focuses invalid forms and labels required and optional fields', async ({ page }) => {
  await page.goto('/auth');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page.locator('.field-error').first()).toHaveText('This field is required.');
  await expect(page.getByText('Required').first()).toBeVisible();
  await expect(page.getByRole('alert')).toContainText('complete email address');
});

test('provides searchable contextual help and accessible support routes', async ({ page }) => {
  await page.goto('/help?topic=spaces');
  await expect(page.getByRole('heading', { name: 'How can we help?' })).toBeVisible();
  await expect(page.getByText('Use Glow Spaces safely')).toBeVisible();
  await page.getByLabel('Search help').fill('never queued');
  await expect(page.getByText('Offline and slow-connection behavior')).toBeVisible();
  await expect(page.getByRole('link', { name: 'Email support' })).toHaveAttribute(
    'href',
    /mailto:support@glowbylunamay\.co\.za/,
  );
});

test('shows cached booking details offline without replaying actions', async ({ page, context }) => {
  const booking = {
    id: 'cached-booking',
    businessName: 'Glow Studio',
    serviceName: 'Silk press',
    staffName: 'Lerato',
    startUtc: '2026-12-01T08:00:00Z',
    status: 'Confirmed',
    concurrencyToken: 'v1',
  };
  await page.route('**/api/v1/bookings', (route) => route.fulfill({ json: [booking] }));
  await page.route('**/api/v1/reviews', (route) => route.fulfill({ json: [] }));
  await page.goto('/bookings');
  await expect(page.getByText('Silk press')).toBeVisible();
  await page.getByRole('link', { name: 'Discover' }).click();
  await page.unroute('**/api/v1/bookings');
  await context.setOffline(true);
  try {
    await page.getByRole('link', { name: 'My bookings' }).click();
    await expect(page.getByText('Silk press')).toBeVisible();
    await expect(page.locator('.network-banner')).toContainText(/offline|cached/i);
  } finally {
    await context.setOffline(false);
  }
});

test('keeps the requested Discover, bookings, Studio and Account shell on mobile and desktop', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/home');
  const nav = page.getByRole('navigation', { name: 'Main navigation' });
  await expect(nav.getByRole('link', { name: 'Discover' })).toHaveAttribute('href', '/home');
  await expect(nav.getByRole('link', { name: 'My bookings' })).toHaveAttribute('href', '/bookings');
  await expect(nav.getByRole('link', { name: 'Studio' })).toHaveAttribute('href', '/professional/dashboard');
  await expect(page.getByRole('link', { name: 'Sign in or create account' })).toHaveAttribute('href', '/auth');
  await expect(nav.getByRole('link', { name: /pricing/i })).toHaveCount(0);
  await page.setViewportSize({ width: 1280, height: 900 });
  await expect(nav.getByRole('link', { name: 'Studio' })).toBeVisible();
});

test('guides professionals through nine persisted setup steps', async ({ page }) => {
  await page.route('**/api/v1/professional/onboarding', (route) =>
    route.fulfill({
      json: {
        accountVerified: true,
        businessComplete: true,
        locationComplete: true,
        portfolioComplete: false,
        staffComplete: false,
        servicesComplete: false,
        pricingComplete: false,
        availabilityComplete: false,
        approvalSubmitted: false,
        approvalStatus: 'Draft',
        reviewReason: null,
      },
    }),
  );
  await page.goto('/professional/onboarding');
  await expect(page.getByRole('heading', { name: 'Let’s get your studio ready to glow.' })).toBeVisible();
  await expect(page.locator('.onboarding__steps > li')).toHaveCount(9);
  await expect(page.getByRole('progressbar', { name: 'Studio setup progress' })).toHaveAttribute('aria-valuenow', '33');
  await expect(page.getByRole('button', { name: 'Submit for review' })).toBeDisabled();
});

test('shows the professional daily dashboard without inventing activity', async ({ page }) => {
  await page.route('**/api/v1/professional/dashboard', (route) =>
    route.fulfill({
      json: {
        approvalStatus: 'Draft',
        reviewReason: null,
        setupCompletedCount: 3,
        setupTotalCount: 9,
        todayBookingCount: 0,
        pendingBookingCount: 0,
        activeStaffCount: 1,
        staffScheduledTodayCount: 0,
        publishedSpaceCount: 0,
        pendingSpaceApplicationCount: 0,
        todayBookings: [],
      },
    }),
  );
  await page.goto('/professional/dashboard');
  await expect(page.getByRole('heading', { name: 'Good to see you.' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'No appointments today' })).toBeVisible();
  await expect(page.getByRole('link', { name: /Studio setup/ })).toContainText('33%');
});

test('uses responsive studio navigation and shows calendar capacity and private insights', async ({ page }) => {
  await page.route('**/api/v1/professional/business', (route) =>
    route.fulfill({
      json: {
        staff: [
          {
            id: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
            displayName: 'Lerato',
            biography: 'Stylist',
            workspaceName: 'Chair 1',
            acceptingBookings: true,
          },
        ],
      },
    }),
  );
  await page.route('**/api/v1/professional/calendar?**', (route) =>
    route.fulfill({
      json: {
        from: '2026-11-26',
        to: '2026-12-02',
        timeZone: 'Africa/Johannesburg',
        staff: [],
        events: [],
        days: [
          {
            date: '2026-11-26',
            scheduledMinutes: 480,
            bookedMinutes: 120,
            blockedMinutes: 30,
            utilisationPercent: 25,
            staffedWorkspaceCount: 1,
            conflicts: [],
          },
        ],
      },
    }),
  );
  await page.route('**/api/v1/professional/insights?**', (route) =>
    route.fulfill({
      json: {
        periodDays: 30,
        from: '2026-10-28',
        to: '2026-11-26',
        bookingVolume: 4,
        completedCount: 2,
        cancellationCount: 1,
        noShowCount: 0,
        repeatCustomerCount: null,
        customerPrivacyThresholdMet: false,
        capacityMinutes: 480,
        bookedMinutes: 120,
        chairUtilisationPercent: 25,
        daily: [],
      },
    }),
  );
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/professional/calendar');
  await expect(page.getByRole('heading', { name: 'Calendar and capacity.' })).toBeVisible();
  await expect(page.getByText('120 booked · 480 scheduled · 30 blocked minutes')).toBeVisible();
  const menu = page.getByRole('button', { name: /Studio workspace/ });
  await expect(menu).toHaveAttribute('aria-expanded', 'false');
  await menu.click();
  await expect(menu).toHaveAttribute('aria-expanded', 'true');
  await page
    .getByRole('navigation', { name: 'Professional workspace' })
    .getByRole('link', { name: 'Insights' })
    .click();
  await expect(page.getByRole('heading', { name: 'Understand the rhythm of your studio.' })).toBeVisible();
  await expect(page.getByText('Shown once at least 3 customers')).toBeVisible();
});

test('opens an unread notification at the relevant booking record', async ({ page }) => {
  const payload = Buffer.from(JSON.stringify({ role: 'Customer' })).toString('base64url');
  await page.route('**/api/v1/auth/login', (route) =>
    route.fulfill({
      json: {
        accessToken: `header.${payload}.signature`,
        refreshToken: 'notification-refresh',
        accessTokenExpiresAtUtc: '2026-11-27T08:00:00Z',
      },
    }),
  );
  await page.route('**/api/v1/notifications', (route) =>
    route.fulfill({
      json: {
        unreadCount: 1,
        items: [
          {
            id: 'note-1',
            eventType: 'BookingConfirmed',
            title: 'Booking confirmed',
            message: 'Your booking was confirmed.',
            actionUrl: '/bookings?booking=booking-1',
            createdAtUtc: '2026-11-26T08:00:00Z',
            readAtUtc: null,
          },
        ],
      },
    }),
  );
  await page.route('**/api/v1/notifications/preferences', (route) =>
    route.fulfill({
      json: {
        bookingPush: true,
        applicationPush: true,
        accountPush: true,
        reminderPush: true,
        emailFallback: true,
        pushAvailable: false,
      },
    }),
  );
  await page.route('**/api/v1/notifications/note-1/read', (route) => route.fulfill({ status: 204 }));
  await page.route('**/api/v1/bookings', (route) =>
    route.fulfill({
      json: [
        {
          id: 'booking-1',
          businessName: 'Glow Studio',
          serviceName: 'Silk press',
          staffName: 'Lerato',
          startUtc: '2026-11-27T08:00:00Z',
          status: 'Confirmed',
          concurrencyToken: 'v1',
        },
      ],
    }),
  );
  await page.route('**/api/v1/reviews', (route) => route.fulfill({ json: [] }));

  await page.goto('/auth');
  await page.getByLabel('Email').fill('customer@example.test');
  await page.getByLabel('Password').fill('ValidPass!1');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.getByRole('link', { name: 'Notifications' }).click();
  await expect(page.getByRole('heading', { name: 'Notification centre' })).toBeVisible();
  await page.getByRole('button', { name: /Booking confirmed/ }).click();
  await expect(page).toHaveURL(/bookings\?booking=booking-1/);
  await expect(page.locator('.notification-target')).toContainText('Silk press');
});

test('shows enriched Glow Spaces trust, urgency, distance and gallery details', async ({ page }) => {
  await page.route('**/api/v1/spaces', (route) =>
    route.fulfill({
      json: [
        {
          id: 'space-1',
          businessId: 'business-1',
          businessName: 'Glow Studio',
          city: 'Gqeberha',
          province: 'Eastern Cape',
          workspaceType: 'StylingChair',
          opportunityType: 'ChairRental',
          status: 'Published',
          title: 'Sunny styling chair',
          description: 'A welcoming chair for an independent stylist.',
          requiredSkills: 'Hair styling',
          includedAmenities: 'Wi-Fi',
          houseRules: 'Keep shared areas tidy',
          spacesAvailable: 1,
          rateAmount: 300,
          ratePeriod: 'Day',
          startDate: '2026-11-28',
          endDate: '2026-12-20',
          recurring: false,
          bringOwnClients: true,
          approvalRequired: true,
          imageUrl: null,
          imageUrls: ['assets/glowbook/braids.webp', 'assets/glowbook/nails.webp'],
          applicationCount: 0,
          createdAtUtc: '2026-11-26T08:00:00Z',
          isVerifiedParty: true,
          isDemo: false,
          isExpired: false,
          isUrgent: true,
          distanceKm: 2.4,
          isSaved: false,
        },
      ],
    }),
  );
  await page.goto('/spaces');
  await expect(page.getByRole('heading', { name: 'Sunny styling chair' })).toBeVisible();
  await expect(page.getByText('Approved business')).toBeVisible();
  await expect(page.getByText('Starting or closing soon')).toBeVisible();
  await expect(page.getByText('Approximately 2.4 km away')).toBeVisible();
  await expect(page.getByLabel('Opportunity photos').locator('img')).toHaveCount(1);
});

test('saves a reusable profile and explains professional space matches', async ({ page }) => {
  const payload = Buffer.from(JSON.stringify({ role: 'Professional' })).toString('base64url');
  await page.route('**/api/v1/auth/login', (route) =>
    route.fulfill({
      json: {
        accessToken: `header.${payload}.signature`,
        refreshToken: 'refresh',
        accessTokenExpiresAtUtc: '2026-11-27T08:00:00Z',
      },
    }),
  );
  let profile: object | null = null;
  await page.route('**/api/v1/professional/space-profile', async (route) => {
    if (route.request().method() === 'PUT') {
      profile = { ...(await route.request().postDataJSON()), updatedAtUtc: '2026-11-26T08:00:00Z' };
      return route.fulfill({ json: profile });
    }
    return route.fulfill({ json: profile });
  });
  await page.route('**/api/v1/professional/space-applications/mine', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/spaces', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/professional/spaces/matches', (route) =>
    route.fulfill({
      json: [
        {
          opportunity: {
            id: 'match-1',
            businessId: 'business-1',
            businessName: 'Glow Studio',
            city: 'Gqeberha',
            province: 'Eastern Cape',
            workspaceType: 'StylingChair',
            opportunityType: 'ChairRental',
            status: 'Published',
            title: 'Matched styling chair',
            description: 'A chair that fits your profile.',
            requiredSkills: 'Braiding',
            includedAmenities: '',
            houseRules: '',
            spacesAvailable: 1,
            rateAmount: 300,
            ratePeriod: 'Day',
            startDate: '2026-12-01',
            endDate: null,
            recurring: false,
            bringOwnClients: true,
            approvalRequired: true,
            imageUrl: null,
            imageUrls: [],
            applicationCount: 0,
            createdAtUtc: '2026-11-26T08:00:00Z',
            isVerifiedParty: true,
            isDemo: false,
            isExpired: false,
            isUrgent: true,
            distanceKm: null,
            isSaved: false,
          },
          matchScore: 85,
          matchReasons: ['1 requested skill match', 'In your preferred city, Gqeberha'],
        },
      ],
    }),
  );

  await page.goto('/auth');
  await page.getByLabel('Email').fill('professional@example.test');
  await page.getByLabel('Password').fill('ValidPass!1');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.waitForURL(/\/home/);
  await page.getByRole('link', { name: 'Spaces' }).click();
  await page.getByLabel('Professional summary').fill('Independent stylist with five years of experience.');
  await page.getByLabel('Skills (separate with commas)').fill('Braiding, hair styling');
  await page.getByLabel('Availability').fill('Weekdays');
  await page.getByLabel('Preferred cities (comma-separated)').fill('Gqeberha');
  await page.getByRole('button', { name: 'Save application profile' }).click();
  await expect(page.getByText('Contact details stay private until an application is accepted.')).toBeVisible();
  await page.getByRole('button', { name: 'Matches for me' }).click();
  await expect(page.getByRole('heading', { name: 'Matched styling chair' })).toBeVisible();
  await expect(page.getByText('85% profile match')).toBeVisible();
  await expect(page.getByText('1 requested skill match')).toBeVisible();
});

test('uses a bottom navigation on mobile and restores it to the desktop header', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/home');
  const nav = page.getByRole('navigation', { name: 'Main navigation' });
  const mobileBox = await nav.boundingBox();
  expect(mobileBox?.y).toBeGreaterThan(700);
  await expect(nav.getByRole('link', { name: 'Discover' })).toHaveAttribute('aria-current', 'page');

  await page.setViewportSize({ width: 1280, height: 900 });
  const desktopBox = await nav.boundingBox();
  expect(desktopBox?.y).toBeLessThan(80);
});

test('preserves the previous page during browser back navigation', async ({ page }) => {
  await page.route('**/api/v1/categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/businesses?**', (route) =>
    route.fulfill({ json: { page: 1, pageSize: 20, totalCount: 0, items: [] } }),
  );

  await page.goto('/home');
  await page.getByRole('link', { name: 'Sign in or create account' }).click();
  await expect(page).toHaveURL(/\/auth$/);
  await page.goBack();
  await expect(page).toHaveURL(/\/home$/);
  await expect(page.getByRole('heading', { name: 'Find your glow.' })).toBeVisible();
});

test('keeps routed mobile content below the shared navigation bar', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.route('**/api/v1/categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/businesses?**', (route) =>
    route.fulfill({ json: { page: 1, pageSize: 20, totalCount: 0, items: [] } }),
  );

  await page.goto('/home');
  const topBarBottom = await page
    .locator('.prototype-topbar')
    .evaluate((element) => element.getBoundingClientRect().bottom);
  const pageTop = await page.locator('app-discovery').evaluate((element) => element.getBoundingClientRect().top);
  expect(pageTop).toBeGreaterThanOrEqual(topBarBottom);
  await expect(page.getByRole('heading', { name: 'Find your glow.' })).toBeVisible();
});

test('provides accessible touch targets and keyboard focus', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.route('**/api/v1/categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/businesses?**', (route) =>
    route.fulfill({ json: { page: 1, pageSize: 20, totalCount: 0, items: [] } }),
  );

  await page.goto('/home');
  const discoverLink = page.getByRole('navigation', { name: 'Main navigation' }).getByRole('link', {
    name: 'Discover',
  });
  const filterButton = page.getByRole('button', { name: 'Filters', exact: true });

  for (const control of [discoverLink, filterButton]) {
    const box = await control.boundingBox();
    expect(box?.width).toBeGreaterThanOrEqual(44);
    expect(box?.height).toBeGreaterThanOrEqual(44);
  }

  await discoverLink.focus();
  await expect.poll(() => discoverLink.evaluate((element) => getComputedStyle(element).outlineStyle)).toBe('solid');

  await filterButton.click();
  await expect(filterButton).toHaveAttribute('aria-expanded', 'true');
  await expect.poll(() => filterButton.evaluate((element) => getComputedStyle(element).boxShadow)).not.toBe('none');
});

test('continuously cycles off-screen category filters on mobile', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.route('**/api/v1/categories', (route) =>
    route.fulfill({
      json: [
        { id: 'hair', name: 'Hair & braids' },
        { id: 'nails', name: 'Nails' },
        { id: 'makeup', name: 'Makeup' },
        { id: 'lashes', name: 'Lashes & brows' },
      ],
    }),
  );
  await page.route('**/api/v1/businesses?**', (route) =>
    route.fulfill({ json: { page: 1, pageSize: 20, totalCount: 0, items: [] } }),
  );

  await page.goto('/home');
  const categories = page.locator('.chip-row--discover');
  await expect(categories).toBeVisible();
  await expect(categories.locator('.chip-set')).toHaveCount(2);
  await expect(categories.locator('.chip-track')).toHaveCSS('animation-name', 'category-marquee');

  const hairFilter = page.getByRole('button', { name: 'Hair & braids' });
  await hairFilter.focus();
  await expect(categories.locator('.chip-track')).toHaveCSS('animation-play-state', 'paused');
  await hairFilter.click();
  await expect(hairFilter).toHaveAttribute('aria-pressed', 'true');
  await expect.poll(() => hairFilter.evaluate((element) => getComputedStyle(element).boxShadow)).not.toBe('none');
});

test('disables non-essential motion when reduced motion is requested', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.route('**/api/v1/categories', (route) => route.fulfill({ json: [{ id: 'hair', name: 'Hair & braids' }] }));
  await page.route('**/api/v1/businesses?**', (route) =>
    route.fulfill({ json: { page: 1, pageSize: 20, totalCount: 0, items: [] } }),
  );

  await page.goto('/home');
  await expect(page.locator('.chip-row--discover .chip-track')).toHaveCSS('animation-name', 'none');
  await expect
    .poll(() =>
      page
        .locator('html')
        .evaluate((element) => getComputedStyle(element).getPropertyValue('--glow-motion-standard').trim()),
    )
    .toBe('0.01ms');
});

test('registers and requests account recovery', async ({ page }) => {
  await page.route('**/api/v1/auth/register/customer', (route) => route.fulfill({ status: 202 }));
  await page.route('**/api/v1/auth/forgot-password', (route) => route.fulfill({ status: 202 }));
  await page.goto('/auth');
  await page.locator('ion-segment-button[value="register"]').click();
  await page.getByLabel('Display name').fill('Customer Test');
  await page.getByLabel('Email').fill('customer@example.test');
  await page.getByLabel('Password').fill('ValidPass!1');
  await page.getByText('I accept the terms and privacy notice').click();
  await page.getByText('I confirm that I am at least 18').click();
  await page.getByRole('button', { name: 'Create customer account' }).click();
  await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();
  await page.locator('ion-button', { hasText: 'Verify email or reset password' }).click();
  const recoveryPage = page.locator('app-recovery');
  await recoveryPage.getByLabel('Email', { exact: true }).fill('customer@example.test');
  await recoveryPage.getByRole('button', { name: 'Request password reset' }).click();
  await expect(
    recoveryPage.getByText('If the account exists, password-reset instructions have been sent.'),
  ).toBeVisible();
});

test('registers a clearly selected beauty professional account', async ({ page }) => {
  let professionalRegistration = false;
  await page.route('**/api/v1/auth/register/professional', (route) => {
    professionalRegistration = true;
    return route.fulfill({ status: 202 });
  });
  await page.goto('/auth');
  await page.locator('ion-segment-button[value="register"]').click();
  const professional = page.getByRole('radio', { name: /Beauty professional/ });
  await professional.click();
  await expect(professional).toHaveAttribute('aria-checked', 'true');
  await page.getByLabel('Display name').fill('Glow Professional');
  await page.getByLabel('Email').fill('professional@example.test');
  await page.getByLabel('Password').fill('ValidPass!1');
  await page.getByText('I accept the terms and privacy notice').click();
  await page.getByText('I confirm that I am at least 18').click();
  await page.getByRole('button', { name: 'Create professional account' }).click();
  await expect.poll(() => professionalRegistration).toBe(true);
});

test('signs in, discovers a service, and books a slot', async ({ page }) => {
  const businessId = '11111111-aaaa-aaaa-aaaa-111111111111';
  const staffId = '22222222-aaaa-aaaa-aaaa-222222222222';
  const offeringId = '33333333-aaaa-aaaa-aaaa-333333333333';
  const appointmentStart = new Date();
  appointmentStart.setDate(appointmentStart.getDate() + 1);
  appointmentStart.setHours(9, 0, 0, 0);
  const appointmentEnd = new Date(appointmentStart);
  appointmentEnd.setHours(10, 0, 0, 0);
  const startUtc = appointmentStart.toISOString();

  await page.route('**/api/v1/auth/login', (route) =>
    route.fulfill({
      json: {
        accessToken: 'test-token',
        refreshToken: 'test-refresh',
        accessTokenExpiresAtUtc: '2026-11-09T08:00:00Z',
      },
    }),
  );
  await page.route('**/api/v1/businesses?**', (route) =>
    route.fulfill({
      json: {
        page: 1,
        pageSize: 20,
        totalCount: 1,
        items: [
          {
            id: businessId,
            tradingName: 'Glow Test Studio',
            description: 'Test studio',
            city: 'Johannesburg',
            suburb: 'Sandton',
            province: 'Gauteng',
            fromPrice: 250,
            categories: ['Hair'],
          },
        ],
      },
    }),
  );
  await page.route('**/api/v1/categories', (route) => route.fulfill({ json: [] }));
  await page.route(`**/api/v1/businesses/${businessId}`, (route) =>
    route.fulfill({
      json: {
        tradingName: 'Glow Test Studio',
        description: 'Test studio',
        isDemo: false,
        cancellationPolicy: 'Cancel at least 24 hours before the appointment.',
        publicEmail: 'studio@example.test',
        publicPhone: null,
        addressLine1: '1 Main Road',
        addressLine2: null,
        suburb: 'Sandton',
        city: 'Johannesburg',
        province: 'Gauteng',
        postalCode: '2196',
        portfolioImages: [],
        staff: [{ id: staffId, displayName: 'Lerato', biography: 'Natural hair specialist.' }],
        openingHours: [{ day: 'Monday', opensAt: '09:00', closesAt: '17:00' }],
        services: [
          {
            id: '44444444-aaaa-aaaa-aaaa-444444444444',
            category: 'Hair',
            name: 'Silk press',
            description: 'Wash and style',
            offerings: [{ id: offeringId, staffId, staffName: 'Lerato', price: 250, durationMinutes: 60 }],
          },
        ],
      },
    }),
  );
  await page.route(`**/api/v1/businesses/${businessId}/reviews?**`, (route) =>
    route.fulfill({ json: { averageRating: 0, reviewCount: 0, reviews: [] } }),
  );
  await page.route('**/api/v1/staff/*/availability?**', (route) =>
    route.fulfill({ json: [{ startUtc, endUtc: appointmentEnd.toISOString() }] }),
  );
  await page.route('**/api/v1/bookings', (route) =>
    route.request().method() === 'POST'
      ? route.fulfill({ json: { id: '55555555-aaaa-aaaa-aaaa-555555555555' } })
      : route.fulfill({
          json: [
            {
              id: '55555555-aaaa-aaaa-aaaa-555555555555',
              businessName: 'Glow Test Studio',
              serviceName: 'Silk press',
              staffName: 'Lerato',
              startUtc,
              status: 'Pending',
            },
          ],
        }),
  );
  await page.route('**/api/v1/reviews', (route) => route.fulfill({ json: [] }));

  await page.goto('/auth');
  await page.getByLabel('Email').fill('customer@example.test');
  await page.getByLabel('Password').fill('ValidPass!1');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page.getByText('Glow Test Studio')).toBeVisible();
  await page.getByText('Glow Test Studio').click();
  await expect(page.getByRole('heading', { name: 'Beauty professionals' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Opening hours' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Open directions' })).toBeVisible();
  await expect(page.getByRole('complementary', { name: 'Book this studio' })).toBeVisible();
  await page.locator('#services').getByRole('link', { name: 'Choose a time' }).click();
  await page
    .getByRole('button', { name: /^Select / })
    .first()
    .click();
  await expect(page.getByText('SELECTED TIME')).toBeVisible();
  await page.getByRole('button', { name: 'Request appointment' }).click();
  const bookingsPage = page.locator('app-bookings');
  await expect(bookingsPage.getByText('Silk press')).toBeVisible();
  await expect(bookingsPage.getByText('Pending')).toBeVisible();
});

test('creates a professional profile, service, pricing, and availability', async ({ page }) => {
  const staffId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
  const serviceId = 'ffffffff-ffff-ffff-ffff-ffffffffffff';
  const categoryId = '44444444-4444-4444-4444-000000000001';
  let services: object[] = [];
  const business = {
    tradingName: 'Glow Pro Studio',
    description: 'Studio',
    cancellationPolicy: 'Cancel anytime',
    publicEmail: null,
    publicPhone: null,
    status: 'Draft',
    concurrencyToken: '11111111-2222-3333-4444-555555555555',
    location: {
      addressLine1: '1 Main Road',
      addressLine2: null,
      suburb: 'Sandton',
      city: 'Johannesburg',
      province: 'Gauteng',
      postalCode: '2196',
    },
    staff: [{ id: staffId, displayName: 'Lerato' }],
  };
  await page.route('**/api/v1/professional/business', (route) =>
    route.request().method() === 'GET' ? route.fulfill({ status: 404 }) : route.fulfill({ json: business }),
  );
  await page.route('**/api/v1/professional/business/submit', (route) => route.fulfill({ status: 204 }));
  await page.route('**/api/v1/professional/portfolio-images', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/categories', (route) => route.fulfill({ json: [{ id: categoryId, name: 'Hair' }] }));
  await page.route('**/api/v1/professional/services', (route) => {
    if (route.request().method() === 'POST')
      services = [
        {
          id: serviceId,
          categoryId,
          categoryName: 'Hair',
          name: 'Silk press',
          description: 'Wash and style',
          isActive: false,
          offerings: [],
        },
      ];
    return route.fulfill({ json: route.request().method() === 'GET' ? services : services[0] });
  });
  await page.route(`**/api/v1/professional/services/${serviceId}/offerings`, (route) =>
    route.fulfill({
      json: {
        id: '12121212-1212-1212-1212-121212121212',
        staffMemberId: staffId,
        staffName: 'Lerato',
        price: 250,
        durationMinutes: 60,
        bufferMinutes: 0,
        isActive: true,
      },
    }),
  );
  await page.route('**/api/v1/professional/staff/*/weekly-availability', (route) =>
    route.request().method() === 'GET' ? route.fulfill({ json: [] }) : route.fulfill({ status: 204 }),
  );

  await page.goto('/professional/profile');
  await page.getByLabel('Trading name').fill('Glow Pro Studio');
  await page.getByLabel('Description', { exact: true }).fill('Studio');
  await page.getByLabel('Cancellation policy').fill('Cancel anytime');
  await page.getByLabel('Address', { exact: true }).fill('1 Main Road');
  await page.getByLabel('Suburb').fill('Sandton');
  await page.getByLabel('City').fill('Johannesburg');
  await page.getByLabel('Province').fill('Gauteng');
  await page.getByLabel('Postal code').fill('2196');
  await page.getByRole('button', { name: 'Save draft' }).click();
  await expect(page.getByText('Profile saved.')).toBeVisible();
  await page.getByRole('button', { name: 'Submit for review' }).click();
  await expect(page.getByText('Submitted for review.')).toBeVisible();

  await page.goto('/professional/services');
  await page.locator('ion-select[formcontrolname="categoryId"]').evaluate((element, value) => {
    (element as HTMLElement & { value: unknown }).value = value;
    element.dispatchEvent(new CustomEvent('ionChange', { detail: { value } }));
  }, categoryId);
  await page.getByLabel('Service name').fill('Silk press');
  await page.getByLabel('Description').fill('Wash and style');
  await page.getByRole('button', { name: 'Create service' }).click();
  await expect(page.getByText('Service created.')).toBeVisible();
  await page.locator('ion-select[formcontrolname="serviceId"]').evaluate((element, value) => {
    (element as HTMLElement & { value: unknown }).value = value;
    element.dispatchEvent(new CustomEvent('ionChange', { detail: { value } }));
  }, serviceId);
  await page.locator('ion-select[formcontrolname="staffMemberId"]').evaluate((element, value) => {
    (element as HTMLElement & { value: unknown }).value = value;
    element.dispatchEvent(new CustomEvent('ionChange', { detail: { value } }));
  }, staffId);
  await page.getByLabel('Price (ZAR)').fill('250');
  await page.getByLabel('Duration (minutes)').fill('60');
  await page.getByRole('button', { name: 'Add pricing' }).click();
  await expect(page.getByText('Price and duration added.')).toBeVisible();

  await page.unroute('**/api/v1/professional/business');
  await page.route('**/api/v1/professional/business', (route) => route.fulfill({ json: business }));
  await page.goto('/professional/availability');
  await page
    .locator('ion-select[formcontrolname="staffId"]')
    .first()
    .evaluate((element, value) => {
      (element as HTMLElement & { value: unknown }).value = value;
      element.dispatchEvent(new CustomEvent('ionChange', { detail: { value } }));
    }, staffId);
  await page.getByRole('button', { name: 'Add interval' }).click();
  await page.getByRole('button', { name: 'Save full schedule' }).click();
  await expect(page.getByText('Weekly availability saved.')).toBeVisible();
});

test('confirms, completes, cancels, and reviews bookings', async ({ page }) => {
  const completedId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  const cancelledId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
  let professionalStatus = 'Pending';
  let customerStatus = 'Pending';
  const booking = (id: string, status: string) => ({
    id,
    businessName: 'Glow Test Studio',
    serviceName: id === completedId ? 'Silk press' : 'Trim',
    staffName: 'Lerato',
    startUtc: '2026-11-09T07:00:00Z',
    status,
    concurrencyToken: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  });

  await page.route('**/api/v1/professional/bookings/*/*', async (route) => {
    const action = route.request().url().split('/').at(-1);
    professionalStatus = action === 'confirm' ? 'Confirmed' : action === 'complete' ? 'Completed' : professionalStatus;
    await route.fulfill({ status: 204 });
  });
  await page.route('**/api/v1/professional/bookings', (route) =>
    route.fulfill({ json: [booking(completedId, professionalStatus)] }),
  );
  await page.route(`**/api/v1/bookings/${cancelledId}/cancel`, (route) => {
    customerStatus = 'Cancelled';
    return route.fulfill({ status: 204 });
  });
  await page.route('**/api/v1/bookings/*/review', (route) =>
    route.fulfill({ json: { id: 'dddddddd-dddd-dddd-dddd-dddddddddddd' } }),
  );
  await page.route('**/api/v1/bookings', (route) =>
    route.fulfill({ json: [booking(completedId, 'Completed'), booking(cancelledId, customerStatus)] }),
  );
  await page.route('**/api/v1/reviews', (route) => route.fulfill({ json: [] }));

  await page.goto('/professional/bookings');
  const professionalPage = page.locator('app-professional-bookings');
  await professionalPage.getByRole('button', { name: 'Confirm' }).click();
  await expect(professionalPage.getByRole('button', { name: 'Complete' })).toBeVisible();
  await professionalPage.getByRole('button', { name: 'Complete' }).click();
  await expect(professionalPage.getByText('Completed')).toBeVisible();

  await page.goto('/bookings');
  const customerPage = page.locator('app-bookings');
  await customerPage.getByRole('button', { name: 'Cancel booking' }).click();
  await page.locator('ion-alert').getByRole('button', { name: 'Cancel booking' }).click();
  await expect(customerPage.getByText('Cancelled', { exact: true })).toBeVisible();
  await customerPage.getByRole('button', { name: 'Submit review' }).click();
  await expect(customerPage.getByText('Thank you for your review.')).toBeVisible();
});

test('administrator reviews a pending professional from the operations centre', async ({ page }) => {
  const payload = Buffer.from(JSON.stringify({ role: 'Admin' })).toString('base64url');
  const accessToken = `header.${payload}.signature`;
  await page.route('**/api/v1/auth/login', (route) =>
    route.fulfill({
      json: { accessToken, refreshToken: 'admin-refresh', accessTokenExpiresAtUtc: '2026-11-09T08:00:00Z' },
    }),
  );
  await page.route('**/api/v1/categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/businesses?**', (route) =>
    route.fulfill({ json: { page: 1, pageSize: 20, totalCount: 0, items: [] } }),
  );
  await page.route('**/api/v1/admin/businesses/pending', (route) =>
    route.fulfill({
      json: [
        {
          id: 'business-1',
          tradingName: 'Lerato Beauty Studio',
          description: 'Natural hair and beauty care.',
          status: 'PendingReview',
          location: { suburb: 'Rosebank', city: 'Johannesburg', province: 'Gauteng' },
          staff: [{ id: 'staff-1' }],
        },
      ],
    }),
  );
  await page.route('**/api/v1/admin/businesses', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/admin/users?**', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/admin/bookings', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/admin/audit-logs?**', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/admin/reviews', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/admin/portfolio-images', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/admin/space-reports', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/admin/businesses/business-1/approve', (route) => route.fulfill({ status: 204 }));
  await page.route('**/api/v1/me/data', (route) =>
    route.fulfill({
      json: {
        id: 'admin-1',
        email: 'admin@example.test',
        displayName: 'Glow Admin',
        marketingConsent: false,
        accountStatus: 'Active',
        createdAtUtc: '2026-11-07T00:00:00Z',
        termsVersion: '1',
        privacyNoticeVersion: '1',
      },
    }),
  );
  await page.route('**/api/v1/me', (route) =>
    route.fulfill({
      json: { id: 'admin-1', email: 'admin@example.test', displayName: 'Glow Admin', roles: ['Admin'] },
    }),
  );

  await page.goto('/auth');
  await page.getByLabel('Email').fill('admin@example.test');
  await page.getByLabel('Password').fill('ValidPass!1');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.getByRole('link', { name: 'Account' }).click();
  await page.getByRole('link', { name: 'Open admin centre' }).click();
  await expect(page.getByRole('heading', { name: 'Keep the marketplace trusted.' })).toBeVisible();
  await expect(page.getByText('Lerato Beauty Studio')).toBeVisible();
  await page.getByRole('button', { name: 'Approve' }).click();
  await page.locator('ion-alert').getByRole('button', { name: 'Approve business' }).click();
  await expect(page.getByText('Lerato Beauty Studio was approved.')).toBeVisible();
  await expect(page.getByText('No professional applications are awaiting review.')).toBeVisible();
});
