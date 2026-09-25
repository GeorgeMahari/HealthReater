/** Rounded y-domain and tick list around the data, clamped to [floor, ceil]. */
export function niceDomain(values: number[], floor: number, ceil: number): { domain: [number, number]; ticks: number[] } {
  const min = Math.min(...values);
  const max = Math.max(...values);
  const pad = Math.max(8, (max - min) * 0.25);
  const span = max - min + pad * 2;
  const step = [5, 10, 20, 25, 50, 100].find((s) => span / s <= 5) ?? 100;
  const lo = Math.max(floor, Math.floor((min - pad) / step) * step);
  const hi = Math.min(ceil, Math.ceil((max + pad) / step) * step);
  const ticks: number[] = [];
  for (let t = lo; t <= hi + 1e-9; t += step) ticks.push(t);
  return { domain: [lo, hi], ticks };
}
