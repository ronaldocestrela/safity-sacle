/** Aligned with `SafetyScale.Application.UnavailableDays.Common.UnavailableDayDto`. */
export type UnavailableDayDto = {
  id: string
  securityGuardId: string
  /** ISO date `YYYY-MM-DD` from API */
  date: string
  reason: string | null
}

export type AddUnavailableDayPayload = {
  date: string
  reason?: string | null
}
