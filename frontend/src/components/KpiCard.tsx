type Props = {
  label: string;
  value: string;
  accent?: "teal" | "amber" | "ink";
  hint?: string;
};

export function KpiCard({ label, value, accent = "ink", hint }: Props) {
  return (
    <article className={`kpi-card ${accent}`}>
      <p className="kpi-label">{label}</p>
      <p className="kpi-value">{value}</p>
      {hint ? <p className="kpi-hint">{hint}</p> : null}
    </article>
  );
}
