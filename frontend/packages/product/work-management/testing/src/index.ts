/**
 * @notrelix/wm-testing — Work Management test fixtures and mocks.
 *
 * Usable by core/state/web/mobile tests.
 * No production dependency on this package.
 */

export { boardFixture } from "./fixtures/board.fixture";
export { itemFixture } from "./fixtures/item.fixture";
export { fieldFixture } from "./fixtures/field.fixture";
export { createBoardSnapshot } from "./factories/create-board-snapshot";
export { createBoardPatch } from "./factories/create-board-patch";
export { mockCommandBus } from "./mocks/mock-command-bus";
