import { describe, expect, it, vi } from 'vitest';
import { createPreviewRegistry } from './previewRegistry';

function deferred<T>() {
  let resolve!: (value: T | PromiseLike<T>) => void;
  let reject!: (reason?: unknown) => void;
  const promise = new Promise<T>((nextResolve, nextReject) => {
    resolve = nextResolve;
    reject = nextReject;
  });
  return { promise, resolve, reject };
}

describe('previewRegistry', () => {
  it('deduplicates in-flight preview requests and caches the ready value', async () => {
    const request = deferred<string>();
    const loader = vi.fn(async () => request.promise);
    const previewRegistry = createPreviewRegistry();

    const first = previewRegistry.resolve('wallpaper.png', loader);
    const second = previewRegistry.resolve('wallpaper.png', loader);

    expect(previewRegistry.read('wallpaper.png')).toEqual({ kind: 'loading' });

    request.resolve('data:image/png;base64,preview');

    await expect(first).resolves.toBe('data:image/png;base64,preview');
    await expect(second).resolves.toBe('data:image/png;base64,preview');
    expect(loader).toHaveBeenCalledTimes(1);
    expect(previewRegistry.read('wallpaper.png')).toEqual({
      kind: 'ready',
      dataUrl: 'data:image/png;base64,preview',
    });
  });

  it('records preview errors and allows a retry for the same image path', async () => {
    const loader = vi
      .fn<() => Promise<string>>()
      .mockRejectedValueOnce(new Error('boom'))
      .mockResolvedValueOnce('data:image/png;base64,retry');
    const previewRegistry = createPreviewRegistry();

    await expect(previewRegistry.resolve('wallpaper.png', loader)).rejects.toMatchObject({
      message: 'boom',
    });
    expect(previewRegistry.read('wallpaper.png')).toEqual({
      kind: 'error',
      error: { code: 'unknown_error', message: 'boom' },
    });

    await expect(previewRegistry.resolve('wallpaper.png', loader)).resolves.toBe(
      'data:image/png;base64,retry',
    );
    expect(loader).toHaveBeenCalledTimes(2);
  });
});
