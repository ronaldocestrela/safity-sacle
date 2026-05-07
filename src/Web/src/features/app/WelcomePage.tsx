/**
 * Operational dashboard — UI aligned to Google Stitch "Dashboard Operacional SafetyScale".
 * Reference: projects/9334796298126275303/screens/973c4f6064054619a1742a14796ea5eb
 * Design system: Sentinel Command (see agents.md).
 */
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../shared/auth/useAuth'
import { ApiError as GuardsApiError, listSecurityGuards } from '../security-guards/securityGuardsApi'
import type { SecurityGuardDto } from '../security-guards/types'
import { ApiError as SchedulesApiError, getScheduleByMonthYear } from '../schedules/schedulesApi'
import type { MonthlyScheduleDto, ScheduleItemDto } from '../schedules/types'
import styles from './WelcomePage.module.css'

function pad2(n: number): string {
  return String(n).padStart(2, '0')
}

function dateKeyFromLocal(d: Date): string {
  return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`
}

function todayKeyLocal(): string {
  return dateKeyFromLocal(new Date())
}

type CalCell = { key: string; label: number; inMonth: boolean }

function buildMonthGrid(viewYear: number, viewMonth0: number): CalCell[] {
  const first = new Date(viewYear, viewMonth0, 1)
  const startPad = first.getDay()
  const daysInMonth = new Date(viewYear, viewMonth0 + 1, 0).getDate()
  const cells: CalCell[] = []

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

function currentMonthYear(): { month: number; year: number } {
  const d = new Date()
  return { month: d.getMonth() + 1, year: d.getFullYear() }
}

const MONTH_NAMES = [
  'January',
  'February',
  'March',
  'April',
  'May',
  'June',
  'July',
  'August',
  'September',
  'October',
  'November',
  'December',
] as const

function formatMonthYear(month: number, year: number): string {
  if (month < 1 || month > 12) {
    return `${year}`
  }
  return `${MONTH_NAMES[month - 1]} ${year}`
}

function userInitials(email: string | undefined): string {
  if (!email) {
    return '?'
  }
  const parts = email.split('@')[0].split(/[.\-_]/).filter(Boolean)
  if (parts.length >= 2) {
    return (parts[0][0] + parts[1][0]).toUpperCase().slice(0, 2)
  }
  return email.slice(0, 2).toUpperCase()
}

function displayAssignmentDate(iso: string): string {
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso.trim())
  if (m) {
    const y = Number(m[1])
    const mo = Number(m[2]) - 1
    const day = Number(m[3])
    const dt = new Date(y, mo, day)
    return dt.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' })
  }
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) {
    return iso
  }
  return d.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' })
}

function shortGuardId(id: string): string {
  const alnum = id.replace(/[^a-zA-Z0-9]/g, '').toUpperCase()
  return (alnum + '0000').slice(0, 4)
}

function assignmentsForSelectedDate(items: ScheduleItemDto[] | undefined, selectedKey: string | null): ScheduleItemDto[] {
  if (!items?.length || !selectedKey) {
    return []
  }
  return [...items]
    .filter((i) => i.date === selectedKey)
    .sort(
      (a, b) =>
        a.securityGuardName.localeCompare(b.securityGuardName) || a.id.localeCompare(b.id),
    )
}

function calendarAriaLabel(dateKey: string, count: number): string {
  const label = displayAssignmentDate(dateKey)
  if (count === 0) {
    return `${label}, no assignments`
  }
  if (count === 1) {
    return `${label}, 1 assignment`
  }
  return `${label}, ${count} assignments`
}

const WEEKDAY_LABELS = ['S', 'M', 'T', 'W', 'T', 'F', 'S'] as const

function initialSelectedDateKey(schedule: MonthlyScheduleDto | null, month: number, year: number): string | null {
  if (!schedule) {
    return null
  }
  const t = todayKeyLocal()
  const first = `${year}-${pad2(month)}-01`
  const lastN = new Date(year, month, 0).getDate()
  const last = `${year}-${pad2(month)}-${pad2(lastN)}`
  if (t >= first && t <= last) {
    return t
  }
  return first
}

function errorMessage(e: unknown, fallback: string): string {
  if (e instanceof GuardsApiError || e instanceof SchedulesApiError) {
    return e.message || fallback
  }
  return fallback
}

export function WelcomePage() {
  const { session, logout } = useAuth()
  const isAdmin = Boolean(session?.roles.includes('Admin'))
  const primaryRole = session?.roles.includes('Admin') ? 'Admin' : 'Supervisor'

  const loadTokenRef = useRef(0)

  const [{ month, year }] = useState(currentMonthYear)
  const [guards, setGuards] = useState<SecurityGuardDto[]>([])
  const [schedule, setSchedule] = useState<MonthlyScheduleDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [guardsError, setGuardsError] = useState<string | null>(null)
  const [scheduleError, setScheduleError] = useState<string | null>(null)
  const [selectedDateKey, setSelectedDateKey] = useState<string | null>(null)

  const refresh = useCallback(async () => {
    const token = ++loadTokenRef.current
    setLoading(true)
    setGuardsError(null)
    setScheduleError(null)

    const [gRes, sRes] = await Promise.allSettled([
      listSecurityGuards(undefined),
      getScheduleByMonthYear(month, year),
    ])

    if (loadTokenRef.current !== token) {
      return
    }

    if (gRes.status === 'fulfilled') {
      setGuards(gRes.value)
    } else {
      setGuards([])
      setGuardsError(errorMessage(gRes.reason, 'Could not load personnel.'))
    }

    if (sRes.status === 'fulfilled') {
      const sched = sRes.value
      setSchedule(sched)
      setSelectedDateKey(initialSelectedDateKey(sched, month, year))
    } else {
      setSchedule(null)
      setSelectedDateKey(null)
      setScheduleError(errorMessage(sRes.reason, 'Could not load monthly roster.'))
    }

    setLoading(false)
  }, [month, year])

  useEffect(() => {
    queueMicrotask(() => {
      void refresh()
    })
  }, [refresh])

  const calendarCells = useMemo(() => buildMonthGrid(year, month - 1), [year, month])

  const assignmentCountByDate = useMemo(() => {
    const m = new Map<string, number>()
    for (const i of schedule?.items ?? []) {
      m.set(i.date, (m.get(i.date) ?? 0) + 1)
    }
    return m
  }, [schedule])

  const selectedDayItems = useMemo(
    () => assignmentsForSelectedDate(schedule?.items, selectedDateKey),
    [schedule, selectedDateKey],
  )

  const kpis = useMemo(() => {
    const active = guards.filter((g) => g.isActive).length
    const inactive = guards.filter((g) => !g.isActive).length
    const items = schedule?.items ?? []
    const assignments = items.length
    const weekend = items.filter((i) => i.isWeekend).length
    return { active, inactive, assignments, weekend }
  }, [guards, schedule])

  const periodLabel = formatMonthYear(month, year)
  const todayKey = todayKeyLocal()

  return (
    <div className={styles.page}>
      <header className={styles.topAppBar}>
        <div className={styles.topAppBarInner}>
          <div className={styles.topAppBarLeft}>
            <div className={styles.avatarRing} aria-hidden>
              <span className={styles.avatarInitials}>{userInitials(session?.email ?? undefined)}</span>
            </div>
            <div className={styles.titleBlock}>
              <h1 className={styles.topAppBarTitle}>SentryOps</h1>
              <p className={styles.emailChip} title={session?.email ?? undefined}>
                {session?.email ?? '—'}
              </p>
            </div>
          </div>
          <div className={styles.topAppBarMeta}>
            <span className={styles.roleBadge}>{primaryRole}</span>
            <span className={styles.statusPill}>System online</span>
            <button type="button" className={styles.iconGhost} aria-label="Notifications">
              <span className={`material-symbols-outlined ${styles.iconMd}`}>notifications</span>
            </button>
            <button type="button" className={styles.iconGhost} aria-label="Log out" onClick={logout}>
              <span className={`material-symbols-outlined ${styles.iconMd}`}>logout</span>
            </button>
          </div>
        </div>
      </header>

      <main className={styles.main}>
        {guardsError ? (
          <div role="alert" className={`${styles.banner} ${styles.bannerError}`}>
            <span>{guardsError}</span>
            <button type="button" className={styles.retryBtn} onClick={() => void refresh()}>
              Retry
            </button>
          </div>
        ) : null}
        {scheduleError ? (
          <div role="alert" className={`${styles.banner} ${styles.bannerError}`}>
            <span>{scheduleError}</span>
            <button type="button" className={styles.retryBtn} onClick={() => void refresh()}>
              Retry
            </button>
          </div>
        ) : null}

        {loading ? (
          <div className={styles.skeletonStack} aria-busy="true" aria-label="Loading dashboard">
            <div className={styles.skeletonCard} />
            <div className={styles.skeletonGrid}>
              <div className={styles.skeletonKpi} />
              <div className={styles.skeletonKpi} />
              <div className={styles.skeletonKpi} />
              <div className={styles.skeletonKpi} />
            </div>
            <div className={styles.skeletonCard} />
          </div>
        ) : (
          <>
            <p className={styles.sectionIntro}>
              Operational snapshot for <strong>{periodLabel}</strong>. KPIs use live API data; schedule may be missing
              until generated.
            </p>

            <div className={styles.kpiGrid}>
              <div className={styles.kpiCard}>
                <span className={styles.kpiLabel}>Active guards</span>
                <span className={styles.kpiValue} data-testid="kpi-active">
                  {kpis.active}
                </span>
              </div>
              <div className={styles.kpiCard}>
                <span className={styles.kpiLabel}>Inactive guards</span>
                <span className={styles.kpiValue} data-testid="kpi-inactive">
                  {kpis.inactive}
                </span>
              </div>
              <div className={styles.kpiCard}>
                <span className={styles.kpiLabel}>Assignments</span>
                <span className={styles.kpiValue} data-testid="kpi-assignments">
                  {kpis.assignments}
                </span>
              </div>
              <div className={styles.kpiCard}>
                <span className={styles.kpiLabel}>Weekend shifts</span>
                <span className={styles.kpiValue} data-testid="kpi-weekend">
                  {kpis.weekend}
                </span>
              </div>
            </div>

            <section className={styles.rosterSection} aria-labelledby="roster-heading">
              <h2 id="roster-heading" className={styles.sectionTitle}>
                Current month roster
              </h2>
              {!schedule ? (
                <div className={styles.emptyCard}>
                  <span className={`material-symbols-outlined ${styles.emptyIcon}`} aria-hidden>
                    event_busy
                  </span>
                  <p className={styles.emptyTitle}>No schedule for {periodLabel}</p>
                  <p className={styles.emptySub}>
                    {isAdmin
                      ? 'Generate the monthly roster from Schedules when your team is ready.'
                      : 'Ask an administrator to generate the schedule, or open Schedules to review when available.'}
                  </p>
                  <Link
                    className={isAdmin ? styles.ctaPrimary : styles.ctaSecondary}
                    to="/app/schedules"
                  >
                    {isAdmin ? 'Open schedules to generate' : 'View schedules'}
                  </Link>
                </div>
              ) : (
                <>
                  <div className={styles.calendarCard}>
                    <p className={styles.calendarHint}>
                      Tap a date to see who is assigned. Dots mark days with coverage.
                    </p>
                    <div className={styles.weekdayRow}>
                      {WEEKDAY_LABELS.map((w, i) => (
                        <span key={`w-${i.toString()}`} className={styles.weekdayCell}>
                          {w}
                        </span>
                      ))}
                    </div>
                    <div className={styles.calendarGrid}>
                      {calendarCells.map((cell) => {
                        const count = cell.inMonth ? (assignmentCountByDate.get(cell.key) ?? 0) : 0
                        if (!cell.inMonth) {
                          return (
                            <span key={cell.key} className={styles.calPad} aria-hidden>
                              <span className={styles.calPadNum}>{cell.label}</span>
                            </span>
                          )
                        }
                        const selected = cell.key === selectedDateKey
                        const isToday = cell.key === todayKey
                        return (
                          <button
                            key={cell.key}
                            type="button"
                            className={`${styles.calDay} ${selected ? styles.calDaySelected : ''} ${isToday ? styles.calDayToday : ''}`}
                            onClick={() => setSelectedDateKey(cell.key)}
                            aria-label={calendarAriaLabel(cell.key, count)}
                            aria-pressed={selected}
                          >
                            <span className={styles.calDayNum}>{cell.label}</span>
                            {count > 0 ? (
                              <span className={styles.calDayDotWrap} aria-hidden>
                                <span className={styles.calDayDot} />
                              </span>
                            ) : (
                              <span className={styles.calDayDotSpacer} aria-hidden />
                            )}
                          </button>
                        )
                      })}
                    </div>
                  </div>

                  <div
                    className={styles.dayDetail}
                    role="region"
                    aria-labelledby="day-detail-heading"
                  >
                    <h3 id="day-detail-heading" className={styles.dayDetailTitle}>
                      {selectedDateKey ? displayAssignmentDate(selectedDateKey) : 'Select a date'}
                    </h3>
                    {selectedDateKey && selectedDayItems.some((i) => i.isWeekend) ? (
                      <p className={styles.dayDetailWeekend} data-testid="weekend-day-hint">
                        Weekend shift
                      </p>
                    ) : null}
                    {!selectedDateKey ? (
                      <p className={styles.dayDetailEmpty}>Choose a day on the calendar.</p>
                    ) : selectedDayItems.length === 0 ? (
                      <p className={styles.dayDetailEmpty}>No assignments on this day.</p>
                    ) : (
                      <ul className={styles.shiftList}>
                        {selectedDayItems.map((item) => (
                          <li key={item.id} className={styles.shiftCard}>
                            <span className={styles.shiftStripe} aria-hidden />
                            <div className={styles.shiftBody}>
                              <p className={styles.shiftName}>{item.securityGuardName}</p>
                              <p className={styles.shiftMeta}>
                                <span className={styles.mono}>ID {shortGuardId(item.securityGuardId)}</span>
                                <span className={styles.shiftDot} aria-hidden>
                                  ·
                                </span>
                                <span>{item.securityGuardIsActive ? 'Active roster' : 'Inactive guard'}</span>
                              </p>
                            </div>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>

                  <p className={styles.rosterFooter}>
                    <Link className={styles.rosterFooterLink} to="/app/schedules">
                      Open full schedule view
                    </Link>
                  </p>
                </>
              )}
            </section>

            <section className={styles.shortcutsSection} aria-label="Quick navigation">
              <h2 className={styles.sectionTitle}>Shortcuts</h2>
              <div className={styles.shortcutsGrid}>
                <Link className={styles.shortcut} to="/app/security-guards">
                  <span className={`material-symbols-outlined ${styles.shortcutIcon}`}>shield_person</span>
                  <span className={styles.shortcutLabel}>Guards</span>
                </Link>
                <Link className={styles.shortcut} to="/app/unavailable-days">
                  <span className={`material-symbols-outlined ${styles.shortcutIcon}`}>event_available</span>
                  <span className={styles.shortcutLabel}>Availability</span>
                </Link>
                <Link className={styles.shortcut} to="/app/schedules">
                  <span className={`material-symbols-outlined ${styles.shortcutIcon}`}>settings_suggest</span>
                  <span className={styles.shortcutLabel}>Schedules</span>
                </Link>
              </div>
            </section>
          </>
        )}
      </main>
    </div>
  )
}
