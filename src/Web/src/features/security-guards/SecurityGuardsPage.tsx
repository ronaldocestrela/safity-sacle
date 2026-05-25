import { type FormEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { AppHeader } from '../../shared/components/AppHeader/AppHeader'
import { useAuth } from '../../shared/auth/useAuth'
import { ApiError as SectorsApiError, listSectors } from '../sectors/sectorsApi'
import type { SectorDto } from '../sectors/types'
import {
  ApiError,
  activateSecurityGuard,
  createSecurityGuard,
  inactivateSecurityGuard,
  listSecurityGuards,
  setGuardSectors,
  updateSecurityGuard,
} from './securityGuardsApi'
import type { SecurityGuardDto } from './types'
import styles from './SecurityGuardsPage.module.css'

type ChipFilter = 'all' | 'activeOnly'

function displayGuardId(id: string): string {
  const alnum = id.replace(/[^a-zA-Z0-9]/g, '').toUpperCase()
  const core = (alnum + '0000').slice(0, 4)
  return `#SO-${core}`
}

function chipToActiveQuery(chip: ChipFilter): boolean | undefined {
  if (chip === 'activeOnly') {
    return true
  }
  return undefined
}

export function SecurityGuardsPage() {
  const { session, logout } = useAuth()
  const isAdmin = Boolean(session?.roles.includes('Admin'))
  const loadTokenRef = useRef(0)

  const [chip, setChip] = useState<ChipFilter>('all')
  const [sectorFilterId, setSectorFilterId] = useState<string>('')
  const [sectorsCatalog, setSectorsCatalog] = useState<SectorDto[]>([])
  const [sectorsPickList, setSectorsPickList] = useState<SectorDto[]>([])
  const [sectorPickError, setSectorPickError] = useState<string | null>(null)

  const [search, setSearch] = useState('')
  const [items, setItems] = useState<SecurityGuardDto[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)

  const [banner, setBanner] = useState<{ kind: 'success' | 'error'; message: string } | null>(null)

  const [nameFormOpen, setNameFormOpen] = useState<'create' | 'edit' | null>(null)
  const [editing, setEditing] = useState<SecurityGuardDto | null>(null)
  const [nameDraft, setNameDraft] = useState('')
  const [selectedSectorIds, setSelectedSectorIds] = useState<string[]>([])
  const [nameTouched, setNameTouched] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [nameFormError, setNameFormError] = useState<string | null>(null)

  const [inactivateTarget, setInactivateTarget] = useState<SecurityGuardDto | null>(null)
  const [inactivateSubmitting, setInactivateSubmitting] = useState(false)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const all = await listSectors(undefined)
        if (!cancelled) {
          setSectorsCatalog(all)
        }
      } catch {
        if (!cancelled) {
          setSectorsCatalog([])
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!nameFormOpen || !isAdmin) {
      setSectorsPickList([])
      setSectorPickError(null)
      return
    }
    let cancelled = false
    void (async () => {
      try {
        const active = await listSectors(true)
        if (!cancelled) {
          setSectorsPickList(active)
          setSectorPickError(null)
        }
      } catch (e: unknown) {
        if (!cancelled) {
          setSectorsPickList([])
          const msg = e instanceof SectorsApiError ? e.message : 'Could not load active sectors.'
          setSectorPickError(msg ?? 'Could not load active sectors.')
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [nameFormOpen, isAdmin])

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
    if (sectorFilterId) {
      list = list.filter((g) => g.sectors.some((s) => s.id === sectorFilterId))
    }
    const q = search.trim().toLowerCase()
    if (q) {
      list = list.filter(
        (g) =>
          g.name.toLowerCase().includes(q) ||
          displayGuardId(g.id).toLowerCase().includes(q) ||
          g.sectors.some((s) => s.name.toLowerCase().includes(q)),
      )
    }
    return list
  }, [items, sectorFilterId, search])

  const nameInvalid = nameTouched && nameDraft.trim() === ''

  function closeNameForm() {
    setNameFormOpen(null)
    setEditing(null)
    setNameDraft('')
    setSelectedSectorIds([])
    setNameTouched(false)
    setNameFormError(null)
    setSubmitting(false)
  }

  function openCreate() {
    setBanner(null)
    setNameFormOpen('create')
    setEditing(null)
    setNameDraft('')
    setSelectedSectorIds([])
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
    setSelectedSectorIds(g.sectors.map((s) => s.id))
    setNameTouched(false)
    setNameFormError(null)
  }

  function toggleSectorSelection(id: string) {
    setSelectedSectorIds((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]))
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
        const { id } = await createSecurityGuard(nameDraft.trim())
        await setGuardSectors(id, selectedSectorIds)
        setBanner({ kind: 'success', message: 'Personnel created.' })
      } else if (nameFormOpen === 'edit' && editing) {
        await updateSecurityGuard(editing.id, nameDraft.trim())
        await setGuardSectors(editing.id, selectedSectorIds)
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
  ]

  return (
    <div className={styles.page}>
      <AppHeader
        title="Gestão de seguranças"
        email={session?.email}
        showNotifications
        showLogout
        onLogout={logout}
      />

      <div className={styles.stickySearch}>
        <div className={styles.searchField}>
          <span className={`material-symbols-outlined ${styles.searchIcon}`}>search</span>
          <input
            className={styles.searchInput}
            type="search"
            placeholder="Pesquisar seguranças por nome, ID ou setor..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            aria-label="Pesquisar seguranças por nome, ID ou setor"
          />
          <button type="button" className={styles.tuneBtn} aria-label="Filtro de ajuste" title="Filtros">
            <span className={`material-symbols-outlined ${styles.iconMd}`}>tune</span>
          </button>
        </div>
        <div className={styles.chipRow} style={{ alignItems: 'center' }}>
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
          <label style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: '0.35rem', flexShrink: 0 }}>
            <span className={styles.cardLocation} style={{ fontSize: '0.7rem', textTransform: 'uppercase', fontWeight: 700 }}>
              Setor
            </span>
            <select
              className={styles.input}
              style={{ minWidth: '8rem', fontSize: '0.75rem', padding: '0.35rem 0.5rem' }}
              aria-label="Filtrar por setor"
              value={sectorFilterId}
              onChange={(ev) => setSectorFilterId(ev.target.value)}
            >
              <option value="">Todos os setores</option>
              {sectorsCatalog.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                  {!s.isActive ? ' (inativo)' : ''}
                </option>
              ))}
            </select>
          </label>
        </div>
      </div>

      {banner ? (
        <div
          className={`${styles.bannerShared} ${banner.kind === 'success' ? styles.bannerSuccess : styles.bannerError}`}
          role="status"
        >
          <span className={styles.bannerText}>{banner.message}</span>
          <button type="button" className={styles.bannerDismiss} onClick={() => dismissBanner()} aria-label="Dispensar">
            ✕
          </button>
        </div>
      ) : null}

      {loadError ? (
        <div className={`${styles.bannerAlt} ${styles.bannerError}`} role="alert">
          <span>
            {loadError}{' '}
            <button type="button" className={styles.linkBtn} onClick={() => void refreshList()}>
              Tentar novamente
            </button>
          </span>
        </div>
      ) : null}

      <section className={styles.listSection} aria-busy={loading} aria-label="Personnel list">
        {loading && displayedRows.length === 0 ? <p className={styles.muted}>Carregando…</p> : null}

        {!loading && displayedRows.length === 0 && !loadError ? (
          <p className={styles.muted} role="status">
            Não há seguranças encontradas para este filtro.
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
        <button type="button" className={styles.fab} aria-label="Adicionar segurança" onClick={() => openCreate()}>
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
              {nameFormOpen === 'create' ? 'Nova segurança' : 'Editar segurança'}
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

              {isAdmin ? (
                <fieldset style={{ marginTop: '0.85rem', border: '1px solid #e2e8f0', borderRadius: '0.35rem', padding: '0.65rem' }}>
                  <legend className={styles.label} style={{ padding: '0 0.25rem' }}>
                    Setores (ativos)
                  </legend>
                  {sectorPickError ? (
                    <p className={styles.fieldErr} role="alert">
                      {sectorPickError}
                    </p>
                  ) : null}
                  {sectorsPickList.length === 0 && !sectorPickError ? (
                    <p className={styles.cardLocation}>Não há setores ativos. Crie setores primeiro.</p>
                  ) : (
                    <ul style={{ listStyle: 'none', margin: 0, padding: 0, display: 'flex', flexDirection: 'column', gap: '0.35rem' }}>
                      {sectorsPickList.map((s) => (
                        <li key={s.id}>
                          <label style={{ display: 'flex', alignItems: 'center', gap: '0.45rem', fontSize: '0.875rem' }}>
                            <input
                              type="checkbox"
                              checked={selectedSectorIds.includes(s.id)}
                              disabled={submitting}
                              onChange={() => toggleSectorSelection(s.id)}
                            />
                            <span>{s.name}</span>
                          </label>
                        </li>
                      ))}
                    </ul>
                  )}
                </fieldset>
              ) : null}

              <div className={styles.dialogFooter}>
                <button type="button" className={styles.btnGhost} onClick={() => closeNameForm()} disabled={submitting}>
                  Cancelar
                </button>
                <button type="submit" className={styles.btnPrimary} disabled={submitting}>
                  {submitting ? 'Salvando…' : nameFormOpen === 'create' ? 'Criar' : 'Salvar'}
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
            <h2 className={styles.dialogTitle}>Desativar segurança</h2>
            <p className={styles.confirmBody}>
              Confirmar desativação de <strong>{inactivateTarget.name}</strong>. O histórico de agendamentos permanece.
            </p>
            <div className={styles.dialogFooter}>
              <button
                type="button"
                className={styles.btnGhost}
                onClick={() => setInactivateTarget(null)}
                disabled={inactivateSubmitting}
              >
                Cancelar
              </button>
              <button
                type="button"
                className={styles.btnDanger}
                onClick={() => void confirmInactivate()}
                disabled={inactivateSubmitting}
              >
                {inactivateSubmitting ? 'Processando…' : 'Confirmar'}
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
  const sectorLabel =
    row.sectors.length > 0
      ? row.sectors
          .map((s) => s.name)
          .sort((a, b) => a.localeCompare(b))
          .join(', ')
      : 'Não há setores atribuídos'

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
              <button type="button" className={styles.nameBtn} onClick={onEdit} title="Editar segurança">
                {row.name}
              </button>
            ) : (
              row.name
            )}
          </h3>
          <span className={styles.cardId}>{displayGuardId(row.id)}</span>
        </div>
        <div className={styles.cardMetaRow}>
          {row.isActive ? <span className={styles.badgeActive}>Ativo</span> : <span className={styles.badgeInactive}>Inativo</span>}
          <span className={styles.cardLocation}>{row.isActive ? sectorLabel : 'Férias'}</span>
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
