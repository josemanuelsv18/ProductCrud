import './globals.css';
import type { ReactNode } from 'react';

export const metadata = {
  title: 'Northstep Studio',
  description: 'Gestión de productos para una marca de calzado'
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="es">
      <body>
        <main className="mx-auto min-h-screen max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
          {children}
        </main>
      </body>
    </html>
  );
}
