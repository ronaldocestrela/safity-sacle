/** Aligned with `SafetyScale.Application.Sectors.Common.SectorDto`. */
export type SectorDto = {
  id: string
  name: string
  description: string | null
  isActive: boolean
  /** ISO datetime from API */
  createdAt: string
}
