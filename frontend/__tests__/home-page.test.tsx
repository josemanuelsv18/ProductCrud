import { render, screen } from '@testing-library/react';
import React from 'react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import HomePage from '@/app/page';

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: { href: string; children: ReactNode }) => (
    <a href={href} {...props}>{children}</a>
  )
}));

describe('HomePage', () => {
  it('shows the Northstep Studio brand and primary CTA', () => {
    render(<HomePage />);

    expect(screen.getByText('Gestión de productos para una marca de calzado.')).toBeTruthy();
    expect(screen.getByText('Control claro para cada par de zapatos.')).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Ver catálogo' }).getAttribute('href')).toBe('/products');
    expect(screen.getAllByRole('link', { name: 'Ingresar' }).every((link) => link.getAttribute('href') === '/login')).toBe(true);
  });
});
