import React, { createContext, useState, useEffect, useCallback } from 'react';
import { loginApi, registerApi } from '../api/authApi';
import { STORAGE_KEYS } from '../config/constants';
import { isTokenExpired } from '../utils/tokenUtils';

export const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  // Initialize auth state from localStorage on startup
  useEffect(() => {
    const initializeAuth = () => {
      try {
        const storedToken = localStorage.getItem(STORAGE_KEYS.TOKEN);
        const storedUserJson = localStorage.getItem(STORAGE_KEYS.USER);

        if (storedToken && storedUserJson) {
          if (isTokenExpired(storedToken)) {
            // Token expired -> purge storage
            localStorage.removeItem(STORAGE_KEYS.TOKEN);
            localStorage.removeItem(STORAGE_KEYS.USER);
          } else {
            const parsedUser = JSON.parse(storedUserJson);
            setToken(storedToken);
            setUser(parsedUser);
          }
        }
      } catch (error) {
        console.error('Error restoring auth state from localStorage:', error);
        localStorage.removeItem(STORAGE_KEYS.TOKEN);
        localStorage.removeItem(STORAGE_KEYS.USER);
      } finally {
        setIsLoading(false);
      }
    };

    initializeAuth();
  }, []);

  const saveAuthSession = (authData) => {
    const { token: jwtToken, userId, email, role, expiresAt } = authData;
    const userData = { userId, email, role, expiresAt };

    setToken(jwtToken);
    setUser(userData);

    localStorage.setItem(STORAGE_KEYS.TOKEN, jwtToken);
    localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(userData));
  };

  const login = async (credentials) => {
    const authData = await loginApi(credentials);
    saveAuthSession(authData);
    return authData;
  };

  const register = async (userData) => {
    const authData = await registerApi(userData);
    saveAuthSession(authData);
    return authData;
  };

  const logout = useCallback(() => {
    setToken(null);
    setUser(null);
    localStorage.removeItem(STORAGE_KEYS.TOKEN);
    localStorage.removeItem(STORAGE_KEYS.USER);
  }, []);

  const value = {
    user,
    token,
    isAuthenticated: !!token && !!user,
    isLoading,
    login,
    register,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
