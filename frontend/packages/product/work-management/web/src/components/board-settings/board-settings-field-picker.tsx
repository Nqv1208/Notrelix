import { useEffect, useMemo, useRef, useState } from "react";
import { Search } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { BoardSettingsFieldType } from "@notrelix/work-management-core";
import type { BoardSettingsCopy } from "./board-settings.copy";
import { TypeMark, fieldIcon } from "./board-settings-parts";

type PickableType = keyof BoardSettingsCopy["picker"]["names"];

const GROUPS: readonly {
  key: keyof BoardSettingsCopy["picker"]["groups"];
  types: readonly PickableType[];
}[] = [
  {
    key: "basic",
    types: ["text", "longtext", "number", "date", "checkbox", "file"],
  },
  { key: "sorting", types: ["select", "multiselect", "status", "person"] },
  { key: "computed", types: ["relation", "formula", "rollup", "progress"] },
];

/**
 * The field type picker (mockup `.pop`): a search box, three groups of types
 * and, at the foot, what the highlighted type does. Arrow keys move the
 * highlight, Enter adds the field, Escape closes the popover.
 */
export function FieldTypePicker({
  copy,
  onPick,
}: {
  copy: BoardSettingsCopy;
  onPick: (type: BoardSettingsFieldType) => void;
}) {
  const [query, setQuery] = useState("");
  const [active, setActive] = useState<PickableType>("date");
  const inputRef = useRef<HTMLInputElement>(null);
  useEffect(() => inputRef.current?.focus(), []);

  const groups = useMemo(() => {
    const needle = query.trim().toLowerCase();
    return GROUPS.map((group) => ({
      ...group,
      types: group.types.filter((type) => {
        if (!needle) return true;
        return `${copy.picker.names[type]} ${copy.picker.hints[type]}`
          .toLowerCase()
          .includes(needle);
      }),
    })).filter((group) => group.types.length > 0);
  }, [copy, query]);
  const flat = groups.flatMap((group) => group.types);
  const current = flat.includes(active) ? active : (flat[0] ?? null);

  function move(step: number) {
    if (flat.length === 0) return;
    const index = current ? flat.indexOf(current) : 0;
    setActive(flat[(index + step + flat.length) % flat.length]!);
  }

  return (
    <div
      role="group"
      aria-label={copy.picker.label}
      onKeyDown={(event) => {
        if (event.key === "ArrowDown" || event.key === "ArrowRight") {
          event.preventDefault();
          move(1);
        } else if (event.key === "ArrowUp" || event.key === "ArrowLeft") {
          event.preventDefault();
          move(-1);
        } else if (event.key === "Enter" && current) {
          event.preventDefault();
          onPick(current);
        }
      }}
    >
      <div className="flex h-[42px] items-center gap-2 border-b border-line px-3.5 text-item-ui text-meta-foreground">
        <Search aria-hidden="true" className="size-4 shrink-0" />
        <input
          ref={inputRef}
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder={copy.picker.search}
          aria-label={copy.picker.search}
          className="min-w-0 flex-1 border-0 bg-transparent text-item-ui text-foreground outline-none placeholder:text-meta-foreground"
        />
        <kbd className="rounded-[4px] border border-line-strong bg-keycap px-1.25 font-mono text-key text-muted-foreground">
          Esc
        </kbd>
      </div>
      {groups.length === 0 ? (
        <p className="m-0 px-4 py-6 text-center text-item-small text-meta-foreground">
          {copy.picker.none}
        </p>
      ) : (
        groups.map((group) => (
          <div key={group.key} className="px-2.5 pt-2 pb-1">
            <div className="px-1.5 py-1 text-metadata font-semibold text-meta-foreground">
              {copy.picker.groups[group.key]}
            </div>
            <div className="grid grid-cols-2 gap-0.5">
              {group.types.map((type) => {
                const name = copy.picker.names[type];
                const hint = copy.picker.hints[type];
                return (
                  <button
                    key={type}
                    type="button"
                    onClick={() => onPick(type)}
                    onMouseEnter={() => setActive(type)}
                    onFocus={() => setActive(type)}
                    aria-describedby={undefined}
                    className={cn(
                      "flex cursor-pointer items-center gap-2.5 rounded-lg border-0 bg-transparent px-2 py-[7px] text-left outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                      type === current && "bg-row-selected",
                    )}
                  >
                    <TypeMark icon={fieldIcon(type)} />
                    <span className="min-w-0">
                      <b className="block text-item-small font-semibold">
                        {name}
                      </b>
                      <small className="block text-[11.5px] leading-snug text-meta-foreground">
                        {hint}
                      </small>
                    </span>
                  </button>
                );
              })}
            </div>
          </div>
        ))
      )}
      {current ? (
        <div className="mt-1 border-t border-line bg-background px-3.5 py-2.5 text-crumb leading-normal text-muted-foreground">
          <b className="font-semibold text-foreground">
            {copy.picker.names[current]}
          </b>{" "}
          · {copy.picker.help[current]}
        </div>
      ) : null}
    </div>
  );
}
