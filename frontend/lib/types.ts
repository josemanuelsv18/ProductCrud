export type AuthUser = {
  id: number;
  userName: string;
  email: string;
  fullName: string;
  role: string;
};

export type AuthResponse = {
  token: string;
  user: AuthUser;
};

export type Brand = {
  id: number;
  name: string;
};

export type Product = {
  id: string;
  name: string;
  description: string | null;
  imageUrl: string | null;
  price: number;
  stock: number;
  status: boolean;
  brandId: number;
  brandName: string;
  usuarioCreacion: string;
  fechaCreacion: string;
  usuarioModificacion: string | null;
  fechaModificacion: string | null;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};
