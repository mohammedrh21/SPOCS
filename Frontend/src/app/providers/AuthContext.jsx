import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { authApi } from '../../features/auth/api/authApi';
import { useToast } from './ToastContext';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(() => localStorage.getItem('spocs_auth_token'));
  const [isLoading, setIsLoading] = useState(true);
  const [isAuthModalOpen, setIsAuthModalOpen] = useState(false);
  const [authModalMode, setAuthModalMode] = useState('login'); // 'login' | 'register'
  const toast = useToast();

  const loadCurrentUser = useCallback(async () => {
    const savedToken = localStorage.getItem('spocs_auth_token');
    if (!savedToken) {
      setUser(null);
      setIsLoading(false);
      return;
    }

    try {
      const userData = await authApi.getCurrentUser();
      setUser(userData);
    } catch (err) {
      console.warn('Failed to restore user session', err);
      localStorage.removeItem('spocs_auth_token');
      localStorage.removeItem('spocs_auth_user');
      setUser(null);
      setToken(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadCurrentUser();

    const handleSessionLogout = () => {
      setUser(null);
      setToken(null);
      toast.info('Session expired. Please log in again.');
    };

    window.addEventListener('spocs_auth_logout', handleSessionLogout);
    return () => window.removeEventListener('spocs_auth_logout', handleSessionLogout);
  }, [loadCurrentUser, toast]);

  const login = async (email, password) => {
    setIsLoading(true);
    try {
      const response = await authApi.login({ email, password });
      localStorage.setItem('spocs_auth_token', response.token);
      setToken(response.token);
      setUser(response.user);
      toast.success(`Welcome back, ${response.user.fullName || response.user.email}!`);
      setIsAuthModalOpen(false);
      return response;
    } catch (err) {
      const errorMsg = err.response?.data?.message || 'Login failed. Please check your credentials.';
      toast.error(errorMsg);
      throw new Error(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (registerData) => {
    setIsLoading(true);
    try {
      const response = await authApi.register(registerData);
      localStorage.setItem('spocs_auth_token', response.token);
      setToken(response.token);
      setUser(response.user);
      toast.success(`Account created successfully! Welcome to SPOCS.`);
      setIsAuthModalOpen(false);
      return response;
    } catch (err) {
      const errorMsg = err.response?.data?.message || 'Registration failed. Please check your inputs.';
      toast.error(errorMsg);
      throw new Error(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  const logout = () => {
    localStorage.removeItem('spocs_auth_token');
    localStorage.removeItem('spocs_auth_user');
    setUser(null);
    setToken(null);
    toast.info('You have logged out.');
  };

  const openAuthModal = (mode = 'login') => {
    setAuthModalMode(mode);
    setIsAuthModalOpen(true);
  };

  const closeAuthModal = () => {
    setIsAuthModalOpen(false);
  };

  const isAuthenticated = Boolean(user && token);

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated,
        isLoading,
        login,
        register,
        logout,
        isAuthModalOpen,
        authModalMode,
        setAuthModalMode,
        openAuthModal,
        closeAuthModal,
        refreshUser: loadCurrentUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
