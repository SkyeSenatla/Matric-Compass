import type { components } from './schema'

// Friendly names for the generated schemas. These are aliases, not copies:
// if the API renames a field, every component using it stops compiling.
type Schemas = components['schemas']

export type StudentResponse = Schemas['StudentResponse']
export type SubjectResponse = Schemas['SubjectResponse']
export type TertiaryApplicationResponse = Schemas['TertiaryApplicationResponse']
export type DashboardBursaryApplication = Schemas['DashboardBursaryApplication']
