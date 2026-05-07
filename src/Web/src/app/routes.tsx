import { Navigate, Route, Routes } from 'react-router-dom'
import { HomePage } from '../features/home/HomePage'
import { LoginPage } from '../features/auth/LoginPage'
import { WelcomePage } from '../features/app/WelcomePage'
import { SchedulesPage } from '../features/schedules/SchedulesPage'
import { SecurityGuardsPage } from '../features/security-guards/SecurityGuardsPage'
import { UnavailableDaysPage } from '../features/unavailable-days/UnavailableDaysPage'
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
              <RoleRoute allowedRoles={['Admin', 'Supervisor']}>
                <SecurityGuardsPage />
              </RoleRoute>
            }
          />
          <Route
            path="unavailable-days"
            element={
              <RoleRoute allowedRoles={['Admin', 'Supervisor']}>
                <UnavailableDaysPage />
              </RoleRoute>
            }
          />
          <Route
            path="schedules"
            element={
              <RoleRoute allowedRoles={['Admin', 'Supervisor']}>
                <SchedulesPage />
              </RoleRoute>
            }
          />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
