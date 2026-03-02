import * as d3 from "d3";
import { useMemo } from "react";

type Point = {
  x: string;
  y: number;
};

type Props = {
  points: Point[];
  color?: string;
};

export function LineChart({ points, color = "var(--accent-teal)" }: Props) {
  const path = useMemo(() => {
    if (points.length < 2) {
      return "";
    }

    const width = 540;
    const height = 200;
    const x = d3
      .scalePoint()
      .domain(points.map((point) => point.x))
      .range([20, width - 20]);
    const y = d3
      .scaleLinear()
      .domain([d3.min(points, (point) => point.y) ?? 0, d3.max(points, (point) => point.y) ?? 1])
      .nice()
      .range([height - 20, 20]);

    const line = d3
      .line<Point>()
      .x((point) => x(point.x) ?? 0)
      .y((point) => y(point.y))
      .curve(d3.curveCatmullRom.alpha(0.4));

    return line(points) ?? "";
  }, [points]);

  return (
    <svg className="line-chart" viewBox="0 0 540 200" role="img" aria-label="Trend chart">
      <defs>
        <linearGradient id="trendGradient" x1="0%" y1="0%" x2="100%" y2="0%">
          <stop offset="0%" stopColor={color} stopOpacity="0.9" />
          <stop offset="100%" stopColor="var(--accent-amber)" stopOpacity="0.9" />
        </linearGradient>
      </defs>
      <path d={path} fill="none" stroke="url(#trendGradient)" strokeWidth="3" />
    </svg>
  );
}
