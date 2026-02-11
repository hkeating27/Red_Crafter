export async function getHealth() {
    const res = await fetch("/api/health");
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return await res.json();
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

export async function getTopProfits(world) {
    const params = new URLSearchParams({ world: world ?? "" });

    const res = await fetch(`/api/profits/top?${params.toString()}`);
    if (!res.ok) throw new Error(`HTTP ${res.status}`);

    const data = await res.json();

    console.log("TOP PROFITS API RESPONSE:", data);

    return data;
}
