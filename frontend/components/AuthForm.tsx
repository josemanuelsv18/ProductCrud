'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { api } from '@/lib/api';
import { saveSession } from '@/lib/storage';

type Props = {
  mode: 'login' | 'register';
};

export function AuthForm({ mode }: Props) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(formData: FormData) {
    setLoading(true);
    setError(null);

    try {
      const result = mode === 'login'
        ? await api.login({
            identifier: String(formData.get('identifier') ?? ''),
            password: String(formData.get('password') ?? '')
          })
        : await api.register({
            userName: String(formData.get('userName') ?? ''),
            email: String(formData.get('email') ?? ''),
            fullName: String(formData.get('fullName') ?? ''),
            password: String(formData.get('password') ?? '')
          });

      saveSession(result);
      router.push('/products');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo completar la operación.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        void handleSubmit(new FormData(event.currentTarget));
      }}
      className="card mx-auto w-full max-w-lg space-y-5 p-8"
    >
      <div>
        <p className="text-sm font-semibold uppercase tracking-[0.3em] text-brand-600">Northstep Studio</p>
        <h1 className="mt-2 text-2xl font-semibold text-slate-900">{mode === 'login' ? 'Iniciar sesión' : 'Crear cuenta'}</h1>
        <p className="mt-1 text-sm text-slate-600">
          {mode === 'login' ? 'Accede al catálogo de calzado.' : 'Crea una cuenta con rol User para acceder al catálogo.'}
        </p>
      </div>

      {mode === 'register' && (
        <>
          <input className="input" name="userName" placeholder="Usuario" required />
          <input className="input" name="fullName" placeholder="Nombre completo" required />
          <input className="input" name="email" type="email" placeholder="Correo electrónico" required />
        </>
      )}

      {mode === 'login' && <input className="input" name="identifier" placeholder="Usuario o correo" required />}
      <input className="input" name="password" type="password" placeholder="Contraseña" required />

      {error && <p className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">{error}</p>}

      <button className="button-primary w-full" type="submit" disabled={loading}>
        {loading ? 'Espera un momento...' : mode === 'login' ? 'Ingresar' : 'Crear cuenta'}
      </button>
    </form>
  );
}
