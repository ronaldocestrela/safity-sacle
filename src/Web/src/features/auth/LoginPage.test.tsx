import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from '../../shared/auth/AuthProvider'
import { clearSession } from '../../shared/auth/session'
import { LoginPage } from './LoginPage'
import { loginRequest } from './loginApi'
import { expSoon, makeUnsignedJwt } from '../../test/jwtTestUtils'

vi.mock('./loginApi', () => ({
  loginRequest: vi.fn(),
}))

const mockedLogin = vi.mocked(loginRequest)

function renderLogin(initialPath = '/login') {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/app" element={<h1>Área autenticada</h1>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

describe('LoginPage', () => {
  afterEach(() => {
    vi.clearAllMocks()
    clearSession()
    sessionStorage.clear()
  })

  it('shows validation hints when fields are empty', async () => {
    const user = userEvent.setup()
    renderLogin()
    await user.click(screen.getByRole('button', { name: /entrar/i }))
    expect(await screen.findByText(/informe o e-mail/i)).toBeInTheDocument()
    expect(screen.getByText(/informe a senha/i)).toBeInTheDocument()
  })

  it('submits and navigates on success', async () => {
    const user = userEvent.setup()
    const token = makeUnsignedJwt({ exp: expSoon(), role: 'Admin', email: 'a@b.com' })
    mockedLogin.mockResolvedValue({ token })

    renderLogin()
    await user.type(screen.getByLabelText(/e-mail/i), 'a@b.com')
    await user.type(screen.getByLabelText(/^senha$/i), 'secret')
    await user.click(screen.getByRole('button', { name: /^entrar$/i }))

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /área autenticada/i })).toBeInTheDocument()
    })
    expect(mockedLogin).toHaveBeenCalledWith('a@b.com', 'secret')
  })

  it('shows error on invalid credentials', async () => {
    const user = userEvent.setup()
    mockedLogin.mockResolvedValue({ error: 'invalid' })
    renderLogin()
    await user.type(screen.getByLabelText(/e-mail/i), 'a@b.com')
    await user.type(screen.getByLabelText(/^senha$/i), 'wrong')
    await user.click(screen.getByRole('button', { name: /^entrar$/i }))
    expect(await screen.findByRole('alert')).toHaveTextContent(/inválidos/i)
  })
})
