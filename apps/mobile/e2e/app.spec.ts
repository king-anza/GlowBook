import { expect, test } from '@playwright/test';

test('shows the discovery page', async ({ page }) => {
  await page.route('**/api/v1/categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/v1/businesses?**', (route) =>
    route.fulfill({ json: { page: 1, pageSize: 20, totalCount: 0, items: [] } }),
  );
  await page.goto('/');
  await expect(page.getByText('Discover beauty services')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Search' })).toBeVisible();
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
  await page.getByRole('button', { name: 'Create account' }).click();
  await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();
  await page.locator('ion-button', { hasText: 'Verify email or reset password' }).click();
  const recoveryPage = page.locator('app-recovery');
  await recoveryPage.getByLabel('Email', { exact: true }).fill('customer@example.test');
  await recoveryPage.getByRole('button', { name: 'Request password reset' }).click();
  await expect(
    recoveryPage.getByText('If the account exists, password-reset instructions have been sent.'),
  ).toBeVisible();
});

test('signs in, discovers a service, and books a slot', async ({ page }) => {
  const businessId = '11111111-aaaa-aaaa-aaaa-111111111111';
  const staffId = '22222222-aaaa-aaaa-aaaa-222222222222';
  const offeringId = '33333333-aaaa-aaaa-aaaa-333333333333';
  const startUtc = '2026-11-09T07:00:00Z';

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
        addressLine1: '1 Main Road',
        suburb: 'Sandton',
        city: 'Johannesburg',
        province: 'Gauteng',
        portfolioImages: [],
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
    route.fulfill({ json: [{ startUtc, endUtc: '2026-11-09T08:00:00Z' }] }),
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
  await page.getByRole('link', { name: 'Choose a time' }).click();
  await page.getByRole('button', { name: 'Book' }).click();
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
  await expect(customerPage.getByText('Cancelled')).toBeVisible();
  await customerPage.getByRole('button', { name: 'Submit review' }).click();
  await expect(customerPage.getByText('Thank you for your review.')).toBeVisible();
});
