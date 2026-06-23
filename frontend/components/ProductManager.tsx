'use client';

import React from 'react';
import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { getHttpsImageUrlError, normalizeHttpsImageUrl } from '@/lib/image-url';
import { clearSession, getSession } from '@/lib/storage';
import type { AuthUser, Brand, Product } from '@/lib/types';

type ProductFormState = {
  id?: string;
  name: string;
  description: string;
  imageUrl: string;
  price: string;
  stock: string;
  brandId: string;
  status: boolean;
};

const initialForm: ProductFormState = {
  name: '',
  description: '',
  imageUrl: '',
  price: '',
  stock: '',
  brandId: '',
  status: true
};

export function ProductImageCell({ product }: { product: Pick<Product, 'name' | 'imageUrl'> }) {
  return (
    <div className="flex h-16 w-16 shrink-0 overflow-hidden rounded-2xl border border-slate-200 bg-slate-100">
      {product.imageUrl ? (
        <img
          src={product.imageUrl}
          alt={product.name}
          className="h-full w-full object-cover"
          loading="lazy"
          referrerPolicy="no-referrer"
        />
      ) : (
        <div className="grid h-full w-full place-items-center text-[10px] font-semibold uppercase tracking-[0.25em] text-slate-400">
          Sin imagen
        </div>
      )}
    </div>
  );
}

export function ProductImagePreview({ imageUrl }: { imageUrl: string }) {
  const previewImageUrl = normalizeHttpsImageUrl(imageUrl);

  return previewImageUrl ? (
    <div className="overflow-hidden rounded-2xl border border-slate-200 bg-slate-50">
      <img src={previewImageUrl} alt="Vista previa del producto" className="h-56 w-full object-cover" loading="lazy" referrerPolicy="no-referrer" />
    </div>
  ) : (
    <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-sm text-slate-500">
      Agrega una URL de imagen para ver la vista previa.
    </div>
  );
}

export function ProductManager() {
  const router = useRouter();
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<AuthUser | null>(null);
  const [brands, setBrands] = useState<Brand[]>([]);
  const [items, setItems] = useState<Product[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(8);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [brandId, setBrandId] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState<ProductFormState>(initialForm);

  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalCount / pageSize)), [pageSize, totalCount]);
  const activeCount = useMemo(() => items.filter((product) => product.status).length, [items]);
  const imageCount = useMemo(() => items.filter((product) => product.imageUrl).length, [items]);
  const isAdmin = user?.role === 'Admin';

  useEffect(() => {
    const session = getSession();
    if (!session.token) {
      router.push('/login');
      return;
    }

    setToken(session.token);
    setUser(session.user);
  }, [router]);

  useEffect(() => {
    if (token === null) return;
    const authToken = token;

    let active = true;

    async function load() {
      setLoading(true);
      setError(null);

      try {
        const [brandResult, productResult] = await Promise.all([
          api.brands(authToken),
          api.products({ search, brandId: brandId || undefined, page, pageSize, includeInactive: isAdmin }, authToken)
        ]);

        if (!active) return;

        setBrands(brandResult);
        setItems(productResult.items);
        setTotalCount(productResult.totalCount);
      } catch (err) {
        if (!active) return;
        setError(err instanceof Error ? err.message : 'No se pudieron cargar los productos.');
      } finally {
        if (active) setLoading(false);
      }
    }

    void load();

    return () => {
      active = false;
    };
  }, [brandId, isAdmin, page, pageSize, search, token]);

  function startEdit(product: Product) {
    setForm({
      id: product.id,
      name: product.name,
      description: product.description ?? '',
      imageUrl: product.imageUrl ?? '',
      price: String(product.price),
      stock: String(product.stock),
      brandId: String(product.brandId),
      status: product.status
    });
  }

  function resetForm() {
    setForm(initialForm);
  }

  async function submitForm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!token) return;

    const imageUrlError = getHttpsImageUrlError(form.imageUrl);
    if (imageUrlError) {
      setError(imageUrlError);
      return;
    }

    setSaving(true);
    setError(null);

    try {
      const payload = {
        name: form.name,
        description: form.description.trim() || null,
        imageUrl: normalizeHttpsImageUrl(form.imageUrl),
        price: Number(form.price),
        stock: Number(form.stock),
        brandId: Number(form.brandId),
        status: form.status
      };

      if (form.id) {
        await api.updateProduct(form.id, payload, token);
      } else {
        await api.createProduct(payload, token);
      }

      resetForm();
      const productResult = await api.products({ search, brandId: brandId || undefined, page, pageSize, includeInactive: isAdmin }, token);
      setItems(productResult.items);
      setTotalCount(productResult.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo guardar el producto.');
    } finally {
      setSaving(false);
    }
  }

  async function removeProduct(id: string) {
    if (!token || !window.confirm('¿Desactivar este producto? Se ocultará del catálogo.')) return;

    try {
      await api.deleteProduct(id, token);
      const productResult = await api.products({ search, brandId: brandId || undefined, page, pageSize, includeInactive: isAdmin }, token);
      setItems(productResult.items);
      setTotalCount(productResult.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo desactivar el producto.');
    }
  }

  async function downloadReport() {
    if (!token) return;

    try {
      const url = api.productReportUrl({ search, brandId: brandId || undefined, includeInactive: isAdmin });
      const response = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });

      if (!response.ok) {
        const message = (await response.text()).trim();
        setError(message || 'No se pudo descargar el reporte PDF.');
        return;
      }

      const blob = await response.blob();
      const link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = 'products-report.pdf';
      link.click();
      URL.revokeObjectURL(link.href);
    } catch {
      setError('No se pudo descargar el reporte PDF.');
    }
  }

  function logout() {
    clearSession();
    router.push('/login');
  }

  if (!token) {
    return <div className="p-6 text-sm text-slate-600">Cargando sesión...</div>;
  }

  return (
    <div className="space-y-6">
      <section className="overflow-hidden rounded-[2rem] border border-slate-200 bg-slate-950 text-white shadow-2xl">
        <div className="bg-[radial-gradient(circle_at_top_right,rgba(56,189,248,0.28),transparent_32%),radial-gradient(circle_at_bottom_left,rgba(37,99,235,0.28),transparent_30%)] px-6 py-6 sm:px-8">
          <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
            <div className="max-w-3xl space-y-3">
              <p className="text-sm font-semibold uppercase tracking-[0.35em] text-sky-200">Northstep Studio</p>
              <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">Gestión de calzado</h1>
              <p className="max-w-2xl text-sm leading-6 text-slate-300 sm:text-base">
                Organiza productos, stock, precios e imágenes desde un panel claro.
              </p>
            </div>

            <div className="flex flex-wrap gap-3">
              <button className="button-secondary" onClick={downloadReport}>Descargar PDF</button>
              <button className="button-secondary" onClick={logout}>Cerrar sesión</button>
            </div>
          </div>

          <div className="mt-6 grid gap-3 sm:grid-cols-3">
            <div className="rounded-2xl border border-white/10 bg-white/10 px-4 py-3">
              <p className="text-xs uppercase tracking-[0.25em] text-slate-400">Usuario</p>
              <p className="mt-1 font-medium text-white">{user?.fullName ?? 'Usuario'}</p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-white/10 px-4 py-3">
              <p className="text-xs uppercase tracking-[0.25em] text-slate-400">Productos</p>
              <p className="mt-1 font-medium text-white">{totalCount}</p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-white/10 px-4 py-3">
              <p className="text-xs uppercase tracking-[0.25em] text-slate-400">Catálogo</p>
              <p className="mt-1 font-medium text-emerald-300">{activeCount} activos · {imageCount} con imagen</p>
            </div>
          </div>
        </div>
      </section>

      <div className="grid gap-6 lg:grid-cols-[1.1fr_0.9fr]">
        <section className="card space-y-4 p-6">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 className="text-xl font-semibold text-slate-950">Productos</h2>
              <p className="text-sm text-slate-600">Busca, filtra y administra el catálogo.</p>
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

          <div className="grid gap-3 md:grid-cols-3">
            <input
              className="input"
              placeholder="Buscar productos"
              value={search}
              onChange={(e) => {
                setPage(1);
                setSearch(e.target.value);
              }}
            />
            <select
              className="input"
              value={brandId}
              onChange={(e) => {
                setPage(1);
                setBrandId(e.target.value);
              }}
            >
              <option value="">Todas las marcas</option>
              {brands.map((brand) => (
                <option key={brand.id} value={brand.id}>{brand.name}</option>
              ))}
            </select>
            <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
              {totalCount} productos encontrados
            </div>
          </div>

          {error && <div className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div>}

          <div className="overflow-hidden rounded-2xl border border-slate-200">
            <table className="min-w-full divide-y divide-slate-200 bg-white text-sm">
              <thead className="bg-slate-50 text-left text-slate-600">
                <tr>
                  <th className="px-4 py-3">Producto</th>
                  <th className="px-4 py-3">Marca</th>
                  <th className="px-4 py-3">Precio</th>
                  <th className="px-4 py-3">Stock</th>
                  <th className="px-4 py-3">Estado</th>
                  {isAdmin && <th className="px-4 py-3">Acciones</th>}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {loading ? (
                  <tr><td className="px-4 py-6" colSpan={isAdmin ? 6 : 5}>Cargando...</td></tr>
                ) : items.length === 0 ? (
                  <tr><td className="px-4 py-6" colSpan={isAdmin ? 6 : 5}>No se encontraron productos.</td></tr>
                ) : (
                  items.map((product) => (
                    <tr key={product.id} className={!product.status ? 'bg-slate-50 text-slate-400' : ''}>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-3">
                          <ProductImageCell product={product} />
                          <div>
                            <div className="font-medium text-slate-900">{product.name}</div>
                            <div className="text-xs text-slate-500">{product.description || 'Sin descripción'}</div>
                          </div>
                        </div>
                      </td>
                      <td className="px-4 py-3">{product.brandName}</td>
                      <td className="px-4 py-3">${product.price.toFixed(2)}</td>
                      <td className="px-4 py-3">{product.stock}</td>
                      <td className="px-4 py-3">
                        <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-medium ${product.status ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
                          {product.status ? 'Activo' : 'Inactivo'}
                        </span>
                      </td>
                      {isAdmin && (
                        <td className="px-4 py-3">
                          <div className="flex gap-2">
                            <button className="button-secondary px-3 py-2" onClick={() => startEdit(product)}>Editar</button>
                            <button className="button-danger px-3 py-2" onClick={() => removeProduct(product.id)}>Desactivar</button>
                          </div>
                        </td>
                      )}
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between">
            <button className="button-secondary" disabled={page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>Anterior</button>
            <p className="text-sm text-slate-600">Página {page} de {totalPages}</p>
            <button className="button-secondary" disabled={page >= totalPages} onClick={() => setPage((value) => Math.min(totalPages, value + 1))}>Siguiente</button>
          </div>
        </section>

        <section className="card space-y-4 p-6">
          <div>
            <h2 className="text-xl font-semibold text-slate-950">{form.id ? 'Editar producto' : 'Agregar producto'}</h2>
            <p className="text-sm text-slate-600">{isAdmin ? 'Puedes crear y editar productos.' : 'Estás viendo el catálogo en modo lectura.'}</p>
          </div>

          {isAdmin ? (
            <form className="space-y-3" onSubmit={submitForm}>
              <input className="input" placeholder="Nombre" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
              <textarea className="input min-h-28" placeholder="Descripción" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
              <input
                className="input"
                type="url"
                placeholder="URL HTTPS de imagen (opcional)"
                value={form.imageUrl}
                onChange={(e) => setForm({ ...form, imageUrl: e.target.value })}
              />
              <p className="text-xs text-slate-500">Usa una imagen HTTPS o deja este campo vacío.</p>

              <ProductImagePreview imageUrl={form.imageUrl} />

              <div className="grid gap-3 md:grid-cols-2">
                <input className="input" type="number" step="0.01" min="0.01" placeholder="Precio" value={form.price} onChange={(e) => setForm({ ...form, price: e.target.value })} required />
                <input className="input" type="number" min="0" placeholder="Stock" value={form.stock} onChange={(e) => setForm({ ...form, stock: e.target.value })} required />
              </div>

              <select className="input" value={form.brandId} onChange={(e) => setForm({ ...form, brandId: e.target.value })} required>
                <option value="">Selecciona una marca</option>
                {brands.map((brand) => <option key={brand.id} value={brand.id}>{brand.name}</option>)}
              </select>

              <label className="flex items-center gap-2 text-sm text-slate-700">
                <input type="checkbox" checked={form.status} onChange={(e) => setForm({ ...form, status: e.target.checked })} /> Activo
              </label>

              <div className="flex flex-wrap gap-3">
                <button className="button-primary" disabled={saving} type="submit">{saving ? 'Guardando...' : form.id ? 'Actualizar producto' : 'Crear producto'}</button>
                <button className="button-secondary" type="button" onClick={resetForm}>Limpiar</button>
              </div>
            </form>
          ) : (
            <div className="rounded-2xl bg-slate-50 p-4 text-sm text-slate-600">
              Necesitas una cuenta administradora para crear, editar o desactivar productos.
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
