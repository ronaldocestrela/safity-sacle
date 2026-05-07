import { type ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../../shared/auth/useAuth'
import type { UserRole } from '../../shared/auth/types'

export function RoleRoute({
  allowedRoles,
  children,
}: {
  allowedRoles: UserRole[]
  children: ReactNode
}) {
  const { session } = useAuth()
  const ok = session?.roles.some((r) => allowedRoles.includes(r)) ?? false
  if (!ok) {
    return <Navigate to="/app/access-denied" replace />
  }
  return <>{children}</>
}
