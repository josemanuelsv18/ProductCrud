import React from 'react';
import Link from 'next/link';

export default function HomePage() {
  return (
    <div className="space-y-6 py-6 lg:py-10">
      <header className="flex flex-col gap-4 rounded-[2rem] border border-white/70 bg-white/80 px-6 py-5 shadow-sm backdrop-blur md:flex-row md:items-center md:justify-between">
        <div>
          <p className="text-sm font-semibold uppercase tracking-[0.35em] text-brand-600">Northstep Studio</p>
          <p className="mt-1 text-sm text-slate-500">Gestión de productos para una marca de calzado.</p>
        </div>
        <div className="flex flex-wrap gap-3 text-sm font-medium">
          <Link className="button-secondary" href="/login">Ingresar</Link>
          <Link className="button-primary" href="/products">Ver catálogo</Link>
        </div>
      </header>

      <section className="grid min-h-[620px] gap-8 lg:grid-cols-[1.02fr_0.98fr] lg:items-center">
        <div className="space-y-6">
          <span className="inline-flex rounded-full border border-brand-200 bg-brand-50 px-4 py-2 text-sm font-medium text-brand-700">
            Catálogo de calzado
          </span>
          <div className="space-y-4">
            <h1 className="max-w-3xl text-5xl font-semibold tracking-tight text-slate-950 sm:text-6xl lg:text-7xl">
              Control claro para cada par de zapatos.
            </h1>
            <p className="max-w-xl text-lg leading-8 text-slate-600">
              Administra productos, marcas, precios, stock e imágenes desde un espacio limpio pensado para una tienda de calzado moderna.
            </p>
          </div>

          <div className="flex flex-wrap gap-3">
            <Link className="button-primary px-6 py-3" href="/login">Ingresar</Link>
            <Link className="button-secondary px-6 py-3" href="/products">Abrir productos</Link>
          </div>
        </div>

        <div className="relative overflow-hidden rounded-[2.5rem] border border-slate-200 bg-slate-950 p-6 text-white shadow-2xl">
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_20%_20%,rgba(255,255,255,0.18),transparent_28%),radial-gradient(circle_at_90%_10%,rgba(56,189,248,0.24),transparent_28%),linear-gradient(145deg,rgba(15,23,42,0.2),rgba(2,6,23,0.9))]" />
          <div className="relative space-y-5">
            <div className="flex items-center justify-between text-sm text-slate-300">
              <span>Nueva colección</span>
              <span>Northstep</span>
            </div>

            <div className="overflow-hidden rounded-[2rem] border border-white/10 bg-white/10 p-5 backdrop-blur">
              <div className="flex flex-col gap-5 sm:flex-row sm:items-center">
                <div className="flex h-28 w-28 shrink-0 items-center justify-center rounded-[2rem] bg-gradient-to-br from-sky-200 via-white to-indigo-200 text-3xl font-semibold text-slate-950 shadow-lg">
                  NS
                </div>
                <div>
                  <p className="text-sm uppercase tracking-[0.3em] text-slate-300">Aero Glide</p>
                  <h2 className="mt-2 text-3xl font-semibold">Zapatilla urbana ligera</h2>
                  <p className="mt-3 max-w-sm text-sm leading-6 text-slate-300">Una ficha simple para ver imagen, precio, stock y estado del producto.</p>
                </div>
              </div>

              <div className="mt-5 grid gap-3 sm:grid-cols-3">
                <div className="rounded-2xl bg-white/10 px-4 py-3">
                  <p className="text-xs uppercase tracking-[0.25em] text-slate-400">Precio</p>
                  <p className="mt-1 text-lg font-semibold">$120</p>
                </div>
                <div className="rounded-2xl bg-white/10 px-4 py-3">
                  <p className="text-xs uppercase tracking-[0.25em] text-slate-400">Stock</p>
                  <p className="mt-1 text-lg font-semibold">48 pares</p>
                </div>
                <div className="rounded-2xl bg-white/10 px-4 py-3">
                  <p className="text-xs uppercase tracking-[0.25em] text-slate-400">Estado</p>
                  <p className="mt-1 text-lg font-semibold text-emerald-300">Activo</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
