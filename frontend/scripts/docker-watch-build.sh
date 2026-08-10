#!/bin/sh
# Diagnostic watchdog for web container build.
# Runs the build in background, samples memory/CPU every 5s, kills after
# WATCHDOG_TIMEOUT seconds, and prints the build log.
# Distinguishes: RSS rising -> memory pressure; RSS flat + CPU ~0 -> deadlock;
# CPU high -> real heavy workload.
set -u

BUILD_CMD="$1"
TIMEOUT_SECONDS="${WATCHDOG_TIMEOUT:-150}"
INTERVAL=5

echo "[watchdog] running: ${BUILD_CMD}"
echo "[watchdog] timeout: ${TIMEOUT_SECONDS}s, interval: ${INTERVAL}s"

sh -c "${BUILD_CMD}" >/tmp/build.log 2>&1 &
BUILD_PID=$!

START=$(date +%s)
LAST_CPU=0
LAST_TOTAL=0
DEADLOCK=1

while kill -0 "${BUILD_PID}" 2>/dev/null; do
  NOW=$(date +%s)
  ELAPSED=$((NOW - START))

  RSS=$(awk '/VmRSS:/{print $2}' "/proc/${BUILD_PID}/status" 2>/dev/null || echo "?")
  MEM_CUR=$(cat /sys/fs/cgroup/memory.current 2>/dev/null || echo "?")
  MEM_MAX=$(cat /sys/fs/cgroup/memory.max 2>/dev/null || echo "?")
  STAT=$(awk '{print $14, $15}' "/proc/${BUILD_PID}/stat" 2>/dev/null || echo "0 0")
  UTIME=$(echo "$STAT" | awk '{print $1}')
  STIME=$(echo "$STAT" | awk '{print $2}')
  TOTAL=$((UTIME + STIME))
  if [ "$ELAPSED" -gt 0 ] && [ -n "$LAST_TOTAL" ] && [ "$LAST_TOTAL" -gt 0 ]; then
    CPU_PCT=$(( (TOTAL - LAST_TOTAL) * 100 / (INTERVAL * 100) ))
    [ "$CPU_PCT" -lt 0 ] && CPU_PCT=0
  else
    CPU_PCT=0
  fi
  LAST_TOTAL=$TOTAL

  echo "[watchdog] t=${ELAPSED}s rss_kb=${RSS} cpu=${CPU_PCT}% mem_current=${MEM_CUR} mem_max=${MEM_MAX}"

  if [ "${ELAPSED}" -ge "${TIMEOUT_SECONDS}" ]; then
    echo "[watchdog] TIMEOUT after ${ELAPSED}s — dumping process tree"
    ps -eo pid,ppid,stat,%cpu,rss,comm 2>/dev/null | grep -E "PID|${BUILD_PID}" | head -15
    kill -9 "${BUILD_PID}" 2>/dev/null
    wait "${BUILD_PID}" 2>/dev/null
    echo "[watchdog] === build log tail ==="
    tail -40 /tmp/build.log
    echo "[watchdog] EXIT_MODE=TIMEOUT(deadlock-or-slow)"
    exit 124
  fi

  sleep "${INTERVAL}"
done

wait "${BUILD_PID}"
RC=$?
echo "[watchdog] build finished exit=${RC}"
echo "[watchdog] === build log ==="
cat /tmp/build.log
exit "${RC}"
