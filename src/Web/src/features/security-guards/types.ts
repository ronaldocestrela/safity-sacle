/** Aligned with `SafetyScale.Application.Sectors.Common.SectorDto` (summary projection on guards). */
export type SectorNestedDto = {
  id: string
  name: string
  description: string | null
  requiredGuardsPerDay: number
  isActive: boolean
  createdAt: string
}

/** Aligned with `SafetyScale.Application.SecurityGuards.Common.SecurityGuardDto`. */
export type SecurityGuardDto = {
  id: string
  name: string
  isActive: boolean
  /** ISO datetime from API */
  createdAt: string
  sectors: SectorNestedDto[]
}
