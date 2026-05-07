import { useAuth } from '../../shared/auth/useAuth'
import styles from './WelcomePage.module.css'

export function WelcomePage() {
  const { session } = useAuth()
  const isAdmin = session?.roles.includes('Admin')

  return (
    <div className={styles.wrap}>
      <h1 className={styles.headline}>Bem-vindo ao SafetyScale</h1>
      <p className={styles.sub}>
        {isAdmin
          ? 'Você está logado como Administrador. Use a barra lateral para gerenciar pessoal e escalas.'
          : 'Você está logado como Supervisor. Use a barra lateral para consultas e visualização.'}
      </p>
      <p className={styles.hint}>
        Telas de negócio (seguranças, indisponibilidades, escalas) serão expandidas nas fases F2–F4.
      </p>
    </div>
  )
}
