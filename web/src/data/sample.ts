import type {
  DashboardBursaryApplication,
  StudentResponse,
  TertiaryApplicationResponse,
} from '../api/types'

// Hard-coded for today. Day 3 replaces these with live API calls, and the
// cards won't change, because they only depend on their props.

export const students: StudentResponse[] = [
  {
    id: '6f1c2a9e-1d4b-4c8e-9a51-2b7d3e8f0a11',
    fullName: 'Thandiwe Nkosi',
    learnerReferenceNumber: '9001015009087',
    subjects: [
      { code: 'MATH', currentMark: 72, targetMark: 80 },
      { code: 'PHSC', currentMark: 81, targetMark: 75 },
      { code: 'ENGH', currentMark: 68, targetMark: 70 },
    ],
  },
  {
    id: 'a3e4b5c6-7d8e-4f90-8123-456789abcdef',
    fullName: 'Sipho Dlamini',
    learnerReferenceNumber: '9105125009081',
    subjects: [],
  },
]

export const bursaryApplications: DashboardBursaryApplication[] = [
  {
    id: '0b9e2f6c-3a1d-4e57-9c8b-1f2e3d4c5b6a',
    funder: 'NSFAS',
    status: 'Submitted',
    deadline: '2026-11-30T21:59:59Z',
    daysUntilDeadline: 46,
    amountInCents: 4_500_010,
  },
  {
    id: '7c6d5e4f-3b2a-4190-8f7e-6d5c4b3a2918',
    funder: 'Funza Lushaka',
    status: 'Draft',
    deadline: '2026-10-15T21:59:59Z',
    daysUntilDeadline: 0,
    amountInCents: 3_200_000,
  },
  {
    id: 'e1d2c3b4-a596-4877-b6a5-948372615049',
    funder: 'Sasol',
    status: 'Rejected',
    deadline: '2026-09-30T21:59:59Z',
    daysUntilDeadline: -15,
    amountInCents: 10,
  },
]

export const tertiaryApplications: TertiaryApplicationResponse[] = [
  {
    id: '5a4b3c2d-1e0f-4a9b-8c7d-6e5f4a3b2c1d',
    studentId: '6f1c2a9e-1d4b-4c8e-9a51-2b7d3e8f0a11',
    institutionName: 'University of Cape Town',
    programmeName: 'BSc Computer Science',
    status: 'DocumentsSubmitted',
  },
  {
    id: '9f8e7d6c-5b4a-4392-8170-fedcba987654',
    studentId: '6f1c2a9e-1d4b-4c8e-9a51-2b7d3e8f0a11',
    institutionName: 'Wits University',
    programmeName: 'BEng Electrical',
    status: 'Accepted',
  },
]
