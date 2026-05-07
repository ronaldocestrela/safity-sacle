export type ScheduleItemDto = {
  id: string
  securityGuardId: string
  securityGuardName: string
  securityGuardIsActive: boolean
  date: string
  isWeekend: boolean
}

export type MonthlyScheduleDto = {
  id: string
  month: number
  year: number
  generatedAt: string
  items: ScheduleItemDto[]
}
