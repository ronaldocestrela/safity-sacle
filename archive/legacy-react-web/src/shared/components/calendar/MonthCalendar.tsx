import type { ReactNode } from 'react'

import type { MonthGridCell } from './monthGrid'
import fallbackStyles from './MonthCalendar.module.css'

export type MonthCalendarMonthNavProps = {
  title: string
  onPrevMonth: () => void
  onNextMonth: () => void
  headClassName: string
  titleClassName: string
  navButtonClassName: string
  /** Appended with `material-symbols-outlined` on chevron icons */
  navIconClassName?: string
  prevAriaLabel?: string
  nextAriaLabel?: string
}

export type MonthCalendarProps = {
  cells: MonthGridCell[]
  weekdays: readonly string[]
  cardClassName: string
  weekdayRowClassName: string
  weekdayCellClassName: string
  gridClassName: string
  renderCell: (cell: MonthGridCell) => ReactNode
  topContent?: ReactNode
  monthNav?: MonthCalendarMonthNavProps
  /** When set, replaces the day grid (e.g. loading / error panels) */
  gridReplacement?: ReactNode
}

export function MonthCalendar({
  cells,
  weekdays,
  cardClassName,
  weekdayRowClassName,
  weekdayCellClassName,
  gridClassName,
  renderCell,
  topContent,
  monthNav,
  gridReplacement,
}: MonthCalendarProps) {
  const navIconFallback = fallbackStyles.defaultNavIcon

  return (
    <div className={cardClassName}>
      {topContent}

      {monthNav ? (
        <div className={monthNav.headClassName}>
          <button
            type="button"
            className={monthNav.navButtonClassName}
            aria-label={monthNav.prevAriaLabel ?? 'Previous month'}
            onClick={monthNav.onPrevMonth}
          >
            <span
              className={`material-symbols-outlined ${monthNav.navIconClassName ?? navIconFallback}`}
            >
              chevron_left
            </span>
          </button>
          <h2 className={monthNav.titleClassName}>{monthNav.title}</h2>
          <button
            type="button"
            className={monthNav.navButtonClassName}
            aria-label={monthNav.nextAriaLabel ?? 'Next month'}
            onClick={monthNav.onNextMonth}
          >
            <span
              className={`material-symbols-outlined ${monthNav.navIconClassName ?? navIconFallback}`}
            >
              chevron_right
            </span>
          </button>
        </div>
      ) : null}

      <div className={weekdayRowClassName}>
        {weekdays.map((w, i) => (
          <span key={`${w}-${i.toString()}`} className={weekdayCellClassName}>
            {w}
          </span>
        ))}
      </div>

      {gridReplacement ?? <div className={gridClassName}>{cells.map((cell) => renderCell(cell))}</div>}
    </div>
  )
}
