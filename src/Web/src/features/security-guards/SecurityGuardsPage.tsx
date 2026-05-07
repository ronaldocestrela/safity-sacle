import { type FormEvent, useCallback, useEffect, useRef, useState } from 'react'
import { useAuth } from '../../shared/auth/useAuth'
import {
  ApiError,
  createSecurityGuard,
  inactivateSecurityGuard,
  listSecurityGuards,
  updateSecurityGuard,
} from './securityGuardsApi'
import type { SecurityGuardDto } from './types'
import styles from './SecurityGuardsPage.module.css'

type Filter = 'all' | 'active' | 'inactive'

function filterToQuery(f: Filter): boolean | undefined {
  if (f === 'all') {
    return undefined
  }
  return f === 'active'
}

function formatDt(iso: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) {
    return iso
  }
  return d.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}

export function SecurityGuardsPage() {
  const { session } = useAuth()
  const isAdmin = Boolean(session?.roles.includes('Admin'))
  const loadTokenRef = useRef(0)

  const [filter, setFilter] = useState<Filter>('all')
  const [items, setItems] = useState<SecurityGuardDto[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)

  const [banner, setBanner] = useState<{ kind: 'success' | 'error'; message: string } | null>(null)

  /** `create` | `edit` | null when closed */
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
      const list = await listSecurityGuards(filterToQuery(filter))
      if (loadTokenRef.current !== token) return
      setItems(list)
    } catch (e: unknown) {
      if (loadTokenRef.current !== token) return
      const fallback = 'Não foi possível carregar seguranças.'
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
  }, [filter])

  useEffect(() => {
    queueMicrotask(() => {
      void refreshList()
    })
  }, [refreshList])

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
        setBanner({ kind: 'success', message: 'Segurança criada.' })
      } else if (nameFormOpen === 'edit' && editing) {
        await updateSecurityGuard(editing.id, nameDraft.trim())
        setBanner({ kind: 'success', message: 'Alterações salvas.' })
      }
      closeNameForm()
      await refreshList()
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setNameFormError(err.message || 'Erro ao salvar.')
      } else {
        setNameFormError('Erro ao salvar.')
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
      setBanner({ kind: 'success', message: 'Segurança inativada.' })
      setInactivateTarget(null)
      await refreshList()
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setBanner({ kind: 'error', message: err.message })
      } else {
        setBanner({ kind: 'error', message: 'Não foi possível inativar.' })
      }
    } finally {
      setInactivateSubmitting(false)
    }
  }

  function dismissBanner() {
    setBanner(null)
  }

  return (
    <div className={styles.wrap}>
      <header className={styles.header}>
        <div className={styles.titleBlock}>
          <h1 className={styles.title}>Seguranças</h1>
          <p className={styles.subtitle}>
            {isAdmin
              ? 'Cadastre, edite ou inative registros conforme permissões da API.'
              : 'Consulta de seguranças — gestão apenas para Administrador.'}
          </p>
        </div>

        <div className={styles.toolbar}>
          <label className={styles.field} htmlFor="sg-filter">
            Situação
            <select
              id="sg-filter"
              className={styles.select}
              aria-label="Filtrar seguranças por situação"
              value={filter}
              onChange={(e) => setFilter(e.target.value as Filter)}
            >
              <option value="all">Todos</option>
              <option value="active">Ativos</option>
              <option value="inactive">Inativos</option>
            </select>
          </label>

          {isAdmin ? (
            <button type="button" className={styles.primary} onClick={() => openCreate()}>
              Nova segurança
            </button>
          ) : null}
        </div>
      </header>

      {banner ? (
        <div
          className={`${styles.bannerShared} ${banner.kind === 'success' ? styles.bannerSuccess : styles.bannerError}`}
          role="status"
        >
          <button
            type="button"
            onClick={() => dismissBanner()}
            aria-label="Fechar aviso"
            style={{
              float: 'right',
              border: 'none',
              background: 'transparent',
              cursor: 'pointer',
              font: 'inherit',
              color: 'inherit',
            }}
          >
            ✕
          </button>
          {banner.message}
        </div>
      ) : null}

      {loadError ? (
        <div className={`${styles.bannerShared} ${styles.bannerError}`} role="alert">
          {loadError}{' '}
          <button type="button" className={styles.actionLink} style={{ verticalAlign: 'baseline' }} onClick={() => void refreshList()}>
            Tentar novamente
          </button>
        </div>
      ) : null}

      {loading && items.length === 0 ? <p className={styles.loading}>Carregando…</p> : null}

      {!loading && items.length === 0 && !loadError ? (
        <p className={styles.empty} role="status">
          Nenhum registro encontrado para este filtro.
        </p>
      ) : null}

      {items.length > 0 ? (
        <div className={styles.tableWrap} aria-busy={loading}>
          <table className={styles.table}>
            <thead>
              <tr>
                <th scope="col">Nome</th>
                <th scope="col">Situação</th>
                <th scope="col">Cadastro</th>
                {isAdmin ? <th scope="col">Ações</th> : null}
              </tr>
            </thead>
            <tbody>
              {items.map((row) => (
                <tr key={row.id} className={!row.isActive ? styles.rowInactive : undefined}>
                  <td>{row.name}</td>
                  <td>
                    <span className={row.isActive ? styles.badgeActive : styles.badgeInactive}>
                      {row.isActive ? 'Ativo' : 'Inativo'}
                    </span>
                  </td>
                  <td>{formatDt(row.createdAt)}</td>
                  {isAdmin ? (
                    <td>
                      <div className={styles.actions}>
                        <button type="button" className={styles.actionLink} onClick={() => openEdit(row)}>
                          Editar
                        </button>
                        {row.isActive ? (
                          <button type="button" className={styles.actionDanger} onClick={() => setInactivateTarget(row)}>
                            Inativar
                          </button>
                        ) : null}
                      </div>
                    </td>
                  ) : null}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}

      {nameFormOpen ? (
        <div
          className={styles.backdrop}
          role="presentation"
          onClick={(evt) => {
            if (evt.target === evt.currentTarget) closeNameForm()
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
                Nome
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
              {nameInvalid ? <span className={styles.fieldErr}>Informe o nome.</span> : null}

              <div className={styles.dialogFooter}>
                <button type="button" className={styles.btnGhost} onClick={() => closeNameForm()} disabled={submitting}>
                  Cancelar
                </button>
                <button type="submit" className={styles.primary} disabled={submitting}>
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
            if (evt.target === evt.currentTarget && !inactivateSubmitting) setInactivateTarget(null)
          }}
        >
          <div className={styles.dialogInner}>
            <h2 className={styles.dialogTitle}>Inativar segurança</h2>
            <p style={{ margin: '0 0 0.5rem', lineHeight: 1.45, fontSize: '0.875rem', color: '#334155' }}>
              Confirme a inativação de{' '}
              <strong>{inactivateTarget.name}</strong>. O histórico de escalas existente será preservado.
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
              <button type="button" className={styles.btnDanger} onClick={() => void confirmInactivate()} disabled={inactivateSubmitting}>
                {inactivateSubmitting ? 'Processando…' : 'Confirmar'}
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  )
}
