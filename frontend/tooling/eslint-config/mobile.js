/**
 * ESLint config for mobile apps (Expo / React Native).
 */

import baseConfig from "./index.js";

export default [
  ...baseConfig,
  {
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: ["next/*"],
              message: "Mobile app cannot import next/*.",
            },
            {
              group: ["@notrelix/ui-web", "@notrelix/runtime-web"],
              message: "Mobile app cannot import web packages.",
            },
            {
              group: ["shadcn", "radix-ui/*"],
              message: "Mobile app cannot use web UI libraries.",
            },
          ],
        },
      ],
    },
  },
];
