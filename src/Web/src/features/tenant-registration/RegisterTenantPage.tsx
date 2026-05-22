import { type FormEvent, useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../../shared/auth/useAuth'
import styles from '../auth/LoginPage.module.css'
import { registerTenantRequest } from './registerTenantApi'

export function RegisterTenantPage() {
  const { session } = useAuth()
  const navigate = useNavigate()

  const [tenantName, setTenantName] = useState('')
  const [adminName, setAdminName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [touched, setTouched] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (session) {
    return <Navigate to="/app" replace />
  }

  const tenantInvalid = touched && tenantName.trim() === ''
  const adminInvalid = touched && adminName.trim() === ''
  const emailInvalid = touched && email.trim() === ''
  const passwordInvalid = touched && password === ''
  const confirmInvalid = touched && confirmPassword === ''

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setTouched(true)
    setError(null)

    if (!tenantName.trim() || !adminName.trim() || !email.trim() || !password || !confirmPassword) {
      return
    }

    if (password !== confirmPassword) {
      setError('A senha e a confirmação não conferem.')
      return
    }

    setSubmitting(true)
    const result = await registerTenantRequest({
      tenantName: tenantName.trim(),
      adminName: adminName.trim(),
      adminEmail: email.trim(),
      adminPassword: password,
      confirmPassword,
    })
    setSubmitting(false)

    if (result.ok) {
      navigate('/login', {
        replace: true,
        state: { registrationSuccess: true, registeredEmail: email.trim() },
      })
      return
    }

    if (result.reason === 'email-exists') {
      setError('Este e-mail já está cadastrado.')
      return
    }
    if (result.reason === 'tenant-exists') {
      setError('Não foi possível concluir o cadastro. Tente alterar o nome da empresa.')
      return
    }
    if ((result.reason === 'validation' || result.reason === 'invalid-password') && result.messages?.length) {
      setError(result.messages.join(' '))
      return
    }
    if (result.reason === 'invalid-password') {
      setError(
        'A senha deve ter pelo menos 8 caracteres, incluindo maiúsculas, minúsculas, números e um carácter especial.',
      )
      return
    }
    setError('Não foi possível conectar à API. Verifique se o servidor está no ar.')
  }

  return (
    <div className={styles.page}>
      <div className={`${styles.card} ${styles.cardWide}`}>
        <div className={styles.cardHeader}>
          <span className={styles.logo}>SafetyScale</span>
          <h1 className={styles.title}>Cadastro da empresa</h1>
          <p className={styles.lead}>Cria uma nova empresa e conta de administrador.</p>
        </div>

        <form className={styles.form} onSubmit={handleSubmit} noValidate>
          {error ? (
            <p className={styles.error} role="alert">
              {error}
            </p>
          ) : null}

          <label className={styles.label}>
            Nome da empresa
            <input
              className={`${styles.input} ${tenantInvalid ? styles.inputInvalid : ''}`}
              name="tenantName"
              type="text"
              autoComplete="organization"
              value={tenantName}
              onChange={(e) => setTenantName(e.target.value)}
              onBlur={() => setTouched(true)}
              disabled={submitting}
              aria-invalid={tenantInvalid}
            />
            {tenantInvalid ? (
              <span className={styles.fieldError}>Informe o nome da empresa.</span>
            ) : null}
          </label>

          <label className={styles.label}>
            Nome do administrador
            <input
              className={`${styles.input} ${adminInvalid ? styles.inputInvalid : ''}`}
              name="adminName"
              type="text"
              autoComplete="name"
              value={adminName}
              onChange={(e) => setAdminName(e.target.value)}
              onBlur={() => setTouched(true)}
              disabled={submitting}
              aria-invalid={adminInvalid}
            />
            {adminInvalid ? (
              <span className={styles.fieldError}>Informe o nome do administrador.</span>
            ) : null}
          </label>

          <label className={styles.label}>
            E-mail do administrador
            <input
              className={`${styles.input} ${emailInvalid ? styles.inputInvalid : ''}`}
              name="adminEmail"
              type="email"
              autoComplete="email"
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
              name="adminPassword"
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              onBlur={() => setTouched(true)}
              disabled={submitting}
              aria-invalid={passwordInvalid}
            />
            {passwordInvalid ? <span className={styles.fieldError}>Informe a senha.</span> : null}
          </label>

          <label className={styles.label}>
            Confirmar senha
            <input
              className={`${styles.input} ${confirmInvalid ? styles.inputInvalid : ''}`}
              name="confirmPassword"
              type="password"
              autoComplete="new-password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              onBlur={() => setTouched(true)}
              disabled={submitting}
              aria-invalid={confirmInvalid}
            />
            {confirmInvalid ? (
              <span className={styles.fieldError}>Confirme a senha.</span>
            ) : null}
          </label>

          <button type="submit" className={styles.submit} disabled={submitting}>
            {submitting ? 'Cadastrando…' : 'Cadastrar empresa'}
          </button>
        </form>

        <p className={styles.footer}>
          <Link className={styles.link} to="/login">
            Já tenho conta
          </Link>
          {' · '}
          <Link className={styles.link} to="/">
            Página inicial
          </Link>
        </p>
      </div>
    </div>
  )
}
