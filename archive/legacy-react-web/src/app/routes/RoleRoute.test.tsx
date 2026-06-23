import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { AuthProvider } from '../../shared/auth/AuthProvider'
import { AUTH_SESSION_STORAGE_KEY, clearSession } from '../../shared/auth/session'
import { ProtectedRoute } from './ProtectedRoute'
import { RoleRoute } from './RoleRoute'
import { expSoon, makeUnsignedJwt } from '../../test/jwtTestUtils'

function harness(initialPath: string) {
  return (
    <MemoryRouter initialEntries={[initialPath]}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<h1>Login</h1>} />
          <Route element={<ProtectedRoute />}>
            <Route path="/app/admin-only" element={<RoleRoute allowedRoles={['Admin']}><p>Zona admin</p></RoleRoute>} />
            <Route path="/app/access-denied" element={<p>Acesso negado</p>} />
          </Route>
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  )
}

describe('RoleRoute', () => {
  it('blocks supervisor from admin-only route', async () => {
    clearSession()
    sessionStorage.clear()
    const token = makeUnsignedJwt({ exp: expSoon(), role: 'Supervisor' })
    sessionStorage.setItem(AUTH_SESSION_STORAGE_KEY, JSON.stringify({ token }))
    render(harness('/app/admin-only'))
    await waitFor(() => {
      expect(screen.getByText(/Acesso negado/)).toBeInTheDocument()
    })
    expect(screen.queryByText(/Zona admin/)).not.toBeInTheDocument()
  })

  it('allows admin', async () => {
    clearSession()
    sessionStorage.clear()
    const token = makeUnsignedJwt({ exp: expSoon(), role: 'Admin' })
    sessionStorage.setItem(AUTH_SESSION_STORAGE_KEY, JSON.stringify({ token }))
    render(harness('/app/admin-only'))
    await waitFor(() => {
      expect(screen.getByText(/Zona admin/)).toBeInTheDocument()
    })
  })
})
