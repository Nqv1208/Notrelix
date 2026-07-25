import type { QueryClient, QueryKey } from '@tanstack/react-query';

export interface OptimisticCommandOptions<TData, TVariables> {
  queryClient: QueryClient;
  queryKey: QueryKey;
  updateFn: (oldData: TData | undefined, variables: TVariables) => TData;
  mutationFn: (variables: TVariables) => Promise<unknown>;
  variables: TVariables;
}

export async function executeOptimisticCommand<TData, TVariables>({
  queryClient,
  queryKey,
  updateFn,
  mutationFn,
  variables,
}: OptimisticCommandOptions<TData, TVariables>) {
  await queryClient.cancelQueries({ queryKey });

  const previousData = queryClient.getQueryData<TData>(queryKey);

  queryClient.setQueryData<TData>(queryKey, (old) => updateFn(old, variables));

  try {
    const result = await mutationFn(variables);
    return result;
  } catch (error) {
    if (previousData !== undefined) {
      queryClient.setQueryData<TData>(queryKey, previousData);
    }
    throw error;
  } finally {
    queryClient.invalidateQueries({ queryKey });
  }
}
