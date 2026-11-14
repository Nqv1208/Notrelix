import { expect, test, type Page } from "@playwright/test";

/**
 * The move-board dialog against the October 2026 canvas artboard `WM-MoveBoard` (`src/WM-MoveBoard.dc.html`),
 * frames 1 (nothing picked), 3 (three people left behind) and 4 (nobody left behind), light and dark, at 700 px.
 * Each row is one box of the artboard read in Chromium: [x, y, width, height, font size, font weight, radius,
 * background, colour] relative to the dialog's top-left. Kinds: `data` x and width only (the box is as tall as the
 * mock world's places), `head` x, y, width and height (above the list), `size` width and height only (below the list,
 * where the mock world has more Spaces than the mockup). Fonts and radii are exact; colours are exact except
 * where the mockup draws a tone the design tokens do not have (the amber chip, the green and red washes in dark,
 * the selected row, which are skipped). The mock world has seven Spaces where the mockup has four, so the
 * per-place people count (no producer: GetSpaceTree carries no member count) and the blocked "Nhân sự" row are
 * measured on the picked and current rows only. Frames 12 and 13 are the 390 px sheet. The loading frame 2 and the network-error frames 8
 * (700 px) and 14 (390 px) need their own servers, as the mock world's latency and faults are set at start:
 *   loading: VITE_MOCK_LATENCY=slow
 *   failed:  VITE_MOCK_FAULTS=/boards/mock-board-erp/move-to-space=network
 * They are measured in the same way as the others, and are skipped on the default server.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "instant";
const MOVE_FAULT = "/boards/mock-board-erp/move-to-space=network";
const SETTINGS =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/settings?from=table&section=general";

type Measure = readonly [
  number,
  number,
  number,
  number,
  string,
  string,
  string,
  string,
  string,
];
interface Frame {
  readonly pairs: readonly { readonly sel: string; readonly kind: string }[];
  readonly pick: string;
  readonly viewport: readonly [number, number];
  /** The 390 px sheet: the app's sheet is 390 wide where the artboard's is 388 inside its frame border. */
  readonly mobile?: boolean;
  /** Needs its own server: `loading` holds the impact read open (VITE_MOCK_LATENCY=slow), `failed` refuses the move. */
  readonly state?: "loading" | "failed";
  readonly sang: readonly Measure[];
  readonly dem: readonly Measure[];
}
// prettier-ignore
const FRAMES: Record<string, Frame> = {"1":{"pairs":[{"sel":"[data-f=\"mdlg\"]","kind":"data"},{"sel":"[data-f=\"mh\"]","kind":"head"},{"sel":"[data-f=\"mt\"]","kind":"head"},{"sel":"[data-f=\"msub\"]","kind":"head"},{"sel":"[data-f=\"mclose\"]","kind":"head"},{"sel":"[data-f=\"places\"]","kind":"data"},{"sel":"[data-f=\"place\"]:nth-child(1)","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) > span:nth-child(1)","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) [data-f=\"mark\"]","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) b","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) small","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) > span:last-child","kind":"head"},{"sel":"[data-f=\"mf\"]","kind":"data"},{"sel":"[data-f=\"mcancel\"]","kind":"size"},{"sel":"[data-f=\"mgo\"]","kind":"size"},{"sel":"[data-f=\"mf\"] > span","kind":"size"}],"pick":"","viewport":[1440,1100],"mobile":false,"sang":[[0,0,600,452.64,"14px","400","14px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[1,1,598,88.33,"14px","400","0px","transparent","rgb(22, 26, 33)"],[21,17,516,24.64,"17px","650","0px","transparent","rgb(22, 26, 33)"],[21,43.64,516,37.69,"13px","400","0px","transparent","rgb(79, 88, 102)"],[547,33.16,32,32,"14px","400","7px","transparent","rgb(79, 88, 102)"],[21,97.33,558,281.31,"13.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[22,98.33,556,55.06,"13.5px","400","0px","transparent","rgb(22, 26, 33)"],[36,116.86,18,18,"13.5px","400","50%","transparent","rgb(22, 26, 33)"],[66,111.86,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[106,106.33,361.45,19.56,"13.5px","500","0px","transparent","rgb(22, 26, 33)"],[106,127.89,361.45,17.5,"12.5px","400","0px","transparent","rgb(95, 104, 117)"],[479.45,114.86,84.55,22,"12px","600","6px","rgb(238, 240, 244)","rgb(62, 71, 87)"],[1,394.64,598,57,"14px","400","0px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[404.61,407.64,50.42,32,"13px","500","7px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[463.03,407.64,115.97,32,"13px","500","7px","rgb(230, 233, 238)","rgb(95, 104, 117)"],[21,414.89,375.61,17.5,"12.5px","400","0px","transparent","rgb(95, 104, 117)"]],"dem":[[0,0,600,452.64,"14px","400","14px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[1,1,598,88.33,"14px","400","0px","transparent","rgb(230, 233, 238)"],[21,17,516,24.64,"17px","650","0px","transparent","rgb(230, 233, 238)"],[21,43.64,516,37.69,"13px","400","0px","transparent","rgb(170, 179, 192)"],[547,33.16,32,32,"14px","400","7px","transparent","rgb(170, 179, 192)"],[21,97.33,558,281.31,"13.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[22,98.33,556,55.06,"13.5px","400","0px","transparent","rgb(230, 233, 238)"],[36,116.86,18,18,"13.5px","400","50%","transparent","rgb(230, 233, 238)"],[66,111.86,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[106,106.33,361.45,19.56,"13.5px","500","0px","transparent","rgb(230, 233, 238)"],[106,127.89,361.45,17.5,"12.5px","400","0px","transparent","rgb(143, 153, 167)"],[479.45,114.86,84.55,22,"12px","600","6px","rgb(35, 43, 53)","rgb(195, 203, 214)"],[1,394.64,598,57,"14px","400","0px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[404.61,407.64,50.42,32,"13px","500","7px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[463.03,407.64,115.97,32,"13px","500","7px","rgb(35, 42, 52)","rgb(143, 153, 167)"],[21,414.89,375.61,17.5,"12.5px","400","0px","transparent","rgb(143, 153, 167)"]]},"2":{"pairs":[{"sel":"[data-f=\"mdlg\"]","kind":"data"},{"sel":"[data-f=\"mh\"]","kind":"head"},{"sel":"[data-f=\"mt\"]","kind":"head"},{"sel":"[data-f=\"msub\"]","kind":"head"},{"sel":"[data-f=\"mclose\"]","kind":"head"},{"sel":"[data-f=\"places\"]","kind":"data"},{"sel":"[data-f=\"place\"]:nth-child(1)","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) [data-f=\"mark\"]","kind":"head"},{"sel":"[data-f=\"impact\"] > [role=\"status\"]","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(1)","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(1) > :nth-child(1)","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(1) > :nth-child(2)","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(1) > :nth-child(2) > :nth-child(1)","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(1) > :nth-child(2) > :nth-child(2)","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(2)","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(2) > :nth-child(1)","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(2) > :nth-child(2)","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(2) > :nth-child(2) > :nth-child(1)","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(2) > :nth-child(2) > :nth-child(2)","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(3)","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(3) > :nth-child(1)","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(3) > :nth-child(2)","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(3) > :nth-child(2) > :nth-child(1)","kind":"size"},{"sel":"[data-f=\"impact\"] [role=\"status\"] > div:nth-child(3) > :nth-child(2) > :nth-child(2)","kind":"size"},{"sel":"[data-f=\"mf\"]","kind":"data"},{"sel":"[data-f=\"mf\"] > span","kind":"size"},{"sel":"[data-f=\"mcancel\"]","kind":"size"},{"sel":"[data-f=\"mgo\"]","kind":"size"}],"sang":[[0,0,600,660.64,"14px","400","14px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[1,1,598,88.33,"14px","400","0px","transparent","rgb(22, 26, 33)"],[21,17,516,24.64,"17px","650","0px","transparent","rgb(22, 26, 33)"],[21,43.64,516,37.69,"13px","400","0px","transparent","rgb(79, 88, 102)"],[547,33.16,32,32,"14px","400","7px","transparent","rgb(79, 88, 102)"],[21,97.33,558,281.31,"13.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[22,98.33,556,55.06,"13.5px","400","0px","transparent","rgb(22, 26, 33)"],[66,111.86,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[21,392.64,558,194,"13.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[22,393.64,556,64,"13.5px","400","0px","transparent","rgb(22, 26, 33)"],[36,411.64,28,28,"13.5px","400","7px","rgb(230, 233, 238)","rgb(22, 26, 33)"],[76,411.64,488,28,"13.5px","400","0px","transparent","rgb(22, 26, 33)"],[76,411.64,224.47,10,"13.5px","400","4px","rgb(230, 233, 238)","rgb(22, 26, 33)"],[76,429.64,400.16,10,"13.5px","400","4px","rgb(230, 233, 238)","rgb(22, 26, 33)"],[22,457.64,556,64,"13.5px","400","0px","transparent","rgb(22, 26, 33)"],[36,476.14,28,28,"13.5px","400","7px","rgb(230, 233, 238)","rgb(22, 26, 33)"],[76,476.14,488,28,"13.5px","400","0px","transparent","rgb(22, 26, 33)"],[76,476.14,195.19,10,"13.5px","400","4px","rgb(230, 233, 238)","rgb(22, 26, 33)"],[76,494.14,370.88,10,"13.5px","400","4px","rgb(230, 233, 238)","rgb(22, 26, 33)"],[22,521.64,556,64,"13.5px","400","0px","transparent","rgb(22, 26, 33)"],[36,540.14,28,28,"13.5px","400","7px","rgb(230, 233, 238)","rgb(22, 26, 33)"],[76,540.14,488,28,"13.5px","400","0px","transparent","rgb(22, 26, 33)"],[76,540.14,253.75,10,"13.5px","400","4px","rgb(230, 233, 238)","rgb(22, 26, 33)"],[76,558.14,292.8,10,"13.5px","400","4px","rgb(230, 233, 238)","rgb(22, 26, 33)"],[1,602.64,598,57,"14px","400","0px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[21,622.89,375.61,17.5,"12.5px","400","0px","transparent","rgb(95, 104, 117)"],[404.61,615.64,50.42,32,"13px","500","7px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[463.03,615.64,115.97,32,"13px","500","7px","rgb(230, 233, 238)","rgb(95, 104, 117)"]],"dem":[[0,0,600,660.64,"14px","400","14px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[1,1,598,88.33,"14px","400","0px","transparent","rgb(230, 233, 238)"],[21,17,516,24.64,"17px","650","0px","transparent","rgb(230, 233, 238)"],[21,43.64,516,37.69,"13px","400","0px","transparent","rgb(170, 179, 192)"],[547,33.16,32,32,"14px","400","7px","transparent","rgb(170, 179, 192)"],[21,97.33,558,281.31,"13.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[22,98.33,556,55.06,"13.5px","400","0px","transparent","rgb(230, 233, 238)"],[66,111.86,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[21,392.64,558,194,"13.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[22,393.64,556,64,"13.5px","400","0px","transparent","rgb(230, 233, 238)"],[36,411.64,28,28,"13.5px","400","7px","rgb(35, 42, 52)","rgb(230, 233, 238)"],[76,411.64,488,28,"13.5px","400","0px","transparent","rgb(230, 233, 238)"],[76,411.64,224.47,10,"13.5px","400","4px","rgb(35, 42, 52)","rgb(230, 233, 238)"],[76,429.64,400.16,10,"13.5px","400","4px","rgb(35, 42, 52)","rgb(230, 233, 238)"],[22,457.64,556,64,"13.5px","400","0px","transparent","rgb(230, 233, 238)"],[36,476.14,28,28,"13.5px","400","7px","rgb(35, 42, 52)","rgb(230, 233, 238)"],[76,476.14,488,28,"13.5px","400","0px","transparent","rgb(230, 233, 238)"],[76,476.14,195.19,10,"13.5px","400","4px","rgb(35, 42, 52)","rgb(230, 233, 238)"],[76,494.14,370.88,10,"13.5px","400","4px","rgb(35, 42, 52)","rgb(230, 233, 238)"],[22,521.64,556,64,"13.5px","400","0px","transparent","rgb(230, 233, 238)"],[36,540.14,28,28,"13.5px","400","7px","rgb(35, 42, 52)","rgb(230, 233, 238)"],[76,540.14,488,28,"13.5px","400","0px","transparent","rgb(230, 233, 238)"],[76,540.14,253.75,10,"13.5px","400","4px","rgb(35, 42, 52)","rgb(230, 233, 238)"],[76,558.14,292.8,10,"13.5px","400","4px","rgb(35, 42, 52)","rgb(230, 233, 238)"],[1,602.64,598,57,"14px","400","0px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[21,622.89,375.61,17.5,"12.5px","400","0px","transparent","rgb(143, 153, 167)"],[404.61,615.64,50.42,32,"13px","500","7px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[463.03,615.64,115.97,32,"13px","500","7px","rgb(35, 42, 52)","rgb(143, 153, 167)"]],"pick":"Kế toán","viewport":[1440,1300],"mobile":false,"state":"loading"},"3":{"pairs":[{"sel":"[data-f=\"mdlg\"]","kind":"data"},{"sel":"[data-f=\"mh\"]","kind":"head"},{"sel":"[data-f=\"mt\"]","kind":"head"},{"sel":"[data-f=\"msub\"]","kind":"head"},{"sel":"[data-f=\"mclose\"]","kind":"head"},{"sel":"[data-f=\"places\"]","kind":"data"},{"sel":"[data-f=\"place\"]:nth-child(1)","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) > span:nth-child(1)","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) [data-f=\"mark\"]","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) b","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) small","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) > span:last-child","kind":"head"},{"sel":"[data-f=\"mf\"]","kind":"data"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"]","kind":"size"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"] > span:nth-child(1)","kind":"size"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"] [data-f=\"mark\"]","kind":"size"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"] b","kind":"size"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"] small","kind":"size"},{"sel":"[data-f=\"sections\"] > :nth-child(1)","kind":"size"},{"sel":"[data-f=\"sections\"] > :nth-child(1) b","kind":"size"},{"sel":"[data-f=\"sections\"] > :nth-child(1) small","kind":"size"},{"sel":"[data-f=\"warn\"]","kind":"size"},{"sel":"[data-f=\"warn\"] b","kind":"size"},{"sel":"[data-f=\"stranded\"]","kind":"size"},{"sel":"[data-f=\"srow\"]:nth-child(1)","kind":"size"},{"sel":"[data-f=\"cbx\"]","kind":"size"},{"sel":"[data-f=\"srow\"]:nth-child(1) > span:nth-child(2)","kind":"size"},{"sel":"[data-f=\"srow\"]:nth-child(1) b","kind":"size"},{"sel":"[data-f=\"srow\"]:nth-child(1) small","kind":"size"},{"sel":"[data-f=\"srow\"]:nth-child(1) > span:last-child","kind":"size"},{"sel":"[data-f=\"sections\"] > :nth-child(4)","kind":"size"},{"sel":"[data-f=\"sections\"] > :nth-child(5)","kind":"size"},{"sel":"[data-f=\"mcancel\"]","kind":"size"},{"sel":"[data-f=\"mwithout\"]","kind":"size"},{"sel":"[data-f=\"mgo\"]","kind":"size"}],"pick":"Kế toán","viewport":[1440,1300],"mobile":false,"sang":[[0,0,600,1001.28,"14px","400","14px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[1,1,598,88.33,"14px","400","0px","transparent","rgb(22, 26, 33)"],[21,17,516,24.64,"17px","650","0px","transparent","rgb(22, 26, 33)"],[21,43.64,516,37.69,"13px","400","0px","transparent","rgb(79, 88, 102)"],[547,33.16,32,32,"14px","400","7px","transparent","rgb(79, 88, 102)"],[21,97.33,558,281.31,"13.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[22,98.33,556,55.06,"13.5px","400","0px","transparent","rgb(22, 26, 33)"],[36,116.86,18,18,"13.5px","400","50%","transparent","rgb(22, 26, 33)"],[66,111.86,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[106,106.33,361.45,19.56,"13.5px","500","0px","transparent","rgb(22, 26, 33)"],[106,127.89,361.45,17.5,"12.5px","400","0px","transparent","rgb(95, 104, 117)"],[479.45,114.86,84.55,22,"12px","600","6px","rgb(238, 240, 244)","rgb(62, 71, 87)"],[1,943.28,598,57,"14px","400","0px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[22,153.39,556,56.06,"13.5px","400","0px","rgb(231, 240, 252)","rgb(22, 26, 33)"],[36,172.92,18,18,"13.5px","400","50%","transparent","rgb(22, 26, 33)"],[66,167.92,28,28,"11px","700","6px","rgb(154, 106, 18)","rgb(255, 255, 255)"],[106,162.39,458,19.56,"13.5px","600","0px","transparent","rgb(22, 26, 33)"],[106,183.95,458,17.5,"12.5px","400","0px","transparent","rgb(95, 104, 117)"],[21,392.64,558,74.56,"13.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[64,401.64,500,19.56,"13.5px","600","0px","transparent","rgb(22, 26, 33)"],[64,423.2,500,35,"12.5px","400","0px","transparent","rgb(95, 104, 117)"],[21,479.2,558,76.27,"13px","400","10px","rgb(255, 244, 214)","rgb(138, 90, 0)"],[61,489.2,504,18.89,"13.5px","600","0px","transparent","rgb(22, 26, 33)"],[21,567.47,558,169.19,"13.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[22,568.47,556,55.06,"13.5px","400","0px","transparent","rgb(22, 26, 33)"],[36,587,18,18,"13.5px","400","5px","rgb(22, 102, 204)","rgb(255, 255, 255)"],[66,582,28,28,"11px","700","50%","rgb(30, 111, 208)","rgb(255, 255, 255)"],[106,576.47,309.55,19.56,"13.5px","600","0px","transparent","rgb(22, 26, 33)"],[106,598.03,309.55,17.5,"12.5px","400","0px","transparent","rgb(95, 104, 117)"],[427.55,585,136.45,22,"12px","600","6px","rgb(251, 240, 211)","rgb(110, 79, 10)"],[21,748.66,558,74.56,"13.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[21,835.22,558,92.06,"13.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[157.52,956.28,50.42,32,"13px","500","7px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[215.94,956.28,171.8,32,"13px","500","7px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[395.73,956.28,183.27,32,"13px","600","7px","rgb(22, 102, 204)","rgb(255, 255, 255)"]],"dem":[[0,0,600,1001.28,"14px","400","14px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[1,1,598,88.33,"14px","400","0px","transparent","rgb(230, 233, 238)"],[21,17,516,24.64,"17px","650","0px","transparent","rgb(230, 233, 238)"],[21,43.64,516,37.69,"13px","400","0px","transparent","rgb(170, 179, 192)"],[547,33.16,32,32,"14px","400","7px","transparent","rgb(170, 179, 192)"],[21,97.33,558,281.31,"13.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[22,98.33,556,55.06,"13.5px","400","0px","transparent","rgb(230, 233, 238)"],[36,116.86,18,18,"13.5px","400","50%","transparent","rgb(230, 233, 238)"],[66,111.86,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[106,106.33,361.45,19.56,"13.5px","500","0px","transparent","rgb(230, 233, 238)"],[106,127.89,361.45,17.5,"12.5px","400","0px","transparent","rgb(143, 153, 167)"],[479.45,114.86,84.55,22,"12px","600","6px","rgb(35, 43, 53)","rgb(195, 203, 214)"],[1,943.28,598,57,"14px","400","0px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[22,153.39,556,56.06,"13.5px","400","0px","rgb(87, 168, 255)","rgb(230, 233, 238)"],[36,172.92,18,18,"13.5px","400","50%","transparent","rgb(230, 233, 238)"],[66,167.92,28,28,"11px","700","6px","rgb(154, 106, 18)","rgb(255, 255, 255)"],[106,162.39,458,19.56,"13.5px","600","0px","transparent","rgb(230, 233, 238)"],[106,183.95,458,17.5,"12.5px","400","0px","transparent","rgb(143, 153, 167)"],[21,392.64,558,74.56,"13.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[64,401.64,500,19.56,"13.5px","600","0px","transparent","rgb(230, 233, 238)"],[64,423.2,500,35,"12.5px","400","0px","transparent","rgb(143, 153, 167)"],[21,479.2,558,76.27,"13px","400","10px","rgb(241, 191, 78)","rgb(242, 193, 78)"],[61,489.2,504,18.89,"13.5px","600","0px","transparent","rgb(230, 233, 238)"],[21,567.47,558,169.19,"13.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[22,568.47,556,55.06,"13.5px","400","0px","transparent","rgb(230, 233, 238)"],[36,587,18,18,"13.5px","400","5px","rgb(90, 169, 255)","rgb(4, 17, 31)"],[66,582,28,28,"11px","700","50%","rgb(30, 111, 208)","rgb(255, 255, 255)"],[106,576.47,309.55,19.56,"13.5px","600","0px","transparent","rgb(230, 233, 238)"],[106,598.03,309.55,17.5,"12.5px","400","0px","transparent","rgb(143, 153, 167)"],[427.55,585,136.45,22,"12px","600","6px","rgb(48, 40, 26)","rgb(242, 193, 78)"],[21,748.66,558,74.56,"13.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[21,835.22,558,92.06,"13.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[157.52,956.28,50.42,32,"13px","500","7px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[215.94,956.28,171.8,32,"13px","500","7px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[395.73,956.28,183.27,32,"13px","600","7px","rgb(90, 169, 255)","rgb(4, 17, 31)"]]},"4":{"pairs":[{"sel":"[data-f=\"mdlg\"]","kind":"data"},{"sel":"[data-f=\"mh\"]","kind":"head"},{"sel":"[data-f=\"mt\"]","kind":"head"},{"sel":"[data-f=\"msub\"]","kind":"head"},{"sel":"[data-f=\"mclose\"]","kind":"head"},{"sel":"[data-f=\"places\"]","kind":"data"},{"sel":"[data-f=\"place\"]:nth-child(1)","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) > span:nth-child(1)","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) [data-f=\"mark\"]","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) b","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) small","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) > span:last-child","kind":"head"},{"sel":"[data-f=\"mf\"]","kind":"data"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"]","kind":"size"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"] > span:nth-child(1)","kind":"size"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"] [data-f=\"mark\"]","kind":"size"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"] b","kind":"size"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"] small","kind":"size"},{"sel":"[data-f=\"sections\"] > :nth-child(1)","kind":"size"},{"sel":"[data-f=\"ok\"]","kind":"size"},{"sel":"[data-f=\"ok\"] b","kind":"size"},{"sel":"[data-f=\"sections\"] > :nth-child(3)","kind":"size"},{"sel":"[data-f=\"sections\"] > :nth-child(4)","kind":"size"},{"sel":"[data-f=\"mcancel\"]","kind":"size"},{"sel":"[data-f=\"mgo\"]","kind":"h"}],"pick":"Marketing","viewport":[1440,1300],"mobile":false,"sang":[[0,0,600,801.91,"14px","400","14px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[1,1,598,88.33,"14px","400","0px","transparent","rgb(22, 26, 33)"],[21,17,516,24.64,"17px","650","0px","transparent","rgb(22, 26, 33)"],[21,43.64,516,37.69,"13px","400","0px","transparent","rgb(79, 88, 102)"],[547,33.16,32,32,"14px","400","7px","transparent","rgb(79, 88, 102)"],[21,97.33,558,281.31,"13.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[22,98.33,556,55.06,"13.5px","400","0px","transparent","rgb(22, 26, 33)"],[36,116.86,18,18,"13.5px","400","50%","transparent","rgb(22, 26, 33)"],[66,111.86,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[106,106.33,361.45,19.56,"13.5px","500","0px","transparent","rgb(22, 26, 33)"],[106,127.89,361.45,17.5,"12.5px","400","0px","transparent","rgb(95, 104, 117)"],[479.45,114.86,84.55,22,"12px","600","6px","rgb(238, 240, 244)","rgb(62, 71, 87)"],[1,743.91,598,57,"14px","400","0px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[22,209.45,556,56.06,"13.5px","400","0px","rgb(231, 240, 252)","rgb(22, 26, 33)"],[36,228.98,18,18,"13.5px","400","50%","transparent","rgb(22, 26, 33)"],[66,223.98,28,28,"11px","700","6px","rgb(194, 65, 12)","rgb(255, 255, 255)"],[106,218.45,458,19.56,"13.5px","600","0px","transparent","rgb(22, 26, 33)"],[106,240.02,458,17.5,"12.5px","400","0px","transparent","rgb(95, 104, 117)"],[21,392.64,558,74.56,"13.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[21,479.2,558,58.08,"13px","400","10px","rgb(229, 244, 236)","rgb(30, 122, 76)"],[61,489.2,504,18.89,"13.5px","600","0px","transparent","rgb(22, 26, 33)"],[21,549.28,558,74.56,"13.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[21,635.84,558,92.06,"13.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[342.8,756.91,50.42,32,"13px","500","7px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[401.22,756.91,177.78,32,"13px","600","7px","rgb(22, 102, 204)","rgb(255, 255, 255)"]],"dem":[[0,0,600,801.91,"14px","400","14px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[1,1,598,88.33,"14px","400","0px","transparent","rgb(230, 233, 238)"],[21,17,516,24.64,"17px","650","0px","transparent","rgb(230, 233, 238)"],[21,43.64,516,37.69,"13px","400","0px","transparent","rgb(170, 179, 192)"],[547,33.16,32,32,"14px","400","7px","transparent","rgb(170, 179, 192)"],[21,97.33,558,281.31,"13.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[22,98.33,556,55.06,"13.5px","400","0px","transparent","rgb(230, 233, 238)"],[36,116.86,18,18,"13.5px","400","50%","transparent","rgb(230, 233, 238)"],[66,111.86,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[106,106.33,361.45,19.56,"13.5px","500","0px","transparent","rgb(230, 233, 238)"],[106,127.89,361.45,17.5,"12.5px","400","0px","transparent","rgb(143, 153, 167)"],[479.45,114.86,84.55,22,"12px","600","6px","rgb(35, 43, 53)","rgb(195, 203, 214)"],[1,743.91,598,57,"14px","400","0px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[22,209.45,556,56.06,"13.5px","400","0px","rgb(87, 168, 255)","rgb(230, 233, 238)"],[36,228.98,18,18,"13.5px","400","50%","transparent","rgb(230, 233, 238)"],[66,223.98,28,28,"11px","700","6px","rgb(194, 65, 12)","rgb(255, 255, 255)"],[106,218.45,458,19.56,"13.5px","600","0px","transparent","rgb(230, 233, 238)"],[106,240.02,458,17.5,"12.5px","400","0px","transparent","rgb(143, 153, 167)"],[21,392.64,558,74.56,"13.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[21,479.2,558,58.08,"13px","400","10px","rgb(85, 198, 135)","rgb(86, 201, 138)"],[61,489.2,504,18.89,"13.5px","600","0px","transparent","rgb(230, 233, 238)"],[21,549.28,558,74.56,"13.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[21,635.84,558,92.06,"13.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[342.8,756.91,50.42,32,"13px","500","7px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[401.22,756.91,177.78,32,"13px","600","7px","rgb(90, 169, 255)","rgb(4, 17, 31)"]]},"8":{"pairs":[{"sel":"[data-f=\"mdlg\"]","kind":"data"},{"sel":"[data-f=\"mh\"]","kind":"head"},{"sel":"[data-f=\"mt\"]","kind":"head"},{"sel":"[data-f=\"msub\"]","kind":"head"},{"sel":"[data-f=\"mclose\"]","kind":"head"},{"sel":"[data-f=\"merr\"]","kind":"head"},{"sel":"[data-f=\"merr\"] > svg","kind":"head"},{"sel":"[data-f=\"merr\"] > span","kind":"head"},{"sel":"[data-f=\"merr\"] b","kind":"head"},{"sel":"[data-f=\"merr\"] > span > span","kind":"head"},{"sel":"[data-f=\"places\"]","kind":"data"},{"sel":"[data-f=\"place\"]:nth-child(1)","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) [data-f=\"mark\"]","kind":"head"},{"sel":"[data-f=\"mf\"]","kind":"data"},{"sel":"[data-f=\"mcancel\"]","kind":"size"},{"sel":"[data-f=\"mgo\"]","kind":"size"},{"sel":"[data-f=\"mgo\"] > svg","kind":"size"}],"sang":[[0,0,600,542.2,"14px","400","14px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[1,1,598,88.33,"14px","400","0px","transparent","rgb(22, 26, 33)"],[21,17,516,24.64,"17px","650","0px","transparent","rgb(22, 26, 33)"],[21,43.64,516,37.69,"13px","400","0px","transparent","rgb(79, 88, 102)"],[547,33.16,32,32,"14px","400","7px","transparent","rgb(79, 88, 102)"],[21,97.33,558,75.56,"13px","400","10px","rgb(253, 236, 239)","rgb(196, 35, 64)"],[35,109.33,16,16,"13px","400","0px","transparent","rgb(196, 35, 64)"],[61,107.33,504,55.56,"13px","400","0px","transparent","rgb(196, 35, 64)"],[61,107.33,504,18.19,"13px","600","0px","transparent","rgb(22, 26, 33)"],[61,126.52,504,36.38,"13px","400","0px","transparent","rgb(196, 35, 64)"],[21,186.89,558,281.31,"13.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[22,187.89,556,55.06,"13.5px","400","0px","transparent","rgb(22, 26, 33)"],[66,201.42,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[1,484.2,598,57,"14px","400","0px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[429.36,497.2,50.42,32,"13px","500","7px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[487.78,497.2,91.22,32,"13px","600","7px","rgb(22, 102, 204)","rgb(255, 255, 255)"],[500.78,505.2,16,16,"13px","600","0px","transparent","rgb(255, 255, 255)"]],"dem":[[0,0,600,542.2,"14px","400","14px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[1,1,598,88.33,"14px","400","0px","transparent","rgb(230, 233, 238)"],[21,17,516,24.64,"17px","650","0px","transparent","rgb(230, 233, 238)"],[21,43.64,516,37.69,"13px","400","0px","transparent","rgb(170, 179, 192)"],[547,33.16,32,32,"14px","400","7px","transparent","rgb(170, 179, 192)"],[21,97.33,558,75.56,"13px","400","10px","rgb(255, 106, 128)","rgb(255, 107, 129)"],[35,109.33,16,16,"13px","400","0px","transparent","rgb(255, 107, 129)"],[61,107.33,504,55.56,"13px","400","0px","transparent","rgb(255, 107, 129)"],[61,107.33,504,18.19,"13px","600","0px","transparent","rgb(230, 233, 238)"],[61,126.52,504,36.38,"13px","400","0px","transparent","rgb(255, 107, 129)"],[21,186.89,558,281.31,"13.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[22,187.89,556,55.06,"13.5px","400","0px","transparent","rgb(230, 233, 238)"],[66,201.42,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[1,484.2,598,57,"14px","400","0px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[429.36,497.2,50.42,32,"13px","500","7px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[487.78,497.2,91.22,32,"13px","600","7px","rgb(90, 169, 255)","rgb(4, 17, 31)"],[500.78,505.2,16,16,"13px","600","0px","transparent","rgb(4, 17, 31)"]],"pick":"Marketing","viewport":[1440,1300],"mobile":false,"state":"failed"},"12":{"pairs":[{"sel":"[data-f=\"mdlg\"]","kind":"data"},{"sel":"[data-f=\"mh\"]","kind":"head"},{"sel":"[data-f=\"mt\"]","kind":"head"},{"sel":"[data-f=\"msub\"]","kind":"head"},{"sel":"[data-f=\"mclose\"]","kind":"head"},{"sel":"[data-f=\"places\"]","kind":"data"},{"sel":"[data-f=\"place\"]:nth-child(1)","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) > span:nth-child(1)","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) [data-f=\"mark\"]","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) b","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) small","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) > span:last-child","kind":"head"},{"sel":"[data-f=\"mf\"]","kind":"data"},{"sel":"[data-f=\"mcancel\"]","kind":"size"}],"pick":"","viewport":[390,844],"mobile":true,"sang":[[0,0,388,465.64,"14px","400","16px 16px 0px 0px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[0,1,388,86.33,"14px","400","0px","transparent","rgb(22, 26, 33)"],[16,15,310,24.64,"17px","650","0px","transparent","rgb(22, 26, 33)"],[16,41.64,310,37.69,"13px","400","0px","transparent","rgb(79, 88, 102)"],[336,25.16,44,44,"14px","400","7px","transparent","rgb(79, 88, 102)"],[16,95.33,356,281.31,"14.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[17,96.33,354,55.06,"14.5px","400","0px","transparent","rgb(22, 26, 33)"],[31,114.86,18,18,"14.5px","400","50%","transparent","rgb(22, 26, 33)"],[61,109.86,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[101,104.33,159.45,19.56,"13.5px","500","0px","transparent","rgb(22, 26, 33)"],[101,125.89,159.45,17.5,"12.5px","400","0px","transparent","rgb(95, 104, 117)"],[272.45,112.86,84.55,22,"12px","600","6px","rgb(238, 240, 244)","rgb(62, 71, 87)"],[0,392.64,388,73,"14px","400","0px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[16,405.64,174,44,"13px","500","7px","rgb(255, 255, 255)","rgb(22, 26, 33)"]],"dem":[[0,0,388,465.64,"14px","400","16px 16px 0px 0px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[0,1,388,86.33,"14px","400","0px","transparent","rgb(230, 233, 238)"],[16,15,310,24.64,"17px","650","0px","transparent","rgb(230, 233, 238)"],[16,41.64,310,37.69,"13px","400","0px","transparent","rgb(170, 179, 192)"],[336,25.16,44,44,"14px","400","7px","transparent","rgb(170, 179, 192)"],[16,95.33,356,281.31,"14.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[17,96.33,354,55.06,"14.5px","400","0px","transparent","rgb(230, 233, 238)"],[31,114.86,18,18,"14.5px","400","50%","transparent","rgb(230, 233, 238)"],[61,109.86,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[101,104.33,159.45,19.56,"13.5px","500","0px","transparent","rgb(230, 233, 238)"],[101,125.89,159.45,17.5,"12.5px","400","0px","transparent","rgb(143, 153, 167)"],[272.45,112.86,84.55,22,"12px","600","6px","rgb(35, 43, 53)","rgb(195, 203, 214)"],[0,392.64,388,73,"14px","400","0px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[16,405.64,174,44,"13px","500","7px","rgb(26, 32, 40)","rgb(230, 233, 238)"]]},"13":{"pairs":[{"sel":"[data-f=\"mdlg\"]","kind":"data"},{"sel":"[data-f=\"mh\"]","kind":"head"},{"sel":"[data-f=\"mt\"]","kind":"head"},{"sel":"[data-f=\"msub\"]","kind":"head"},{"sel":"[data-f=\"mclose\"]","kind":"head"},{"sel":"[data-f=\"places\"]","kind":"data"},{"sel":"[data-f=\"place\"]:nth-child(1)","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) > span:nth-child(1)","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) [data-f=\"mark\"]","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) b","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) small","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) > span:last-child","kind":"head"},{"sel":"[data-f=\"mf\"]","kind":"data"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"]","kind":"size"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"] > span:nth-child(1)","kind":"size"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"] [data-f=\"mark\"]","kind":"size"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"] b","kind":"size"},{"sel":"[data-f=\"place\"][aria-checked=\"true\"] small","kind":"size"},{"sel":"[data-f=\"sections\"] > :nth-child(1)","kind":"size"},{"sel":"[data-f=\"sections\"] > :nth-child(1) b","kind":"size"},{"sel":"[data-f=\"sections\"] > :nth-child(1) small","kind":"size"},{"sel":"[data-f=\"warn\"]","kind":"size"},{"sel":"[data-f=\"warn\"] b","kind":"size"},{"sel":"[data-f=\"stranded\"]","kind":"size"},{"sel":"[data-f=\"srow\"]:nth-child(1)","kind":"size"},{"sel":"[data-f=\"cbx\"]","kind":"size"},{"sel":"[data-f=\"srow\"]:nth-child(1) > span:nth-child(2)","kind":"size"},{"sel":"[data-f=\"srow\"]:nth-child(1) b","kind":"size"},{"sel":"[data-f=\"srow\"]:nth-child(1) small","kind":"size"},{"sel":"[data-f=\"srow\"]:nth-child(1) > span:last-child","kind":"size"},{"sel":"[data-f=\"sections\"] > :nth-child(4)","kind":"size"},{"sel":"[data-f=\"sections\"] > :nth-child(5)","kind":"size"},{"sel":"[data-f=\"mcancel\"]","kind":"size"},{"sel":"[data-f=\"mwithout\"]","kind":"size"},{"sel":"[data-f=\"mgo\"]","kind":"size"}],"pick":"Kế toán","viewport":[390,844],"mobile":true,"sang":[[0,0,388,1155.86,"14px","400","16px 16px 0px 0px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[0,1,388,86.33,"14px","400","0px","transparent","rgb(22, 26, 33)"],[16,15,310,24.64,"17px","650","0px","transparent","rgb(22, 26, 33)"],[16,41.64,310,37.69,"13px","400","0px","transparent","rgb(79, 88, 102)"],[336,25.16,44,44,"14px","400","7px","transparent","rgb(79, 88, 102)"],[16,95.33,356,281.31,"14.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[17,96.33,354,55.06,"14.5px","400","0px","transparent","rgb(22, 26, 33)"],[31,114.86,18,18,"14.5px","400","50%","transparent","rgb(22, 26, 33)"],[61,109.86,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[101,104.33,159.45,19.56,"13.5px","500","0px","transparent","rgb(22, 26, 33)"],[101,125.89,159.45,17.5,"12.5px","400","0px","transparent","rgb(95, 104, 117)"],[272.45,112.86,84.55,22,"12px","600","6px","rgb(238, 240, 244)","rgb(62, 71, 87)"],[0,1030.86,388,125,"14px","400","0px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[17,151.39,354,56.06,"14.5px","400","0px","rgb(231, 240, 252)","rgb(22, 26, 33)"],[31,170.92,18,18,"14.5px","400","50%","transparent","rgb(22, 26, 33)"],[61,165.92,28,28,"11px","700","6px","rgb(154, 106, 18)","rgb(255, 255, 255)"],[101,160.39,256,19.56,"13.5px","600","0px","transparent","rgb(22, 26, 33)"],[101,181.95,256,17.5,"12.5px","400","0px","transparent","rgb(95, 104, 117)"],[16,390.64,356,74.56,"14.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[59,399.64,298,19.56,"13.5px","600","0px","transparent","rgb(22, 26, 33)"],[59,421.2,298,35,"12.5px","400","0px","transparent","rgb(95, 104, 117)"],[16,477.2,356,113.34,"13px","400","10px","rgb(255, 244, 214)","rgb(138, 90, 0)"],[56,487.2,302,37.78,"13.5px","600","0px","transparent","rgb(22, 26, 33)"],[16,602.55,356,186.69,"14.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[17,603.55,354,72.56,"14.5px","400","0px","transparent","rgb(22, 26, 33)"],[31,630.83,18,18,"14.5px","400","5px","rgb(22, 102, 204)","rgb(255, 255, 255)"],[61,625.83,28,28,"11px","700","50%","rgb(30, 111, 208)","rgb(255, 255, 255)"],[101,611.55,107.55,19.56,"13.5px","600","0px","transparent","rgb(22, 26, 33)"],[101,633.11,107.55,35,"12.5px","400","0px","transparent","rgb(95, 104, 117)"],[220.55,628.83,136.45,22,"12px","600","6px","rgb(251, 240, 211)","rgb(110, 79, 10)"],[16,801.23,356,92.06,"14.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[16,905.3,356,109.56,"14.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[16,1043.86,174,44,"13px","500","7px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[198,1043.86,174,44,"13px","500","7px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[16,1095.86,356,44,"13px","600","7px","rgb(22, 102, 204)","rgb(255, 255, 255)"]],"dem":[[0,0,388,1155.86,"14px","400","16px 16px 0px 0px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[0,1,388,86.33,"14px","400","0px","transparent","rgb(230, 233, 238)"],[16,15,310,24.64,"17px","650","0px","transparent","rgb(230, 233, 238)"],[16,41.64,310,37.69,"13px","400","0px","transparent","rgb(170, 179, 192)"],[336,25.16,44,44,"14px","400","7px","transparent","rgb(170, 179, 192)"],[16,95.33,356,281.31,"14.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[17,96.33,354,55.06,"14.5px","400","0px","transparent","rgb(230, 233, 238)"],[31,114.86,18,18,"14.5px","400","50%","transparent","rgb(230, 233, 238)"],[61,109.86,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[101,104.33,159.45,19.56,"13.5px","500","0px","transparent","rgb(230, 233, 238)"],[101,125.89,159.45,17.5,"12.5px","400","0px","transparent","rgb(143, 153, 167)"],[272.45,112.86,84.55,22,"12px","600","6px","rgb(35, 43, 53)","rgb(195, 203, 214)"],[0,1030.86,388,125,"14px","400","0px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[17,151.39,354,56.06,"14.5px","400","0px","rgb(87, 168, 255)","rgb(230, 233, 238)"],[31,170.92,18,18,"14.5px","400","50%","transparent","rgb(230, 233, 238)"],[61,165.92,28,28,"11px","700","6px","rgb(154, 106, 18)","rgb(255, 255, 255)"],[101,160.39,256,19.56,"13.5px","600","0px","transparent","rgb(230, 233, 238)"],[101,181.95,256,17.5,"12.5px","400","0px","transparent","rgb(143, 153, 167)"],[16,390.64,356,74.56,"14.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[59,399.64,298,19.56,"13.5px","600","0px","transparent","rgb(230, 233, 238)"],[59,421.2,298,35,"12.5px","400","0px","transparent","rgb(143, 153, 167)"],[16,477.2,356,113.34,"13px","400","10px","rgb(241, 191, 78)","rgb(242, 193, 78)"],[56,487.2,302,37.78,"13.5px","600","0px","transparent","rgb(230, 233, 238)"],[16,602.55,356,186.69,"14.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[17,603.55,354,72.56,"14.5px","400","0px","transparent","rgb(230, 233, 238)"],[31,630.83,18,18,"14.5px","400","5px","rgb(90, 169, 255)","rgb(4, 17, 31)"],[61,625.83,28,28,"11px","700","50%","rgb(30, 111, 208)","rgb(255, 255, 255)"],[101,611.55,107.55,19.56,"13.5px","600","0px","transparent","rgb(230, 233, 238)"],[101,633.11,107.55,35,"12.5px","400","0px","transparent","rgb(143, 153, 167)"],[220.55,628.83,136.45,22,"12px","600","6px","rgb(48, 40, 26)","rgb(242, 193, 78)"],[16,801.23,356,92.06,"14.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[16,905.3,356,109.56,"14.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[16,1043.86,174,44,"13px","500","7px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[198,1043.86,174,44,"13px","500","7px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[16,1095.86,356,44,"13px","600","7px","rgb(90, 169, 255)","rgb(4, 17, 31)"]]},"14":{"pairs":[{"sel":"[data-f=\"mdlg\"]","kind":"data"},{"sel":"[data-f=\"mh\"]","kind":"head"},{"sel":"[data-f=\"mt\"]","kind":"head"},{"sel":"[data-f=\"msub\"]","kind":"head"},{"sel":"[data-f=\"mclose\"]","kind":"head"},{"sel":"[data-f=\"merr\"]","kind":"head"},{"sel":"[data-f=\"merr\"] > svg","kind":"head"},{"sel":"[data-f=\"merr\"] > span","kind":"head"},{"sel":"[data-f=\"merr\"] b","kind":"head"},{"sel":"[data-f=\"merr\"] > span > span","kind":"head"},{"sel":"[data-f=\"places\"]","kind":"data"},{"sel":"[data-f=\"place\"]:nth-child(1)","kind":"head"},{"sel":"[data-f=\"place\"]:nth-child(1) [data-f=\"mark\"]","kind":"head"},{"sel":"[data-f=\"mf\"]","kind":"data"},{"sel":"[data-f=\"mcancel\"]","kind":"size"},{"sel":"[data-f=\"mgo\"]","kind":"size"},{"sel":"[data-f=\"mgo\"] > svg","kind":"size"}],"sang":[[0,0,388,555.2,"14px","400","16px 16px 0px 0px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[0,1,388,86.33,"14px","400","0px","transparent","rgb(22, 26, 33)"],[16,15,310,24.64,"17px","650","0px","transparent","rgb(22, 26, 33)"],[16,41.64,310,37.69,"13px","400","0px","transparent","rgb(79, 88, 102)"],[336,25.16,44,44,"14px","400","7px","transparent","rgb(79, 88, 102)"],[16,95.33,356,75.56,"13px","400","10px","rgb(253, 236, 239)","rgb(196, 35, 64)"],[30,107.33,16,16,"13px","400","0px","transparent","rgb(196, 35, 64)"],[56,105.33,302,55.56,"13px","400","0px","transparent","rgb(196, 35, 64)"],[56,105.33,302,18.19,"13px","600","0px","transparent","rgb(22, 26, 33)"],[56,124.52,302,36.38,"13px","400","0px","transparent","rgb(196, 35, 64)"],[16,184.89,356,281.31,"14.5px","400","10px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[17,185.89,354,55.06,"14.5px","400","0px","transparent","rgb(22, 26, 33)"],[61,199.42,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[0,482.2,388,73,"14px","400","0px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[16,495.2,174,44,"13px","500","7px","rgb(255, 255, 255)","rgb(22, 26, 33)"],[198,495.2,174,44,"13px","600","7px","rgb(22, 102, 204)","rgb(255, 255, 255)"],[252.39,509.2,16,16,"13px","600","0px","transparent","rgb(255, 255, 255)"]],"dem":[[0,0,388,555.2,"14px","400","16px 16px 0px 0px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[0,1,388,86.33,"14px","400","0px","transparent","rgb(230, 233, 238)"],[16,15,310,24.64,"17px","650","0px","transparent","rgb(230, 233, 238)"],[16,41.64,310,37.69,"13px","400","0px","transparent","rgb(170, 179, 192)"],[336,25.16,44,44,"14px","400","7px","transparent","rgb(170, 179, 192)"],[16,95.33,356,75.56,"13px","400","10px","rgb(255, 106, 128)","rgb(255, 107, 129)"],[30,107.33,16,16,"13px","400","0px","transparent","rgb(255, 107, 129)"],[56,105.33,302,55.56,"13px","400","0px","transparent","rgb(255, 107, 129)"],[56,105.33,302,18.19,"13px","600","0px","transparent","rgb(230, 233, 238)"],[56,124.52,302,36.38,"13px","400","0px","transparent","rgb(255, 107, 129)"],[16,184.89,356,281.31,"14.5px","400","10px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[17,185.89,354,55.06,"14.5px","400","0px","transparent","rgb(230, 233, 238)"],[61,199.42,28,28,"11px","700","6px","rgb(30, 111, 208)","rgb(255, 255, 255)"],[0,482.2,388,73,"14px","400","0px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[16,495.2,174,44,"13px","500","7px","rgb(26, 32, 40)","rgb(230, 233, 238)"],[198,495.2,174,44,"13px","600","7px","rgb(90, 169, 255)","rgb(4, 17, 31)"],[252.39,509.2,16,16,"13px","600","0px","transparent","rgb(4, 17, 31)"]],"pick":"Marketing","viewport":[390,844],"mobile":true,"state":"failed"}};

/** Colours the design tokens do not carry as the mockup draws them. */
const COLOUR_SKIP = new Set([
  '[data-f="place"][aria-checked="true"]',
  '[data-f="srow"]:nth-child(1) > span:last-child',
  '[data-f="merr"]',
  '[data-f="warn"]',
  '[data-f="warn"] b',
  '[data-f="sections"] > :nth-child(1)',
  '[data-f="ok"]',
  '[data-f="sections"] > :nth-child(4)',
  '[data-f="sections"] > :nth-child(5)',
  '[data-f="mgo"]',
  '[data-f="place"]:nth-child(1) > span:last-child',
  '[data-f="srow"]:nth-child(1) > span:nth-child(2)',
  '[data-f="place"]:nth-child(1) [data-f="mark"]',
]);

async function openDialog(
  page: Page,
  dark: boolean,
  pick: string,
  state?: "loading" | "failed",
) {
  await page.addInitScript(
    (mode) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode);
        window.localStorage.setItem("notrelix-system-rail", "pinned");
      } catch {
        // Private mode: the defaults apply.
      }
    },
    dark ? "dark" : "light",
  );
  await page.goto(SETTINGS);
  await page.getByRole("button", { name: "Chuyển sang phòng khác" }).click();
  await expect(page.locator('[data-f="mdlg"]')).toBeVisible();
  if (pick) {
    await page.getByRole("radio", { name: new RegExp(pick) }).click();
    if (state === "loading")
      await expect(
        page.locator('[data-f="impact"] [role="status"]'),
      ).toBeVisible();
    else await expect(page.locator('[data-f="sections"]')).toBeVisible();
  }
  if (state === "failed") {
    await page.locator('[data-f="mgo"]').click();
    await expect(page.locator('[data-f="merr"]')).toBeVisible();
  }
  // The dialog zooms in and the sections fade in; measure at rest.
  await page.waitForTimeout(600);
}

async function measure(
  page: Page,
  selectors: readonly string[],
): Promise<(Measure | null)[]> {
  return page.evaluate((list) => {
    const origin = document
      .querySelector('[data-f="mdlg"]')!
      .getBoundingClientRect();
    const rgb = (value: string) => {
      const canvas = document.createElement("canvas");
      canvas.width = canvas.height = 1;
      const context = canvas.getContext("2d", { willReadFrequently: true })!;
      context.clearRect(0, 0, 1, 1);
      context.fillStyle = "#000";
      context.fillStyle = value;
      context.fillRect(0, 0, 1, 1);
      const [r, g, b, a] = context.getImageData(0, 0, 1, 1).data;
      return a === 0 ? "transparent" : `rgb(${r}, ${g}, ${b})`;
    };
    return list.map((selector) => {
      const element = document.querySelector(selector);
      if (!element) return null;
      const box = element.getBoundingClientRect();
      const style = getComputedStyle(element);
      return [
        box.x - origin.x,
        box.y - origin.y,
        box.width,
        box.height,
        style.fontSize,
        style.fontWeight,
        style.borderRadius,
        rgb(style.backgroundColor),
        rgb(style.color),
      ] as const;
    });
  }, selectors);
}

test.describe("Move-board dialog (WM-MoveBoard, owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Owner checks need the default owner world",
  );

  for (const [frame, data] of Object.entries(FRAMES)) {
    for (const dark of [false, true]) {
      const mode = dark ? "dem" : "sang";
      test(`frame ${frame} (${mode}) matches the artboard within 1 px`, async ({
        page,
      }) => {
        test.skip(
          data.state === "loading"
            ? latency !== "slow" || faults !== ""
            : data.state === "failed"
              ? faults !== MOVE_FAULT || latency !== "instant"
              : faults !== "" || latency !== "instant",
          "This frame needs its own server (see the file header)",
        );
        await page.setViewportSize({
          width: data.viewport[0],
          height: data.viewport[1],
        });
        await openDialog(page, dark, data.pick, data.state);
        const actual = await measure(
          page,
          data.pairs.map((pair) => pair.sel),
        );
        const wrong: string[] = [];
        data.pairs.forEach((pair, index) => {
          const want = data[mode][index]!;
          const got = actual[index];
          if (!got) {
            wrong.push(`${pair.sel}: missing`);
            return;
          }
          const keys =
            pair.kind === "data"
              ? [0, 2]
              : pair.kind === "head"
                ? [0, 1, 2, 3]
                : pair.kind === "h"
                  ? [3]
                  : [2, 3];
          for (const key of keys) {
            // Flexible boxes of the sheet grow with its two extra pixels (all of it, or half of it in two columns).
            const width = want[2] as number;
            // Flexible boxes grow with the sheet's two extra pixels (a two-column button by one: 1 px tolerance).
            const grow = !data.mobile
              ? 0
              : key === 2
                ? width > 100 && !pair.sel.endsWith(":last-child")
                  ? 2
                  : 0
                : key === 0 && width <= 100 && (want[0] as number) + width > 330
                  ? 2
                  : 0;
            if (
              Math.abs((got[key] as number) - (want[key] as number) - grow) > 1
            )
              wrong.push(
                `${pair.sel}: box[${key}] ${(got[key] as number).toFixed(1)} vs ${want[key]}`,
              );
          }
          if (pair.kind !== "data") {
            if (got[4] !== want[4] || got[5] !== want[5])
              wrong.push(
                `${pair.sel}: font ${got[4]}/${got[5]} vs ${want[4]}/${want[5]}`,
              );
            const round = want[6] === "50%" && parseFloat(got[6]) > 1e6;
            if (got[6] !== want[6] && !round)
              wrong.push(`${pair.sel}: radius ${got[6]} vs ${want[6]}`);
          }
          if (COLOUR_SKIP.has(pair.sel)) return;
          for (const key of [7, 8]) {
            if (got[key] !== want[key])
              wrong.push(
                `${pair.sel}: colour[${key}] ${got[key]} vs ${want[key]}`,
              );
          }
        });
        expect(wrong, wrong.join("\n")).toEqual([]);
      });
    }
  }

  test("the sections sit 14 px under the list and 12 px apart, the footer under them", async ({
    page,
  }) => {
    test.skip(faults !== "" || latency !== "instant", "Default server only");
    await page.setViewportSize({ width: 1440, height: 1300 });
    await openDialog(page, false, "Kế toán");
    const boxes = await page.evaluate(() => {
      const y = (selector: string) => {
        const box = document.querySelector(selector)!.getBoundingClientRect();
        return [box.top, box.bottom];
      };
      return {
        places: y('[data-f="places"]'),
        first: y('[data-f="sections"] > :nth-child(1)'),
        second: y('[data-f="sections"] > :nth-child(2)'),
        last: y('[data-f="sections"] > :nth-child(5)'),
        footer: y('[data-f="mf"]'),
      };
    });
    expect(
      Math.abs(boxes.first[0]! - boxes.places[1]! - 14),
    ).toBeLessThanOrEqual(1);
    expect(
      Math.abs(boxes.second[0]! - boxes.first[1]! - 12),
    ).toBeLessThanOrEqual(1);
    expect(
      Math.abs(boxes.footer[0]! - boxes.last[1]! - 16),
    ).toBeLessThanOrEqual(1);
  });
});
