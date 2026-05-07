import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../shared/auth/useAuth'
import styles from './AppLayout.module.css'

export function AppLayout() {
  const { session, logout } = useAuth()
  const isAdmin = session?.roles.includes('Admin')
  const primaryRole = session?.roles.includes('Admin') ? 'Admin' : 'Supervisor'

  return (
    <div className={styles.shell}>
      <aside className={styles.sidebar} aria-label="Navegação principal">
        <div className={styles.brand}>SafetyScale</div>
        <nav className={styles.nav}>
          <NavLink className={navClass} to="/app" end>
            Início
          </NavLink>
          <>
            <NavLink className={navClass} to="/app/security-guards">
              Seguranças
            </NavLink>
            {isAdmin ? (
              <NavLink className={navClass} to="/app/unavailable-days">
                Indisponibilidades
              </NavLink>
            ) : (
              <span className={`${styles.navItem} ${styles.navItemLocked}`} title="Requer perfil Admin">
                Indisponibilidades
              </span>
            )}
          </>
          <NavLink className={navClass} to="/app/schedules">
            Escalas
          </NavLink>
        </nav>
      </aside>
      <div className={styles.mainColumn}>
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
        <main className={styles.main}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}

function navClass({ isActive }: { isActive: boolean }): string {
  return isActive ? `${styles.navLink} ${styles.navLinkActive}` : styles.navLink
}
