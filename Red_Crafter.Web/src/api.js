const RAW_BASE = import.meta.env.VITE_API_BASE_URL;

// Normalize so we don't get double slashes
const API_BASE = (RAW_BASE || "").replace(/\/+$/, "");

// Debug (remove later if you want)
console.log("VITE_API_BASE_URL =", RAW_BASE);
console.log("API_BASE =", API_BASE);

async function fetchJson(path) {
    // If API_BASE is set, call the backend domain.
    // If not set, fall back to same-origin (local dev proxy).
    const url = API_BASE ? `${API_BASE}${path}` : path;

    console.log("FETCH:", url);

    const res = await fetch(url);
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return await res.json();
}

export function getHealth() {
    return fetchJson(`/api/health`);
}

export function getTopProfits(world) {
    return fetchJson(`/api/profits/top?world=${encodeURIComponent(world)}`);
}

export function getUniversalisCurrent({ world, itemId }) {
    const params = new URLSearchParams({
        world: String(world ?? ""),
        itemId: String(itemId ?? ""),
    });

    return fetchJson(`/api/universalis/current?${params.toString()}`);
}