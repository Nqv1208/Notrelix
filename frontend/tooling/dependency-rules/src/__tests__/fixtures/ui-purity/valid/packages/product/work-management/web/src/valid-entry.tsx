import React from "react";
import { WorkManagementEntity } from "@notrelix/work-management-core";
import { localPresentationValue } from "./valid-helper";

export { ValidButton } from "./valid-barrel";

export const ValidElement = React.createElement(
  "span",
  null,
  WorkManagementEntity,
  localPresentationValue,
);
