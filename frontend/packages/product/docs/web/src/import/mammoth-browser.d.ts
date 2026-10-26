/**
 * The prebuilt browser bundle of mammoth (it carries its own XML parser, so it
 * does not depend on the workspace's `@xmldom/xmldom` override) has the same API
 * as the package's main entry.
 */
declare module "mammoth/mammoth.browser.min.js" {
  import type mammothApi from "mammoth";

  const mammoth: typeof mammothApi;
  export default mammoth;
}
