import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Mail, Lock, Sparkles } from 'lucide-react';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import { useAuth } from '../../../app/providers/AuthContext';
import { DEMO_USER } from '../../../constants/config';

export default function LoginPage() {
  const { login, isLoading } = useAuth();
  const navigate = useNavigate();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const handleFillDemo = () => {
    setEmail(DEMO_USER.email);
    setPassword(DEMO_USER.password);
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    try {
      await login(email, password);
      navigate('/');
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <div className="min-h-[70vh] flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-md bg-white rounded-3xl border border-surface-subtle p-8 shadow-card space-y-6">
        <div className="text-center space-y-1">
          <h1 className="nike-heading text-3xl text-ink">WELCOME BACK</h1>
          <p className="text-xs text-ink-muted">Sign in to your SPOCS customer account</p>
        </div>

        {/* Demo Fast Fill */}
        <div className="p-3.5 bg-gradient-to-r from-indigo-50/80 to-teal-50/80 border border-indigo-100 rounded-2xl flex items-center justify-between">
          <div className="space-y-0.5">
            <span className="text-xs font-bold text-ink flex items-center gap-1.5">
              <Sparkles className="w-3.5 h-3.5 text-accent-indigo" />
              Demo Test Account
            </span>
            <p className="text-[11px] text-ink-muted font-mono">{DEMO_USER.email}</p>
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleFillDemo}
            className="text-xs font-semibold bg-white"
          >
            Fill Credentials
          </Button>
        </div>

        {error && (
          <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-xs text-rose-700 font-medium">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Email Address"
            type="email"
            placeholder="customer@spocs.com"
            icon={Mail}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
          <Input
            label="Password"
            type="password"
            placeholder="••••••••"
            icon={Lock}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
          <Button
            type="submit"
            variant="nike-dark"
            size="lg"
            isLoading={isLoading}
            className="w-full uppercase font-bold tracking-wider text-xs h-12"
          >
            Sign In
          </Button>
        </form>

        <div className="text-center text-xs text-ink-muted pt-2 border-t border-surface-subtle">
          Don't have an account?{' '}
          <Link to="/register" className="font-bold text-ink hover:text-accent-orange underline">
            Register now
          </Link>
        </div>
      </div>
    </div>
  );
}
