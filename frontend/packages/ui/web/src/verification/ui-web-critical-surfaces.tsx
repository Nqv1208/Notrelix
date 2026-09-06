import * as React from "react";

import { NotrelixLogo } from "../components/brand/notrelix-logo";
import { AccessDeniedState } from "../components/feedback/access-denied-state";
import { EmptyState } from "../components/feedback/empty-state";
import { ErrorState } from "../components/feedback/error-state";
import { ForbiddenState } from "../components/feedback/forbidden-state";
import { LoadingState } from "../components/feedback/loading-state";
import { MockDisabledState } from "../components/feedback/mock-disabled-state";
import { NotFoundState } from "../components/feedback/not-found-state";
import { UpgradeRequiredState } from "../components/feedback/upgrade-required-state";
import { SubmitState } from "../components/forms/submit-state";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "../components/ui/accordion";
import { Alert, AlertDescription, AlertTitle } from "../components/ui/alert";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "../components/ui/alert-dialog";
import { AspectRatio } from "../components/ui/aspect-ratio";
import { Avatar, AvatarFallback } from "../components/ui/avatar";
import { Badge } from "../components/ui/badge";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "../components/ui/breadcrumb";
import { Button } from "../components/ui/button";
import {
  ButtonGroup,
  ButtonGroupSeparator,
} from "../components/ui/button-group";
import { Calendar } from "../components/ui/calendar";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import {
  Carousel,
  CarouselContent,
  CarouselItem,
  CarouselNext,
  CarouselPrevious,
} from "../components/ui/carousel";
import { ChartContainer } from "../components/ui/chart";
import { Checkbox } from "../components/ui/checkbox";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "../components/ui/collapsible";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "../components/ui/command";
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuLabel,
  ContextMenuTrigger,
} from "../components/ui/context-menu";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "../components/ui/dialog";
import {
  Drawer,
  DrawerContent,
  DrawerDescription,
  DrawerHeader,
  DrawerTitle,
  DrawerTrigger,
} from "../components/ui/drawer";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuTrigger,
} from "../components/ui/dropdown-menu";
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyTitle,
} from "../components/ui/empty";
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "../components/ui/field";
import {
  HoverCard,
  HoverCardContent,
  HoverCardTrigger,
} from "../components/ui/hover-card";
import { Input } from "../components/ui/input";
import {
  InputGroup,
  InputGroupAddon,
  InputGroupInput,
} from "../components/ui/input-group";
import {
  InputOTP,
  InputOTPGroup,
  InputOTPSlot,
} from "../components/ui/input-otp";
import {
  Item,
  ItemContent,
  ItemDescription,
  ItemTitle,
} from "../components/ui/item";
import { Kbd, KbdGroup } from "../components/ui/kbd";
import { Label } from "../components/ui/label";
import {
  Menubar,
  MenubarContent,
  MenubarItem,
  MenubarMenu,
  MenubarTrigger,
} from "../components/ui/menubar";
import {
  NavigationMenu,
  NavigationMenuItem,
  NavigationMenuLink,
  NavigationMenuList,
  NavigationMenuTrigger,
} from "../components/ui/navigation-menu";
import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "../components/ui/pagination";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "../components/ui/popover";
import { Progress } from "../components/ui/progress";
import { RadioGroup, RadioGroupItem } from "../components/ui/radio-group";
import {
  ResizableHandle,
  ResizablePanel,
  ResizablePanelGroup,
} from "../components/ui/resizable";
import { ScrollArea } from "../components/ui/scroll-area";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "../components/ui/select";
import { Separator } from "../components/ui/separator";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "../components/ui/sheet";
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarTrigger,
} from "../components/ui/sidebar";
import { Skeleton } from "../components/ui/skeleton";
import { Slider } from "../components/ui/slider";
import { Spinner } from "../components/ui/spinner";
import { Switch } from "../components/ui/switch";
import {
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "../components/ui/table";
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from "../components/ui/tabs";
import { Textarea } from "../components/ui/textarea";
import { Toaster } from "../components/ui/sonner";
import { Toggle } from "../components/ui/toggle";
import { ToggleGroup, ToggleGroupItem } from "../components/ui/toggle-group";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "../components/ui/tooltip";

function SurfaceFrame({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <section className="space-y-4 rounded-lg border bg-background p-6 text-foreground">
      <div>
        <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
          UI Web critical surface
        </p>
        <h2 className="text-xl font-semibold">{title}</h2>
      </div>
      <div className="flex flex-wrap items-center gap-4">{children}</div>
    </section>
  );
}

export function UiWebBrandLogoSurface() {
  return (
    <SurfaceFrame title="Brand logo">
      <NotrelixLogo size="lg" />
      <NotrelixLogo size="sm" showWordmark={false} />
    </SurfaceFrame>
  );
}

export function UiWebFeedbackStatesSurface() {
  return (
    <div className="grid gap-4 bg-background p-6 text-foreground md:grid-cols-2">
      <AccessDeniedState />
      <EmptyState title="No records" description="Create the first record." />
      <ErrorState error={new Error("Network unavailable")} />
      <ForbiddenState />
      <LoadingState title="Loading" description="Fetching data." />
      <MockDisabledState featureName="Offline boards" />
      <NotFoundState title="Not found" description="Resource is missing." />
      <UpgradeRequiredState title="Upgrade required" />
    </div>
  );
}

export function UiWebSubmitStateSurface() {
  return (
    <SurfaceFrame title="Submit state">
      <SubmitState variant="outline">Save changes</SubmitState>
      <SubmitState variant="outline" pending pendingLabel="Saving changes">
        Save changes
      </SubmitState>
      <SubmitState variant="outline" disabled>
        Disabled save
      </SubmitState>
    </SurfaceFrame>
  );
}

export function UiWebFormControlsSurface() {
  return (
    <SurfaceFrame title="Form controls">
      <Label htmlFor="ui-web-email">Email</Label>
      <Input id="ui-web-email" type="email" placeholder="name@example.com" />
      <Label htmlFor="ui-web-notes">Notes</Label>
      <Textarea id="ui-web-notes" placeholder="Write notes" />
      <InputGroup className="max-w-64">
        <InputGroupAddon>@</InputGroupAddon>
        <InputGroupInput aria-label="Username" placeholder="workspace" />
      </InputGroup>
      <Label htmlFor="ui-web-otp">Verification code</Label>
      <InputOTP id="ui-web-otp" maxLength={3} aria-label="Verification code">
        <InputOTPGroup>
          <InputOTPSlot index={0} />
          <InputOTPSlot index={1} />
          <InputOTPSlot index={2} />
        </InputOTPGroup>
      </InputOTP>
      <Label className="flex items-center gap-2">
        <Checkbox aria-label="Enabled" defaultChecked /> Enabled
      </Label>
      <Switch aria-label="Notifications" defaultChecked />
      <RadioGroup defaultValue="medium" aria-label="Priority">
        <div className="flex items-center gap-2">
          <RadioGroupItem
            aria-label="Low priority"
            value="low"
            id="ui-web-low"
          />
          <Label htmlFor="ui-web-low">Low</Label>
        </div>
        <div className="flex items-center gap-2">
          <RadioGroupItem
            aria-label="Medium priority"
            value="medium"
            id="ui-web-medium"
          />
          <Label htmlFor="ui-web-medium">Medium</Label>
        </div>
      </RadioGroup>
      <Select defaultValue="todo">
        <SelectTrigger aria-label="Status" className="w-40">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="todo">To do</SelectItem>
          <SelectItem value="done">Done</SelectItem>
        </SelectContent>
      </Select>
      <Slider
        aria-label="Progress target"
        defaultValue={[64]}
        className="w-48"
      />
      <FieldGroup className="max-w-80">
        <Field>
          <FieldLabel htmlFor="ui-web-field">Workspace name</FieldLabel>
          <Input id="ui-web-field" defaultValue="Platform" />
          <FieldDescription>Visible to workspace members.</FieldDescription>
          <FieldError className="text-foreground">
            Use at least three characters.
          </FieldError>
        </Field>
      </FieldGroup>
    </SurfaceFrame>
  );
}

export function UiWebNavigationPrimitivesSurface() {
  return (
    <div className="space-y-6 bg-background p-6 text-foreground">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="#">Home</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Workspace</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <Tabs defaultValue="overview" className="w-full max-w-md">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="activity">Activity</TabsTrigger>
        </TabsList>
        <TabsContent value="overview">Overview content.</TabsContent>
        <TabsContent value="activity">Activity content.</TabsContent>
      </Tabs>
      <NavigationMenu>
        <NavigationMenuList>
          <NavigationMenuItem>
            <NavigationMenuTrigger>Products</NavigationMenuTrigger>
            <NavigationMenuLink href="#">Boards</NavigationMenuLink>
          </NavigationMenuItem>
        </NavigationMenuList>
      </NavigationMenu>
      <Menubar>
        <MenubarMenu>
          <MenubarTrigger>File</MenubarTrigger>
          <MenubarContent>
            <MenubarItem>New board</MenubarItem>
          </MenubarContent>
        </MenubarMenu>
      </Menubar>
      <Pagination>
        <PaginationContent>
          <PaginationItem>
            <PaginationPrevious href="#" />
          </PaginationItem>
          <PaginationItem>
            <PaginationLink href="#" isActive>
              1
            </PaginationLink>
          </PaginationItem>
          <PaginationItem>
            <PaginationNext href="#" />
          </PaginationItem>
        </PaginationContent>
      </Pagination>
      <div className="h-56 overflow-hidden rounded-lg border">
        <SidebarProvider>
          <Sidebar collapsible="icon">
            <SidebarHeader />
            <SidebarContent>
              <SidebarGroup>
                <SidebarGroupLabel>Workspace</SidebarGroupLabel>
                <SidebarGroupContent>
                  <SidebarMenu>
                    <SidebarMenuItem>
                      <SidebarMenuButton>Boards</SidebarMenuButton>
                    </SidebarMenuItem>
                  </SidebarMenu>
                </SidebarGroupContent>
              </SidebarGroup>
            </SidebarContent>
          </Sidebar>
          <main className="flex flex-1 gap-2 p-3">
            <SidebarTrigger />
            <span>Content</span>
          </main>
        </SidebarProvider>
      </div>
    </div>
  );
}

export function UiWebOverlayPrimitivesSurface() {
  return (
    <SurfaceFrame title="Overlay primitives">
      <Dialog>
        <DialogTrigger asChild>
          <Button variant="outline">Open dialog</Button>
        </DialogTrigger>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create board</DialogTitle>
            <DialogDescription>Configure a new board.</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button>Create</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      <AlertDialog>
        <AlertDialogTrigger asChild>
          <Button variant="outline">Delete board</Button>
        </AlertDialogTrigger>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete board?</AlertDialogTitle>
            <AlertDialogDescription>
              This cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction>Delete</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
      <Sheet>
        <SheetTrigger asChild>
          <Button variant="outline">Open sheet</Button>
        </SheetTrigger>
        <SheetContent>
          <SheetHeader>
            <SheetTitle>Filters</SheetTitle>
            <SheetDescription>Refine visible work.</SheetDescription>
          </SheetHeader>
        </SheetContent>
      </Sheet>
      <Drawer>
        <DrawerTrigger asChild>
          <Button variant="outline">Open drawer</Button>
        </DrawerTrigger>
        <DrawerContent>
          <DrawerHeader>
            <DrawerTitle>Quick actions</DrawerTitle>
            <DrawerDescription>Actions available here.</DrawerDescription>
          </DrawerHeader>
        </DrawerContent>
      </Drawer>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline">Open menu</Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent>
          <DropdownMenuLabel>Actions</DropdownMenuLabel>
          <DropdownMenuItem>Rename</DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
      <Popover>
        <PopoverTrigger asChild>
          <Button variant="outline">Open popover</Button>
        </PopoverTrigger>
        <PopoverContent>Popover content.</PopoverContent>
      </Popover>
      <ContextMenu>
        <ContextMenuTrigger className="rounded border border-dashed px-3 py-2">
          Context target
        </ContextMenuTrigger>
        <ContextMenuContent>
          <ContextMenuLabel>Item</ContextMenuLabel>
          <ContextMenuItem>Open</ContextMenuItem>
        </ContextMenuContent>
      </ContextMenu>
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <Button variant="outline">Tooltip trigger</Button>
          </TooltipTrigger>
          <TooltipContent>Tooltip content</TooltipContent>
        </Tooltip>
      </TooltipProvider>
      <HoverCard>
        <HoverCardTrigger asChild>
          <Button variant="outline">Hover card</Button>
        </HoverCardTrigger>
        <HoverCardContent>Member since 2024.</HoverCardContent>
      </HoverCard>
      <Command className="max-w-80 rounded-lg border">
        <CommandInput placeholder="Search commands" />
        <CommandList>
          <CommandEmpty>No results.</CommandEmpty>
          <CommandGroup heading="Suggestions">
            <CommandItem>Create board</CommandItem>
          </CommandGroup>
        </CommandList>
      </Command>
    </SurfaceFrame>
  );
}

export function UiWebDataDisplayPrimitivesSurface() {
  return (
    <div className="space-y-6 bg-background p-6 text-foreground">
      <Alert>
        <AlertTitle>Status</AlertTitle>
        <AlertDescription>System is available.</AlertDescription>
      </Alert>
      <Card className="max-w-sm">
        <CardHeader>
          <CardTitle>Product launch</CardTitle>
          <CardDescription>Track release work.</CardDescription>
        </CardHeader>
        <CardContent className="flex items-center gap-3">
          <Avatar>
            <AvatarFallback>PL</AvatarFallback>
          </Avatar>
          <Badge>Active</Badge>
          <Progress value={64} aria-label="Completion" />
        </CardContent>
      </Card>
      <ButtonGroup>
        <Button variant="outline">Undo</Button>
        <ButtonGroupSeparator />
        <Button variant="outline">Redo</Button>
      </ButtonGroup>
      <Accordion type="single" collapsible className="max-w-md">
        <AccordionItem value="one">
          <AccordionTrigger>What is tracked?</AccordionTrigger>
          <AccordionContent>Workspace work items.</AccordionContent>
        </AccordionItem>
      </Accordion>
      <Collapsible className="max-w-md">
        <CollapsibleTrigger asChild>
          <Button variant="outline">Toggle details</Button>
        </CollapsibleTrigger>
        <CollapsibleContent className="pt-2">
          Details content.
        </CollapsibleContent>
      </Collapsible>
      <Table className="max-w-lg">
        <TableCaption>Board inventory</TableCaption>
        <TableHeader>
          <TableRow>
            <TableHead>Board</TableHead>
            <TableHead>Items</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          <TableRow>
            <TableCell>Launch</TableCell>
            <TableCell>24</TableCell>
          </TableRow>
        </TableBody>
      </Table>
      <Item>
        <ItemContent>
          <ItemTitle>Platform initiative</ItemTitle>
          <ItemDescription>Owned by platform.</ItemDescription>
        </ItemContent>
      </Item>
      <div className="flex flex-wrap items-center gap-4">
        <KbdGroup>
          <Kbd>⌘</Kbd>
          <Kbd>K</Kbd>
        </KbdGroup>
        <Toggle>Bold</Toggle>
        <ToggleGroup type="single" aria-label="Density">
          <ToggleGroupItem value="compact">Compact</ToggleGroupItem>
        </ToggleGroup>
        <Spinner aria-label="Loading indicator" />
        <Separator orientation="vertical" className="h-8" />
        <Skeleton className="h-4 w-24" />
      </div>
      <ScrollArea className="h-24 max-w-md rounded border p-2">
        {Array.from({ length: 12 }, (_, index) => (
          <p key={index}>Scrollable row {index + 1}</p>
        ))}
      </ScrollArea>
      <ResizablePanelGroup
        direction="horizontal"
        className="max-w-md rounded border"
      >
        <ResizablePanel defaultSize={50}>
          <div className="p-3">Panel A</div>
        </ResizablePanel>
        <ResizableHandle />
        <ResizablePanel defaultSize={50}>
          <div className="p-3">Panel B</div>
        </ResizablePanel>
      </ResizablePanelGroup>
      <Carousel className="max-w-md">
        <CarouselContent>
          <CarouselItem>
            <div className="rounded bg-muted p-8 text-center">Slide one</div>
          </CarouselItem>
          <CarouselItem>
            <div className="rounded bg-muted p-8 text-center">Slide two</div>
          </CarouselItem>
        </CarouselContent>
        <CarouselPrevious aria-label="Previous slide" />
        <CarouselNext aria-label="Next slide" />
      </Carousel>
      <Calendar mode="single" defaultMonth={new Date("2026-09-01T00:00:00Z")} />
      <AspectRatio ratio={16 / 9} className="max-w-xs rounded bg-muted">
        <div className="flex h-full items-center justify-center">16:9</div>
      </AspectRatio>
      <ChartContainer
        config={{ items: { label: "Items", color: "var(--chart-1)" } }}
        className="h-24 max-w-md"
      >
        <div
          aria-label="Chart placeholder"
          role="img"
          className="h-full rounded bg-muted"
        />
      </ChartContainer>
      <Empty>
        <EmptyHeader>
          <EmptyTitle>No rows</EmptyTitle>
          <EmptyDescription>Add data to continue.</EmptyDescription>
        </EmptyHeader>
      </Empty>
      <Toaster />
    </div>
  );
}
