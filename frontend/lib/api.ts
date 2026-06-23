import type { AuthResponse, Brand, PagedResult, Product } from '@/lib/types';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';

async function request<T>(path: string, options: RequestInit = {}, token?: string | null): Promise<T> {
  const headers = new Headers(options.headers);

  if (options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers,
    cache: 'no-store'
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export const api = {
  login: (body: { identifier: string; password: string }) =>
    request<AuthResponse>('/api/auth/login', { method: 'POST', body: JSON.stringify(body) }),
  register: (body: { userName: string; email: string; fullName: string; password: string }) =>
    request<AuthResponse>('/api/auth/register', { method: 'POST', body: JSON.stringify(body) }),
  brands: (token: string) => request<Brand[]>('/api/brands', {}, token),
  products: (query: Record<string, string | number | boolean | undefined>, token: string) => {
    const params = new URLSearchParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== '') {
        params.set(key, String(value));
      }
    });
    return request<PagedResult<Product>>(`/api/products?${params.toString()}`, {}, token);
  },
  createProduct: (body: unknown, token: string) => request<Product>('/api/products', { method: 'POST', body: JSON.stringify(body) }, token),
  updateProduct: (id: string, body: unknown, token: string) => request<Product>(`/api/products/${id}`, { method: 'PUT', body: JSON.stringify(body) }, token),
  deleteProduct: (id: string, token: string) => request<void>(`/api/products/${id}`, { method: 'DELETE' }, token),
  productReportUrl: (query: Record<string, string | number | boolean | undefined>) => {
    const params = new URLSearchParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== '') {
        params.set(key, String(value));
      }
    });
    return `${API_URL}/api/products/report/pdf?${params.toString()}`;
  }
};
