import createNextIntlPlugin from "next-intl/plugin";
import type { NextConfig } from "next";

/* eslint-disable @typescript-eslint/no-explicit-any */

const withNextIntl = createNextIntlPlugin();

const nextConfig: NextConfig = {
  output: "standalone",
  images: {
    remotePatterns: [
      {
        protocol: "https",
        hostname: "**",
      },
    ],
  },
  turbopack: {
    rules: {
      "*.svg": {
        loaders: ["@svgr/webpack"],
        as: "*.js",
      },
    },
  },
  webpack(config: any) {
    const fileLoaderRule = config.module?.rules?.find((rule: any) =>
      rule?.test?.test?.(".svg"),
    );
    config.module.rules.push(
      { ...fileLoaderRule, test: /\.svg$/i, resourceQuery: /url/ },
      {
        test: /\.svg$/i,
        issuer: fileLoaderRule?.issuer,
        use: ["@svgr/webpack"],
      },
    );
    if (fileLoaderRule) {
      fileLoaderRule.exclude = /\.svg$/i;
    }
    return config;
  },
};

export default withNextIntl(nextConfig);
