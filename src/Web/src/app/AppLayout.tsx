import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../shared/auth/useAuth'
import styles from './AppLayout.module.css'

const bottomNav = [
  { to: '/app', end: true, label: 'Dashboard', icon: 'calendar_month' as const },
  { to: '/app/security-guards', end: false, label: 'Guards', icon: 'shield_person' as const },
  { to: '/app/unavailable-days', end: false, label: 'Availability', icon: 'event_available' as const },
  { to: '/app/schedules', end: false, label: 'Rules', icon: 'settings_suggest' as const },
]

export function AppLayout() {
  const { session, logout } = useAuth()
  const primaryRole = session?.roles.includes('Admin') ? 'Admin' : 'Supervisor'
  const { pathname } = useLocation()
  const hideShellHeader = pathname === '/app/security-guards'

  return (
    <div className={styles.shell}>
      <div className={styles.mainColumn}>
        {hideShellHeader ? null : (
          <header className={styles.header}>
            <div className={styles.headerSpacer} />
            <div className={styles.headerRight}>
              <span className={styles.email} title={session?.email ?? undefined}>
                {session?.email ?? '—'}
              </span>
              <span className={styles.badge}>{primaryRole}</span>
              <button type="button" className={styles.logout} onClick={logout}>
                Sair
              </button>
            </div>
          </header>
        )}
        <main className={hideShellHeader ? `${styles.main} ${styles.mainFlush}` : styles.main}>
          <Outlet />
        </main>
      </div>

      <nav className={styles.bottomNav} aria-label="Navegação principal">
        {bottomNav.map(({ to, end, label, icon }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            className={({ isActive }) => (isActive ? `${styles.bottomLink} ${styles.bottomLinkActive}` : styles.bottomLink)}
          >
            {({ isActive }) => (
              <>
                <span
                  className={`${styles.materialIcon} material-symbols-outlined`}
                  aria-hidden
                  style={isActive ? { fontVariationSettings: "'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24" } : undefined}
                >
                  {icon}
                </span>
                <span className={styles.bottomLabel}>{label}</span>
              </>
            )}
          </NavLink>
        ))}
      </nav>
    </div>
  )
}
