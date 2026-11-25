# Advanced discovery controls

- Search text, category, province, city, suburb, price range, selected sort and page persist in the URL so returning from a profile and sharing a search preserve context.
- Exact device coordinates are never written to the URL. They remain in memory for the current session; distance sorting is enabled only while coordinates are available.
- The Filters control shows the number of active criteria. Clear all is available both beside the summary and inside the expanded panel.
- Applying filters returns to page one and closes the panel.
- Sorting occurs in the API before pagination, so the selected order describes the complete matching result set rather than only the current page.

Recommended order prioritises distance when the customer has provided location. Without location it uses rating, review evidence, availability and business name as deterministic tie-breakers.
