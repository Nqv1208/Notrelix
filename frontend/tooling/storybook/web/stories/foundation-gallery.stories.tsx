import type { Meta, StoryObj } from "@storybook/react";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
  Alert,
  AlertDescription,
  AlertTitle,
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
  AspectRatio,
  Avatar,
  AvatarFallback,
  AvatarGroup,
  AvatarGroupCount,
  Badge,
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
  Button,
  ButtonGroup,
  ButtonGroupSeparator,
  Calendar,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Carousel,
  CarouselContent,
  CarouselItem,
  CarouselNext,
  CarouselPrevious,
  ChartContainer,
  ChartLegend,
  ChartLegendContent,
  ChartStyle,
  ChartTooltip,
  ChartTooltipContent,
  Checkbox,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuLabel,
  ContextMenuSeparator,
  ContextMenuShortcut,
  ContextMenuTrigger,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  Drawer,
  DrawerContent,
  DrawerDescription,
  DrawerHeader,
  DrawerTitle,
  DrawerTrigger,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
  HoverCard,
  HoverCardContent,
  HoverCardTrigger,
  Input,
  InputGroup,
  InputGroupAddon,
  InputGroupInput,
  InputGroupText,
  InputOTP,
  InputOTPGroup,
  InputOTPSeparator,
  InputOTPSlot,
  Item,
  ItemContent,
  ItemDescription,
  ItemMedia,
  ItemTitle,
  Kbd,
  KbdGroup,
  Label,
  Menubar,
  MenubarContent,
  MenubarItem,
  MenubarMenu,
  MenubarSeparator,
  MenubarTrigger,
  NavigationMenu,
  NavigationMenuContent,
  NavigationMenuItem,
  NavigationMenuLink,
  NavigationMenuList,
  NavigationMenuTrigger,
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
  Popover,
  PopoverContent,
  PopoverTrigger,
  Progress,
  RadioGroup,
  RadioGroupItem,
  ResizableHandle,
  ResizablePanel,
  ResizablePanelGroup,
  ScrollArea,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Separator,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarTrigger,
  Skeleton,
  Slider,
  Spinner,
  Switch,
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  Textarea,
  Toggle,
  ToggleGroup,
  ToggleGroupItem,
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
  Toaster,
  AccessDeniedState,
  EmptyState,
  ErrorState,
  ForbiddenState,
  LoadingState,
  MockDisabledState,
  NotFoundState,
  UpgradeRequiredState,
  ThemeProvider,
  useTheme,
} from "@notrelix/ui-web";

const meta: Meta = {
  title: "Foundation/Gallery",
  parameters: {
    layout: "fullscreen",
  },
};

export default meta;
type Story = StoryObj;

function Row({ children }: { children: React.ReactNode }) {
  return (
    <div
      style={{
        display: "flex",
        flexWrap: "wrap",
        gap: "12px",
        alignItems: "center",
        marginBottom: "16px",
      }}
    >
      {children}
    </div>
  );
}

function Section({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <section style={{ marginBottom: "40px" }}>
      <h2 style={{ margin: "0 0 16px", fontSize: "16px", fontWeight: 600 }}>
        {title}
      </h2>
      {children}
    </section>
  );
}

export const Primitives: Story = {
  render: () => (
    <div style={{ padding: "24px" }}>
      <Section title="Button">
        <Row>
          <Button>Default</Button>
          <Button variant="secondary">Secondary</Button>
          <Button variant="outline">Outline</Button>
          <Button variant="destructive">Destructive</Button>
          <Button variant="ghost">Ghost</Button>
          <Button variant="link">Link</Button>
          <Button size="sm">Small</Button>
          <Button size="lg">Large</Button>
          <Button disabled>Disabled</Button>
        </Row>
      </Section>

      <Section title="Button Group">
        <Row>
          <ButtonGroup>
            <Button variant="outline">Undo</Button>
            <ButtonGroupSeparator />
            <Button variant="outline">Redo</Button>
            <ButtonGroupSeparator />
            <Button variant="outline">Reset</Button>
          </ButtonGroup>
        </Row>
      </Section>

      <Section title="Badge">
        <Row>
          <Badge>Default</Badge>
          <Badge variant="secondary">Secondary</Badge>
          <Badge variant="destructive">Destructive</Badge>
          <Badge variant="outline">Outline</Badge>
        </Row>
      </Section>

      <Section title="Alert">
        <Alert>
          <AlertTitle>Heads up</AlertTitle>
          <AlertDescription>
            This is a default alert with a description.
          </AlertDescription>
        </Alert>
        <Alert variant="destructive">
          <AlertTitle>Error</AlertTitle>
          <AlertDescription>
            Something went wrong with destructive styling.
          </AlertDescription>
        </Alert>
      </Section>

      <Section title="Card">
        <Card style={{ width: "320px" }}>
          <CardHeader>
            <CardTitle>Project Alpha</CardTitle>
            <CardDescription>
              Track the alpha release milestones.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <p style={{ margin: 0, fontSize: "14px" }}>
              3 items · 2 in progress · 1 done
            </p>
          </CardContent>
        </Card>
      </Section>

      <Section title="Input & Textarea">
        <Row>
          <Label htmlFor="input-basic">Email</Label>
          <Input
            id="input-basic"
            type="email"
            placeholder="name@example.com"
            style={{ maxWidth: "280px" }}
          />
          <Input
            disabled
            placeholder="Disabled input"
            style={{ maxWidth: "200px" }}
          />
        </Row>
        <Row>
          <Label htmlFor="textarea-basic">Notes</Label>
          <Textarea
            id="textarea-basic"
            placeholder="Write a note…"
            style={{ maxWidth: "360px" }}
          />
        </Row>
      </Section>

      <Section title="Input Group">
        <Row>
          <InputGroup style={{ maxWidth: "320px" }}>
            <InputGroupAddon>@</InputGroupAddon>
            <InputGroupInput placeholder="username" />
          </InputGroup>
        </Row>
        <Row>
          <InputGroup style={{ maxWidth: "320px" }}>
            <InputGroupText>Amount</InputGroupText>
            <InputGroupInput placeholder="0.00" />
          </InputGroup>
        </Row>
      </Section>

      <Section title="Input OTP">
        <Row>
          <Label htmlFor="otp-basic">Verification code</Label>
          <InputOTP id="otp-basic" maxLength={6} aria-label="Verification code">
            <InputOTPGroup>
              <InputOTPSlot index={0} />
              <InputOTPSlot index={1} />
              <InputOTPSlot index={2} />
            </InputOTPGroup>
            <InputOTPSeparator />
            <InputOTPGroup>
              <InputOTPSlot index={3} />
              <InputOTPSlot index={4} />
              <InputOTPSlot index={5} />
            </InputOTPGroup>
          </InputOTP>
        </Row>
      </Section>

      <Section title="Checkbox & Switch">
        <Row>
          <Label
            htmlFor="checkbox-basic"
            style={{ display: "flex", alignItems: "center", gap: "8px" }}
          >
            <Checkbox id="checkbox-basic" defaultChecked /> Accept terms
          </Label>
        </Row>
        <Row>
          <Label
            htmlFor="switch-basic"
            style={{ display: "flex", alignItems: "center", gap: "8px" }}
          >
            <Switch id="switch-basic" defaultChecked /> Notifications
          </Label>
        </Row>
      </Section>

      <Section title="Radio Group">
        <fieldset
          style={{
            border: "none",
            margin: 0,
            padding: 0,
            display: "flex",
            gap: "12px",
          }}
        >
          <legend style={{ marginBottom: "8px" }}>Priority</legend>
          <RadioGroup defaultValue="medium">
            <Row>
              <Label
                htmlFor="radio-low"
                style={{ display: "flex", alignItems: "center", gap: "8px" }}
              >
                <RadioGroupItem value="low" id="radio-low" /> Low
              </Label>
              <Label
                htmlFor="radio-medium"
                style={{ display: "flex", alignItems: "center", gap: "8px" }}
              >
                <RadioGroupItem value="medium" id="radio-medium" /> Medium
              </Label>
              <Label
                htmlFor="radio-high"
                style={{ display: "flex", alignItems: "center", gap: "8px" }}
              >
                <RadioGroupItem value="high" id="radio-high" /> High
              </Label>
            </Row>
          </RadioGroup>
        </fieldset>
      </Section>

      <Section title="Select">
        <Row>
          <Label htmlFor="select-basic">Status</Label>
          <Select defaultValue="todo">
            <SelectTrigger id="select-basic" style={{ minWidth: "200px" }}>
              <SelectValue placeholder="Pick a status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="todo">To do</SelectItem>
              <SelectItem value="in-progress">In progress</SelectItem>
              <SelectItem value="done">Done</SelectItem>
            </SelectContent>
          </Select>
        </Row>
      </Section>

      <Section title="Slider & Progress">
        <Row>
          <Slider
            aria-label="Volume"
            defaultValue={[64]}
            style={{ width: "240px" }}
          />
          <Progress
            aria-label="Upload progress"
            value={64}
            style={{ width: "240px" }}
          />
        </Row>
      </Section>

      <Section title="Tabs">
        <Tabs defaultValue="overview" style={{ width: "420px" }}>
          <TabsList>
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="activity">Activity</TabsTrigger>
            <TabsTrigger value="settings">Settings</TabsTrigger>
          </TabsList>
          <TabsContent value="overview">Overview content.</TabsContent>
          <TabsContent value="activity">Activity content.</TabsContent>
          <TabsContent value="settings">Settings content.</TabsContent>
        </Tabs>
      </Section>

      <Section title="Toggle">
        <Row>
          <Toggle>Bold</Toggle>
          <Toggle defaultPressed>Italic</Toggle>
        </Row>
      </Section>

      <Section title="Toggle Group">
        <Row>
          <ToggleGroup type="multiple" aria-label="Text formatting">
            <ToggleGroupItem value="bold">Bold</ToggleGroupItem>
            <ToggleGroupItem value="italic">Italic</ToggleGroupItem>
            <ToggleGroupItem value="underline">Underline</ToggleGroupItem>
          </ToggleGroup>
        </Row>
      </Section>

      <Section title="Kbd">
        <Row>
          <Kbd>⌘</Kbd>
          <Kbd>K</Kbd>
          <KbdGroup>
            <Kbd>⌘</Kbd>
            <Kbd>Shift</Kbd>
            <Kbd>P</Kbd>
          </KbdGroup>
        </Row>
      </Section>

      <Section title="Accordion">
        <Accordion type="single" collapsible style={{ width: "420px" }}>
          <AccordionItem value="item-1">
            <AccordionTrigger>What is Notrelix?</AccordionTrigger>
            <AccordionContent>A work-management platform.</AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger>Is it free?</AccordionTrigger>
            <AccordionContent>
              Plans are available for every team size.
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      </Section>

      <Section title="Collapsible">
        <Collapsible style={{ width: "420px" }}>
          <CollapsibleTrigger asChild>
            <Button variant="outline" size="sm">
              Toggle details
            </Button>
          </CollapsibleTrigger>
          <CollapsibleContent>
            <p style={{ marginTop: "12px", fontSize: "14px" }}>
              Hidden details revealed by the collapsible.
            </p>
          </CollapsibleContent>
        </Collapsible>
      </Section>

      <Section title="Table">
        <Table style={{ width: "560px" }}>
          <TableCaption>Board inventory</TableCaption>
          <TableHeader>
            <TableRow>
              <TableHead>Board</TableHead>
              <TableHead>Items</TableHead>
              <TableHead>Status</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            <TableRow>
              <TableCell>Product launch</TableCell>
              <TableCell>24</TableCell>
              <TableCell>Active</TableCell>
            </TableRow>
            <TableRow>
              <TableCell>Bug triage</TableCell>
              <TableCell>12</TableCell>
              <TableCell>Active</TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </Section>

      <Section title="Item">
        <Item>
          <ItemMedia>
            <Avatar>
              <AvatarFallback>P</AvatarFallback>
            </Avatar>
          </ItemMedia>
          <ItemContent>
            <ItemTitle>Platform initiative</ItemTitle>
            <ItemDescription>Owned by the platform team.</ItemDescription>
          </ItemContent>
        </Item>
      </Section>

      <Section title="Avatar">
        <Row>
          <Avatar>
            <AvatarFallback>AL</AvatarFallback>
          </Avatar>
          <AvatarGroup>
            <Avatar>
              <AvatarFallback>A</AvatarFallback>
            </Avatar>
            <Avatar>
              <AvatarFallback>B</AvatarFallback>
            </Avatar>
            <Avatar>
              <AvatarFallback>C</AvatarFallback>
            </Avatar>
            <AvatarGroupCount>+3</AvatarGroupCount>
          </AvatarGroup>
        </Row>
      </Section>

      <Section title="Aspect Ratio">
        <div style={{ width: "200px" }}>
          <AspectRatio ratio={16 / 9}>
            <div
              style={{
                width: "100%",
                height: "100%",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                background: "var(--secondary)",
                borderRadius: "8px",
              }}
            >
              16:9
            </div>
          </AspectRatio>
        </div>
      </Section>

      <Section title="Skeleton">
        <Row>
          <div style={{ width: "280px" }}>
            <Skeleton
              style={{ height: "16px", width: "60%", marginBottom: "8px" }}
            />
            <Skeleton
              style={{ height: "12px", width: "90%", marginBottom: "8px" }}
            />
            <Skeleton style={{ height: "12px", width: "75%" }} />
          </div>
        </Row>
      </Section>

      <Section title="Spinner & Separator">
        <Row>
          <Spinner />
          <Separator orientation="vertical" style={{ height: "32px" }} />
          <span style={{ fontSize: "13px" }}>Separated content</span>
        </Row>
      </Section>

      <Section title="Breadcrumb">
        <Breadcrumb>
          <BreadcrumbList>
            <BreadcrumbItem>
              <BreadcrumbLink href="#">Home</BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator />
            <BreadcrumbItem>
              <BreadcrumbLink href="#">Workspaces</BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator />
            <BreadcrumbItem>
              <BreadcrumbPage>Alpha</BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>
      </Section>

      <Section title="Pagination">
        <Pagination>
          <PaginationContent>
            <PaginationItem>
              <PaginationPrevious href="#" />
            </PaginationItem>
            <PaginationItem>
              <PaginationLink href="#">1</PaginationLink>
            </PaginationItem>
            <PaginationItem>
              <PaginationLink href="#" isActive>
                2
              </PaginationLink>
            </PaginationItem>
            <PaginationItem>
              <PaginationEllipsis />
            </PaginationItem>
            <PaginationItem>
              <PaginationNext href="#" />
            </PaginationItem>
          </PaginationContent>
        </Pagination>
      </Section>

      <Section title="Field">
        <FieldGroup style={{ maxWidth: "360px" }}>
          <FieldLabel htmlFor="field-email">Email</FieldLabel>
          <FieldDescription>We will never share your email.</FieldDescription>
          <Input id="field-email" type="email" placeholder="name@example.com" />
          <FieldError>That email is already registered.</FieldError>
        </FieldGroup>
      </Section>

      <Section title="Scroll Area">
        <ScrollArea
          style={{
            height: "160px",
            width: "360px",
            border: "1px solid var(--border)",
            borderRadius: "8px",
            padding: "8px",
          }}
        >
          <div style={{ padding: "4px", fontSize: "14px", lineHeight: "1.6" }}>
            {Array.from({ length: 30 }, (_, i) => (
              <p key={i} style={{ margin: "0 0 8px" }}>
                Scrollable line {i + 1}
              </p>
            ))}
          </div>
        </ScrollArea>
      </Section>

      <Section title="Resizable">
        <ResizablePanelGroup
          direction="horizontal"
          style={{
            width: "480px",
            border: "1px solid var(--border)",
            borderRadius: "8px",
          }}
        >
          <ResizablePanel defaultSize={50}>
            <div style={{ padding: "12px", fontSize: "14px" }}>Panel A</div>
          </ResizablePanel>
          <ResizableHandle />
          <ResizablePanel defaultSize={50}>
            <div style={{ padding: "12px", fontSize: "14px" }}>Panel B</div>
          </ResizablePanel>
        </ResizablePanelGroup>
      </Section>

      <Section title="Carousel">
        <Carousel style={{ width: "420px" }}>
          <CarouselContent>
            {["First", "Second", "Third"].map((label) => (
              <CarouselItem key={label}>
                <div
                  style={{
                    height: "120px",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    background: "var(--secondary)",
                    borderRadius: "8px",
                  }}
                >
                  {label}
                </div>
              </CarouselItem>
            ))}
          </CarouselContent>
          <CarouselPrevious aria-label="Previous slide" />
          <CarouselNext aria-label="Next slide" />
        </Carousel>
      </Section>

      <Section title="Calendar">
        <Calendar mode="single" aria-label="Pick a date" />
      </Section>

      <Section title="Chart">
        <ChartContainer
          config={{
            items: { label: "Items", color: "var(--chart-1, #6161ff)" },
          }}
          style={{ width: "480px", height: "180px" }}
        >
          <ChartStyle id="gallery-chart" config={{}} />
        </ChartContainer>
      </Section>
    </div>
  ),
};

export const Overlays: Story = {
  render: () => (
    <div style={{ padding: "24px" }}>
      <Section title="Dialog, AlertDialog, Sheet, Drawer, Popover">
        <Row>
          <Dialog>
            <DialogTrigger asChild>
              <Button variant="outline">Open dialog</Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>New board</DialogTitle>
                <DialogDescription>
                  Create a board inside this workspace.
                </DialogDescription>
              </DialogHeader>
              <DialogFooter>
                <Button type="button" variant="outline">
                  Cancel
                </Button>
                <Button type="button">Create</Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>

          <AlertDialog>
            <AlertDialogTrigger asChild>
              <Button variant="destructive">Delete board</Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>Are you absolutely sure?</AlertDialogTitle>
                <AlertDialogDescription>
                  This action cannot be undone.
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <AlertDialogAction>Continue</AlertDialogAction>
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
                <SheetDescription>Refine the visible items.</SheetDescription>
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
                <DrawerDescription>
                  Actions available on this item.
                </DrawerDescription>
              </DrawerHeader>
            </DrawerContent>
          </Drawer>

          <Popover>
            <PopoverTrigger asChild>
              <Button variant="outline">Open popover</Button>
            </PopoverTrigger>
            <PopoverContent>Popover content.</PopoverContent>
          </Popover>
        </Row>
      </Section>

      <Section title="Dropdown Menu">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline">Board menu</Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent>
            <DropdownMenuLabel>Board</DropdownMenuLabel>
            <DropdownMenuItem>Rename</DropdownMenuItem>
            <DropdownMenuItem>Duplicate</DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem>Archive</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </Section>

      <Section title="Context Menu">
        <ContextMenu>
          <ContextMenuTrigger asChild>
            <div
              style={{
                width: "240px",
                height: "96px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                border: "1px dashed var(--border)",
                borderRadius: "8px",
                fontSize: "13px",
              }}
            >
              Right-click here
            </div>
          </ContextMenuTrigger>
          <ContextMenuContent>
            <ContextMenuLabel>Item</ContextMenuLabel>
            <ContextMenuItem>Edit</ContextMenuItem>
            <ContextMenuItem>Duplicate</ContextMenuItem>
            <ContextMenuSeparator />
            <ContextMenuItem>
              Delete <ContextMenuShortcut>⌘⌫</ContextMenuShortcut>
            </ContextMenuItem>
          </ContextMenuContent>
        </ContextMenu>
      </Section>

      <Section title="Hover Card">
        <HoverCard>
          <HoverCardTrigger asChild>
            <Button variant="link">hover@notrelix.com</Button>
          </HoverCardTrigger>
          <HoverCardContent>Member since 2024.</HoverCardContent>
        </HoverCard>
      </Section>

      <Section title="Tooltip">
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button variant="outline">Hover me</Button>
            </TooltipTrigger>
            <TooltipContent>Tooltip content</TooltipContent>
          </Tooltip>
        </TooltipProvider>
      </Section>

      <Section title="Menubar">
        <Menubar>
          <MenubarMenu>
            <MenubarTrigger>File</MenubarTrigger>
            <MenubarContent>
              <MenubarItem>New board</MenubarItem>
              <MenubarItem>Import</MenubarItem>
              <MenubarSeparator />
              <MenubarItem>Preferences</MenubarItem>
            </MenubarContent>
          </MenubarMenu>
        </Menubar>
      </Section>

      <Section title="Navigation Menu">
        <NavigationMenu>
          <NavigationMenuList>
            <NavigationMenuItem>
              <NavigationMenuTrigger>Products</NavigationMenuTrigger>
              <NavigationMenuContent>
                <NavigationMenuLink href="#">Boards</NavigationMenuLink>
              </NavigationMenuContent>
            </NavigationMenuItem>
          </NavigationMenuList>
        </NavigationMenu>
      </Section>

      <Section title="Command">
        <div style={{ width: "360px" }}>
          <Command>
            <CommandInput
              placeholder="Search commands…"
              aria-label="Search commands"
            />
            <CommandList>
              <CommandEmpty>No results found.</CommandEmpty>
              <CommandGroup heading="Suggestions">
                <CommandItem>New board</CommandItem>
                <CommandItem>Invite member</CommandItem>
              </CommandGroup>
              <CommandSeparator />
              <CommandGroup heading="Settings">
                <CommandItem>Profile</CommandItem>
              </CommandGroup>
            </CommandList>
          </Command>
        </div>
      </Section>

      <Section title="Sidebar">
        <div style={{ width: "100%", maxWidth: "720px", height: "320px" }}>
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
                      <SidebarMenuItem>
                        <SidebarMenuButton>Documents</SidebarMenuButton>
                      </SidebarMenuItem>
                      <SidebarMenuItem>
                        <SidebarMenuButton>Automations</SidebarMenuButton>
                      </SidebarMenuItem>
                    </SidebarMenu>
                  </SidebarGroupContent>
                </SidebarGroup>
              </SidebarContent>
              <SidebarFooter />
            </Sidebar>
            <div
              style={{
                flex: 1,
                display: "flex",
                alignItems: "flex-start",
                padding: "12px",
              }}
            >
              <SidebarTrigger />
              <span style={{ marginLeft: "8px", fontSize: "14px" }}>
                Main content
              </span>
            </div>
          </SidebarProvider>
        </div>
      </Section>

      <Section title="Sonner Toaster">
        <Toaster />
        <span style={{ fontSize: "13px", color: "var(--muted-foreground)" }}>
          Toaster mounted; toasts are rendered on demand at runtime.
        </span>
      </Section>
    </div>
  ),
};

export const FeedbackStates: Story = {
  render: () => {
    function ThemedContent() {
      const { theme } = useTheme();
      return (
        <div style={{ padding: "24px" }}>
          <Section title="Access Denied">
            <AccessDeniedState />
          </Section>
          <Section title="Empty State">
            <EmptyState
              title="No boards yet"
              description="Create your first board to get started."
            />
          </Section>
          <Section title="Error State">
            <ErrorState error={new Error("Something went wrong")} />
          </Section>
          <Section title="Forbidden State">
            <ForbiddenState />
          </Section>
          <Section title="Loading State">
            <LoadingState />
          </Section>
          <Section title="Not Found State">
            <NotFoundState />
          </Section>
          <Section title="Upgrade Required">
            <UpgradeRequiredState />
          </Section>
          <Section title="Mock Disabled">
            <MockDisabledState featureName="Boards" />
          </Section>
          <p style={{ fontSize: "12px", color: "var(--muted-foreground)" }}>
            Current theme: {theme}
          </p>
        </div>
      );
    }
    return (
      <ThemeProvider defaultTheme="light" storageKey="notrelix-gallery-theme">
        <ThemedContent />
      </ThemeProvider>
    );
  },
};
