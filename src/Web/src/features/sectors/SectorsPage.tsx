import { type FormEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { AppHeader } from '../../shared/components/AppHeader/AppHeader'
import { useAuth } from '../../shared/auth/useAuth'
import {
  ApiError,
  activateSector,
  createSector,
  inactivateSector,
  listSectors,
  updateSector,
} from './sectorsApi'
import type { SectorDto } from './types'
import styles from '../security-guards/SecurityGuardsPage.module.css'

type ChipFilter = 'all' | 'activeOnly'

function chipToActiveQuery(chip: ChipFilter): boolean | undefined {
  if (chip === 'activeOnly') {
    return true
  }
  return undefined
}

export function SectorsPage() {
  const { session, logout } = useAuth()
  const isAdmin = Boolean(session?.roles.includes('Admin'))
  const loadTokenRef = useRef(0)

  const [chip, setChip] = useState<ChipFilter>('all')
  const [search, setSearch] = useState('')
  const [items, setItems] = useState<SectorDto[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)

  const [banner, setBanner] = useState<{ kind: 'success' | 'error'; message: string } | null>(null)

  const [formOpen, setFormOpen] = useState<'create' | 'edit' | null>(null)
  const [editing, setEditing] = useState<SectorDto | null>(null)
  const [nameDraft, setNameDraft] = useState('')
  const [descriptionDraft, setDescriptionDraft] = useState('')
  const [positionsDraft, setPositionsDraft] = useState(1)
  const [nameTouched, setNameTouched] = useState(false)
  const [positionsTouched, setPositionsTouched] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const [inactivateTarget, setInactivateTarget] = useState<SectorDto | null>(null)
  const [inactivateSubmitting, setInactivateSubmitting] = useState(false)

  const refreshList = useCallback(async () => {
    const token = ++loadTokenRef.current
    setLoading(true)
    setLoadError(null)
    try {
      const list = await listSectors(chipToActiveQuery(chip))
      if (loadTokenRef.current !== token) {
        return
      }
      setItems(list)
    } catch (e: unknown) {
      if (loadTokenRef.current !== token) {
        return
      }
      const fallback = 'Não foi possível carregar setores.'
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
    const q = search.trim().toLowerCase()
    if (q) {
      list = list.filter((s) => s.name.toLowerCase().includes(q) || (s.description?.toLowerCase().includes(q) ?? false))
    }
    return list
  }, [items, search])

  const nameInvalid = nameTouched && nameDraft.trim() === ''
  const positionsInvalid =
    !Number.isFinite(positionsDraft) || !Number.isInteger(positionsDraft) || positionsDraft < 1 || positionsDraft > 500
  const positionsShowError = positionsTouched && positionsInvalid

  function closeForm() {
    setFormOpen(null)
    setEditing(null)
    setNameDraft('')
    setDescriptionDraft('')
    setPositionsDraft(1)
    setNameTouched(false)
    setPositionsTouched(false)
    setFormError(null)
    setSubmitting(false)
  }

  function openCreate() {
    setBanner(null)
    setFormOpen('create')
    setEditing(null)
    setNameDraft('')
    setDescriptionDraft('')
    setPositionsDraft(1)
    setNameTouched(false)
    setPositionsTouched(false)
    setFormError(null)
  }

  function openEdit(s: SectorDto) {
    if (!isAdmin) {
      return
    }
    setBanner(null)
    setFormOpen('edit')
    setEditing(s)
    setNameDraft(s.name)
    setDescriptionDraft(s.description ?? '')
    setPositionsDraft(s.requiredGuardsPerDay)
    setNameTouched(false)
    setPositionsTouched(false)
    setFormError(null)
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setNameTouched(true)
    setPositionsTouched(true)
    setFormError(null)
    if (!nameDraft.trim()) {
      return
    }
    if (positionsInvalid) {
      setFormError('As posições por dia devem ser um número inteiro de 1 a 500.')
      return
    }

    setSubmitting(true)
    try {
      const descTrim = descriptionDraft.trim()
      const descPayload = descTrim.length > 0 ? descTrim : null
      if (formOpen === 'create') {
        await createSector(nameDraft.trim(), descPayload, positionsDraft)
        setBanner({ kind: 'success', message: 'Setor criado.' })
      } else if (formOpen === 'edit' && editing) {
        await updateSector(editing.id, nameDraft.trim(), descPayload, positionsDraft)
        setBanner({ kind: 'success', message: 'Alterações salvas.' })
      }
      closeForm()
      await refreshList()
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setFormError(err.message || 'Falha ao salvar.')
      } else {
        setFormError('Falha ao salvar.')
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
      await inactivateSector(inactivateTarget.id)
      setBanner({ kind: 'success', message: 'Setor desativado.' })
      setInactivateTarget(null)
      await refreshList()
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setBanner({ kind: 'error', message: err.message })
      } else {
        setBanner({ kind: 'error', message: 'Não foi possível desativar.' })
      }
    } finally {
      setInactivateSubmitting(false)
    }
  }

  async function handleActivate(sectorId: string): Promise<void> {
    setBanner(null)
    try {
      await activateSector(sectorId)
      setBanner({ kind: 'success', message: 'Setor ativado.' })
      await refreshList()
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setBanner({ kind: 'error', message: err.message })
      } else {
        setBanner({ kind: 'error', message: 'Não foi possível ativar.' })
      }
    }
  }

  function dismissBanner() {
    setBanner(null)
  }

  const chips: { id: ChipFilter; label: string }[] = [
    { id: 'all', label: 'Todos os setores' },
    { id: 'activeOnly', label: 'Apenas ativos' },
  ]

  return (
    <div className={styles.page}>
      <AppHeader
        title="Gestão de setores"
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
            placeholder="Pesquisar setores por nome ou descrição..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            aria-label="Pesquisar setores por nome ou descrição"
          />
          <button type="button" className={styles.tuneBtn} aria-label="Filtro de ajuste" title="Filtros">
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

      <section className={styles.listSection} aria-busy={loading} aria-label="Sectors list">
        {loading && displayedRows.length === 0 ? <p className={styles.muted}>Carregando…</p> : null}

        {!loading && displayedRows.length === 0 && !loadError ? (
          <p className={styles.muted} role="status">
            Não há setores encontrados para este filtro.
          </p>
        ) : null}

        <ul className={styles.cardList}>
          {displayedRows.map((row) => (
            <SectorCard
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
        <button type="button" className={styles.fab} aria-label="Adicionar setor" onClick={() => openCreate()}>
          <span className={`material-symbols-outlined ${styles.fabIcon}`}>business</span>
        </button>
      ) : null}

      {formOpen ? (
        <div
          className={styles.backdrop}
          role="presentation"
          onClick={(evt) => {
            if (evt.target === evt.currentTarget) {
              closeForm()
            }
          }}
        >
          <div className={styles.dialogInner}>
            <h2 id="sector-form-title" className={styles.dialogTitle}>
              {formOpen === 'create' ? 'Novo setor' : 'Editar setor'}
            </h2>

            <form aria-labelledby="sector-form-title" onSubmit={(e) => void handleSubmit(e)}>
              {formError ? (
                <p className={styles.alert} role="alert">
                  {formError}
                </p>
              ) : null}

              <label className={styles.label} htmlFor="sector-name-input">
                Nome
                <input
                  id="sector-name-input"
                  className={`${styles.input} ${nameInvalid ? styles.inputInvalid : ''}`}
                  value={nameDraft}
                  onChange={(ev) => setNameDraft(ev.target.value)}
                  onBlur={() => setNameTouched(true)}
                  aria-invalid={nameInvalid}
                  autoFocus
                  disabled={submitting}
                />
              </label>
              {nameInvalid ? <span className={styles.fieldErr}>Informe um nome.</span> : null}

              <label className={styles.label} htmlFor="sector-desc-input">
                Descrição
                <textarea
                  id="sector-desc-input"
                  className={styles.input}
                  rows={3}
                  value={descriptionDraft}
                  onChange={(ev) => setDescriptionDraft(ev.target.value)}
                  disabled={submitting}
                />
              </label>

              <label className={styles.label} htmlFor="sector-positions-input">
                Posições por dia
                <input
                  id="sector-positions-input"
                  className={`${styles.input} ${positionsShowError ? styles.inputInvalid : ''}`}
                  type="number"
                  inputMode="numeric"
                  min={1}
                  max={500}
                  step={1}
                  value={Number.isFinite(positionsDraft) ? positionsDraft : ''}
                  onChange={(ev) =>
                    setPositionsDraft(ev.target.value === '' ? Number.NaN : Number(ev.target.value))
                  }
                  onBlur={() => setPositionsTouched(true)}
                  disabled={submitting}
                  aria-invalid={positionsShowError}
                />
              </label>
              {positionsShowError ? (
                <span className={styles.fieldErr}>Use um número inteiro entre 1 e 500.</span>
              ) : null}

              <div className={styles.dialogFooter}>
                <button type="button" className={styles.btnGhost} onClick={() => closeForm()} disabled={submitting}>
                  Cancelar
                </button>
                <button type="submit" className={styles.btnPrimary} disabled={submitting}>
                  {submitting ? 'Salvando…' : formOpen === 'create' ? 'Criar' : 'Salvar'}
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
            <h2 className={styles.dialogTitle}>Desativar setor</h2>
            <p className={styles.confirmBody}>
              Confirmar desativação de <strong>{inactivateTarget.name}</strong>. Agendamentos ligados a este
              setor permanecem até serem alterados.
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

function SectorCard({
  row,
  isAdmin,
  onEdit,
  onToggleOff,
  onToggleOn,
}: {
  row: SectorDto
  isAdmin: boolean
  onEdit: () => void
  onToggleOff: () => void
  onToggleOn: () => void
}) {
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
              <button type="button" className={styles.nameBtn} onClick={onEdit} title="Editar setor">
                {row.name}
              </button>
            ) : (
              row.name
            )}
          </h3>
          <span className={styles.cardId} title={row.id}>
            {row.id.slice(0, 8)}…
          </span>
        </div>
        <div className={styles.cardMetaRow}>
          {row.isActive ? <span className={styles.badgeActive}>Ativo</span> : <span className={styles.badgeInactive}>Inativo</span>}
          <span className={styles.cardLocation}>
            {row.requiredGuardsPerDay} position{row.requiredGuardsPerDay === 1 ? '' : 's'}/day
            {' · '}
            {row.description?.trim() ? row.description : 'Sem descrição'}
          </span>
        </div>
      </div>
      <div className={styles.cardToggleCol}>
        <ToggleTrack
          pressed={row.isActive}
          disabled={!isAdmin}
          onActivateOff={onToggleOff}
          onActivateOn={onToggleOn}
          label={`Status ativo para ${row.name}`}
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
