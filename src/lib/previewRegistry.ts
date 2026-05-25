import { normalizeErrorPayload } from './appErrors';
import type { AppErrorPayload } from './types';

export type PreviewRegistryEntry =
  | { kind: 'not-cached' }
  | { kind: 'loading' }
  | { kind: 'ready'; dataUrl: string }
  | { kind: 'error'; error: AppErrorPayload };

interface PreviewRegistryOptions {
  onChange?: () => void;
}

export interface PreviewRegistry {
  read(imagePath: string): PreviewRegistryEntry;
  resolve(imagePath: string, loader: () => Promise<string>): Promise<string>;
  remember(imagePath: string, dataUrl: string): void;
  clear(imagePath: string): void;
  dispose(): void;
}

const NOT_CACHED_PREVIEW: PreviewRegistryEntry = { kind: 'not-cached' };

function createPreviewRegistryError(): AppErrorPayload {
  return {
    code: 'preview_source_required',
    message: 'Image path cannot be empty',
  };
}

function removeEntry<T>(record: Record<string, T>, key: string): Record<string, T> {
  const { [key]: _removed, ...rest } = record;
  return rest;
}

export function createPreviewRegistry(
  { onChange }: PreviewRegistryOptions = {},
): PreviewRegistry {
  let entries: Record<string, PreviewRegistryEntry> = {};
  const inFlightRequests = new Map<string, Promise<string>>();

  const notifyChange = () => {
    onChange?.();
  };

  const setEntry = (imagePath: string, entry: PreviewRegistryEntry | null) => {
    entries = entry
      ? { ...entries, [imagePath]: entry }
      : removeEntry(entries, imagePath);
    notifyChange();
  };

  return {
    read(imagePath: string): PreviewRegistryEntry {
      const normalizedPath = imagePath.trim();
      if (!normalizedPath) {
        return NOT_CACHED_PREVIEW;
      }

      return entries[normalizedPath] ?? NOT_CACHED_PREVIEW;
    },

    resolve(imagePath: string, loader: () => Promise<string>): Promise<string> {
      const normalizedPath = imagePath.trim();
      if (!normalizedPath) {
        return Promise.reject(createPreviewRegistryError());
      }

      const currentEntry = entries[normalizedPath];
      if (currentEntry?.kind === 'ready') {
        return Promise.resolve(currentEntry.dataUrl);
      }

      const existingRequest = inFlightRequests.get(normalizedPath);
      if (existingRequest) {
        return existingRequest;
      }

      setEntry(normalizedPath, { kind: 'loading' });

      const request = loader()
        .then((dataUrl) => {
          setEntry(normalizedPath, { kind: 'ready', dataUrl });
          return dataUrl;
        })
        .catch((error) => {
          const normalized = normalizeErrorPayload(error);
          setEntry(normalizedPath, { kind: 'error', error: normalized });
          throw normalized;
        })
        .finally(() => {
          inFlightRequests.delete(normalizedPath);
        });

      inFlightRequests.set(normalizedPath, request);
      return request;
    },

    remember(imagePath: string, dataUrl: string) {
      const normalizedPath = imagePath.trim();
      if (!normalizedPath) {
        return;
      }

      setEntry(normalizedPath, { kind: 'ready', dataUrl });
    },

    clear(imagePath: string) {
      const normalizedPath = imagePath.trim();
      if (!normalizedPath) {
        return;
      }

      inFlightRequests.delete(normalizedPath);
      setEntry(normalizedPath, null);
    },

    dispose() {
      inFlightRequests.clear();
      entries = {};
    },
  };
}
