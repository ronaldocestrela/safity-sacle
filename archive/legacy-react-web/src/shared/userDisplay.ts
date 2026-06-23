/** Short initials from email for avatar chips. */
export function userInitials(email: string | undefined | null): string {
  if (!email) {
    return '?'
  }
  const parts = email.split('@')[0].split(/[.\-_]/).filter(Boolean)
  if (parts.length >= 2) {
    return (parts[0][0] + parts[1][0]).toUpperCase().slice(0, 2)
  }
  return email.slice(0, 2).toUpperCase()
}
