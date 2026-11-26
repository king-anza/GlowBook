# Professional calendar, capacity and insights

The Studio calendar supports day and seven-day views in the business location time zone. Owners can filter by professional, compare scheduled, booked and blocked minutes, see chair/workspace assignments, and remove blocked periods. Calendar changes use explicit controls rather than drag-and-drop, so a pointer slip cannot move an appointment. The API rejects new blocked periods that overlap pending or confirmed bookings and explains booking, blocked-time and shared-workspace conflicts.

Staff can be assigned an optional chair or workspace label from Services & staff. Two professionals can share a workspace at different times; overlapping active bookings on the same named workspace are highlighted for the owner. Existing staff without an assignment remain valid and appear as independent calendar capacity lanes.

The Insights view offers 30- and 90-day booking volume, completed visits, cancellation and no-show rates, repeat customers and chair utilisation. Metrics are calculated from persisted bookings and availability rather than inferred personal traits. Repeat-customer totals are hidden until at least three distinct customers fall within the selected period, and the API never returns customer names, notes or profile data in insight responses.
