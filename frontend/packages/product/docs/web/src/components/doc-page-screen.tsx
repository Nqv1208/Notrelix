import { useState } from 'react';
import {
  createUsePage,
  createUsePageBlocks,
  createUsePageBreadcrumb,
  createUseCreateBlock,
  createUseUpdateBlock,
  createUseDeleteBlock,
  createUseUpdatePage,
  type DocsApiClient,
  type PageApiEndpoints,
  type Block,
  type BlockType,
} from '@notrelix/docs-core';
import { Button, Input, Skeleton, ThemeProvider } from '@notrelix/ui-web';
import {
  Plus,
  Trash2,
  CheckSquare,
  Square,
  Heading1,
  Heading2,
  Heading3,
  AlignLeft,
  Image as ImageIcon,
  Code as CodeIcon,
  MessageSquare,
  Clock,
  ChevronRight,
  BookOpen,
} from 'lucide-react';

interface CreateDocPageScreenDeps {
  api: DocsApiClient;
  endpoints: PageApiEndpoints;
}

export function createDocPageScreen({ api, endpoints }: CreateDocPageScreenDeps) {
  const usePage = createUsePage(api, endpoints);
  const usePageBlocks = createUsePageBlocks(api, endpoints);
  const usePageBreadcrumb = createUsePageBreadcrumb(api, endpoints);
  const useCreateBlock = createUseCreateBlock(api, endpoints);
  const useUpdateBlock = createUseUpdateBlock(api, endpoints);
  const useDeleteBlock = createUseDeleteBlock(api, endpoints);
  const useUpdatePage = createUseUpdatePage(api, endpoints);

  return function DocPageScreen({ workspaceId, pageId }: { workspaceId: string; pageId: string }) {
    const { data: page, isLoading: pageLoading } = usePage(pageId);
    const { data: blocks = [], isLoading: blocksLoading } = usePageBlocks(pageId);
    const { data: breadcrumbs = [] } = usePageBreadcrumb(pageId);

    const updatePageMutation = useUpdatePage(workspaceId, pageId);
    const createBlockMutation = useCreateBlock(pageId);
    const updateBlockMutation = useUpdateBlock(pageId, '');
    const deleteBlockMutation = useDeleteBlock(pageId);

    const [titleInput, setTitleInput] = useState('');
    const [sidebarOpen, setSidebarOpen] = useState(false);
    const [activeTab, setActiveTab] = useState<'comments' | 'history'>('comments');

    const handleTitleBlur = () => {
      if (page && titleInput.trim() && titleInput !== page.title) {
        updatePageMutation.mutate({ title: titleInput });
      }
    };

    const handleAddBlock = (type: BlockType) => {
      const position = blocks.length > 0 ? Math.max(...blocks.map((b) => b.position)) + 1 : 0;
      createBlockMutation.mutate({
        type,
        properties: { text: type === 'todo' ? 'To-do item' : 'New block text' },
        position,
      });
    };

    const handleUpdateBlockText = (blockId: string, text: string) => {
      // Create a temporary local update helper
      const updateBlock = createUseUpdateBlock(api, endpoints)(pageId, blockId);
      updateBlock.mutate({
        properties: { text },
      });
    };

    const handleToggleTodo = (blockId: string, checked: boolean) => {
      const updateBlock = createUseUpdateBlock(api, endpoints)(pageId, blockId);
      updateBlock.mutate({
        properties: { checked: !checked },
      });
    };

    const handleDeleteBlock = (blockId: string) => {
      deleteBlockMutation.mutate(blockId);
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
        {/* Editor Area */}
        <div className="flex-1 overflow-y-auto flex flex-col h-full">
          {/* Breadcrumb Nav */}
          <div className="px-8 py-3 border-b flex items-center gap-1.5 text-xs text-muted-foreground">
            <BookOpen className="h-3.5 w-3.5" />
            <ChevronRight className="h-3 w-3" />
            {breadcrumbs.map((bc, idx) => (
              <div key={bc.id} className="flex items-center gap-1">
                <span>{bc.title}</span>
                {idx < breadcrumbs.length - 1 && <ChevronRight className="h-3 w-3" />}
              </div>
            ))}
          </div>

          {/* Cover Color & Icon placeholder */}
          <div className="h-44 w-full bg-gradient-to-r from-violet-200 to-indigo-100 dark:from-violet-955/20 dark:to-indigo-950/20 relative" />

          {/* Notion Canvas */}
          <div className="flex-1 max-w-3xl w-full mx-auto px-8 sm:px-12 py-10 space-y-8">
            {/* Page Title */}
            <input
              type="text"
              defaultValue={page.title}
              onBlur={handleTitleBlur}
              placeholder="Untitled"
              className="text-4xl font-bold bg-transparent border-none outline-none w-full text-foreground placeholder:text-muted-foreground/30 focus:ring-0 p-0"
            />

            {/* Blocks List */}
            <div className="space-y-4">
              {blocks.length === 0 ? (
                <p className="text-sm text-muted-foreground italic py-4">
                  This page has no content. Press the buttons below to add blocks.
                </p>
              ) : (
                blocks.map((block) => (
                  <div key={block.id} className="group flex items-start gap-2.5 relative -ml-6 pl-6">
                    {/* Block Action Bar (visible on hover) */}
                    <div className="absolute left-0 top-1 opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-5 w-5 text-muted-foreground hover:text-destructive hover:bg-destructive/10"
                        onClick={() => handleDeleteBlock(block.id)}
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </div>

                    {/* Block Content by Type */}
                    <div className="flex-1">
                      {block.type === 'paragraph' && (
                        <input
                          type="text"
                          defaultValue={block.properties.text || ''}
                          onBlur={(e) => handleUpdateBlockText(block.id, e.target.value)}
                          placeholder="Type something..."
                          className="w-full bg-transparent border-none outline-none text-base text-foreground focus:ring-0 p-0"
                        />
                      )}

                      {block.type === 'heading_1' && (
                        <input
                          type="text"
                          defaultValue={block.properties.text || ''}
                          onBlur={(e) => handleUpdateBlockText(block.id, e.target.value)}
                          placeholder="Heading 1"
                          className="w-full bg-transparent border-none outline-none text-2xl font-bold text-foreground focus:ring-0 p-0"
                        />
                      )}

                      {block.type === 'heading_2' && (
                        <input
                          type="text"
                          defaultValue={block.properties.text || ''}
                          onBlur={(e) => handleUpdateBlockText(block.id, e.target.value)}
                          placeholder="Heading 2"
                          className="w-full bg-transparent border-none outline-none text-xl font-semibold text-foreground focus:ring-0 p-0"
                        />
                      )}

                      {block.type === 'heading_3' && (
                        <input
                          type="text"
                          defaultValue={block.properties.text || ''}
                          onBlur={(e) => handleUpdateBlockText(block.id, e.target.value)}
                          placeholder="Heading 3"
                          className="w-full bg-transparent border-none outline-none text-lg font-medium text-foreground focus:ring-0 p-0"
                        />
                      )}

                      {block.type === 'todo' && (
                        <div className="flex items-center gap-2">
                          <button
                            onClick={() => handleToggleTodo(block.id, block.properties.checked || false)}
                            className="text-muted-foreground hover:text-primary transition-colors"
                          >
                            {block.properties.checked ? (
                              <CheckSquare className="h-4.5 w-4.5 text-primary" />
                            ) : (
                              <Square className="h-4.5 w-4.5" />
                            )}
                          </button>
                          <input
                            type="text"
                            defaultValue={block.properties.text || ''}
                            onBlur={(e) => handleUpdateBlockText(block.id, e.target.value)}
                            className={`w-full bg-transparent border-none outline-none text-base focus:ring-0 p-0 ${
                              block.properties.checked ? 'line-through text-muted-foreground' : 'text-foreground'
                            }`}
                          />
                        </div>
                      )}

                      {block.type === 'callout' && (
                        <div className="flex items-start gap-3 p-4 rounded-xl border bg-muted/30">
                          <span className="text-xl">💡</span>
                          <input
                            type="text"
                            defaultValue={block.properties.text || ''}
                            onBlur={(e) => handleUpdateBlockText(block.id, e.target.value)}
                            className="w-full bg-transparent border-none outline-none text-sm text-foreground focus:ring-0 p-0"
                          />
                        </div>
                      )}
                    </div>
                  </div>
                ))
              )}
            </div>

            {/* Block Insert Toolbar */}
            <div className="border-t pt-6 space-y-3">
              <h3 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                Insert Block
              </h3>
              <div className="flex flex-wrap gap-2">
                <Button variant="outline" size="sm" onClick={() => handleAddBlock('paragraph')} className="h-8 gap-1.5 text-xs">
                  <AlignLeft className="h-3.5 w-3.5" /> Paragraph
                </Button>
                <Button variant="outline" size="sm" onClick={() => handleAddBlock('heading_1')} className="h-8 gap-1.5 text-xs">
                  <Heading1 className="h-3.5 w-3.5" /> Heading 1
                </Button>
                <Button variant="outline" size="sm" onClick={() => handleAddBlock('heading_2')} className="h-8 gap-1.5 text-xs">
                  <Heading2 className="h-3.5 w-3.5" /> Heading 2
                </Button>
                <Button variant="outline" size="sm" onClick={() => handleAddBlock('todo')} className="h-8 gap-1.5 text-xs">
                  <CheckSquare className="h-3.5 w-3.5" /> To-do List
                </Button>
                <Button variant="outline" size="sm" onClick={() => handleAddBlock('callout')} className="h-8 gap-1.5 text-xs">
                  💡 Callout
                </Button>
              </div>
            </div>
          </div>
        </div>

        {/* Optional Collapsible Sidebar for Comments/History */}
        {sidebarOpen && (
          <div className="w-80 border-l bg-card flex flex-col h-full">
            <div className="flex border-b">
              <button
                onClick={() => setActiveTab('comments')}
                className={`flex-1 py-3 text-center text-xs font-medium border-b-2 ${
                  activeTab === 'comments' ? 'border-primary text-primary' : 'border-transparent text-muted-foreground'
                }`}
              >
                Comments
              </button>
              <button
                onClick={() => setActiveTab('history')}
                className={`flex-1 py-3 text-center text-xs font-medium border-b-2 ${
                  activeTab === 'history' ? 'border-primary text-primary' : 'border-transparent text-muted-foreground'
                }`}
              >
                History
              </button>
            </div>
            <div className="flex-1 overflow-y-auto p-4">
              {activeTab === 'comments' ? (
                <div className="text-center text-xs text-muted-foreground italic py-10">
                  No comments yet.
                </div>
              ) : (
                <div className="text-center text-xs text-muted-foreground italic py-10">
                  No edit history recorded.
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    );
  };
}
