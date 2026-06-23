import { fireEvent, render, screen } from '@testing-library/react';
import React, { useState } from 'react';
import { describe, expect, it } from 'vitest';
import { ProductImageCell, ProductImagePreview } from '@/components/ProductManager';
import { normalizeHttpsImageUrl } from '@/lib/image-url';

function ProductImagePreviewHarness() {
  const [imageUrl, setImageUrl] = useState('');
  const previewImageUrl = normalizeHttpsImageUrl(imageUrl);

  return (
    <div>
      <input
        aria-label="URL de imagen del producto"
        type="url"
        placeholder="URL HTTPS de imagen (opcional)"
        value={imageUrl}
        onChange={(event) => setImageUrl(event.target.value)}
      />

      <ProductImagePreview imageUrl={previewImageUrl ?? ''} />
    </div>
  );
}

describe('product image UI behavior', () => {
  it('renders a product thumbnail when an image URL exists', () => {
    render(<ProductImageCell product={{ name: 'Aero Runner', imageUrl: 'https://cdn.example.com/aero-runner.jpg' }} />);

    const image = screen.getByRole('img', { name: 'Aero Runner' });
    expect(image.getAttribute('src')).toBe('https://cdn.example.com/aero-runner.jpg');
    expect(image.getAttribute('referrerpolicy')).toBe('no-referrer');
  });

  it('renders a no-image fallback when no image URL exists', () => {
    render(<ProductImageCell product={{ name: 'Classic Loafer', imageUrl: null }} />);

    expect(screen.getByText('Sin imagen')).toBeTruthy();
  });

  it('shows and clears the HTTPS image preview from form input', () => {
    render(<ProductImagePreviewHarness />);

    const input = screen.getByLabelText('URL de imagen del producto');
    expect(screen.getByText('Agrega una URL de imagen para ver la vista previa.')).toBeTruthy();

    fireEvent.change(input, { target: { value: ' https://cdn.example.com/shoes/runner.jpg ' } });
    expect(screen.getByRole('img', { name: 'Vista previa del producto' }).getAttribute('src')).toBe('https://cdn.example.com/shoes/runner.jpg');

    fireEvent.change(input, { target: { value: '' } });
    expect(screen.getByText('Agrega una URL de imagen para ver la vista previa.')).toBeTruthy();
  });
});
