import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { runApiSmoke, type SmokeResult } from './apiSmoke'
import styles from './HomePage.module.css'

export function HomePage() {
  const [result, setResult] = useState<SmokeResult>({ state: 'loading' })

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      const r = await runApiSmoke()
      if (!cancelled) setResult(r)
    })()
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <main className={styles.main}>
      <h1 className={styles.title}>SafetyScale</h1>
      <p className={styles.lead}>SPA React — autenticação JWT e perfis (Fase F1).</p>
      <p className={styles.actions}>
        <Link className={styles.link} to="/login">
          Ir para login
        </Link>
      </p>

      <section className={styles.panel} aria-live="polite">
        <h2 className={styles.panelTitle}>Smoke da API</h2>
        {result.state === 'loading' && (
          <p className={styles.status}>Verificando conexão…</p>
        )}
        {result.state === 'ok' && (
          <p className={`${styles.status} ${styles.ok}`}>{result.message}</p>
        )}
        {result.state === 'error' && (
          <p className={`${styles.status} ${styles.error}`}>{result.message}</p>
        )}
      </section>
    </main>
  )
}
