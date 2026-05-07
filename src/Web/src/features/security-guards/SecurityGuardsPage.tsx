import { type FormEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useAuth } from '../../shared/auth/useAuth'
import {
  ApiError,
  activateSecurityGuard,
  createSecurityGuard,
  inactivateSecurityGuard,
  listSecurityGuards,
  updateSecurityGuard,
} from './securityGuardsApi'
import type { SecurityGuardDto } from './types'
import styles from './SecurityGuardsPage.module.css'

type ChipFilter = 'all' | 'activeOnly' | 'onSite' | 'sector7'

function hashSeed(id: string, salt: string): number {
  const s = id + salt
  let h = 0
  for (let i = 0; i < s.length; i++) {
    h = (h * 31 + s.charCodeAt(i)) | 0
  }
  return Math.abs(h)
}

function displayGuardId(id: string): string {
  const alnum = id.replace(/[^a-zA-Z0-9]/g, '').toUpperCase()
  const core = (alnum + '0000').slice(0, 4)
  return `#SO-${core}`
}

function guardSector(id: string): string {
  const sectors = ['Sector 7', 'Sector 4', 'Sector 2', 'Perimeter'] as const
  return sectors[hashSeed(id, 's') % sectors.length]
}

function guardPost(id: string): string {
  const posts = ['Post Alpha', 'Main Lobby', 'Off Duty', 'Perimeter Post'] as const
  return posts[hashSeed(id, 'p') % posts.length]
}

function chipToActiveQuery(chip: ChipFilter): boolean | undefined {
  if (chip === 'activeOnly' || chip === 'onSite') {
    return true
  }
  return undefined
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

export function SecurityGuardsPage() {
  const { session, logout } = useAuth()
  const isAdmin = Boolean(session?.roles.includes('Admin'))
  const loadTokenRef = useRef(0)

  const [chip, setChip] = useState<ChipFilter>('all')
  const [search, setSearch] = useState('')
  const [items, setItems] = useState<SecurityGuardDto[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)

  const [banner, setBanner] = useState<{ kind: 'success' | 'error'; message: string } | null>(null)

  const [nameFormOpen, setNameFormOpen] = useState<'create' | 'edit' | null>(null)
  const [editing, setEditing] = useState<SecurityGuardDto | null>(null)
  const [nameDraft, setNameDraft] = useState('')
  const [nameTouched, setNameTouched] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [nameFormError, setNameFormError] = useState<string | null>(null)

  const [inactivateTarget, setInactivateTarget] = useState<SecurityGuardDto | null>(null)
  const [inactivateSubmitting, setInactivateSubmitting] = useState(false)

  const refreshList = useCallback(async () => {
    const token = ++loadTokenRef.current
    setLoading(true)
    setLoadError(null)
    try {
      const list = await listSecurityGuards(chipToActiveQuery(chip))
      if (loadTokenRef.current !== token) {
        return
      }
      setItems(list)
    } catch (e: unknown) {
      if (loadTokenRef.current !== token) {
        return
      }
      const fallback = 'Could not load personnel.'
      if (e instanceof ApiError) {
        setLoadError(e.message || fallback)
      } else {
        setLoadError(fallback)
      }
    } finally {
      if (loadTokenRef.current === token) {
        setLoading(false)
      }
    }
  }, [chip])

  useEffect(() => {
    queueMicrotask(() => {
      void refreshList()
    })
  }, [refreshList])

  const displayedRows = useMemo(() => {
    let list = items
    if (chip === 'sector7') {
      list = list.filter((g) => guardSector(g.id) === 'Sector 7')
    }
    const q = search.trim().toLowerCase()
    if (q) {
      list = list.filter(
        (g) => g.name.toLowerCase().includes(q) || displayGuardId(g.id).toLowerCase().includes(q),
      )
    }
    return list
  }, [items, chip, search])

  const nameInvalid = nameTouched && nameDraft.trim() === ''

  function closeNameForm() {
    setNameFormOpen(null)
    setEditing(null)
    setNameDraft('')
    setNameTouched(false)
    setNameFormError(null)
    setSubmitting(false)
  }

  function openCreate() {
    setBanner(null)
    setNameFormOpen('create')
    setEditing(null)
    setNameDraft('')
    setNameTouched(false)
    setNameFormError(null)
  }

  function openEdit(g: SecurityGuardDto) {
    if (!isAdmin) {
      return
    }
    setBanner(null)
    setNameFormOpen('edit')
    setEditing(g)
    setNameDraft(g.name)
    setNameTouched(false)
    setNameFormError(null)
  }

  async function handleSubmitName(e: FormEvent) {
    e.preventDefault()
    setNameTouched(true)
    setNameFormError(null)
    if (!nameDraft.trim()) {
      return
    }

    setSubmitting(true)
    try {
      if (nameFormOpen === 'create') {
        await createSecurityGuard(nameDraft.trim())
        setBanner({ kind: 'success', message: 'Personnel created.' })
      } else if (nameFormOpen === 'edit' && editing) {
        await updateSecurityGuard(editing.id, nameDraft.trim())
        setBanner({ kind: 'success', message: 'Changes saved.' })
      }
      closeNameForm()
      await refreshList()
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setNameFormError(err.message || 'Save failed.')
      } else {
        setNameFormError('Save failed.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  async function confirmInactivate() {
    if (!inactivateTarget) {
      return
    }
    setInactivateSubmitting(true)
    setBanner(null)
    try {
      await inactivateSecurityGuard(inactivateTarget.id)
      setBanner({ kind: 'success', message: 'Personnel deactivated.' })
      setInactivateTarget(null)
      await refreshList()
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setBanner({ kind: 'error', message: err.message })
      } else {
        setBanner({ kind: 'error', message: 'Could not deactivate.' })
      }
    } finally {
      setInactivateSubmitting(false)
    }
  }

  async function handleActivate(guardId: string): Promise<void> {
    setBanner(null)
    try {
      await activateSecurityGuard(guardId)
      setBanner({ kind: 'success', message: 'Personnel activated.' })
      await refreshList()
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setBanner({ kind: 'error', message: err.message })
      } else {
        setBanner({ kind: 'error', message: 'Could not activate.' })
      }
    }
  }

  function dismissBanner() {
    setBanner(null)
  }

  const chips: { id: ChipFilter; label: string }[] = [
    { id: 'all', label: 'All Personnel' },
    { id: 'activeOnly', label: 'Active Only' },
    { id: 'onSite', label: 'On-Site' },
    { id: 'sector7', label: 'Sector 7' },
  ]

  return (
    <div className={styles.page}>
      <header className={styles.topAppBar}>
        <div className={styles.topAppBarInner}>
          <div className={styles.topAppBarLeft}>
            <div className={styles.avatarRing} aria-hidden>
              <span className={styles.avatarInitials}>{userInitials(session?.email ?? undefined)}</span>
            </div>
            <h1 className={styles.topAppBarTitle}>SentryOps Management</h1>
          </div>
          <div className={styles.topAppBarActions}>
            <button type="button" className={styles.iconGhost} aria-label="Notifications">
              <span className={`material-symbols-outlined ${styles.iconMd}`}>notifications</span>
            </button>
            <button type="button" className={styles.iconGhost} aria-label="Log out" onClick={logout}>
              <span className={`material-symbols-outlined ${styles.iconMd}`}>logout</span>
            </button>
          </div>
        </div>
      </header>

      <div className={styles.stickySearch}>
        <div className={styles.searchField}>
          <span className={`material-symbols-outlined ${styles.searchIcon}`}>search</span>
          <input
            className={styles.searchInput}
            type="search"
            placeholder="Search guards by name, ID or post..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            aria-label="Search guards by name, ID or post"
          />
          <button type="button" className={styles.tuneBtn} aria-label="Filter tune" title="Filters">
            <span className={`material-symbols-outlined ${styles.iconMd}`}>tune</span>
          </button>
        </div>
        <div className={styles.chipRow}>
          {chips.map((c) => (
            <button
              key={c.id}
              type="button"
              className={chip === c.id ? styles.chipActive : styles.chip}
              onClick={() => setChip(c.id)}
            >
              {c.label}
            </button>
          ))}
        </div>
      </div>

      {banner ? (
        <div
          className={`${styles.bannerShared} ${banner.kind === 'success' ? styles.bannerSuccess : styles.bannerError}`}
          role="status"
        >
          <span className={styles.bannerText}>{banner.message}</span>
          <button type="button" className={styles.bannerDismiss} onClick={() => dismissBanner()} aria-label="Dismiss">
            ✕
          </button>
        </div>
      ) : null}

      {loadError ? (
        <div className={`${styles.bannerAlt} ${styles.bannerError}`} role="alert">
          <span>
            {loadError}{' '}
            <button type="button" className={styles.linkBtn} onClick={() => void refreshList()}>
              Retry
            </button>
          </span>
        </div>
      ) : null}

      <section className={styles.listSection} aria-busy={loading} aria-label="Personnel list">
        {loading && displayedRows.length === 0 ? <p className={styles.muted}>Loading…</p> : null}

        {!loading && displayedRows.length === 0 && !loadError ? (
          <p className={styles.muted} role="status">
            No personnel found for this filter.
          </p>
        ) : null}

        <ul className={styles.cardList}>
          {displayedRows.map((row) => (
            <PersonnelCard
              key={row.id}
              row={row}
              isAdmin={isAdmin}
              onEdit={() => openEdit(row)}
              onToggleOff={() => setInactivateTarget(row)}
              onToggleOn={() => void handleActivate(row.id)}
            />
          ))}
        </ul>
      </section>

      {isAdmin ? (
        <button type="button" className={styles.fab} aria-label="Add personnel" onClick={() => openCreate()}>
          <span className={`material-symbols-outlined ${styles.fabIcon}`}>person_add</span>
        </button>
      ) : null}

      {nameFormOpen ? (
        <div
          className={styles.backdrop}
          role="presentation"
          onClick={(evt) => {
            if (evt.target === evt.currentTarget) {
              closeNameForm()
            }
          }}
        >
          <div className={styles.dialogInner}>
            <h2 id="sg-form-title" className={styles.dialogTitle}>
              {nameFormOpen === 'create' ? 'New personnel' : 'Edit personnel'}
            </h2>

            <form aria-labelledby="sg-form-title" onSubmit={(e) => void handleSubmitName(e)}>
              {nameFormError ? (
                <p className={styles.alert} role="alert">
                  {nameFormError}
                </p>
              ) : null}

              <label className={styles.label} htmlFor="sg-name-input">
                Name
                <input
                  id="sg-name-input"
                  className={`${styles.input} ${nameInvalid ? styles.inputInvalid : ''}`}
                  value={nameDraft}
                  onChange={(ev) => setNameDraft(ev.target.value)}
                  onBlur={() => setNameTouched(true)}
                  aria-invalid={nameInvalid}
                  autoFocus
                  disabled={submitting}
                />
              </label>
              {nameInvalid ? <span className={styles.fieldErr}>Enter a name.</span> : null}

              <div className={styles.dialogFooter}>
                <button type="button" className={styles.btnGhost} onClick={() => closeNameForm()} disabled={submitting}>
                  Cancel
                </button>
                <button type="submit" className={styles.btnPrimary} disabled={submitting}>
                  {submitting ? 'Saving…' : nameFormOpen === 'create' ? 'Create' : 'Save'}
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}

      {inactivateTarget ? (
        <div
          className={styles.backdrop}
          role="presentation"
          onClick={(evt) => {
            if (evt.target === evt.currentTarget && !inactivateSubmitting) {
              setInactivateTarget(null)
            }
          }}
        >
          <div className={styles.dialogInner}>
            <h2 className={styles.dialogTitle}>Deactivate personnel</h2>
            <p className={styles.confirmBody}>
              Confirm deactivation of <strong>{inactivateTarget.name}</strong>. Existing schedule history is kept.
            </p>
            <div className={styles.dialogFooter}>
              <button
                type="button"
                className={styles.btnGhost}
                onClick={() => setInactivateTarget(null)}
                disabled={inactivateSubmitting}
              >
                Cancel
              </button>
              <button
                type="button"
                className={styles.btnDanger}
                onClick={() => void confirmInactivate()}
                disabled={inactivateSubmitting}
              >
                {inactivateSubmitting ? 'Processing…' : 'Confirm'}
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  )
}

function PersonnelCard({
  row,
  isAdmin,
  onEdit,
  onToggleOff,
  onToggleOn,
}: {
  row: SecurityGuardDto
  isAdmin: boolean
  onEdit: () => void
  onToggleOff: () => void
  onToggleOn: () => void
}) {
  const sector = guardSector(row.id)
  const post = guardPost(row.id)
  const showOnSiteStyle = row.isActive && hashSeed(row.id, 'badge') % 2 === 1

  return (
    <li
      className={`${styles.personnelCard} ${row.isActive ? styles.personnelCardActive : styles.personnelCardInactive}`}
    >
      <div className={row.isActive ? styles.accentBarActive : styles.accentBarInactive} />
      <div className={styles.cardAvatar} aria-hidden>
        <span className={styles.cardAvatarText}>{row.name.slice(0, 1).toUpperCase()}</span>
      </div>
      <div className={styles.cardBody}>
        <div className={styles.cardTitleRow}>
          <h3 className={styles.cardName}>
            {isAdmin ? (
              <button type="button" className={styles.nameBtn} onClick={onEdit} title="Edit name">
                {row.name}
              </button>
            ) : (
              row.name
            )}
          </h3>
          <span className={styles.cardId}>{displayGuardId(row.id)}</span>
        </div>
        <div className={styles.cardMetaRow}>
          {row.isActive ? (
            showOnSiteStyle ? (
              <span className={styles.badgeOnSite}>On-Site</span>
            ) : (
              <span className={styles.badgeActive}>Active</span>
            )
          ) : (
            <span className={styles.badgeInactive}>Inactive</span>
          )}
          <span className={styles.cardLocation}>
            {row.isActive ? `${sector} • ${post}` : 'Off Duty'}
          </span>
        </div>
      </div>
      <div className={styles.cardToggleCol}>
        <ToggleTrack
          pressed={row.isActive}
          disabled={!isAdmin}
          onActivateOff={onToggleOff}
          onActivateOn={onToggleOn}
          label={`Active status for ${row.name}`}
        />
      </div>
    </li>
  )
}

function ToggleTrack({
  pressed,
  disabled,
  onActivateOff,
  onActivateOn,
  label,
}: {
  pressed: boolean
  disabled: boolean
  onActivateOff: () => void
  onActivateOn: () => void
  label: string
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={pressed}
      aria-label={label}
      disabled={disabled}
      className={styles.toggleBtn}
      onClick={() => {
        if (disabled) {
          return
        }
        if (pressed) {
          onActivateOff()
        } else {
          onActivateOn()
        }
      }}
    >
      <span className={`${styles.toggleTrack} ${pressed ? styles.toggleTrackOn : ''}`}>
        <span className={`${styles.toggleThumb} ${pressed ? styles.toggleThumbOn : ''}`} />
      </span>
    </button>
  )
}
