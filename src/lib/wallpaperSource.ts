import type { FitMode, WallpaperDraft, WallpaperSource } from './types';

export const FIT_OPTIONS: FitMode[] = ['Center', 'Tile', 'Stretch', 'Fit', 'Fill', 'Span'];
export const WALLPAPER_SOURCE_MARKERS = {
  none: '__NONE__',
  solidPrefix: '__SOLID__:',
} as const;
export const NONE_MARKER = WALLPAPER_SOURCE_MARKERS.none;
export const SOLID_PREFIX = WALLPAPER_SOURCE_MARKERS.solidPrefix;
export const DEFAULT_FIT_MODE: FitMode = 'Fill';
export const DEFAULT_SOLID_COLOR = '#000000';

/** Indica si el valor recibido pertenece al contrato persistible de fit mode. */
export function isSupportedFitMode(value: string | null | undefined): value is FitMode {
  const candidate = String(value ?? '').trim() as FitMode;
  return FIT_OPTIONS.includes(candidate);
}

/** Devuelve un valor de fit válido o usa `Fill` como fallback seguro. */
export function normalizeFitMode(value: string | null | undefined): FitMode {
  const candidate = String(value ?? '').trim() as FitMode;
  return isSupportedFitMode(candidate) ? candidate : DEFAULT_FIT_MODE;
}

/** Fuerza un color hexadecimal `#rrggbb` válido. */
export function normalizeColorHex(color: string | null | undefined): string {
  const raw = String(color ?? '').trim().toLowerCase();
  return /^#[0-9a-f]{6}$/u.test(raw) ? raw : DEFAULT_SOLID_COLOR;
}

/** Construye el marcador de color sólido consumido por el backend. */
export function makeSolidMarker(color: string): string {
  return `${SOLID_PREFIX}${normalizeColorHex(color)}`;
}

/** Decodifica la representación persistida de un Wallpaper Source. */
export function decodeWallpaperSource(
  imagePath: string | null | undefined,
): WallpaperSource {
  const value = String(imagePath ?? '');
  if (!value || value === NONE_MARKER) {
    return { type: 'none', color: DEFAULT_SOLID_COLOR, imagePath: '' };
  }

  if (value.startsWith(SOLID_PREFIX)) {
    return {
      type: 'solid',
      color: normalizeColorHex(value.slice(SOLID_PREFIX.length)),
      imagePath: '',
    };
  }

  return { type: 'image', color: DEFAULT_SOLID_COLOR, imagePath: value };
}

/** Codifica un Wallpaper Source en la representación persistible del seam con Tauri. */
export function encodeWallpaperSource(source: WallpaperSource): string {
  if (source.type === 'none') {
    return NONE_MARKER;
  }

  if (source.type === 'solid') {
    return makeSolidMarker(source.color);
  }

  return String(source.imagePath ?? '').trim() || NONE_MARKER;
}

/** Interpreta una ruta de wallpaper en una fuente de UI más expresiva. */
export function parseWallpaperSource(imagePath: string | null | undefined): WallpaperSource {
  return decodeWallpaperSource(imagePath);
}

/** Normaliza la ruta persistible de un borrador. */
export function normalizeImagePath(imagePath: string | null | undefined): string {
  return encodeWallpaperSource(decodeWallpaperSource(imagePath));
}

/** Genera un snapshot consistente para comparar cambios locales contra baseline. */
export function snapshotDraft(draft?: Partial<WallpaperDraft>): WallpaperDraft {
  return {
    imagePath: normalizeImagePath(draft?.imagePath),
    fitMode: normalizeFitMode(draft?.fitMode),
  };
}
