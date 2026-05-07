import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { AuthProvider } from '../../shared/auth/AuthProvider'
import { AUTH_SESSION_STORAGE_KEY, clearSession } from '../../shared/auth/session'
import { ProtectedRoute } from './ProtectedRoute'
import { expSoon, makeUnsignedJwt } from '../../test/jwtTestUtils'

function harness() {
  return (
    <MemoryRouter initialEntries={['/app']}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<h1>Login de Acesso</h1>} />
          <Route element={<ProtectedRoute />}>
            <Route path="/app" element={<p>Conteúdo autenticado</p>} />
          </Route>
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  )
}

describe('ProtectedRoute', () => {
  it('redirects to login when there is no session', async () => {
    clearSession()
    sessionStorage.clear()
    render(harness())
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /login de acesso/i })).toBeInTheDocument()
    })
    expect(screen.queryByText(/Conteúdo autenticado/)).not.toBeInTheDocument()
  })

  it('renders child route when session exists', async () => {
    sessionStorage.clear()
    const token = makeUnsignedJwt({ exp: expSoon(), role: 'Supervisor', email: 's@test.com' })
    sessionStorage.setItem(AUTH_SESSION_STORAGE_KEY, JSON.stringify({ token }))
    render(harness())
    await waitFor(() => {
      expect(screen.getByText(/Conteúdo autenticado/)).toBeInTheDocument()
    })
  })
})
