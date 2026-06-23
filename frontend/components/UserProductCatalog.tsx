'use client';

import Link from 'next/link';
import { useEffect, useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { clearSession } from '@/lib/storage';
import type { AuthUser, Brand, Product } from '@/lib/types';

type Props = {
  token: string;
  user: AuthUser;
};

function ProductCard({ product }: { product: Product }) {
  return (
    <Link href={`/products/${product.id}`} className="group overflow-hidden rounded-[1.75rem] border border-slate-200 bg-white shadow-sm transition hover:-translate-y-0.5 hover:shadow-lg">
      <div className="aspect-[4/3] overflow-hidden bg-slate-100">
        {product.imageUrl ? (
          <img
            src={product.imageUrl}
            alt={product.name}
            className="h-full w-full object-cover transition duration-300 group-hover:scale-[1.02]"
            loading="lazy"
            referrerPolicy="no-referrer"
          />
        ) : (
          <div className="grid h-full place-items-center bg-[linear-gradient(135deg,#0f172a,#1e3a8a)] text-sm font-semibold uppercase tracking-[0.35em] text-sky-100">
            Northstep
          </div>
        )}
      </div>

      <div className="space-y-4 p-5">
        <div className="space-y-2">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-600">{product.brandName}</p>
              <h3 className="mt-1 text-xl font-semibold text-slate-950">{product.name}</h3>
            </div>
            <span className="rounded-full bg-emerald-50 px-3 py-1 text-xs font-medium text-emerald-700">
              {product.status ? 'Activo' : 'Inactivo'}
            </span>
          </div>

          <p className="line-clamp-2 text-sm leading-6 text-slate-600">
            {product.description || 'Producto sin descripción adicional.'}
          </p>
        </div>

        <div className="grid grid-cols-3 gap-3 text-sm">
          <div className="rounded-2xl bg-slate-50 px-3 py-3">
            <p className="text-[11px] uppercase tracking-[0.2em] text-slate-400">Precio</p>
            <p className="mt-1 font-semibold text-slate-950">${product.price.toFixed(2)}</p>
          </div>
          <div className="rounded-2xl bg-slate-50 px-3 py-3">
            <p className="text-[11px] uppercase tracking-[0.2em] text-slate-400">Stock</p>
            <p className="mt-1 font-semibold text-slate-950">{product.stock}</p>
          </div>
          <div className="rounded-2xl bg-slate-50 px-3 py-3">
            <p className="text-[11px] uppercase tracking-[0.2em] text-slate-400">Actualizado</p>
            <p className="mt-1 font-semibold text-slate-950">{new Date(product.fechaModificacion ?? product.fechaCreacion).toLocaleDateString('es-ES')}</p>
          </div>
        </div>

        <div className="text-sm font-medium text-brand-600">Ver detalles</div>
      </div>
    </Link>
  );
}

export function UserProductCatalog({ token, user }: Props) {
  const router = useRouter();
  const [brands, setBrands] = useState<Brand[]>([]);
  const [items, setItems] = useState<Product[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(6);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [brandId, setBrandId] = useState('');
  const [loading, setLoading] = useState(true);
  const [downloadingReport, setDownloadingReport] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalCount / pageSize)), [pageSize, totalCount]);

  useEffect(() => {
    let active = true;

    async function load() {
      setLoading(true);
      setError(null);

      try {
        const [brandResult, productResult] = await Promise.all([
          api.brands(token),
          api.products({ search, brandId: brandId || undefined, page, pageSize, includeInactive: false }, token)
        ]);

        if (!active) {
          return;
        }

        setBrands(brandResult);
        setItems(productResult.items);
        setTotalCount(productResult.totalCount);
      } catch (err) {
        if (!active) {
          return;
        }

        setError(err instanceof Error ? err.message : 'No se pudo cargar el catálogo.');
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
  }, [brandId, page, pageSize, search, token]);

  async function downloadReport() {
    try {
      setDownloadingReport(true);
      setError(null);

      const url = api.productReportUrl({ search, brandId: brandId || undefined, includeInactive: false });
      const response = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });

      if (!response.ok) {
        const message = (await response.text()).trim();
        setError(message || 'No se pudo descargar el reporte PDF.');
        return;
      }

      const blob = await response.blob();
      if (blob.size === 0) {
        setError('El reporte PDF llegó vacío.');
        return;
      }

      const link = document.createElement('a');
      const objectUrl = URL.createObjectURL(blob);
      link.href = objectUrl;
      link.download = 'products-report.pdf';
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
    } catch {
      setError('No se pudo descargar el reporte PDF.');
    } finally {
      setDownloadingReport(false);
    }
  }

  function logout() {
    clearSession();
    router.push('/login');
  }

  return (
    <div className="space-y-8">
      <section className="overflow-hidden rounded-[2.5rem] border border-slate-200 bg-slate-950 text-white shadow-2xl">
        <div className="bg-[radial-gradient(circle_at_top_left,rgba(125,211,252,0.22),transparent_28%),radial-gradient(circle_at_bottom_right,rgba(59,130,246,0.28),transparent_34%)] px-6 py-8 sm:px-8 lg:px-10">
          <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
            <div className="max-w-3xl space-y-4">
              <p className="text-sm font-semibold uppercase tracking-[0.35em] text-sky-200">Northstep Studio</p>
              <div className="space-y-3">
                <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl lg:text-5xl">Catálogo de productos</h1>
                <p className="max-w-2xl text-sm leading-7 text-slate-300 sm:text-base">
                  Encuentra rápido el producto que necesitas y entra a su ficha para revisar imagen, precio, stock y datos completos.
                </p>
              </div>
            </div>

            <div className="flex flex-wrap gap-3">
              <button className="button-secondary" disabled={downloadingReport} onClick={downloadReport}>
                {downloadingReport ? 'Descargando PDF...' : 'Descargar PDF'}
              </button>
              <button className="button-secondary" onClick={logout}>Cerrar sesión</button>
            </div>
          </div>

          <div className="mt-8 grid gap-3 md:grid-cols-3">
            <div className="rounded-2xl border border-white/10 bg-white/10 px-4 py-4">
              <p className="text-xs uppercase tracking-[0.25em] text-slate-400">Usuario</p>
              <p className="mt-1 font-medium text-white">{user.fullName}</p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-white/10 px-4 py-4">
              <p className="text-xs uppercase tracking-[0.25em] text-slate-400">Resultados</p>
              <p className="mt-1 font-medium text-white">{totalCount} productos</p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-white/10 px-4 py-4">
              <p className="text-xs uppercase tracking-[0.25em] text-slate-400">Vista</p>
              <p className="mt-1 font-medium text-emerald-300">Catálogo activo</p>
            </div>
          </div>
        </div>
      </section>

      <section className="card space-y-5 p-6">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h2 className="text-2xl font-semibold text-slate-950">Explorar productos</h2>
            <p className="text-sm text-slate-600">Consulta los datos principales desde la lista y abre cualquier producto para ver todos sus detalles.</p>
          </div>

          <button
            className="button-secondary"
            onClick={() => {
              setSearch('');
              setBrandId('');
              setPage(1);
            }}
          >
            Limpiar filtros
          </button>
        </div>

        <div className="grid gap-3 lg:grid-cols-[1.2fr_0.8fr_0.5fr]">
          <input
            className="input"
            placeholder="Buscar por nombre o descripción"
            value={search}
            onChange={(event) => {
              setPage(1);
              setSearch(event.target.value);
            }}
          />

          <select
            className="input"
            value={brandId}
            onChange={(event) => {
              setPage(1);
              setBrandId(event.target.value);
            }}
          >
            <option value="">Todas las marcas</option>
            {brands.map((brand) => (
              <option key={brand.id} value={brand.id}>{brand.name}</option>
            ))}
          </select>

          <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
            Página {page}/{totalPages}
          </div>
        </div>

        {error && <div className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div>}

        {loading ? (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 px-4 py-12 text-center text-sm text-slate-500">
            Cargando catálogo...
          </div>
        ) : items.length === 0 ? (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 px-4 py-12 text-center text-sm text-slate-500">
            No se encontraron productos con esos filtros.
          </div>
        ) : (
          <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
            {items.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        )}

        <div className="flex items-center justify-between gap-3">
          <button className="button-secondary" disabled={page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>Anterior</button>
          <p className="text-sm text-slate-600">Mostrando una selección de {items.length} productos</p>
          <button className="button-secondary" disabled={page >= totalPages} onClick={() => setPage((value) => Math.min(totalPages, value + 1))}>Siguiente</button>
        </div>
      </section>
    </div>
  );
}
