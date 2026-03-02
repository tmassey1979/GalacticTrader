import type { TopStatusMetric } from "../topbar/TopStatusMetric";

type Props = {
  metrics: TopStatusMetric[];
};

export function TopStatusBar({ metrics }: Props) {
  return (
    <section className="top-status-grid" aria-label="top-status-bar">
      {metrics.map((metric) => (
        <article key={metric.id} className="top-status-card" title={metric.tooltip}>
          <p className="top-status-label">{metric.label}</p>
          <p className="top-status-value">{metric.value}</p>
          <div className="top-status-trend" aria-hidden="true">
            {metric.trend.map((point, index) => (
              <span key={`${metric.id}-${index}`} style={{ height: `${point}%` }} />
            ))}
          </div>
        </article>
      ))}
    </section>
  );
}
