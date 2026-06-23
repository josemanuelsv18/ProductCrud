'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { clearSession, getSession } from '@/lib/storage';
import type { Product } from '@/lib/types';

type Props = {
  productId: string;
};

function formatDate(value: string | null) {
  if (!value) {
    return 'Sin registro';
  }

  return new Date(value).toLocaleString('es-ES');
}

export function ProductDetailView({ productId }: Props) {
  const router = useRouter();
  const [product, setProduct] = useState<Product | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const session = getSession();
    if (!session.token) {
      router.push('/login');
      return;
    }

    let active = true;

    async function load() {
      setLoading(true);
      setError(null);

      try {
        const result = await api.product(productId, session.token!);
        if (!active) {
          return;
        }

        setProduct(result);
      } catch (err) {
        if (!active) {
          return;
        }

        setError(err instanceof Error ? err.message : 'No se pudo cargar el producto.');
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    void load();

    return () => {
      active = false;
    };
  }, [productId, router]);

  function logout() {
    clearSession();
    router.push('/login');
  }

  if (loading) {
    return <div className="p-6 text-sm text-slate-600">Cargando producto...</div>;
  }

  if (error || !product) {
    return (
      <div className="space-y-4">
        <Link href="/products" className="text-sm font-medium text-brand-600">Volver al catálogo</Link>
        <div className="rounded-2xl bg-red-50 px-4 py-4 text-sm text-red-700">{error ?? 'No se encontró el producto.'}</div>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Link href="/products" className="text-sm font-medium text-brand-600">Volver al catálogo</Link>
        <button className="button-secondary" onClick={logout}>Cerrar sesión</button>
      </div>

      <section className="grid gap-8 lg:grid-cols-[1.05fr_0.95fr]">
        <div className="overflow-hidden rounded-[2rem] border border-slate-200 bg-white shadow-sm">
          {product.imageUrl ? (
            <img
              src={product.imageUrl}
              alt={product.name}
              className="h-full min-h-[420px] w-full object-cover"
              referrerPolicy="no-referrer"
            />
          ) : (
            <div className="grid min-h-[420px] place-items-center bg-[linear-gradient(135deg,#0f172a,#1d4ed8)] text-lg font-semibold uppercase tracking-[0.35em] text-sky-100">
              Sin imagen
            </div>
          )}
        </div>

        <div className="space-y-6 rounded-[2rem] border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
          <div className="space-y-3">
            <p className="text-sm font-semibold uppercase tracking-[0.3em] text-brand-600">{product.brandName}</p>
            <h1 className="text-4xl font-semibold tracking-tight text-slate-950">{product.name}</h1>
            <p className="text-base leading-7 text-slate-600">
              {product.description || 'Este producto no tiene descripción registrada.'}
            </p>
          </div>

          <div className="grid gap-3 sm:grid-cols-2">
            <div className="rounded-2xl bg-slate-50 px-4 py-4">
              <p className="text-xs uppercase tracking-[0.25em] text-slate-400">Precio</p>
              <p className="mt-1 text-2xl font-semibold text-slate-950">${product.price.toFixed(2)}</p>
            </div>
            <div className="rounded-2xl bg-slate-50 px-4 py-4">
              <p className="text-xs uppercase tracking-[0.25em] text-slate-400">Stock</p>
              <p className="mt-1 text-2xl font-semibold text-slate-950">{product.stock}</p>
            </div>
            <div className="rounded-2xl bg-slate-50 px-4 py-4">
              <p className="text-xs uppercase tracking-[0.25em] text-slate-400">Estado</p>
              <p className="mt-1 text-lg font-semibold text-slate-950">{product.status ? 'Activo' : 'Inactivo'}</p>
            </div>
            <div className="rounded-2xl bg-slate-50 px-4 py-4">
              <p className="text-xs uppercase tracking-[0.25em] text-slate-400">Marca</p>
              <p className="mt-1 text-lg font-semibold text-slate-950">{product.brandName}</p>
            </div>
          </div>

          <div className="rounded-2xl border border-slate-200 p-4">
            <h2 className="text-sm font-semibold uppercase tracking-[0.25em] text-slate-500">Historial</h2>
            <div className="mt-4 grid gap-4 text-sm text-slate-600">
              <div>
                <p className="font-medium text-slate-900">Creado por</p>
                <p>{product.usuarioCreacion}</p>
                <p>{formatDate(product.fechaCreacion)}</p>
              </div>
              <div>
                <p className="font-medium text-slate-900">Última modificación</p>
                <p>{product.usuarioModificacion ?? 'Sin modificaciones'}</p>
                <p>{formatDate(product.fechaModificacion)}</p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
