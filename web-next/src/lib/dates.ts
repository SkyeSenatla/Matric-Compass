// The API sends UTC instants ("...Z"). Our users live in South Africa, so we
// always show dates in Africa/Johannesburg, whatever time zone the browser is in.
const saDate = new Intl.DateTimeFormat('en-ZA', {
  dateStyle: 'medium',
  timeZone: 'Africa/Johannesburg',
})

export function formatDate(isoInstant: string): string {
  return saDate.format(new Date(isoInstant))
}
