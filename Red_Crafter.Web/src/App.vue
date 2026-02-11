<script setup>
    import { ref, onMounted, watch } from "vue";
    import { getTopProfits } from "./api.js";

    const world = ref("Malboro");

    const topStatus = ref("Loading...");
    const topData = ref(null);

    async function loadTop20() {
        topStatus.value = "Loading...";
        topData.value = null;

        try {
            const data = await getTopProfits(world.value);

            // Sort by Profit % (descending)
            if (data?.items && Array.isArray(data.items)) {
                data.items = [...data.items].sort(
                    (a, b) => (b.profitPercent ?? 0) - (a.profitPercent ?? 0)
                );
            }

            topData.value = data;
            topStatus.value = "Loaded";
        } catch (err) {
            topStatus.value = "Failed";
            topData.value = { error: err?.message ?? String(err) };
        }
    }

    // Load once when the page opens
    onMounted(loadTop20);

    // Reload whenever world changes
    watch(world, () => {
        loadTop20();
    });
</script>

<template>
    <main class="wrap">
        <h1>Red Crafter</h1>
        <p class="hint">Find profitable crafts in Final Fantasy XIV using Universalis market data.</p>

        <section class="card">
            <div class="row topbar">
                <div>
                    <h2 class="title">Top 20 Profitable</h2>
                    <div class="status"><b>Status:</b> {{ topStatus }}</div>
                </div>

                <label class="worldField">
                    World
                    <input v-model="world" />
                </label>
            </div>

            <pre v-if="topData?.error">{{ topData }}</pre>

            <table v-if="topData?.items" class="tbl">
                <thead>
                    <tr>
                        <th align="left">Item</th>
                        <th align="right">Min Price</th>
                        <th align="right">Cost</th>
                        <th align="right">Tax</th>
                        <th align="right">Profit</th>
                        <th align="right">Profit %</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="row in topData.items" :key="row.itemId">
                        <td>{{ row.name }} <span class="muted">({{ row.itemId }})</span></td>
                        <td align="right">{{ row.sellPricePerUnit }}</td>
                        <td align="right">{{ row.costPerUnit }}</td>
                        <td align="right">{{ row.estimatedTaxGil }}</td>
                        <td align="right"><b>{{ row.profitGil }}</b></td>
                        <td align="right">{{ row.profitPercent }}%</td>
                    </tr>
                </tbody>
            </table>
        </section>
    </main>
</template>

<style scoped>
    .wrap {
        max-width: 900px;
        margin: 0 auto;
        padding: 2rem;
        font-family: system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif;
        color: #eaeaea;
    }

    h1 {
        font-size: 3rem;
        margin: 0 0 0.25rem;
    }

    h2 {
        margin: 0;
    }

    .title {
        margin: 0 0 0.25rem;
    }

    .card {
        background: rgba(0, 0, 0, 0.35);
        border: 1px solid rgba(255, 255, 255, 0.12);
        border-radius: 12px;
        padding: 1rem;
        margin: 1rem 0;
    }

    .row {
        display: flex;
        gap: 1rem;
        align-items: center;
        flex-wrap: wrap;
    }

    .topbar {
        justify-content: space-between;
        align-items: flex-end;
    }

    .worldField {
        display: grid;
        gap: 0.4rem;
        min-width: 220px;
    }

    .status {
        opacity: 0.95;
    }

    input {
        padding: 0.55rem 0.6rem;
        border-radius: 10px;
        border: 1px solid rgba(255, 255, 255, 0.18);
        background: rgba(0, 0, 0, 0.35);
        color: #eaeaea;
    }

    pre {
        margin-top: 0.75rem;
        padding: 0.75rem;
        background: rgba(0, 0, 0, 0.55);
        border-radius: 10px;
        overflow: auto;
    }

    .tbl {
        width: 100%;
        margin-top: 0.75rem;
        border-collapse: collapse;
    }

        .tbl th,
        .tbl td {
            padding: 0.5rem 0.4rem;
            border-bottom: 1px solid rgba(255, 255, 255, 0.08);
        }

    .hint {
        opacity: 0.85;
        margin: 0 0 1rem;
    }

    .muted {
        opacity: 0.7;
    }
</style>