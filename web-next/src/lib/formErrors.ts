import { ApiError } from '@/api/errors'

export type ServerErrorPlacement<Field extends string> = {
  // The form field the message belongs to, or null for "the form as a whole".
  field: Field | null
  message: string
}

// Where each message in a 400's errors map should appear on a form.
//
// The API names fields as the REQUEST does, in camelCase ("amountInCents").
// The form may call them something else ("amount"), so the caller passes a
// map. A key the map doesn't know still gets shown, at the top of the form:
// a message that lands nowhere leaves the person clicking Save, seeing
// nothing happen, and not knowing why.
export function placeServerErrors<Field extends string>(
  error: ApiError,
  apiFieldToFormField: Readonly<Record<string, Field>>,
): ServerErrorPlacement<Field>[] {
  return Object.entries(error.fieldErrors).map(([key, messages]) => ({
    field: Object.hasOwn(apiFieldToFormField, key) ? apiFieldToFormField[key] : null,
    message: messages.join(' '),
  }))
}
