import { useMemo, useState } from "react";
import { buildEventsCsv } from "../export/buildEventsCsv";
import { downloadCsv } from "../export/downloadCsv";
import type { RealtimeEvent } from "../types";

type FeedFilter = "all" | RealtimeEvent["type"];

type Props = {
  events: RealtimeEvent[];
};

export function EventFeed({ events }: Props) {
  const [filter, setFilter] = useState<FeedFilter>("all");

  const filteredEvents = useMemo(
    () => events.filter((event) => filter === "all" || event.type === filter),
    [events, filter]
  );

  function exportFilteredEventsCsv() {
    downloadCsv("event-feed.csv", buildEventsCsv(filteredEvents));
  }

  return (
    <section className="event-feed" aria-label="event-feed">
      <header className="panel-header">
        <h3>Event Feed</h3>
        <div className="chip-row">
          <select value={filter} onChange={(event) => setFilter(event.target.value as FeedFilter)} aria-label="event-filter">
            <option value="all">All Events</option>
            <option value="market.tick">Market</option>
            <option value="fleet.status">Fleet</option>
            <option value="combat.result">Combat</option>
            <option value="reputation.update">Reputation</option>
            <option value="connection.state">Connection</option>
          </select>
          <button className="ghost-button" onClick={exportFilteredEventsCsv}>
            Export Events CSV
          </button>
        </div>
      </header>

      <ul className="event-feed-list">
        {filteredEvents.slice(0, 40).map((event, index) => (
          <li key={`${event.type}-${index}`} className="event-row">
            <strong>{event.type}</strong>
            <span>{JSON.stringify(event.payload)}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}
