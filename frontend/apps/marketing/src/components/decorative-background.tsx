export function DecorativeBackground() {
  return (
    <div
      className="pointer-events-none fixed inset-0 -z-10 overflow-hidden"
      aria-hidden="true"
    >
      <div className="absolute inset-0 bg-[linear-gradient(to_right,rgba(21,32,57,0.025)_1px,transparent_1px),linear-gradient(to_bottom,rgba(21,32,57,0.025)_1px,transparent_1px)] bg-[size:72px_72px] [mask-image:linear-gradient(to_bottom,black_0%,transparent_68%)] dark:bg-[linear-gradient(to_right,rgba(255,255,255,0.035)_1px,transparent_1px),linear-gradient(to_bottom,rgba(255,255,255,0.035)_1px,transparent_1px)]" />
      <div className="absolute inset-x-0 top-0 h-[28rem] bg-[linear-gradient(180deg,rgba(236,235,255,0.48),transparent)] dark:bg-[linear-gradient(180deg,rgba(45,43,93,0.34),transparent)]" />
    </div>
  );
}
