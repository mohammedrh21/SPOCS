import React, { useState } from 'react';
import Modal from '../../../components/ui/Modal';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import { useAuth } from '../../../app/providers/AuthContext';
import { DEMO_USER } from '../../../constants/config';
import { Sparkles, Mail, Lock, User as UserIcon, MapPin } from 'lucide-react';

export default function AuthModal() {
  const { isAuthModalOpen, closeAuthModal, authModalMode, setAuthModalMode, login, register, isLoading } = useAuth();

  // Login form state
  const [loginEmail, setLoginEmail] = useState('');
  const [loginPassword, setLoginPassword] = useState('');

  // Register form state
  const [regFullName, setRegFullName] = useState('');
  const [regEmail, setRegEmail] = useState('');
  const [regPassword, setRegPassword] = useState('');
  const [regAddress, setRegAddress] = useState('');
  const [regCity, setRegCity] = useState('');
  const [regPostalCode, setRegPostalCode] = useState('');
  const [regCountry, setRegCountry] = useState('USA');

  const [error, setError] = useState('');

  const handleFillDemo = () => {
    setLoginEmail(DEMO_USER.email);
    setLoginPassword(DEMO_USER.password);
    setError('');
  };

  const handleLoginSubmit = async (e) => {
    e.preventDefault();
    setError('');
    try {
      await login(loginEmail, loginPassword);
    } catch (err) {
      setError(err.message);
    }
  };

  const handleRegisterSubmit = async (e) => {
    e.preventDefault();
    setError('');
    try {
      await register({
        fullName: regFullName,
        email: regEmail,
        password: regPassword,
        address: regAddress,
        city: regCity,
        postalCode: regPostalCode,
        country: regCountry,
      });
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <Modal
      isOpen={isAuthModalOpen}
      onClose={closeAuthModal}
      maxWidth="max-w-md"
      title={authModalMode === 'login' ? 'Customer Sign In' : 'Create an Account'}
    >
      <div className="space-y-6">
        {/* Brand Banner */}
        <div className="text-center space-y-1">
          <h2 className="nike-heading text-2xl text-ink">
            {authModalMode === 'login' ? 'WELCOME TO SPOCS' : 'JOIN THE CLUB'}
          </h2>
          <p className="text-xs text-ink-muted">
            {authModalMode === 'login'
              ? 'Sign in to access your cart, place orders, and save preferences.'
              : 'Create an account to track orders and personalize your shopping.'}
          </p>
        </div>

        {/* Demo Account Quick Fill Button */}
        {authModalMode === 'login' && (
          <div className="p-3.5 bg-gradient-to-r from-indigo-50/80 to-teal-50/80 border border-indigo-100 rounded-2xl flex items-center justify-between">
            <div className="space-y-0.5">
              <span className="text-xs font-bold text-ink flex items-center gap-1.5">
                <Sparkles className="w-3.5 h-3.5 text-accent-indigo" />
                Quick Test Account
              </span>
              <p className="text-[11px] text-ink-muted font-mono">{DEMO_USER.email}</p>
            </div>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={handleFillDemo}
              className="text-xs font-semibold bg-white shadow-xs"
            >
              Fill Credentials
            </Button>
          </div>
        )}

        {/* Global Error Banner */}
        {error && (
          <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-xs text-rose-700 font-medium">
            {error}
          </div>
        )}

        {/* Login Form */}
        {authModalMode === 'login' ? (
          <form onSubmit={handleLoginSubmit} className="space-y-4">
            <Input
              label="Email Address"
              type="email"
              placeholder="customer@spocs.com"
              icon={Mail}
              value={loginEmail}
              onChange={(e) => setLoginEmail(e.target.value)}
              required
            />
            <Input
              label="Password"
              type="password"
              placeholder="••••••••"
              icon={Lock}
              value={loginPassword}
              onChange={(e) => setLoginPassword(e.target.value)}
              required
            />
            <Button
              type="submit"
              variant="nike-dark"
              size="lg"
              isLoading={isLoading}
              className="w-full uppercase font-bold tracking-wider text-sm mt-2"
            >
              Sign In
            </Button>
          </form>
        ) : (
          /* Register Form */
          <form onSubmit={handleRegisterSubmit} className="space-y-3.5">
            <Input
              label="Full Name"
              type="text"
              placeholder="Alex Walker"
              icon={UserIcon}
              value={regFullName}
              onChange={(e) => setRegFullName(e.target.value)}
              required
            />
            <Input
              label="Email Address"
              type="email"
              placeholder="alex@example.com"
              icon={Mail}
              value={regEmail}
              onChange={(e) => setRegEmail(e.target.value)}
              required
            />
            <Input
              label="Password"
              type="password"
              placeholder="Minimum 6 characters"
              icon={Lock}
              value={regPassword}
              onChange={(e) => setRegPassword(e.target.value)}
              required
            />
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="City"
                type="text"
                placeholder="New York"
                value={regCity}
                onChange={(e) => setRegCity(e.target.value)}
              />
              <Input
                label="Postal Code"
                type="text"
                placeholder="10001"
                value={regPostalCode}
                onChange={(e) => setRegPostalCode(e.target.value)}
              />
            </div>
            <Input
              label="Street Address"
              type="text"
              placeholder="123 Fifth Ave"
              icon={MapPin}
              value={regAddress}
              onChange={(e) => setRegAddress(e.target.value)}
            />
            <Button
              type="submit"
              variant="nike-dark"
              size="lg"
              isLoading={isLoading}
              className="w-full uppercase font-bold tracking-wider text-sm mt-2"
            >
              Create Account
            </Button>
          </form>
        )}

        {/* Toggle Mode */}
        <div className="text-center pt-2 border-t border-surface-subtle">
          {authModalMode === 'login' ? (
            <p className="text-xs text-ink-muted">
              Not a member?{' '}
              <button
                type="button"
                onClick={() => {
                  setError('');
                  setAuthModalMode('register');
                }}
                className="font-bold text-ink underline hover:text-accent-indigo transition-colors"
              >
                Join Us
              </button>
            </p>
          ) : (
            <p className="text-xs text-ink-muted">
              Already have an account?{' '}
              <button
                type="button"
                onClick={() => {
                  setError('');
                  setAuthModalMode('login');
                }}
                className="font-bold text-ink underline hover:text-accent-indigo transition-colors"
              >
                Sign In
              </button>
            </p>
          )}
        </div>
      </div>
    </Modal>
  );
}
