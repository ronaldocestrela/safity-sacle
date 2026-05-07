export type UserRole = 'Admin' | 'Supervisor'

export type AuthSession = {
  token: string
  email: string | null
  roles: UserRole[]
}
