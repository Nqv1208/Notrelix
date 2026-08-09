import React, { useState, useRef, useEffect } from "react";
import {
  createUsePage,
  createUsePageBlocks,
  createUsePageBreadcrumb,
  createUseCreateBlock,
  createUseUpdateBlock,
  createUseDeleteBlock,
  createUseReorderBlocks,
  createUseUpdatePage,
  type DocsApiClient,
  type PageApiEndpoints,
  type Block,
  type BlockType,
  type BreadcrumbItem,
} from "@notrelix/docs-state";
import { Button, Skeleton } from "@notrelix/ui-web";
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
  Plus,
  Trash2,
  CheckSquare,
  Square,
  Heading1,
  Heading2,
  Heading3,
  AlignLeft,
  List,
  ListOrdered,
  Quote,
  Minus,
  Code,
  ChevronRight,
  BookOpen,
  GripVertical,
  MessageSquare,
  Clock,
  X,
  Bold,
  Italic,
  Underline,
  Strikethrough,
} from "lucide-react";
import { DocComments } from "./doc-comments";
import { DocHistory } from "./doc-history";
import { createDocPageTree } from "./doc-page-tree";

interface CreateDocPageScreenDeps {
  api: DocsApiClient;
  endpoints: PageApiEndpoints;
}

interface BlockEditorProps {
  workspaceId: string;
  pageId: string;
  block: Block;
  onDelete: (id: string) => void;
  useUpdateBlock: ReturnType<typeof createUseUpdateBlock>;
}

const BLOCK_MENU_ITEMS: Array<{
  type: BlockType;
  label: string;
  icon: React.ReactNode;
  keywords: string[];
}> = [
  {
    type: "paragraph",
    label: "Text",
    icon: <AlignLeft className="h-4 w-4" />,
    keywords: ["text", "paragraph", "plain"],
  },
  {
    type: "heading_1",
    label: "Heading 1",
    icon: <Heading1 className="h-4 w-4" />,
    keywords: ["heading", "h1", "title"],
  },
  {
    type: "heading_2",
    label: "Heading 2",
    icon: <Heading2 className="h-4 w-4" />,
    keywords: ["heading", "h2", "subtitle"],
  },
  {
    type: "heading_3",
    label: "Heading 3",
    icon: <Heading3 className="h-4 w-4" />,
    keywords: ["heading", "h3"],
  },
  {
    type: "bulleted_list",
    label: "Bullet List",
    icon: <List className="h-4 w-4" />,
    keywords: ["bullet", "list", "unordered"],
  },
  {
    type: "numbered_list",
    label: "Numbered List",
    icon: <ListOrdered className="h-4 w-4" />,
    keywords: ["number", "list", "ordered"],
  },
  {
    type: "todo",
    label: "To-do",
    icon: <CheckSquare className="h-4 w-4" />,
    keywords: ["todo", "checkbox", "task"],
  },
  {
    type: "quote",
    label: "Quote",
    icon: <Quote className="h-4 w-4" />,
    keywords: ["quote", "blockquote"],
  },
  {
    type: "divider",
    label: "Divider",
    icon: <Minus className="h-4 w-4" />,
    keywords: ["divider", "hr", "line", "separator"],
  },
  {
    type: "code",
    label: "Code",
    icon: <Code className="h-4 w-4" />,
    keywords: ["code", "pre", "monospace"],
  },
  {
    type: "callout",
    label: "Callout",
    icon: <span className="text-sm">💡</span>,
    keywords: ["callout", "info", "note"],
  },
];

function SlashCommandMenu({
  query,
  onSelect,
  onClose,
}: {
  query: string;
  onSelect: (type: BlockType) => void;
  onClose: () => void;
}) {
  const [selectedIndex, setSelectedIndex] = useState(0);
  const menuRef = useRef<HTMLDivElement>(null);

  const filtered = BLOCK_MENU_ITEMS.filter(
    (item) =>
      item.label.toLowerCase().includes(query.toLowerCase()) ||
      item.keywords.some((kw) => kw.includes(query.toLowerCase())),
  );

  useEffect(() => {
    setSelectedIndex(0);
  }, [query]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "ArrowDown") {
        e.preventDefault();
        setSelectedIndex((i) => (i + 1) % filtered.length);
      } else if (e.key === "ArrowUp") {
        e.preventDefault();
        setSelectedIndex((i) => (i - 1 + filtered.length) % filtered.length);
      } else if (e.key === "Enter") {
        e.preventDefault();
        if (filtered[selectedIndex]) {
          onSelect(filtered[selectedIndex].type);
        }
      } else if (e.key === "Escape") {
        onClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [filtered, selectedIndex, onSelect, onClose]);

  if (filtered.length === 0) return null;

  return (
    <div
      ref={menuRef}
      className="absolute z-50 w-64 bg-popover border border-border rounded-lg shadow-lg overflow-hidden"
    >
      <div className="p-1.5 max-h-64 overflow-y-auto">
        {filtered.map((item, index) => (
          <button
            key={item.type}
            onClick={() => onSelect(item.type)}
            className={`flex items-center gap-2.5 w-full px-2.5 py-2 text-sm rounded-md transition-colors ${
              index === selectedIndex
                ? "bg-accent text-accent-foreground"
                : "text-foreground hover:bg-muted/50"
            }`}
          >
            <span className="text-muted-foreground">{item.icon}</span>
            <span>{item.label}</span>
          </button>
        ))}
      </div>
    </div>
  );
}

function SortableBlockEditor({
  workspaceId,
  pageId,
  block,
  onDelete,
  useUpdateBlock,
}: BlockEditorProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: block.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    zIndex: isDragging ? 50 : undefined,
    opacity: isDragging ? 0.5 : undefined,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      className="group flex items-start gap-2.5 relative -ml-6 pl-6"
    >
      <div className="absolute left-0 top-1 opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1">
        <Button
          variant="ghost"
          size="icon"
          className="h-5 w-5 text-muted-foreground hover:text-foreground cursor-grab active:cursor-grabbing"
          {...attributes}
          {...listeners}
        >
          <GripVertical className="h-3.5 w-3.5" />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          className="h-5 w-5 text-muted-foreground hover:text-destructive hover:bg-destructive/10"
          onClick={() => onDelete(block.id)}
        >
          <Trash2 className="h-3.5 w-3.5" />
        </Button>
      </div>

      <div className="flex-1 relative">
        <BlockEditorContent
          workspaceId={workspaceId}
          pageId={pageId}
          block={block}
          useUpdateBlock={useUpdateBlock}
        />
      </div>
    </div>
  );
}

function BlockEditorContent({
  workspaceId,
  pageId,
  block,
  useUpdateBlock,
}: Omit<BlockEditorProps, "onDelete">) {
  const updateBlockMutation = useUpdateBlock(workspaceId, pageId, block.id);
  const [showSlashMenu, setShowSlashMenu] = useState(false);
  const [slashQuery, setSlashQuery] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);

  const handleUpdateText = (text: string) => {
    if (text.startsWith("/")) {
      setSlashQuery(text.slice(1));
      setShowSlashMenu(true);
      return;
    }
    if (showSlashMenu) {
      setShowSlashMenu(false);
    }
    if (text !== (block.properties.text || "")) {
      updateBlockMutation.mutate({
        properties: { text },
      });
    }
  };

  const handleSlashSelect = (type: BlockType) => {
    setShowSlashMenu(false);
    updateBlockMutation.mutate({ type, properties: { text: "" } });
    setTimeout(() => inputRef.current?.focus(), 50);
  };

  const handleToggleTodo = () => {
    updateBlockMutation.mutate({
      properties: { checked: !block.properties.checked },
    });
  };

  const renderBlockContent = () => {
    switch (block.type) {
      case "paragraph":
        return (
          <input
            ref={inputRef}
            type="text"
            defaultValue={block.properties.text || ""}
            onBlur={(e) => handleUpdateText(e.target.value)}
            placeholder="Type '/' for commands..."
            className="w-full bg-transparent border-none outline-none text-base text-foreground focus:ring-0 p-0"
          />
        );

      case "heading_1":
        return (
          <input
            ref={inputRef}
            type="text"
            defaultValue={block.properties.text || ""}
            onBlur={(e) => handleUpdateText(e.target.value)}
            placeholder="Heading 1"
            className="w-full bg-transparent border-none outline-none text-2xl font-bold text-foreground focus:ring-0 p-0"
          />
        );

      case "heading_2":
        return (
          <input
            ref={inputRef}
            type="text"
            defaultValue={block.properties.text || ""}
            onBlur={(e) => handleUpdateText(e.target.value)}
            placeholder="Heading 2"
            className="w-full bg-transparent border-none outline-none text-xl font-semibold text-foreground focus:ring-0 p-0"
          />
        );

      case "heading_3":
        return (
          <input
            ref={inputRef}
            type="text"
            defaultValue={block.properties.text || ""}
            onBlur={(e) => handleUpdateText(e.target.value)}
            placeholder="Heading 3"
            className="w-full bg-transparent border-none outline-none text-lg font-medium text-foreground focus:ring-0 p-0"
          />
        );

      case "todo":
        return (
          <div className="flex items-center gap-2">
            <button
              onClick={handleToggleTodo}
              className="text-muted-foreground hover:text-primary transition-colors shrink-0"
            >
              {block.properties.checked ? (
                <CheckSquare className="h-4.5 w-4.5 text-primary" />
              ) : (
                <Square className="h-4.5 w-4.5" />
              )}
            </button>
            <input
              ref={inputRef}
              type="text"
              defaultValue={block.properties.text || ""}
              onBlur={(e) => handleUpdateText(e.target.value)}
              placeholder="To-do"
              className={`w-full bg-transparent border-none outline-none text-base focus:ring-0 p-0 ${
                block.properties.checked
                  ? "line-through text-muted-foreground"
                  : "text-foreground"
              }`}
            />
          </div>
        );

      case "bulleted_list":
        return (
          <div className="flex items-start gap-2">
            <span className="text-muted-foreground mt-1.5 shrink-0">•</span>
            <input
              ref={inputRef}
              type="text"
              defaultValue={block.properties.text || ""}
              onBlur={(e) => handleUpdateText(e.target.value)}
              placeholder="List item"
              className="w-full bg-transparent border-none outline-none text-base text-foreground focus:ring-0 p-0"
            />
          </div>
        );

      case "numbered_list":
        return (
          <div className="flex items-start gap-2">
            <span className="text-muted-foreground text-sm mt-0.5 shrink-0">
              1.
            </span>
            <input
              ref={inputRef}
              type="text"
              defaultValue={block.properties.text || ""}
              onBlur={(e) => handleUpdateText(e.target.value)}
              placeholder="List item"
              className="w-full bg-transparent border-none outline-none text-base text-foreground focus:ring-0 p-0"
            />
          </div>
        );

      case "quote":
        return (
          <div className="border-l-2 border-primary/40 pl-4">
            <input
              ref={inputRef}
              type="text"
              defaultValue={block.properties.text || ""}
              onBlur={(e) => handleUpdateText(e.target.value)}
              placeholder="Quote"
              className="w-full bg-transparent border-none outline-none text-base text-muted-foreground italic focus:ring-0 p-0"
            />
          </div>
        );

      case "divider":
        return (
          <div className="py-2">
            <hr className="border-border" />
          </div>
        );

      case "code":
        return (
          <div className="bg-muted rounded-lg p-3">
            <input
              ref={inputRef}
              type="text"
              defaultValue={block.properties.text || ""}
              onBlur={(e) => handleUpdateText(e.target.value)}
              placeholder="Code"
              className="w-full bg-transparent border-none outline-none text-sm font-mono text-foreground focus:ring-0 p-0"
            />
          </div>
        );

      case "callout":
        return (
          <div className="flex items-start gap-3 p-4 rounded-xl border bg-muted/30">
            <span className="text-xl shrink-0">
              {block.properties.icon || "💡"}
            </span>
            <input
              ref={inputRef}
              type="text"
              defaultValue={block.properties.text || ""}
              onBlur={(e) => handleUpdateText(e.target.value)}
              placeholder="Callout"
              className="w-full bg-transparent border-none outline-none text-sm text-foreground focus:ring-0 p-0"
            />
          </div>
        );

      default:
        return (
          <input
            ref={inputRef}
            type="text"
            defaultValue={block.properties.text || ""}
            onBlur={(e) => handleUpdateText(e.target.value)}
            placeholder="Type something..."
            className="w-full bg-transparent border-none outline-none text-base text-foreground focus:ring-0 p-0"
          />
        );
    }
  };

  return (
    <>
      {renderBlockContent()}
      {showSlashMenu && (
        <SlashCommandMenu
          query={slashQuery}
          onSelect={handleSlashSelect}
          onClose={() => setShowSlashMenu(false)}
        />
      )}
    </>
  );
}

function FormattingToolbar({
  onFormat,
}: {
  onFormat: (format: string) => void;
}) {
  const tools = [
    { icon: <Bold className="h-3.5 w-3.5" />, format: "bold", label: "Bold" },
    {
      icon: <Italic className="h-3.5 w-3.5" />,
      format: "italic",
      label: "Italic",
    },
    {
      icon: <Underline className="h-3.5 w-3.5" />,
      format: "underline",
      label: "Underline",
    },
    {
      icon: <Strikethrough className="h-3.5 w-3.5" />,
      format: "strikethrough",
      label: "Strikethrough",
    },
  ];

  return (
    <div className="inline-flex items-center gap-0.5 bg-popover border border-border rounded-lg p-0.5 shadow-sm">
      {tools.map((tool) => (
        <Button
          key={tool.format}
          variant="ghost"
          size="icon"
          className="h-7 w-7 text-muted-foreground hover:text-foreground"
          onClick={() => onFormat(tool.format)}
          title={tool.label}
        >
          {tool.icon}
        </Button>
      ))}
    </div>
  );
}

export function createDocPageScreen({
  api,
  endpoints,
}: CreateDocPageScreenDeps) {
  const usePage = createUsePage(api, endpoints);
  const usePageBlocks = createUsePageBlocks(api, endpoints);
  const usePageBreadcrumb = createUsePageBreadcrumb(api, endpoints);
  const useCreateBlock = createUseCreateBlock(api, endpoints);
  const useUpdateBlock = createUseUpdateBlock(api, endpoints);
  const useDeleteBlock = createUseDeleteBlock(api, endpoints);
  const useReorderBlocks = createUseReorderBlocks(api, endpoints);
  const useUpdatePage = createUseUpdatePage(api, endpoints);
  const DocPageTree = createDocPageTree({ api, endpoints });

  return function DocPageScreen({
    workspaceId,
    pageId,
  }: {
    workspaceId: string;
    pageId: string;
  }) {
    const { data: page, isLoading: pageLoading } = usePage(workspaceId, pageId);
    const { data: blocks = [], isLoading: blocksLoading } = usePageBlocks(
      workspaceId,
      pageId,
    ) as { data: Block[]; isLoading: boolean };
    const { data: breadcrumbs = [] } = usePageBreadcrumb(workspaceId, pageId);

    const updatePageMutation = useUpdatePage(workspaceId, pageId);
    const createBlockMutation = useCreateBlock(workspaceId, pageId);
    const deleteBlockMutation = useDeleteBlock(workspaceId, pageId);
    const reorderBlocksMutation = useReorderBlocks(workspaceId, pageId);

    const [sidePanel, setSidePanel] = useState<
      "comments" | "history" | "tree" | null
    >(null);

    const sensors = useSensors(
      useSensor(PointerSensor, {
        activationConstraint: { distance: 8 },
      }),
      useSensor(KeyboardSensor, {
        coordinateGetter: sortableKeyboardCoordinates,
      }),
    );

    const handleTitleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
      const text = e.target.value;
      if (page && text.trim() && text !== page.title) {
        updatePageMutation.mutate({ title: text });
      }
    };

    const handleAddBlock = (type: BlockType) => {
      const position =
        blocks.length > 0 ? Math.max(...blocks.map((b) => b.position)) + 1 : 0;
      const defaults: Partial<Record<BlockType, { text: string }>> = {
        todo: { text: "To-do item" },
        bulleted_list: { text: "List item" },
        numbered_list: { text: "List item" },
        quote: { text: "Quote" },
        callout: { text: "Callout" },
        code: { text: "// code" },
      };
      createBlockMutation.mutate({
        type,
        properties: defaults[type] || { text: "New block" },
        position,
      });
    };

    const handleDeleteBlock = (blockId: string) => {
      deleteBlockMutation.mutate(blockId as never);
    };

    const handleDragEnd = (event: DragEndEvent) => {
      const { active, over } = event;
      if (!over || active.id === over.id) return;

      const oldIndex = blocks.findIndex((b: Block) => b.id === active.id);
      const newIndex = blocks.findIndex((b: Block) => b.id === over.id);

      if (oldIndex === -1 || newIndex === -1) return;

      const reordered = arrayMove(blocks, oldIndex, newIndex);

      reorderBlocksMutation.mutate({
        pageId,
        orderedBlockIds: reordered.map((b) => b.id as string),
      });
    };

    const handleFormat = (format: string) => {
      const selection = window.getSelection();
      if (!selection || selection.rangeCount === 0) return;

      const range = selection.getRangeAt(0);
      const text = range.toString();

      if (!text) return;

      let wrapped: string;
      switch (format) {
        case "bold":
          wrapped = `**${text}**`;
          break;
        case "italic":
          wrapped = `*${text}*`;
          break;
        case "underline":
          wrapped = `<u>${text}</u>`;
          break;
        case "strikethrough":
          wrapped = `~~${text}~~`;
          break;
        default:
          return;
      }

      range.deleteContents();
      range.insertNode(document.createTextNode(wrapped));
      selection.removeAllRanges();
    };

    if (pageLoading || blocksLoading) {
      return (
        <div className="p-8 space-y-6 max-w-3xl mx-auto">
          <Skeleton className="h-8 w-1/3" />
          <Skeleton className="h-64 w-full rounded-2xl" />
          <Skeleton className="h-6 w-3/4" />
          <Skeleton className="h-6 w-1/2" />
        </div>
      );
    }

    if (!page) {
      return (
        <div className="p-8 text-center text-muted-foreground">
          Page not found or you do not have permission.
        </div>
      );
    }

    return (
      <div className="flex h-full overflow-hidden bg-background">
        {/* Main content */}
        <div className="flex-1 overflow-y-auto flex flex-col h-full">
          {/* Breadcrumb */}
          <div className="px-8 py-2 border-b flex items-center gap-1.5 text-xs text-muted-foreground">
            <BookOpen className="h-3.5 w-3.5" />
            <ChevronRight className="h-3 w-3" />
            {breadcrumbs.map((bc: BreadcrumbItem, idx: number) => (
              <div key={bc.id} className="flex items-center gap-1">
                <span>{bc.title}</span>
                {idx < breadcrumbs.length - 1 && (
                  <ChevronRight className="h-3 w-3" />
                )}
              </div>
            ))}
          </div>

          {/* Cover */}
          <div className="h-32 w-full bg-gradient-to-r from-violet-200 to-indigo-100 dark:from-violet-950/20 dark:to-indigo-950/20" />

          {/* Actions bar */}
          <div className="px-8 py-2 flex items-center justify-between border-b">
            <div className="flex items-center gap-1">
              <Button
                variant="ghost"
                size="sm"
                className={`h-7 gap-1.5 text-xs ${
                  page.isFavorited
                    ? "text-yellow-500"
                    : "text-muted-foreground hover:text-foreground"
                }`}
              >
                <span className={page.isFavorited ? "fill-yellow-500" : ""}>
                  ★
                </span>
                {page.isFavorited ? "Favorited" : "Favorite"}
              </Button>
              <Button
                variant="ghost"
                size="sm"
                className="h-7 gap-1.5 text-xs text-muted-foreground hover:text-foreground"
              >
                Share
              </Button>
            </div>

            <div className="flex items-center gap-1">
              <Button
                variant={sidePanel === "tree" ? "secondary" : "ghost"}
                size="icon"
                className="h-7 w-7 text-muted-foreground hover:text-foreground"
                onClick={() =>
                  setSidePanel(sidePanel === "tree" ? null : "tree")
                }
              >
                <BookOpen className="h-4 w-4" />
              </Button>
              <Button
                variant={sidePanel === "comments" ? "secondary" : "ghost"}
                size="icon"
                className="h-7 w-7 text-muted-foreground hover:text-foreground"
                onClick={() =>
                  setSidePanel(sidePanel === "comments" ? null : "comments")
                }
              >
                <MessageSquare className="h-4 w-4" />
              </Button>
              <Button
                variant={sidePanel === "history" ? "secondary" : "ghost"}
                size="icon"
                className="h-7 w-7 text-muted-foreground hover:text-foreground"
                onClick={() =>
                  setSidePanel(sidePanel === "history" ? null : "history")
                }
              >
                <Clock className="h-4 w-4" />
              </Button>
            </div>
          </div>

          {/* Editor area */}
          <div className="flex-1 max-w-3xl w-full mx-auto px-8 sm:px-12 py-10 space-y-6">
            <input
              type="text"
              defaultValue={page.title}
              onBlur={handleTitleBlur}
              placeholder="Untitled"
              className="text-4xl font-bold bg-transparent border-none outline-none w-full text-foreground placeholder:text-muted-foreground/30 focus:ring-0 p-0"
            />

            <FormattingToolbar onFormat={handleFormat} />

            <div className="space-y-3">
              {blocks.length === 0 ? (
                <p className="text-sm text-muted-foreground italic py-4">
                  Press{" "}
                  <kbd className="px-1.5 py-0.5 bg-muted rounded text-xs font-mono">
                    /
                  </kbd>{" "}
                  for commands or type to start writing.
                </p>
              ) : (
                <DndContext
                  sensors={sensors}
                  collisionDetection={closestCenter}
                  onDragEnd={handleDragEnd}
                >
                  <SortableContext
                    items={blocks.map((b) => String(b.id))}
                    strategy={verticalListSortingStrategy}
                  >
                    {blocks.map((block) => (
                      <SortableBlockEditor
                        key={block.id}
                        workspaceId={workspaceId}
                        pageId={pageId}
                        block={block}
                        onDelete={handleDeleteBlock}
                        useUpdateBlock={useUpdateBlock}
                      />
                    ))}
                  </SortableContext>
                </DndContext>
              )}
            </div>

            <div className="border-t pt-4 flex flex-wrap gap-1.5">
              {BLOCK_MENU_ITEMS.filter((item) =>
                [
                  "paragraph",
                  "heading_1",
                  "heading_2",
                  "todo",
                  "bulleted_list",
                  "quote",
                  "divider",
                ].includes(item.type),
              ).map((item) => (
                <Button
                  key={item.type}
                  variant="ghost"
                  size="sm"
                  onClick={() => handleAddBlock(item.type)}
                  className="h-7 gap-1.5 text-xs text-muted-foreground hover:text-foreground"
                >
                  {item.icon}
                  {item.label}
                </Button>
              ))}
            </div>
          </div>
        </div>

        {/* Side panel */}
        {sidePanel && (
          <div className="w-80 border-l bg-card overflow-y-auto">
            <div className="sticky top-0 bg-card border-b px-4 py-3 flex items-center justify-between">
              <h3 className="font-semibold text-sm capitalize">{sidePanel}</h3>
              <Button
                variant="ghost"
                size="icon"
                className="h-6 w-6"
                onClick={() => setSidePanel(null)}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
            <div className="p-4">
              {sidePanel === "comments" && (
                <DocComments
                  api={api}
                  endpoints={endpoints}
                  workspaceId={workspaceId}
                  pageId={pageId}
                />
              )}
              {sidePanel === "history" && (
                <DocHistory
                  api={api}
                  endpoints={endpoints}
                  workspaceId={workspaceId}
                  pageId={pageId}
                />
              )}
              {sidePanel === "tree" && (
                <DocPageTree workspaceId={workspaceId} currentPageId={pageId} />
              )}
            </div>
          </div>
        )}
      </div>
    );
  };
}
