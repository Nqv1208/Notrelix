export interface ClockPort {
  now(): Date;
  isoNow(): string;
}
