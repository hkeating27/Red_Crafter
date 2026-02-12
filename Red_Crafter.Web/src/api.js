export function getHealth() {
    return fetchJson(`/api/health`);
}

const RAW_BASE = import.meta.env.VITE_API_BASE_URL;

// Normalize so we don't get double slashes
const API_BASE = (RAW_BASE || "").replace(/\/+$/, "");

// Debug (you can remove later)
console.log("VITE_API_BASE_URL =", RAW_BASE);
console.log("API_BASE =", API_BASE);

async function fetchJson(path) {
    const url = API_BASE ? `${API_BASE}${path}` : path; // fallback for local proxy
    console.log("FETCH:", url);

    const res = await fetch(url);
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return await res.json();
}

const API_BASE = import.meta.env.VITE_API_BASE_URL || ""; // "" = same-origin for local dev

export function getTopProfits(world) {
    return fetchJson(`/api/profits/top?world=${encodeURIComponent(world)}`);
}


export async function getUniversalisCurrent(world, itemId) {
    const worldName =
        typeof world === "string"
            ? world
            : world?.world ?? world?.value ?? world?.name ?? "";

    const resolvedItemId =
        itemId ?? (typeof world === "object" ? world?.itemId : undefined) ?? "";

    const params = new URLSearchParams({
        world: worldName,
        itemId: String(resolvedItemId),
    });

    console.log("params:", params.toString());

    const res = await fetch(`/api/universalis/current?${params.toString()}`);
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return await res.json();
}

/*
export async function getTopProfits(world) {
    const params = new URLSearchParams({ world: world ?? "" });

    const res = await fetch(`/api/profits/top?${params.toString()}`);
    if (!res.ok) throw new Error(`HTTP ${res.status}`);

    const data = await res.json();

    console.log("TOP PROFITS API RESPONSE:", data);

    return data;
}*/
