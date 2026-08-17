import baseConfig from "../../tooling/eslint/base.js";

export default [
  ...baseConfig,
  {
    rules: {
      "@typescript-eslint/no-explicit-any": "error",
    },
  },
];
