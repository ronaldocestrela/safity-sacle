import { Navigate, Route, Routes } from 'react-router-dom'
import { HomePage } from '../features/home/HomePage'
import { LoginPage } from '../features/auth/LoginPage'
import { WelcomePage } from '../features/app/WelcomePage'
import { ModulePlaceholderPage } from '../features/app/ModulePlaceholderPage'
import { AppLayout } from './AppLayout'
import { ProtectedRoute } from './routes/ProtectedRoute'
import { RoleRoute } from './routes/RoleRoute'
import { AccessDeniedPage } from './routes/AccessDeniedPage'

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/app" element={<AppLayout />}>
          <Route index element={<WelcomePage />} />
          <Route path="access-denied" element={<AccessDeniedPage />} />
          <Route
            path="security-guards"
            element={
              <RoleRoute allowedRoles={['Admin']}>
                <ModulePlaceholderPage title="Seguranças" />
              </RoleRoute>
            }
          />
          <Route
            path="unavailable-days"
            element={
              <RoleRoute allowedRoles={['Admin']}>
                <ModulePlaceholderPage title="Indisponibilidades" />
              </RoleRoute>
            }
          />
          <Route
            path="schedules"
            element={
              <ModulePlaceholderPage title="Escalas" description="Geração e consultas de escala chegam na Fase F4 (UI)." />
            }
          />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
