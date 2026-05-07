/** Aligned with `SafetyScale.Application.SecurityGuards.Common.SecurityGuardDto`. */
export type SecurityGuardDto = {
  id: string
  name: string
  isActive: boolean
  /** ISO datetime from API */
  createdAt: string
}
