import styles from './ModulePlaceholderPage.module.css'

export function ModulePlaceholderPage({
  title,
  description,
}: {
  title: string
  description?: string
}) {
  return (
    <div className={styles.wrap}>
      <h1 className={styles.title}>{title}</h1>
      <p className={styles.text}>
        {description ?? 'Módulo em construção — integração com a API nas próximas fases do roadmap.'}
      </p>
    </div>
  )
}
