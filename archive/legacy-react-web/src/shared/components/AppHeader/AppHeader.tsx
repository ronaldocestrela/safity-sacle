import { userInitials } from '../../userDisplay'
import styles from './AppHeader.module.css'

export type AppHeaderProps = {
  title: string
  /** Used for avatar initials when `avatarSrc` is not set. */
  email?: string | null
  avatarSrc?: string
  avatarAlt?: string
  showNotifications?: boolean
  showLogout?: boolean
  onLogout?: () => void
}

export function AppHeader({
  title,
  email,
  avatarSrc,
  avatarAlt = '',
  showNotifications = true,
  showLogout = false,
  onLogout,
}: AppHeaderProps) {
  const initials = userInitials(email)

  return (
    <header className={styles.topAppBar}>
      <div className={styles.topAppBarInner}>
        <div className={styles.topAppBarLeft}>
          {avatarSrc ? (
            <div className={`${styles.avatarRing} ${styles.avatarRingImage}`} aria-hidden>
              <img className={styles.avatarImg} src={avatarSrc} alt={avatarAlt} />
            </div>
          ) : (
            <div className={styles.avatarRing} aria-hidden>
              <span className={styles.avatarInitials}>{initials}</span>
            </div>
          )}
          <h1 className={styles.topAppBarTitle}>{title}</h1>
        </div>
        <div className={styles.topAppBarActions}>
          {showNotifications ? (
            <button type="button" className={styles.iconGhost} aria-label="Notifications">
              <span className={`material-symbols-outlined ${styles.iconMd}`}>notifications</span>
            </button>
          ) : null}
          {showLogout && onLogout ? (
            <button type="button" className={styles.iconGhost} aria-label="Log out" onClick={onLogout}>
              <span className={`material-symbols-outlined ${styles.iconMd}`}>logout</span>
            </button>
          ) : null}
        </div>
      </div>
    </header>
  )
}
