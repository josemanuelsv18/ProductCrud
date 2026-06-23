'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ProductManager } from '@/components/ProductManager';
import { UserProductCatalog } from '@/components/UserProductCatalog';
import { getSession } from '@/lib/storage';
import type { AuthUser } from '@/lib/types';

export function ProductsHome() {
  const router = useRouter();
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<AuthUser | null>(null);

  useEffect(() => {
    const session = getSession();
    if (!session.token || !session.user) {
      router.push('/login');
      return;
    }

    setToken(session.token);
    setUser(session.user);
  }, [router]);

  if (!token || !user) {
    return <div className="p-6 text-sm text-slate-600">Cargando sesión...</div>;
  }

  if (user.role === 'Admin') {
    return <ProductManager />;
  }

  return <UserProductCatalog token={token} user={user} />;
}
