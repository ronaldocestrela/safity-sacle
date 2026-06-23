import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from '../../shared/auth/AuthProvider'
import { clearSession } from '../../shared/auth/session'
import { RegisterTenantPage } from './RegisterTenantPage'
import { registerTenantRequest } from './registerTenantApi'

vi.mock('./registerTenantApi', () => ({
  registerTenantRequest: vi.fn(),
}))

const mockedRegister = vi.mocked(registerTenantRequest)

function renderSignup() {
  return render(
    <MemoryRouter initialEntries={['/signup']}>
      <AuthProvider>
        <Routes>
          <Route path="/signup" element={<RegisterTenantPage />} />
          <Route path="/login" element={<div>login-page-marker</div>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

describe('RegisterTenantPage', () => {
  afterEach(() => {
    vi.clearAllMocks()
    clearSession()
    sessionStorage.clear()
  })

  it('shows field validation when submitting empty form', async () => {
    const user = userEvent.setup()
    renderSignup()
    await user.click(screen.getByRole('button', { name: /cadastrar empresa/i }))
    expect(await screen.findByText(/informe o nome da empresa/i)).toBeInTheDocument()
  })

  it('shows mismatch error when passwords differ', async () => {
    const user = userEvent.setup()
    const { container } = renderSignup()
    const pwdFields = container.querySelectorAll('input[type="password"]')

    await user.type(screen.getByLabelText(/nome da empresa/i), 'Empresa LTDA')
    await user.type(screen.getByLabelText(/nome do administrador/i), 'Maria')
    await user.type(screen.getByLabelText(/e-mail do administrador/i), 'maria@test.local')
    await user.type(pwdFields[0]!, 'Aa!23456z')
    await user.type(pwdFields[1]!, 'Other!123Aa')

    await user.click(screen.getByRole('button', { name: /cadastrar empresa/i }))
    expect(mockedRegister).not.toHaveBeenCalled()
    expect(await screen.findByRole('alert')).toHaveTextContent(/não conferem/i)
  })

  it('navigates to login after successful registration', async () => {
    const user = userEvent.setup()
    mockedRegister.mockResolvedValue({
      ok: true,
      tenantId: '00000000-0000-0000-0000-000000000001',
      adminUserId: 'user-1',
      tenantSlug: 'empresa-slug',
    })

    const { container } = renderSignup()
    const pwdFields = container.querySelectorAll('input[type="password"]')

    await user.type(screen.getByLabelText(/nome da empresa/i), 'Empresa Alpha')
    await user.type(screen.getByLabelText(/nome do administrador/i), 'João')
    await user.type(screen.getByLabelText(/e-mail do administrador/i), 'joao@test.local')

    await user.type(pwdFields[0]!, 'Aa!23456z')
    await user.type(pwdFields[1]!, 'Aa!23456z')

    await user.click(screen.getByRole('button', { name: /cadastrar empresa/i }))

    await waitFor(() => {
      expect(screen.getByText('login-page-marker')).toBeInTheDocument()
    })

    expect(mockedRegister).toHaveBeenCalledWith(
      expect.objectContaining({
        tenantName: 'Empresa Alpha',
        adminName: 'João',
        adminEmail: 'joao@test.local',
      }),
    )
  })

  it('shows email duplicate message from api', async () => {
    const user = userEvent.setup()
    mockedRegister.mockResolvedValue({ ok: false, reason: 'email-exists' })
    const { container } = renderSignup()
    const pwdFields = container.querySelectorAll('input[type="password"]')

    await user.type(screen.getByLabelText(/nome da empresa/i), 'Beta')
    await user.type(screen.getByLabelText(/nome do administrador/i), 'Z')
    await user.type(screen.getByLabelText(/e-mail do administrador/i), 'dup@test.local')

    await user.type(pwdFields[0]!, 'Aa!23456z')
    await user.type(pwdFields[1]!, 'Aa!23456z')
    await user.click(screen.getByRole('button', { name: /cadastrar empresa/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/já está cadastrado/i)
  })
})
