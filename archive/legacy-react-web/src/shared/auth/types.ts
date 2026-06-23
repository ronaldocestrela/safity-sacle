export type UserRole = 'Admin' | 'Supervisor'

export type AuthSession = {
  token: string
  email: string | null
  roles: UserRole[]
  /** Azure AD–style lowercase claim emitted by SafetyScale JWT (`tenant_id`). */
  tenantId: string
}
