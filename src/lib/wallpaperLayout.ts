import type { LayoutMonitor, MonitorInfo } from './types';

/** Calcula las posiciones del minimapa de monitores respetando topología y relación de aspecto. */
export function computeLayoutMonitors(
  monitors: readonly MonitorInfo[],
  containerWidth: number,
  containerHeight: number,
): LayoutMonitor[] {
  if (!monitors.length || containerWidth <= 0 || containerHeight <= 0) {
    return [];
  }

  const pad = 12;
  const gap = 2;
  const minX = Math.min(...monitors.map((monitor) => monitor.x));
  const minY = Math.min(...monitors.map((monitor) => monitor.y));
  const maxX = Math.max(...monitors.map((monitor) => monitor.x + monitor.width));
  const maxY = Math.max(...monitors.map((monitor) => monitor.y + monitor.height));

  const virtualWidth = Math.max(1, maxX - minX);
  const virtualHeight = Math.max(1, maxY - minY);
  const scaleX = (containerWidth - pad * 2) / virtualWidth;
  const scaleY = (containerHeight - pad * 2) / virtualHeight;
  const scale = Math.max(0.02, Math.min(scaleX, scaleY));
  const contentWidth = virtualWidth * scale;
  const contentHeight = virtualHeight * scale;
  const offsetX = Math.max(pad, (containerWidth - contentWidth) / 2);
  const offsetY = Math.max(pad, (containerHeight - contentHeight) / 2);

  return monitors.map((monitor) => ({
    id: monitor.id,
    displayIndex: monitor.displayIndex,
    width: monitor.width,
    height: monitor.height,
    left: Math.round((monitor.x - minX) * scale + offsetX + gap / 2),
    top: Math.round((monitor.y - minY) * scale + offsetY + gap / 2),
    layoutWidth: Math.max(54, Math.round(monitor.width * scale) - gap),
    layoutHeight: Math.max(36, Math.round(monitor.height * scale) - gap),
  }));
}
