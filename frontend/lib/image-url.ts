const MAX_IMAGE_URL_LENGTH = 2048;

export function normalizeHttpsImageUrl(imageUrl: string): string | null {
  const normalized = imageUrl.trim();

  if (!normalized) {
    return null;
  }

  if (normalized.length > MAX_IMAGE_URL_LENGTH) {
    return null;
  }

  try {
    const url = new URL(normalized);
    return url.protocol === 'https:' ? normalized : null;
  } catch {
    return null;
  }
}

export function getHttpsImageUrlError(imageUrl: string): string | null {
  const normalized = imageUrl.trim();

  if (!normalized) {
    return null;
  }

  if (normalized.length > MAX_IMAGE_URL_LENGTH) {
    return 'La URL de imagen debe tener 2048 caracteres o menos.';
  }

  try {
    const url = new URL(normalized);
    if (url.protocol !== 'https:') {
      return 'La URL de imagen debe ser una URL HTTPS válida.';
    }
  } catch {
    return 'La URL de imagen debe ser una URL HTTPS válida.';
  }

  return null;
}
