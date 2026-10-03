import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { NotificationProvider } from './context/NotificationContext';
import { ThemeProvider } from './context/ThemeContext';
import Navbar from './components/common/Navbar';
import Footer from './components/common/Footer';
import ScrollProgress from './components/common/ScrollProgress';
import ProtectedRoute from './components/common/ProtectedRoute';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import ForgotPasswordPage from './pages/ForgotPasswordPage';
import OnboardingPage from './pages/OnboardingPage';
import DashboardPage from './pages/DashboardPage';
import CareerDiscoveryPage from './pages/CareerDiscoveryPage';
import HomePage from './pages/HomePage';
import AboutPage from './pages/AboutPage';
import ContactPage from './pages/ContactPage';
import NotFoundPage from './pages/NotFoundPage';

import CounsellorDashboardPage from "./pages/CounsellorDashboardPage.jsx";
import AdminDashboardPage from './pages/AdminDashboardPage.jsx';
import RoleDashboardRoute from './components/common/RoleDashboardRoute.jsx';
import StudentRealityCheckPage from './pages/StudentRealityCheckPage.jsx';
import ConsultantDashboardPage from './pages/ConsultantDashboardPage.jsx';
import SupportPage from './pages/SupportPage.jsx';

function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <NotificationProvider>
          <Router>
            <div className="app-container">
              <ScrollProgress />
              <Navbar />
              <main className="main-content">
                <Routes>
                  <Route path="/" element={<HomePage />} />
                  <Route path="/about" element={<AboutPage />} />
                  <Route path="/contact" element={<ContactPage />} />
                  <Route path="/login" element={<LoginPage />} />
                  <Route path="/register" element={<RegisterPage />} />
                  <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                  <Route path="/reset-password" element={<ForgotPasswordPage />} />
                  <Route
                    path="/counsellor/dashboard"
                    element={
                      <ProtectedRoute allowedRoles={['Counsellor', 'Admin']}>
                        <CounsellorDashboardPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path="/onboarding"
                    element={
                      <ProtectedRoute allowedRoles={['Student']}>
                        <OnboardingPage />
                      </ProtectedRoute>

                    }
                  />
                  <Route
                    path="/dashboard"
                    element={
                      <ProtectedRoute>
                        <RoleDashboardRoute />
                      </ProtectedRoute>
                    }
                  />
                  <Route path="/student/dashboard" element={<ProtectedRoute allowedRoles={['Student']}><DashboardPage /></ProtectedRoute>} />
                  <Route path="/student/reality-check" element={<ProtectedRoute allowedRoles={['Student']}><StudentRealityCheckPage /></ProtectedRoute>} />
                  <Route path="/student/support" element={<ProtectedRoute allowedRoles={['Student']}><SupportPage /></ProtectedRoute>} />
                  <Route
                    path="/consultant/dashboard"
                    element={
                      <ProtectedRoute allowedRoles={['Consultant', 'Admin']}>
                        <ConsultantDashboardPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route path="/admin/dashboard" element={<ProtectedRoute allowedRoles={['Admin']}><AdminDashboardPage /></ProtectedRoute>} />
                  <Route
                    path="/career-discovery"
                    element={
                      <ProtectedRoute allowedRoles={['Student']}>
                        <CareerDiscoveryPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route path="*" element={<NotFoundPage />} />
                </Routes>
              </main>
              <Footer />
            </div>
          </Router>
        </NotificationProvider>
      </AuthProvider>
    </ThemeProvider>
  );
}

export default App;
