import { type FormEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { AppHeader } from '../../shared/components/AppHeader/AppHeader'
import { MonthCalendar } from '../../shared/components/calendar/MonthCalendar'
import { WEEKDAY_LABELS_SMTWTFS, buildMonthGrid } from '../../shared/components/calendar/monthGrid'
import { useAuth } from '../../shared/auth/useAuth'
import { ApiError as GuardsApiError, listSecurityGuards } from '../security-guards/securityGuardsApi'
import type { SecurityGuardDto } from '../security-guards/types'
import {
  ApiError,
  addUnavailableDay,
  listUnavailableDays,
  removeUnavailableDay,
} from './unavailableDaysApi'
import type { UnavailableDayDto } from './types'
import styles from './UnavailableDaysPage.module.css'

type PendingMap = Record<string, 'add' | 'remove'>

function displayGuardHandle(id: string): string {
  const alnum = id.replace(/[^a-zA-Z0-9]/g, '').toUpperCase()
  const core = (alnum + '0000').slice(0, 4)
  return core
}

function guardOptionLabel(g: SecurityGuardDto): string {
  return `${g.name} (ID: ${displayGuardHandle(g.id)})`
}

function baselineMap(items: UnavailableDayDto[]): Map<string, string> {
  const m = new Map<string, string>()
  for (const u of items) {
    m.set(u.date, u.id)
  }
  return m
}

function effectiveUnavailable(key: string, base: Map<string, string>, pending: PendingMap): boolean {
  const p = pending[key]
  if (p === 'add') {
    return true
  }
  if (p === 'remove') {
    return false
  }
  return base.has(key)
}

function togglePending(key: string, base: Map<string, string>, pending: PendingMap): PendingMap {
  const eff = effectiveUnavailable(key, base, pending)
  const next: PendingMap = { ...pending }

  if (eff) {
    if (base.has(key)) {
      next[key] = 'remove'
    } else {
      delete next[key]
    }
  } else if (base.has(key) && next[key] === 'remove') {
    delete next[key]
  } else {
    next[key] = 'add'
  }

  return next
}

export function UnavailableDaysPage() {
  const { session } = useAuth()
  const isAdmin = Boolean(session?.roles.includes('Admin'))
  const email = session?.email

  const now = useMemo(() => new Date(), [])
  const [viewYear, setViewYear] = useState(now.getFullYear())
  const [viewMonth0, setViewMonth0] = useState(now.getMonth())

  const [guards, setGuards] = useState<SecurityGuardDto[]>([])
  const [guardsLoading, setGuardsLoading] = useState(true)
  const [guardsError, setGuardsError] = useState<string | null>(null)

  const [selectedGuardId, setSelectedGuardId] = useState<string>('')

  const [serverDays, setServerDays] = useState<UnavailableDayDto[]>([])
  const [daysLoading, setDaysLoading] = useState(false)
  const [daysError, setDaysError] = useState<string | null>(null)

  const [pending, setPending] = useState<PendingMap>({})
  const [focusKey, setFocusKey] = useState<string | null>(null)

  const [reasonDraft, setReasonDraft] = useState('')

  const [banner, setBanner] = useState<{ kind: 'success' | 'error'; message: string } | null>(null)
  const [saving, setSaving] = useState(false)

  const base = useMemo(() => baselineMap(serverDays), [serverDays])
  const loadGuardsToken = useRef(0)
  const loadDaysToken = useRef(0)

  const refreshGuards = useCallback(async () => {
    const token = ++loadGuardsToken.current
    setGuardsLoading(true)
    setGuardsError(null)
    try {
      const list = await listSecurityGuards(undefined)
      if (loadGuardsToken.current !== token) {
        return
      }
      setGuards(list)
      setSelectedGuardId((cur) => {
        if (cur && list.some((g) => g.id === cur)) {
          return cur
        }
        return list[0]?.id ?? ''
      })
    } catch (e: unknown) {
      if (loadGuardsToken.current !== token) {
        return
      }
      if (e instanceof GuardsApiError) {
        setGuardsError(e.message)
      } else {
        setGuardsError('Could not load personnel.')
      }
    } finally {
      if (loadGuardsToken.current === token) {
        setGuardsLoading(false)
      }
    }
  }, [])

  const refreshDays = useCallback(async (guardId: string) => {
    if (!guardId) {
      setServerDays([])
      return
    }
    const token = ++loadDaysToken.current
    setDaysLoading(true)
    setDaysError(null)
    try {
      const list = await listUnavailableDays(guardId)
      if (loadDaysToken.current !== token) {
        return
      }
      setServerDays(list)
    } catch (e: unknown) {
      if (loadDaysToken.current !== token) {
        return
      }
      if (e instanceof ApiError) {
        setDaysError(e.message)
        setServerDays([])
      } else {
        setDaysError('Could not load restrictions.')
        setServerDays([])
      }
    } finally {
      if (loadDaysToken.current === token) {
        setDaysLoading(false)
      }
    }
  }, [])

  useEffect(() => {
    queueMicrotask(() => {
      void refreshGuards()
    })
  }, [refreshGuards])

  useEffect(() => {
    queueMicrotask(() => {
      void refreshDays(selectedGuardId)
    })
  }, [selectedGuardId, refreshDays])

  const grid = useMemo(() => buildMonthGrid(viewYear, viewMonth0), [viewYear, viewMonth0])

  const monthTitle = useMemo(
    () =>
      new Date(viewYear, viewMonth0, 1).toLocaleString('en-US', {
        month: 'long',
        year: 'numeric',
      }),
    [viewYear, viewMonth0],
  )

  const hasPending = Object.keys(pending).length > 0

  function shiftMonth(delta: number): void {
    const d = new Date(viewYear, viewMonth0 + delta, 1)
    setViewYear(d.getFullYear())
    setViewMonth0(d.getMonth())
  }

  function handleDayClick(key: string): void {
    if (!isAdmin || !selectedGuardId || daysLoading) {
      return
    }
    setFocusKey(key)
    setPending((p) => togglePending(key, base, p))
    setBanner(null)
  }

  async function handleSave(e: FormEvent): Promise<void> {
    e.preventDefault()
    if (!isAdmin || !selectedGuardId || !hasPending) {
      return
    }
    setSaving(true)
    setBanner(null)
    const reason = reasonDraft.trim() ? reasonDraft.trim() : null
    const adds: string[] = []
    const removes: string[] = []
    for (const [k, v] of Object.entries(pending)) {
      if (v === 'add') {
        adds.push(k)
      } else if (v === 'remove') {
        const id = base.get(k)
        if (id) {
          removes.push(id)
        }
      }
    }
    try {
      for (const id of removes) {
        await removeUnavailableDay(id)
      }
      for (const date of adds) {
        await addUnavailableDay(selectedGuardId, { date, reason })
      }
      setPending({})
      setReasonDraft('')
      setBanner({ kind: 'success', message: 'Restrictions saved.' })
      await refreshDays(selectedGuardId)
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setBanner({ kind: 'error', message: err.message })
      } else {
        setBanner({ kind: 'error', message: 'Save failed.' })
      }
      await refreshDays(selectedGuardId)
      setPending({})
    } finally {
      setSaving(false)
    }
  }

  const showCalendar = Boolean(selectedGuardId) && !guardsError

  return (
    <div className={styles.page}>
      <AppHeader title="Availability" email={email} showNotifications />

      <main className={styles.main}>
        <div className={styles.section}>
          {banner?.kind === 'error' ? (
            <div className={styles.bannerError} role="alert">
              {banner.message}
            </div>
          ) : null}
          {banner?.kind === 'success' ? (
            <div className={styles.bannerSuccess} role="status">
              {banner.message}
            </div>
          ) : null}

          <div className={styles.field}>
            <label className={styles.labelCaps} htmlFor="personnel-select">
              Select Personnel
            </label>
            <div className={styles.selectWrap}>
              <span className={styles.selectIconLeft}>
                <span className={`${styles.materialIcon} material-symbols-outlined`}>search</span>
              </span>
              <select
                id="personnel-select"
                className={styles.select}
                value={selectedGuardId}
                onChange={(ev) => {
                  const id = ev.target.value
                  setPending({})
                  setFocusKey(null)
                  setBanner(null)
                  setSelectedGuardId(id)
                }}
                disabled={guardsLoading || guards.length === 0}
              >
                {guards.length === 0 ? (
                  <option value="">No personnel</option>
                ) : (
                  guards.map((g) => (
                    <option key={g.id} value={g.id}>
                      {guardOptionLabel(g)}
                    </option>
                  ))
                )}
              </select>
              <span className={styles.selectIconRight}>
                <span className={`${styles.materialIcon} material-symbols-outlined`}>arrow_drop_down</span>
              </span>
            </div>
          </div>

          {guardsLoading ? (
            <p className={styles.spinnerText}>Loading personnel…</p>
          ) : null}

          {guardsError ? (
            <div className={styles.empty} role="alert">
              {guardsError}
              <div>
                <button type="button" className={styles.retryBtn} onClick={() => void refreshGuards()}>
                  Retry
                </button>
              </div>
            </div>
          ) : null}

          {!guardsLoading && guards.length === 0 ? (
            <p className={styles.empty}>No personnel found.</p>
          ) : null}

          {showCalendar ? (
            <>
              <MonthCalendar
                cells={grid}
                weekdays={WEEKDAY_LABELS_SMTWTFS}
                cardClassName={styles.calendarCard}
                weekdayRowClassName={styles.weekdayRow}
                weekdayCellClassName={styles.weekday}
                gridClassName={styles.grid}
                monthNav={{
                  title: monthTitle,
                  onPrevMonth: () => shiftMonth(-1),
                  onNextMonth: () => shiftMonth(1),
                  headClassName: styles.calendarHead,
                  titleClassName: styles.monthTitle,
                  navButtonClassName: styles.calNavBtn,
                  navIconClassName: styles.materialIcon,
                  prevAriaLabel: 'Previous month',
                  nextAriaLabel: 'Next month',
                }}
                gridReplacement={
                  daysLoading ? (
                    <p className={styles.spinnerText} style={{ padding: '1rem' }}>
                      Loading calendar…
                    </p>
                  ) : daysError ? (
                    <div className={styles.empty} role="alert">
                      {daysError}
                      <div>
                        <button
                          type="button"
                          className={styles.retryBtn}
                          onClick={() => void refreshDays(selectedGuardId)}
                        >
                          Retry
                        </button>
                      </div>
                    </div>
                  ) : undefined
                }
                renderCell={(cell) => {
                  const unav = effectiveUnavailable(cell.key, base, pending)
                  const focused = focusKey === cell.key && !unav

                  const className = [
                    styles.cell,
                    !cell.inMonth && styles.cellMuted,
                    cell.inMonth && isAdmin && !daysLoading && !daysError && styles.cellClickable,
                    unav && styles.cellUnavailable,
                    focused && styles.cellFocus,
                  ]
                    .filter(Boolean)
                    .join(' ')

                  return (
                    <button
                      key={`${cell.key}-${cell.label}-${cell.inMonth}`}
                      type="button"
                      className={className}
                      disabled={!isAdmin || daysLoading || Boolean(daysError)}
                      aria-pressed={unav}
                      aria-label={cell.key}
                      onClick={() => handleDayClick(cell.key)}
                    >
                      <span className={styles.dayNum}>{cell.label}</span>
                      {unav ? <span className={styles.unavailTag}>UNAVAIL</span> : null}
                      {focused ? <span className={styles.focusDot} aria-hidden /> : null}
                    </button>
                  )
                }}
              />

              <div className={styles.infoBox}>
                <span className={`${styles.materialIcon} ${styles.infoIcon} material-symbols-outlined`}>
                  info
                </span>
                <p className={styles.infoText}>
                  Tapping a date will toggle it as <strong style={{ color: '#ba1a1a' }}>Unavailable</strong>. These
                  restrictions will automatically prevent scheduling this personnel for any shifts on the selected
                  days.
                </p>
              </div>

              {isAdmin ? (
                <div className={styles.field}>
                  <label className={styles.labelCaps} htmlFor="reason-optional">
                    Reason (optional)
                  </label>
                  <textarea
                    id="reason-optional"
                    className={styles.reasonInput}
                    maxLength={250}
                    value={reasonDraft}
                    onChange={(ev) => setReasonDraft(ev.target.value)}
                    placeholder="Applied to newly saved unavailable days."
                  />
                </div>
              ) : null}
            </>
          ) : null}
        </div>
      </main>

      {isAdmin && showCalendar && !daysError ? (
        <div className={styles.saveBar}>
          <form onSubmit={(e) => void handleSave(e)}>
            <button className={styles.saveBtn} type="submit" disabled={saving || !hasPending}>
              <span
                className={`${styles.materialIcon} material-symbols-outlined`}
                style={{ fontVariationSettings: "'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24" }}
              >
                save
              </span>
              SAVE RESTRICTIONS
            </button>
          </form>
        </div>
      ) : null}
    </div>
  )
}
