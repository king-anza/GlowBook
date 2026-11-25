import { HttpResponse, type HttpInterceptorFn } from '@angular/common/http';
import { of } from 'rxjs';

import { environment } from '../../../environments/environment';

type Business = {
  id: string;
  tradingName: string;
  description: string;
  city: string;
  suburb: string;
  province: string;
  fromPrice: number;
  categories: string[];
  averageRating: number;
  reviewCount: number;
  nextAvailableDate: string;
};

const categories = [
  { id: 'hair', name: 'Hair & braids' },
  { id: 'nails', name: 'Nails' },
  { id: 'makeup', name: 'Makeup' },
  { id: 'lashes', name: 'Lashes & brows' },
];

const businesses: Business[] = [
  {
    id: 'nomsa',
    tradingName: 'Nomsa Braid Studio',
    description: 'Medium knotless braids · Small knotless braids',
    city: 'Thohoyandou',
    suburb: 'Block F',
    province: 'Limpopo',
    fromPrice: 320,
    categories: ['Hair & braids'],
    averageRating: 4.9,
    reviewCount: 86,
    nextAvailableDate: nextDate(1),
  },
  {
    id: 'lumi',
    tradingName: 'Lumi Nail Bar',
    description: 'Gel overlay manicure · Acrylic tips',
    city: 'Thohoyandou',
    suburb: 'Sibasa',
    province: 'Limpopo',
    fromPrice: 280,
    categories: ['Nails'],
    averageRating: 4.8,
    reviewCount: 64,
    nextAvailableDate: nextDate(2),
  },
  {
    id: 'zola',
    tradingName: 'Zola Artistry',
    description: 'Soft glam makeup · Full glam makeup',
    city: 'Thohoyandou',
    suburb: 'Makwarela',
    province: 'Limpopo',
    fromPrice: 520,
    categories: ['Makeup'],
    averageRating: 4.9,
    reviewCount: 52,
    nextAvailableDate: nextDate(2),
  },
  {
    id: 'nandi',
    tradingName: 'Nandi Lash Lounge',
    description: 'Classic lashes · Brow shape and tint',
    city: 'Thohoyandou',
    suburb: 'Manini',
    province: 'Limpopo',
    fromPrice: 240,
    categories: ['Lashes & brows'],
    averageRating: 4.7,
    reviewCount: 41,
    nextAvailableDate: nextDate(3),
  },
  {
    id: 'imara',
    tradingName: 'Imara Beauty Co.',
    description: 'Natural hair care · Protective styling',
    city: 'Thohoyandou',
    suburb: 'Shayandima',
    province: 'Limpopo',
    fromPrice: 350,
    categories: ['Hair & braids'],
    averageRating: 4.8,
    reviewCount: 37,
    nextAvailableDate: nextDate(1),
  },
  {
    id: 'abeni',
    tradingName: 'Abeni Beauty Room',
    description: 'Express gel manicure · Nail art',
    city: 'Thohoyandou',
    suburb: 'P-East',
    province: 'Limpopo',
    fromPrice: 260,
    categories: ['Nails'],
    averageRating: 4.6,
    reviewCount: 29,
    nextAvailableDate: nextDate(4),
  },
];

let bookings: Record<string, unknown>[] = [
  {
    id: 'demo-upcoming-booking',
    businessName: 'Nomsa Braid Studio',
    serviceName: 'Medium knotless braids',
    staffName: 'Nomsa Braid',
    startUtc: timeOnDate(2, '09:00:00'),
    status: 'Confirmed',
    concurrencyToken: 'demo-upcoming-token',
  },
  {
    id: 'demo-completed-booking',
    businessName: 'Lumi Nail Bar',
    serviceName: 'Gel overlay manicure',
    staffName: 'Lumi Nail',
    startUtc: timeOnDate(-7, '11:30:00'),
    status: 'Completed',
    concurrencyToken: 'demo-completed-token',
  },
];
let reviews: Record<string, unknown>[] = [
  {
    id: 'demo-booking-review',
    bookingId: 'demo-completed-booking',
    rating: 5,
    comment: 'The studio was welcoming and my nails lasted beautifully.',
    concurrencyToken: 'demo-review-token',
  },
];
let account = {
  id: 'demo-customer',
  email: 'demo@glowbook.test',
  displayName: 'Demo Guest',
  marketingConsent: false,
  accountStatus: 'Active',
  createdAtUtc: '2026-01-01T00:00:00Z',
  termsVersion: '1.0',
  privacyNoticeVersion: '1.0',
};

export const demoInterceptor: HttpInterceptorFn = (request, next) => {
  if (!environment.demoMode || !request.url.includes('/api/v1/')) return next(request);

  const url = new URL(request.url, 'https://demo.glowbook.local');
  const path = url.pathname;
  const method = request.method;
  const response = (body: unknown, status = 200) => of(new HttpResponse({ body, status }));

  if (path === '/api/v1/categories' && method === 'GET') return response(categories);
  if (path === '/api/v1/businesses' && method === 'GET') return response(searchBusinesses(url));
  if (path.startsWith('/api/v1/businesses/') && path.endsWith('/reviews') && method === 'GET')
    return response(businessReviews(path.split('/')[4]));
  if (path.startsWith('/api/v1/businesses/') && method === 'GET') return response(businessDetail(path.split('/')[4]));
  if (path.startsWith('/api/v1/staff/') && path.endsWith('/availability') && method === 'GET') return response(slots());

  if (path === '/api/v1/auth/login' && method === 'POST') return response(tokens());
  if (path.startsWith('/api/v1/auth/register/') && method === 'POST') return response(null, 202);
  if (['/api/v1/auth/refresh'].includes(path) && method === 'POST') return response(tokens());
  if (
    ['/api/v1/auth/logout', '/api/v1/auth/verify-email', '/api/v1/auth/reset-password'].includes(path) &&
    method === 'POST'
  )
    return response(null, 204);
  if (['/api/v1/auth/resend-verification', '/api/v1/auth/forgot-password'].includes(path) && method === 'POST')
    return response(null, 202);

  if (path === '/api/v1/me' && method === 'GET') return response({ ...account, roles: ['Customer'] });
  if (path === '/api/v1/me/data' && method === 'GET') return response({ ...account, bookings, reviews });
  if (path === '/api/v1/me' && method === 'PATCH') {
    account = { ...account, ...(request.body as Partial<typeof account>) };
    return response(account);
  }
  if (path === '/api/v1/me' && method === 'DELETE') return response(null, 204);

  if (path === '/api/v1/bookings' && method === 'GET') return response(bookings);
  if (path === '/api/v1/bookings' && method === 'POST') {
    const body = request.body as { offeringId: string; startUtc: string };
    const business = businesses.find((item) => item.id === offeringBusiness(body.offeringId)) ?? businesses[0];
    bookings = [
      {
        id: crypto.randomUUID(),
        businessName: business.tradingName,
        serviceName: serviceName(business),
        staffName: staffName(business),
        startUtc: body.startUtc,
        status: 'Pending',
        concurrencyToken: crypto.randomUUID(),
      },
      ...bookings,
    ];
    return response(bookings[0]);
  }
  if (path.startsWith('/api/v1/bookings/') && path.endsWith('/cancel') && method === 'POST') {
    const id = path.split('/')[4];
    bookings = bookings.map((item) => (item['id'] === id ? { ...item, status: 'Cancelled' } : item));
    return response({});
  }
  if (path.startsWith('/api/v1/bookings/') && path.endsWith('/review') && method === 'POST') {
    const id = path.split('/')[4];
    const body = request.body as { rating: number; comment: string | null };
    const review = {
      id: crypto.randomUUID(),
      bookingId: id,
      rating: body.rating,
      comment: body.comment,
      concurrencyToken: crypto.randomUUID(),
    };
    reviews = [review, ...reviews];
    return response(review);
  }
  if (path === '/api/v1/reviews' && method === 'GET') return response(reviews);
  if (path.startsWith('/api/v1/reviews/') && method === 'PATCH') return response(request.body);
  if (path.startsWith('/api/v1/reviews/') && method === 'DELETE') {
    reviews = reviews.filter((item) => item['id'] !== path.split('/')[4]);
    return response(null, 204);
  }

  return response([], 200);
};

function nextDate(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return date.toISOString().slice(0, 10);
}

function timeOnDate(days: number, time: string): string {
  return `${nextDate(days)}T${time}Z`;
}

function searchBusinesses(url: URL) {
  const term = url.searchParams.get('search')?.toLowerCase().trim() ?? '';
  const categoryId = url.searchParams.get('categoryId');
  const city = url.searchParams.get('city')?.toLowerCase().trim() ?? '';
  const items = businesses.filter(
    (item) =>
      (!term || `${item.tradingName} ${item.description}`.toLowerCase().includes(term)) &&
      (!categoryId ||
        item.categories.some((category) => category.toLowerCase().includes(categoryId.replace('-', ' ')))) &&
      (!city || item.city.toLowerCase().includes(city)),
  );
  return {
    items: items.map((item) => ({ ...item, isDemo: true, coverImageUrl: null, distanceKm: null })),
    page: 1,
    pageSize: 20,
    totalCount: items.length,
  };
}

function businessDetail(id: string) {
  const business = businesses.find((item) => item.id === id) ?? businesses[0];
  const category = business.categories[0];
  const image = category.includes('Nail')
    ? 'nails'
    : category.includes('Makeup')
      ? 'makeup'
      : category.includes('Lash')
        ? 'lashes'
        : 'braids';
  return {
    ...business,
    addressLine1: '12 Demo Street',
    services: [
      {
        id: `${business.id}-service`,
        category,
        name: serviceName(business),
        description: business.description,
        offerings: [
          {
            id: `${business.id}-offering`,
            staffId: `${business.id}-staff`,
            staffName: staffName(business),
            price: business.fromPrice,
            durationMinutes: 90,
          },
        ],
      },
    ],
    portfolioImages: [
      {
        id: `${business.id}-image`,
        imageUrl: `assets/glowbook/${image}.webp`,
        altText: `${business.tradingName} sample work`,
      },
    ],
  };
}

function businessReviews(id: string) {
  const business = businesses.find((item) => item.id === id) ?? businesses[0];
  return {
    averageRating: business.averageRating,
    reviewCount: business.reviewCount,
    reviews: [{ id: `${business.id}-review`, rating: 5, comment: 'Lovely experience and beautiful results.' }],
  };
}

function slots() {
  return [1, 2, 3].flatMap((day) =>
    ['09:00:00', '11:30:00', '14:00:00'].map((time) => ({
      startUtc: `${nextDate(day)}T${time}Z`,
      endUtc: `${nextDate(day)}T${time === '14:00:00' ? '15:30:00' : time === '11:30:00' ? '13:00:00' : '10:30:00'}Z`,
    })),
  );
}
function offeringBusiness(offeringId: string) {
  return offeringId.replace('-offering', '');
}
function serviceName(business: Business) {
  return business.categories[0].includes('Nail')
    ? 'Gel overlay manicure'
    : business.categories[0].includes('Makeup')
      ? 'Soft glam makeup'
      : business.categories[0].includes('Lash')
        ? 'Classic lash set'
        : 'Medium knotless braids';
}
function staffName(business: Business) {
  return business.tradingName.replace(/ (Studio|Bar|Artistry|Lounge|Co\.|Room)$/, '');
}
function tokens() {
  return {
    accessToken: `header.${btoa(JSON.stringify({ role: 'Customer' })).replaceAll('=', '')}.demo`,
    accessTokenExpiresAtUtc: new Date(Date.now() + 900000).toISOString(),
    refreshToken: 'demo-refresh-token',
  };
}
