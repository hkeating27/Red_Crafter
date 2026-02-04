export async function getHealth() {
    const res = await fetch("/api/health");
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return await res.json();
}

export async function getUniversalisCurrent(world, itemId) {
    const params = new URLSearchParams({
        world: world ?? "",
        itemId: String(itemId ?? ""),
    });

    const res = await fetch(`/api/universalis/current?${params.toString()}`);
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return await res.json();
}

export async function getTopProfits(world) {
    const params = new URLSearchParams({ world: world ?? "" });

    const res = await fetch(`/api/profits/top?${params.toString()}`);
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return await res.json();
}
