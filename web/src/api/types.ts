import type { components } from './schema'

// Friendly names for the generated schemas. These are aliases, not copies:
// if the API renames a field, every component using it stops compiling.
type Schemas = components['schemas']

export type StudentResponse = Schemas['StudentResponse']
export type SubjectResponse = Schemas['SubjectResponse']
export type TertiaryApplicationResponse = Schemas['TertiaryApplicationResponse']
export type DashboardBursaryApplication = Schemas['DashboardBursaryApplication']
export type BursaryApplicationResponse = Schemas['BursaryApplicationResponse']
export type LearnerDashboardResponse = Schemas['LearnerDashboardResponse']
export type MeResponse = Schemas['MeResponse']
export type BursaryApplicationCreateRequest = Schemas['BursaryApplicationCreateRequest']

// Day 3: the bursary card is now fed by TWO endpoints: the learner's
// dashboard and the counsellor's paged list. Their response types differ,
// but both have the five fields the card actually uses. Depending on just
// those (still taken from the generated type) lets one card serve both.
export type BursaryCardData = Pick<
  DashboardBursaryApplication,
  'id' | 'funder' | 'status' | 'deadline' | 'amountInCents'
>
