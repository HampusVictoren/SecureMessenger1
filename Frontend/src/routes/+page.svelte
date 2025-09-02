<script>
    async function getWeather() {
        const response = await fetch("http://localhost:5271/weatherforecast");
        console.log(response);
    }
</script>

<h1>Welcome to SvelteKit</h1>

GET WEATHER : <button on:click={getWeather}>
    clickhere
</button>