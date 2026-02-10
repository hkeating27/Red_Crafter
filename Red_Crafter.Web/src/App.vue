<script setup>
    import { ref } from "vue";
    import { getHealth, getUniversalisCurrent, getTopProfits } from "./api.js";

    const world = ref("Malboro");
    const itemId = ref(5345); // example test item

    const apiStatus = ref("Not checked");
    const apiResult = ref(null);

    const uniStatus = ref("Not checked");
    const uniResult = ref(null);

    const topStatus = ref("Not loaded");
    const topData = ref(null);

    async function checkApi() {
        apiStatus.value = "Checking...";
        apiResult.value = null;

        try {
            apiResult.value = await getHealth();
            apiStatus.value = "Connected";
        } catch (err) {
            apiStatus.value = "Failed";
            apiResult.value = { error: err?.message ?? String(err) };
        }
    }

    //TODO: Remove when Top 20 is running
    async function checkUniversalis() {
        uniStatus.value = "Checking...";
        uniResult.value = null;

        try {
            uniResult.value = await getUniversalisCurrent({
                world: world.value,
                itemId: Number(itemId.value),
            });
            uniStatus.value = "Universalis OK";
        } catch (err) {
            uniStatus.value = "Failed";
            uniResult.value = { error: err?.message ?? String(err) };
        }
    }

    async function loadTop20() {
        topStatus.value = "Loading...";
        topData.value = null;

        try {
            topData.value = await getTopProfits(world.value);
            topStatus.value = "Loaded";
        } catch (err) {
            topStatus.value = "Failed";
            topData.value = { error: err?.message ?? String(err) };
        }
    }
</script>

<template>
    <main class="wrap">
        <h1>Red Crafter</h1>
        <p class="hint">Find profitable crafts in Final Fantasy XIV using Universalis market data.</p>

        <section class="card">
            <div class="row">
                <button @click="checkApi">Check API</button>
                <div class="status"><b>Status:</b> {{ apiStatus }}</div>
            </div>

            <pre v-if="apiResult">{{ apiResult }}</pre>
        </section>

        <section class="card">
            <h2>Universalis Test</h2>

            <div class="grid">
                <label>
                    World
                    <input v-model="world" />
                </label>

                <label>
                    Item ID
                    <input v-model="itemId" type="number" />
                </label>
            </div>

            <div class="row">
                <button @click="checkUniversalis">Get Current Min Price</button>
                <div class="status"><b>Status:</b> {{ uniStatus }}</div>
            </div>

            <pre v-if="uniResult">{{ uniResult }}</pre>
        </section>

        <section class="card">
            <h2>Top 20 Profitable (Starter List)</h2>

            <div class="row">
                <button @click="loadTop20">Load Top 20</button>
                <div class="status"><b>Status:</b> {{ topStatus }}</div>
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
                        <td align="right">{{ row.minPricePerUnit }}</td>
                        <td align="right">{{ row.costGil }}</td>
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
        margin: 0 0 0.75rem;
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

    .status {
        opacity: 0.95;
    }

    .grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 0.75rem;
        margin: 0.75rem 0;
    }

    label {
        display: grid;
        gap: 0.4rem;
    }

    input {
        padding: 0.55rem 0.6rem;
        border-radius: 10px;
        border: 1px solid rgba(255, 255, 255, 0.18);
        background: rgba(0, 0, 0, 0.35);
        color: #eaeaea;
    }

    button {
        padding: 0.6rem 1rem;
        border-radius: 10px;
        border: 1px solid rgba(255, 255, 255, 0.25);
        background: rgba(0, 0, 0, 0.5);
        color: #fff;
        cursor: pointer;
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

        .tbl th, .tbl td {
            padding: 0.5rem 0.4rem;
            border-bottom: 1px solid rgba(255,255,255,0.08);
        }

    .hint {
        opacity: 0.85;
        margin: 0 0 1rem;
    }

    .muted {
        opacity: 0.7;
    }
</style>