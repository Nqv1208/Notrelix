/**
 * @notrelix/ui-mobile — Mobile UI primitives barrel export
 *
 * Platform-independent mobile UI primitives contract.
 * Explicitly isolated from web DOM and @radix-ui dependencies.
 */

export interface MobileButtonProps {
  readonly title: string;
  readonly onPress?: () => void;
  readonly disabled?: boolean;
  readonly variant?: 'primary' | 'secondary' | 'outline' | 'ghost';
}

export interface MobileCardProps {
  readonly id: string;
  readonly title: string;
  readonly subtitle?: string;
  readonly children?: unknown;
}

export interface MobileInputProps {
  readonly value: string;
  readonly onChangeText?: (text: string) => void;
  readonly placeholder?: string;
  readonly disabled?: boolean;
}
