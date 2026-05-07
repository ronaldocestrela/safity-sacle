import { Link } from 'react-router-dom'
import styles from './AccessDeniedPage.module.css'

export function AccessDeniedPage() {
  return (
    <div className={styles.wrap}>
      <h1 className={styles.title}>Acesso negado</h1>
      <p className={styles.text}>
        Você não tem permissão para acessar esta área. Supervisores têm acesso de consulta conforme o perfil.
      </p>
      <Link className={styles.link} to="/app">
        Voltar ao início
      </Link>
    </div>
  )
}
