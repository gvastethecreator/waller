import type { AppErrorPayload } from './types';

/** Normaliza errores desconocidos a un payload homogéneo. */
export function normalizeErrorPayload(error: unknown): AppErrorPayload {
  if (
    typeof error === 'object' &&
    error !== null &&
    'code' in error &&
    'message' in error &&
    typeof error.code === 'string' &&
    typeof error.message === 'string'
  ) {
    const details =
      'details' in error && typeof error.details === 'string' ? error.details : undefined;

    return details
      ? {
          code: error.code,
          message: error.message,
          details,
        }
      : {
          code: error.code,
          message: error.message,
        };
  }

  if (error instanceof Error) {
    return { code: 'unknown_error', message: error.message };
  }

  return { code: 'unknown_error', message: String(error ?? 'Unknown error') };
}

/** Devuelve un texto corto y legible para mostrar errores en la UI. */
export function formatError(error: unknown): string {
  const normalized = normalizeErrorPayload(error);
  return normalized.details
    ? `${normalized.message} (${normalized.details})`
    : normalized.message;
}
