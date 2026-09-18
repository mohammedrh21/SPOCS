import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Mail, Lock, User, MapPin } from 'lucide-react';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import { useAuth } from '../../../app/providers/AuthContext';

export default function RegisterPage() {
  const { register, isLoading } = useAuth();
  const navigate = useNavigate();

  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [address, setAddress] = useState('');
  const [city, setCity] = useState('');
  const [postalCode, setPostalCode] = useState('');
  const [country, setCountry] = useState('USA');
  const [error, setError] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    try {
      await register({
        fullName,
        email,
        password,
        address,
        city,
        postalCode,
        country,
      });
      navigate('/');
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <div className="min-h-[70vh] flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-lg bg-white rounded-3xl border border-surface-subtle p-8 shadow-card space-y-6">
        <div className="text-center space-y-1">
          <h1 className="nike-heading text-3xl text-ink">JOIN SPOCS</h1>
          <p className="text-xs text-ink-muted">Create an account to personalize your shopping experience</p>
        </div>

        {error && (
          <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-xs text-rose-700 font-medium">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Full Name"
            placeholder="Jane Doe"
            icon={User}
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            required
          />
          <Input
            label="Email Address"
            type="email"
            placeholder="jane@example.com"
            icon={Mail}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
          <Input
            label="Password"
            type="password"
            placeholder="Minimum 6 characters"
            icon={Lock}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />

          <div className="grid grid-cols-2 gap-3">
            <Input
              label="City"
              placeholder="San Francisco"
              value={city}
              onChange={(e) => setCity(e.target.value)}
            />
            <Input
              label="Postal Code"
              placeholder="94105"
              value={postalCode}
              onChange={(e) => setPostalCode(e.target.value)}
            />
          </div>

          <Input
            label="Street Address"
            placeholder="123 Tech Avenue"
            icon={MapPin}
            value={address}
            onChange={(e) => setAddress(e.target.value)}
          />

          <Button
            type="submit"
            variant="nike-dark"
            size="lg"
            isLoading={isLoading}
            className="w-full uppercase font-bold tracking-wider text-xs h-12"
          >
            Create Account
          </Button>
        </form>

        <div className="text-center text-xs text-ink-muted pt-2 border-t border-surface-subtle">
          Already have an account?{' '}
          <Link to="/login" className="font-bold text-ink hover:text-accent-orange underline">
            Sign In
          </Link>
        </div>
      </div>
    </div>
  );
}
