<script>

        let username = '';
    let password = '';
    let loginError = '';
    let isLoggedIn = false;

    async function login() {
        loginError = '';
        // Example POST request, adjust URL and payload as needed
        const response = await fetch('http://localhost:5271/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        if (response.ok) {
            isLoggedIn = true;
        } else {
            loginError = 'Invalid username or password';
        }
    }

    async function getWeather() {
        const response = await fetch("http://localhost:5271/weatherforecast");
        console.log(await response.json());
    }
</script>

<h1>Welcome to SvelteKit</h1>
<!-- Login Form -->
{#if !isLoggedIn}
    <form on:submit|preventDefault={login} style="margin-bottom: 1em;">
        <div>
            <label>
                Username:
                <input type="text" bind:value={username} required />
            </label>
        </div>
        <div>
            <label>
                Password:
                <input type="password" bind:value={password} required />
            </label>
        </div>
        <button type="submit">Login</button>
        {#if loginError}
            <div style="color: red;">{loginError}</div>
        {/if}
    </form>
{:else}
    <div style="margin-bottom: 1em;">Logged in as <b>{username}</b></div>
{/if}


GET WEATHER : <button on:click={getWeather}>
    clickhere
</button>