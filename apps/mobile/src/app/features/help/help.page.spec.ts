import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { HelpPage } from './help.page';

describe('HelpPage', () => {
  it('searches practical guidance and can reset an empty result', async () => {
    await TestBed.configureTestingModule({ imports: [HelpPage], providers: [provideRouter([])] }).compileComponents();
    const page = TestBed.createComponent(HelpPage).componentInstance;
    page.search('never queued');
    expect(page.filtered().map((article) => article.id)).toEqual(['connection']);
    page.chooseTopic('bookings');
    expect(page.filtered()).toEqual([]);
    page.search('cancel');
    expect(page.filtered().map((article) => article.id)).toContain('booking');
  });
});
