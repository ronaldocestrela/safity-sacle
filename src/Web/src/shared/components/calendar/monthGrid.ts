/** Local date key yyyy-MM-dd (no timezone drift for calendar grids). */

function pad2(n: number): string {
  return String(n).padStart(2, '0')
}

export function dateKeyFromLocal(d: Date): string {
  return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`
}

export function todayKeyLocal(): string {
  return dateKeyFromLocal(new Date())
}

export type MonthGridCell = {
  key: string
  label: number
  inMonth: boolean
}

export function buildMonthGrid(viewYear: number, viewMonth0: number): MonthGridCell[] {
  const first = new Date(viewYear, viewMonth0, 1)
  const startPad = first.getDay()
  const daysInMonth = new Date(viewYear, viewMonth0 + 1, 0).getDate()
  const cells: MonthGridCell[] = []

  const prevLast = new Date(viewYear, viewMonth0, 0).getDate()
  for (let i = 0; i < startPad; i++) {
    const dayNum = prevLast - startPad + i + 1
    const d = new Date(viewYear, viewMonth0 - 1, dayNum)
    cells.push({ key: dateKeyFromLocal(d), label: dayNum, inMonth: false })
  }

  for (let day = 1; day <= daysInMonth; day++) {
    const d = new Date(viewYear, viewMonth0, day)
    cells.push({ key: dateKeyFromLocal(d), label: day, inMonth: true })
  }

  const rem = cells.length % 7
  const tail = rem === 0 ? 0 : 7 - rem
  let n = 1
  const ny = viewMonth0 === 11 ? viewYear + 1 : viewYear
  const nm = viewMonth0 === 11 ? 0 : viewMonth0 + 1
  for (let i = 0; i < tail; i++) {
    const d = new Date(ny, nm, n)
    cells.push({ key: dateKeyFromLocal(d), label: n, inMonth: false })
    n++
  }

  return cells
}

export const WEEKDAY_LABELS_SMTWTFS = ['S', 'M', 'T', 'W', 'T', 'F', 'S'] as const
