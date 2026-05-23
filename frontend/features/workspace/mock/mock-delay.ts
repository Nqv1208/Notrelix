export async function mockDelay(min = 120, max = 360) {
  const duration = Math.floor(Math.random() * (max - min + 1)) + min
  return new Promise((resolve) => setTimeout(resolve, duration))
}
