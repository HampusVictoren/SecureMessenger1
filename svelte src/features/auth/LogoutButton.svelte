<script lang="ts">
  import { logout } from '../../api/bff';

  async function onLogout() {
    try {
      await logout();
      location.reload();
    } catch (e) {
      console.error(e);
      alert('Logout failed');
    }
  }
</script>

<button on:click={onLogout}>Logout</button>