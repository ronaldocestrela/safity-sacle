import { type FormEvent, useMemo, useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../shared/auth/useAuth'
import styles from './LoginPage.module.css'

type LoginLocationState = {
  from?: string
  reason?: 'session-expired'
  registrationSuccess?: boolean
  registeredEmail?: string
}

export function LoginPage() {
  const { session, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const nav = useMemo(() => (location.state as LoginLocationState | null) ?? null, [location.state])
  const from = nav?.from

  const [email, setEmail] = useState(() => nav?.registeredEmail?.trim() ?? '')
  const [password, setPassword] = useState('')
  const [touched, setTouched] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (session) {
    return <Navigate to="/app" replace />
  }

  const emailInvalid = touched && email.trim() === ''
  const passwordInvalid = touched && password === ''

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setTouched(true)
    setError(null)
    if (!email.trim() || !password) {
      return
    }
    setSubmitting(true)
    const result = await login(email.trim(), password)
    setSubmitting(false)
    if (!result.ok) {
      if (result.reason === 'invalid') {
        setError('E-mail ou senha inválidos.')
      } else {
        setError('Não foi possível conectar à API. Verifique se o servidor está no ar.')
      }
      return
    }
    navigate(from && from !== '/login' ? from : '/app', { replace: true })
  }

  const sessionHint =
    nav?.reason === 'session-expired'
      ? 'Sua sessão expirou ou o token deixou de ser válido. Entre novamente.'
      : null

  const registrationHint = nav?.registrationSuccess
    ? 'Empresa cadastrada com sucesso. Faça login com as credenciais informadas.'
    : null

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <div className={styles.cardHeader}>
          <span className={styles.logo}>SafetyScale</span>
          <h1 className={styles.title}>Login de Acesso</h1>
          <p className={styles.lead}>Entre com sua conta corporativa.</p>
        </div>

        <form className={styles.form} onSubmit={handleSubmit} noValidate>
          {sessionHint ? <p className={styles.banner}>{sessionHint}</p> : null}
          {registrationHint ? <p className={styles.banner}>{registrationHint}</p> : null}
          {error ? (
            <p className={styles.error} role="alert">
              {error}
            </p>
          ) : null}

          <label className={styles.label}>
            E-mail
            <input
              className={`${styles.input} ${emailInvalid ? styles.inputInvalid : ''}`}
              name="email"
              type="email"
              autoComplete="username"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              onBlur={() => setTouched(true)}
              disabled={submitting}
              aria-invalid={emailInvalid}
            />
            {emailInvalid ? <span className={styles.fieldError}>Informe o e-mail.</span> : null}
          </label>

          <label className={styles.label}>
            Senha
            <input
              className={`${styles.input} ${passwordInvalid ? styles.inputInvalid : ''}`}
              name="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              onBlur={() => setTouched(true)}
              disabled={submitting}
              aria-invalid={passwordInvalid}
            />
            {passwordInvalid ? <span className={styles.fieldError}>Informe a senha.</span> : null}
          </label>

          <button type="submit" className={styles.submit} disabled={submitting}>
            {submitting ? 'Entrando…' : 'Entrar'}
          </button>
        </form>

        <p className={styles.footer}>
          <Link className={styles.link} to="/signup">
            Cadastrar minha empresa
          </Link>
          {' · '}
          <Link className={styles.link} to="/">
            Voltar à página inicial
          </Link>
        </p>
      </div>
    </div>
  )
}
