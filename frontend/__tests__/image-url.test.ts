import { describe, expect, it } from 'vitest';
import { getHttpsImageUrlError, normalizeHttpsImageUrl } from '@/lib/image-url';

describe('image URL validation', () => {
  it('accepts blank values and trims secure URLs', () => {
    expect(normalizeHttpsImageUrl('   ')).toBeNull();
    expect(getHttpsImageUrlError('   ')).toBeNull();
    expect(normalizeHttpsImageUrl(' https://cdn.example.com/shoes/runner.jpg ')).toBe('https://cdn.example.com/shoes/runner.jpg');
  });

  it('rejects non-https and overlength URLs', () => {
    expect(getHttpsImageUrlError('http://cdn.example.com/shoes/runner.jpg')).toBe('La URL de imagen debe ser una URL HTTPS válida.');

    const longPath = 'a'.repeat(2040);
    const longUrl = `https://cdn.example.com/${longPath}.jpg`;
    expect(getHttpsImageUrlError(longUrl)).toBe('La URL de imagen debe tener 2048 caracteres o menos.');
    expect(normalizeHttpsImageUrl(longUrl)).toBeNull();
  });
});
