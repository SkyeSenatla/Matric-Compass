import 'server-only'
import { readFile } from 'node:fs/promises'
import path from 'node:path'
import { cacheLife, cacheTag } from 'next/cache'

export type Career = {
  name: string
  requires: { subject: string; minimum: number }[]
}

// The careers catalogue is the same for every user, and it changes when the
// content team edits careers.json, not on every request. That makes it the
// one thing in Matric Compass worth caching across users.
//
// "use cache": the result is stored and reused, across requests, for as long
// as cacheLife says. No arguments, so there's one entry for everyone. It
// reads no cookies and calls no API with anyone's token.
export async function getCareerCatalogue(): Promise<Career[]> {
  'use cache'
  cacheLife('days')
  cacheTag('career-catalogue')

  console.log('Reading the careers catalogue from disk')
  const file = await readFile(path.join(process.cwd(), 'src/data/careers.json'), 'utf8')
  return JSON.parse(file) as Career[]
}

// Which careers a learner's TARGET marks would open. Not cached: it takes
// one learner's marks, and those are never shared between users.
export function careersFor(catalogue: Career[], subjects: { code: string; targetMark: number }[]): Career[] {
  const markFor = new Map(subjects.map((subject) => [subject.code, subject.targetMark]))
  return catalogue.filter((career) =>
    career.requires.every((rule) => (markFor.get(rule.subject) ?? 0) >= rule.minimum),
  )
}
