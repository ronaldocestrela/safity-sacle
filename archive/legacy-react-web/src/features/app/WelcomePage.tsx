/**
 * Operational dashboard — UI aligned to Google Stitch "Dashboard Operacional SafetyScale".
 * Reference: projects/9334796298126275303/screens/973c4f6064054619a1742a14796ea5eb
 * Design system: Sentinel Command (see agents.md).
 */
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { AppHeader } from '../../shared/components/AppHeader/AppHeader'
import { MonthCalendar } from '../../shared/components/calendar/MonthCalendar'
import { WEEKDAY_LABELS_SMTWTFS, buildMonthGrid, todayKeyLocal } from '../../shared/components/calendar/monthGrid'
import { useAuth } from '../../shared/auth/useAuth'
import { ApiError as GuardsApiError, listSecurityGuards } from '../security-guards/securityGuardsApi'
import type { SecurityGuardDto } from '../security-guards/types'
import { ApiError as SchedulesApiError, getScheduleByMonthYear } from '../schedules/schedulesApi'
import type { MonthlyScheduleDto, ScheduleItemDto } from '../schedules/types'
import styles from './WelcomePage.module.css'

function pad2(n: number): string {
  return String(n).padStart(2, '0')
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
        a.sectorName.localeCompare(b.sectorName, undefined, { sensitivity: 'base' }) ||
        a.securityGuardName.localeCompare(b.securityGuardName, undefined, { sensitivity: 'base' }) ||
        a.id.localeCompare(b.id),
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
      <AppHeader title="SentryOps" email={session?.email} showNotifications showLogout onLogout={logout} />

      <main className={styles.main}>
        {guardsError ? (
          <div role="alert" className={`${styles.banner} ${styles.bannerError}`}>
            <span>{guardsError}</span>
            <button type="button" className={styles.retryBtn} onClick={() => void refresh()}>
              Tentar novamente
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
              Snapshot operacional para <strong>{periodLabel}</strong>. As KPIs usam dados da API em tempo real; os agendamentos podem estar ausentes
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
                Rotação mensal atual
              </h2>
              {!schedule ? (
                <div className={styles.emptyCard}>
                  <span className={`material-symbols-outlined ${styles.emptyIcon}`} aria-hidden>
                    event_busy
                  </span>
                  <p className={styles.emptyTitle}>Não há agendamentos para {periodLabel}</p>
                  <p className={styles.emptySub}>
                    {isAdmin
                      ? 'Gere a rotação mensal a partir de Agendamentos quando seu time estiver pronto.'
                      : 'Ask an administrator to generate the schedule, or open Schedules to review when available.'}
                  </p>
                  <Link
                    className={isAdmin ? styles.ctaPrimary : styles.ctaSecondary}
                    to="/app/schedules"
                  >
                    {isAdmin ? 'Abrir agendamentos para gerar' : 'Visualizar agendamentos'}
                  </Link>
                </div>
              ) : (
                <>
                  <MonthCalendar
                    cells={calendarCells}
                    weekdays={WEEKDAY_LABELS_SMTWTFS}
                    cardClassName={styles.calendarCard}
                    weekdayRowClassName={styles.weekdayRow}
                    weekdayCellClassName={styles.weekdayCell}
                    gridClassName={styles.calendarGrid}
                    topContent={
                      <p className={styles.calendarHint}>
                        Toque em um dia para ver quem está agendado. Os pontos marcam dias com cobertura.
                      </p>
                    }
                    renderCell={(cell) => {
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
                    }}
                  />

                  <div
                    className={styles.dayDetail}
                    role="region"
                    aria-labelledby="day-detail-heading"
                  >
                    <h3 id="day-detail-heading" className={styles.dayDetailTitle}>
                      {selectedDateKey ? displayAssignmentDate(selectedDateKey) : 'Selecione um dia'}
                    </h3>
                    {selectedDateKey && selectedDayItems.some((i) => i.isWeekend) ? (
                      <p className={styles.dayDetailWeekend} data-testid="weekend-day-hint">
                        Turno de final de semana
                      </p>
                    ) : null}
                    {!selectedDateKey ? (
                      <p className={styles.dayDetailEmpty}>Escolha um dia no calendário.</p>
                    ) : selectedDayItems.length === 0 ? (
                      <p className={styles.dayDetailEmpty}>Não há agendamentos para este dia.</p>
                    ) : (
                      <ul className={styles.shiftList}>
                        {selectedDayItems.map((item) => (
                          <li key={item.id} className={styles.shiftCard}>
                            <span className={styles.shiftStripe} aria-hidden />
                            <div className={styles.shiftBody}>
                              <p className={styles.shiftName}>{item.securityGuardName}</p>
                              <p className={styles.shiftSector}>{item.sectorName}</p>
                              <p className={styles.shiftMeta}>
                                <span className={styles.mono}>ID {shortGuardId(item.securityGuardId)}</span>
                                <span className={styles.shiftDot} aria-hidden>
                                  ·
                                </span>
                                <span>{item.securityGuardIsActive ? 'Rotação ativa' : 'Segurança inativa'}</span>
                              </p>
                            </div>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>

                  <p className={styles.rosterFooter}>
                    <Link className={styles.rosterFooterLink} to="/app/schedules">
                      Abrir visualização completa de agendamentos
                    </Link>
                  </p>
                </>
              )}
            </section>

            <section className={styles.shortcutsSection} aria-label="Navegação rápida">
              <h2 className={styles.sectionTitle}>Atalhos</h2>
              <div className={styles.shortcutsGrid}>
                <Link className={styles.shortcut} to="/app/sectors">
                  <span className={`material-symbols-outlined ${styles.shortcutIcon}`}>map</span>
                  <span className={styles.shortcutLabel}>Setores</span>
                </Link>
                <Link className={styles.shortcut} to="/app/security-guards">
                  <span className={`material-symbols-outlined ${styles.shortcutIcon}`}>shield_person</span>
                  <span className={styles.shortcutLabel}>Seguranças</span>
                </Link>
                <Link className={styles.shortcut} to="/app/unavailable-days">
                  <span className={`material-symbols-outlined ${styles.shortcutIcon}`}>event_available</span>
                  <span className={styles.shortcutLabel}>Disponibilidade</span>
                </Link>
                <Link className={styles.shortcut} to="/app/schedules">
                  <span className={`material-symbols-outlined ${styles.shortcutIcon}`}>settings_suggest</span>
                  <span className={styles.shortcutLabel}>Agendamentos</span>
                </Link>
              </div>
            </section>
          </>
        )}
      </main>
    </div>
  )
}
