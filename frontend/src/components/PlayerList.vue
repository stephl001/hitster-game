<template>
  <div class="space-y-2">
    <div
      v-for="player in players"
      :key="player.nickname"
      class="bg-gray-700 rounded-lg p-3 flex justify-between items-center"
    >
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-full bg-blue-600 flex items-center justify-center font-bold">
          {{ player.nickname.charAt(0).toUpperCase() }}
        </div>
        <span class="font-semibold">{{ player.nickname }}</span>
      </div>
      <div>
        <span v-if="player.isHost" class="text-xs bg-blue-900 text-blue-200 px-2 py-1 rounded">
          Host
        </span>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  players: {
    type: Array,
    required: true
  }
})
</script>
