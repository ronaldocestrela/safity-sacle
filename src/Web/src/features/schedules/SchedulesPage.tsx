import { type FormEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useAuth } from '../../shared/auth/useAuth'
import { ApiError, generateSchedule as generateScheduleRequest, getScheduleByMonthYear } from './schedulesApi'
import type { MonthlyScheduleDto } from './types'
import styles from './SchedulesPage.module.css'

/** Stitch "Regras de Escala" — portrait asset */
const STITCH_SUPERVISOR_AVATAR =
  'https://lh3.googleusercontent.com/aida-public/AB6AXuB3nCVQo7BaVjs8Plm0dqDT-E72JA3LPXwRXpOy29LCTHdvE1U5-EfUrZ-8tZimS_qhsHFF3tbZ6vUGALNuRIHq8a5kzFrh0VuT0tqtWlBDhJf5B5Sz-BuS67N70iHrMJCJ41gO9PFGy0Ga9TSdJapjQQgFQDMHxQkrg7PRBL7RegsZqWDyLHaWh-oG6vUTddUnGkCKhX-8Evg0_qkcqk07E1eOADjs_HqJVISLP6Auwqyg11YgZnaa2ozlYUht-0ThG-nIE8lY9UU'

const MONTH_OPTIONS = [
  { value: 1, label: 'January' },
  { value: 2, label: 'February' },
  { value: 3, label: 'March' },
  { value: 4, label: 'April' },
  { value: 5, label: 'May' },
  { value: 6, label: 'June' },
  { value: 7, label: 'July' },
  { value: 8, label: 'August' },
  { value: 9, label: 'September' },
  { value: 10, label: 'October' },
  { value: 11, label: 'November' },
  { value: 12, label: 'December' },
] as const

function initialMonthYear(): { month: number; year: number } {
  const d = new Date()
  return { month: d.getMonth() + 1, year: d.getFullYear() }
}

function formatGeneratedAt(iso: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) {
    return '—'
  }
  const y = d.getFullYear()
  const mo = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  const h = String(d.getHours()).padStart(2, '0')
  const mi = String(d.getMinutes()).padStart(2, '0')
  return `${y}.${mo}.${day} ${h}:${mi}`
}

function displayDateLabel(iso: string): string {
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

function validatePeriod(month: number, year: number): string | null {
  if (!Number.isInteger(month) || month < 1 || month > 12) {
    return 'Month must be between 1 and 12.'
  }
  if (!Number.isInteger(year) || year < 2000 || year > 2100) {
    return 'Year must be between 2000 and 2100.'
  }
  return null
}

export function SchedulesPage() {
  const { session } = useAuth()
  const isAdmin = Boolean(session?.roles.includes('Admin'))
  const loadTokenRef = useRef(0)

  const [{ month, year }, setPeriod] = useState(initialMonthYear)
  const [periodTouched, setPeriodTouched] = useState(false)

  const [schedule, setSchedule] = useState<MonthlyScheduleDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [generating, setGenerating] = useState(false)
  const [banner, setBanner] = useState<{ kind: 'success' | 'error'; message: string } | null>(null)

  const [weekendBalancing, setWeekendBalancing] = useState(true)
  const [prefStrictOvertime, setPrefStrictOvertime] = useState(true)
  const [prefSeniority, setPrefSeniority] = useState(false)
  const [prefManualOverride, setPrefManualOverride] = useState(true)

  const periodError = useMemo(() => (periodTouched ? validatePeriod(month, year) : null), [month, year, periodTouched])

  const loadSchedule = useCallback(async (m: number, y: number) => {
    const err = validatePeriod(m, y)
    if (err) {
      setBanner({ kind: 'error', message: err })
      setSchedule(null)
      return
    }

    const token = ++loadTokenRef.current
    setLoading(true)
    setBanner(null)
    try {
      const data = await getScheduleByMonthYear(m, y)
      if (loadTokenRef.current !== token) {
        return
      }
      setSchedule(data)
      if (!data) {
        setBanner({
          kind: 'error',
          message: `No schedule found for ${m.toString().padStart(2, '0')}/${y}.`,
        })
      }
    } catch (e: unknown) {
      if (loadTokenRef.current !== token) {
        return
      }
      setSchedule(null)
      const msg = e instanceof ApiError ? e.message : 'Could not load roster.'
      setBanner({ kind: 'error', message: msg })
    } finally {
      if (loadTokenRef.current === token) {
        setLoading(false)
      }
    }
  }, [])

  useEffect(() => {
    queueMicrotask(() => {
      void loadSchedule(month, year)
    })
    // Initial fetch only; period changes use the form actions.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- mount
  }, [])

  const onSubmitPeriod = (ev: FormEvent) => {
    ev.preventDefault()
    setPeriodTouched(true)
    const err = validatePeriod(month, year)
    if (err) {
      setBanner({ kind: 'error', message: err })
      return
    }
    void loadSchedule(month, year)
  }

  const onGenerate = async () => {
    setPeriodTouched(true)
    const err = validatePeriod(month, year)
    if (err) {
      setBanner({ kind: 'error', message: err })
      return
    }
    setGenerating(true)
    setBanner(null)
    try {
      await generateScheduleRequest(month, year)
      setBanner({ kind: 'success', message: 'Monthly schedule generated successfully.' })
      await loadSchedule(month, year)
    } catch (e: unknown) {
      const msg = e instanceof ApiError ? e.message : 'Could not generate schedule.'
      setBanner({ kind: 'error', message: msg })
    } finally {
      setGenerating(false)
    }
  }

  const scrollToAssignments = () => {
    document.getElementById('assignments-section')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <div className={styles.headerLeft}>
          <div className={styles.avatarWrap}>
            <img className={styles.avatarImg} src={STITCH_SUPERVISOR_AVATAR} alt="" />
          </div>
          <h1 className={styles.brandTitle}>SentryOps Management</h1>
        </div>
        <div className={styles.headerLeft}>
          <button type="button" className={styles.iconBtn} aria-label="Notifications">
            <span className={`${styles.materialIcon} material-symbols-outlined`}>notifications</span>
          </button>
        </div>
      </header>

      <main className={styles.main}>
        <section className={styles.sectionIntro} aria-labelledby="sched-rules-heading">
          <h2 id="sched-rules-heading" className={styles.displayTitle}>
            Scheduling Rules
          </h2>
          <p className={styles.subtitle}>
            Configure the algorithmic parameters for automated personnel distribution across Sector 7 posts.
          </p>
        </section>

        {banner ? (
          <div
            role="alert"
            className={`${styles.alert} ${banner.kind === 'error' ? styles.alertError : styles.alertSuccess}`}
          >
            {banner.message}
          </div>
        ) : null}

        <div className={styles.grid}>
          <div className={styles.card}>
            <div className={styles.cardRow}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
                <div className={styles.engineAccent} />
                <div>
                  <span className={styles.labelCaps}>Engine Status</span>
                  <p className={styles.headlineSm} style={{ margin: '4px 0 0' }}>
                    {loading ? 'Synchronizing…' : 'Active Optimization'}
                  </p>
                </div>
              </div>
              <div className={styles.pillDark}>SYSTEM V2.4.0</div>
            </div>
          </div>

          <div className={styles.card}>
            <div className={styles.cardHeaderRow}>
              <span className={`${styles.materialIcon} material-symbols-outlined`}>calendar_month</span>
              <h3 className={styles.headlineSm} style={{ margin: 0 }}>
                Target period &amp; roster
              </h3>
            </div>
            <form onSubmit={onSubmitPeriod}>
              <div className={styles.fieldGrid}>
                <div className={styles.field}>
                  <label className={`${styles.labelCaps} ${styles.labelCapsMuted}`} htmlFor="schedule-month">
                    Month
                  </label>
                  <select
                    id="schedule-month"
                    className={styles.select}
                    value={month}
                    onChange={(e) => setPeriod((p) => ({ ...p, month: Number(e.target.value) }))}
                  >
                    {MONTH_OPTIONS.map((o) => (
                      <option key={o.value} value={o.value}>
                        {o.label}
                      </option>
                    ))}
                  </select>
                </div>
                <div className={styles.field}>
                  <label className={`${styles.labelCaps} ${styles.labelCapsMuted}`} htmlFor="schedule-year">
                    Year
                  </label>
                  <input
                    id="schedule-year"
                    className={styles.input}
                    type="number"
                    min={2000}
                    max={2100}
                    value={year}
                    onChange={(e) => setPeriod((p) => ({ ...p, year: Number(e.target.value) }))}
                  />
                </div>
              </div>
              {periodError ? (
                <p className={styles.fieldError} role="status">
                  {periodError}
                </p>
              ) : null}
              <div className={styles.btnRow}>
                <button type="submit" className={styles.btnSecondary} disabled={loading}>
                  {loading ? 'Loading…' : 'Load roster'}
                </button>
                {isAdmin ? (
                  <button
                    type="button"
                    className={styles.btnPrimary}
                    disabled={generating || loading}
                    onClick={() => void onGenerate()}
                  >
                    {generating ? 'Generating…' : 'Generate schedule'}
                  </button>
                ) : null}
              </div>
            </form>
          </div>

          <div className={styles.card}>
            <div>
              <div className={styles.cardHeaderRow}>
                <span className={`${styles.materialIcon} material-symbols-outlined`}>balance</span>
                <h3 className={styles.headlineSm} style={{ margin: 0 }}>
                  Weekend balancing
                </h3>
              </div>
              <p className={styles.bodyText}>
                Evenly distribute Saturday and Sunday rotations across all active personnel.
              </p>
            </div>
            <div className={styles.toggleShell}>
              <span className={styles.bodyText} style={{ margin: 0, fontWeight: 700 }}>
                {weekendBalancing ? 'Enabled' : 'Disabled'}
              </span>
              <label className={styles.switch}>
                <input
                  type="checkbox"
                  checked={weekendBalancing}
                  onChange={(e) => setWeekendBalancing(e.target.checked)}
                />
                <span className={styles.slider} />
              </label>
            </div>
          </div>

          <div className={styles.card} id="assignments-section">
            <div className={styles.assignmentsHead}>
              <span className={`${styles.materialIcon} material-symbols-outlined`}>bedtime</span>
              <h3 className={styles.headlineSm} style={{ margin: 0 }}>
                Monthly assignments
              </h3>
            </div>
            <p className={styles.hint}>
              Regulatory standards recommend validating roster coverage before publishing to Sector 7 operations.
            </p>
            <div className={styles.infoBox}>
              <span className={`${styles.materialIcon} material-symbols-outlined`} style={{ color: '#fd8b00' }}>
                info
              </span>
              <p>
                Live data from the SafetyScale API. Items preserve historical guard names even if a guard is now
                inactive.
              </p>
            </div>
            <div className={styles.listScroll} aria-busy={loading} aria-label="Assignment list">
              {loading ? (
                <div className={styles.emptyList}>Loading assignments…</div>
              ) : schedule && schedule.items.length > 0 ? (
                schedule.items.map((item) => (
                  <div key={item.id} className={styles.listRow}>
                    <div
                      className={`${styles.listStripe} ${item.isWeekend ? styles.listStripeWeekend : ''}`}
                      aria-hidden
                    />
                    <div className={styles.rowMain}>
                      <span className={styles.rowMeta}>ASSIGNMENT</span>
                      <p className={styles.rowName}>{item.securityGuardName || '—'}</p>
                    </div>
                    <div className={styles.rowDate}>{displayDateLabel(item.date)}</div>
                    <div className={styles.badges}>
                      {item.isWeekend ? <span className={`${styles.badge} ${styles.badgeWeekend}`}>Weekend</span> : null}
                      {!item.securityGuardIsActive ? (
                        <span className={`${styles.badge} ${styles.badgeInactive}`}>Inactive</span>
                      ) : null}
                    </div>
                  </div>
                ))
              ) : (
                <div className={styles.emptyList}>
                  {schedule && schedule.items.length === 0
                    ? 'No assignment rows for this schedule.'
                    : 'Load a generated month to see assignments.'}
                </div>
              )}
            </div>
          </div>

          <div className={styles.splitBottom}>
            <div className={`${styles.card} ${styles.prefCol}`}>
              <span
                className={`${styles.labelCaps} ${styles.labelCapsMuted}`}
                style={{ display: 'block', marginBottom: 16 }}
              >
                PREFERENCES
              </span>
              <button
                type="button"
                className={styles.prefRow}
                onClick={() => setPrefStrictOvertime((v) => !v)}
                style={{ width: '100%', background: 'none', border: 'none', cursor: 'pointer', textAlign: 'left' }}
              >
                <span>Strict Overtime Cap</span>
                <span
                  className={`material-symbols-outlined ${styles.materialIconFill}`}
                  style={{ color: prefStrictOvertime ? '#fd8b00' : '#74777d' }}
                >
                  {prefStrictOvertime ? 'toggle_on' : 'toggle_off'}
                </span>
              </button>
              <button
                type="button"
                className={styles.prefRow}
                onClick={() => setPrefSeniority((v) => !v)}
                style={{ width: '100%', background: 'none', border: 'none', cursor: 'pointer', textAlign: 'left' }}
              >
                <span>Seniority Priority</span>
                <span
                  className={`material-symbols-outlined ${styles.materialIconFill}`}
                  style={{ color: prefSeniority ? '#fd8b00' : '#74777d' }}
                >
                  {prefSeniority ? 'toggle_on' : 'toggle_off'}
                </span>
              </button>
              <button
                type="button"
                className={styles.prefRow}
                onClick={() => setPrefManualOverride((v) => !v)}
                style={{ width: '100%', background: 'none', border: 'none', cursor: 'pointer', textAlign: 'left' }}
              >
                <span>Manual Override</span>
                <span
                  className={`material-symbols-outlined ${styles.materialIconFill}`}
                  style={{ color: prefManualOverride ? '#fd8b00' : '#74777d' }}
                >
                  {prefManualOverride ? 'toggle_on' : 'toggle_off'}
                </span>
              </button>
            </div>

            <div className={`${styles.auditCard} ${styles.auditCol}`}>
              <div className={styles.auditLabel}>LAST UPDATE</div>
              <div className={styles.auditRow}>
                <span className={styles.monoHeadline}>
                  {schedule ? formatGeneratedAt(schedule.generatedAt) : '2023.10.24 14:32'}
                </span>
                <div className={styles.auditDivider} />
                <span style={{ fontSize: 14 }}>
                  {schedule ? `Schedule ID · ${schedule.id.slice(0, 8)}…` : 'Admin: K. Jansson'}
                </span>
              </div>
              <button type="button" className={styles.auditLink} onClick={scrollToAssignments}>
                VIEW FULL ASSIGNMENT LIST
                <span className="material-symbols-outlined" style={{ fontSize: 18 }}>
                  arrow_forward
                </span>
              </button>
              <div className={styles.auditWatermark} aria-hidden>
                <span className="material-symbols-outlined">settings_suggest</span>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  )
}
