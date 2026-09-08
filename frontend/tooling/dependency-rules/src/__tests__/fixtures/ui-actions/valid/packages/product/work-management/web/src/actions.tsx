import { Button } from "@notrelix/ui-web";

export function ValidActions() {
  return (
    <div>
      <Button onClick={() => undefined}>Callback</Button>
      <Button disabled>Disabled</Button>
      <Button type="submit">Submit</Button>
      <Button asChild>
        <span>Link</span>
      </Button>
      <button onClick={() => undefined}>Native callback</button>
      <DropdownTrigger asChild>
        <Button>Trigger child</Button>
      </DropdownTrigger>
    </div>
  );
}

function DropdownTrigger({
  asChild,
  children,
}: {
  asChild: boolean;
  children: React.ReactNode;
}) {
  return <div>{asChild ? children : null}</div>;
}
