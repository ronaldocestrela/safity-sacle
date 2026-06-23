/** Aligned with `SafetyScale.Application.Sectors.Common.SectorDto`. */
export type SectorDto = {
  id: string
  name: string
  description: string | null
  /** Positions to fill daily for automated schedule generation. */
  requiredGuardsPerDay: number
  isActive: boolean
  /** ISO datetime from API */
  createdAt: string
}
