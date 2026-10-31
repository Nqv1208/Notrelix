export interface CardLabel {
  id: string;
  name: string;
  color: string;
  /** The label version the list showed; a rename, recolour or delete names it. */
  version?: number;
}
