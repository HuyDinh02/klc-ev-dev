"use client";

import { useState, useMemo, useCallback } from "react";
import { useQuery } from "@tanstack/react-query";

/** Shape returned by cursor-based list APIs. */
interface CursorPagedResult<T> {
  items: T[];
  totalCount?: number;
}

/** Options accepted by useTableQuery. */
interface UseTableQueryOptions<T> {
  /** Primary query key (e.g. "sessions", "faults"). Additional keys are appended automatically. */
  queryKey: string;
  /** Async function that receives merged params and returns paged data. */
  fetchFn: (params: Record<string, unknown>) => Promise<CursorPagedResult<T>>;
  /** Number of items per page. @default 20 */
  pageSize?: number;
  /** TanStack Query refetchInterval in ms. Pass `false` to disable. @default false */
  refetchInterval?: number | false;
  /**
   * Fields to match against the search string for client-side filtering.
   * Each entry should be a property name on the item whose value is a string.
   * Uses `string` rather than `keyof T` to avoid inference issues when T
   * is wider than the constraint (e.g. API response types).
   * @default []
   */
  searchFields?: string[];
  /**
   * Extra static params merged into every request (e.g. `{ sortBy: "name" }`).
   * These are **not** included in the query key automatically -- pass
   * `extraQueryKeys` if they should trigger a refetch.
   */
  extraParams?: Record<string, unknown>;
  /**
   * Additional values appended to the TanStack Query key array so that
   * changes to external state (e.g. sort order) trigger a refetch.
   */
  extraQueryKeys?: unknown[];
}

/** Return value of useTableQuery. */
interface UseTableQueryReturn<T> {
  /* ----- raw data ----- */
  /** Full response from the API (items + totalCount). */
  data: CursorPagedResult<T> | undefined;
  /** Convenience: `data.items ?? []`. */
  items: T[];
  /** Items after client-side search filtering. */
  filteredItems: T[];
  /** Total count as reported by the API. */
  totalCount: number;
  isLoading: boolean;

  /* ----- search ----- */
  search: string;
  setSearch: (value: string) => void;

  /* ----- status filter ----- */
  statusFilter: string;
  setStatusFilter: (value: string) => void;

  /* ----- pagination ----- */
  cursor: string | null;
  pageSize: number;
  goNextPage: () => void;
  goPrevPage: () => void;
  hasNextPage: boolean;
  hasPrevPage: boolean;
  /** Reset cursor back to the first page and clear the cursor stack. */
  resetPage: () => void;
  /** Combined setter: update status filter and reset pagination in one call. */
  setStatusFilterAndReset: (value: string) => void;
}

export function useTableQuery<T extends { id: string }>(
  options: UseTableQueryOptions<T>,
): UseTableQueryReturn<T> {
  const {
    queryKey,
    fetchFn,
    pageSize = 20,
    refetchInterval = false,
    searchFields = [],
    extraParams = {},
    extraQueryKeys = [],
  } = options;

  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [cursor, setCursor] = useState<string | null>(null);
  const [cursorStack, setCursorStack] = useState<(string | null)[]>([]);

  const resetPage = useCallback(() => {
    setCursor(null);
    setCursorStack([]);
  }, []);

  const setStatusFilterAndReset = useCallback((value: string) => {
    setStatusFilter(value);
    setCursor(null);
    setCursorStack([]);
  }, []);

  const { data, isLoading } = useQuery({
    queryKey: [queryKey, statusFilter, cursor, ...extraQueryKeys],
    queryFn: async () => {
      const params: Record<string, unknown> = {
        maxResultCount: pageSize,
        ...extraParams,
      };
      if (statusFilter !== "all") params.status = Number(statusFilter);
      if (cursor) params.cursor = cursor;
      return fetchFn(params);
    },
    refetchInterval: refetchInterval || undefined,
  });

  const items: T[] = data?.items ?? [];
  const totalCount = data?.totalCount ?? items.length;

  // Client-side search filtering
  const filteredItems = useMemo(() => {
    if (!search || searchFields.length === 0) return items;
    const s = search.toLowerCase();
    return items.filter((item) =>
      searchFields.some((field) => {
        const value = (item as Record<string, unknown>)[field];
        return typeof value === "string" && value.toLowerCase().includes(s);
      }),
    );
  }, [items, search, searchFields]);

  const hasNextPage = items.length === pageSize;
  const hasPrevPage = cursorStack.length > 0;

  const goNextPage = useCallback(() => {
    const lastId = items[items.length - 1]?.id;
    if (lastId) {
      setCursorStack((prev) => [...prev, cursor]);
      setCursor(lastId);
    }
  }, [items, cursor]);

  const goPrevPage = useCallback(() => {
    setCursorStack((prev) => {
      const next = [...prev];
      const prevCursor = next.pop() ?? null;
      setCursor(prevCursor);
      return next;
    });
  }, []);

  return {
    data,
    items,
    filteredItems,
    totalCount,
    isLoading,

    search,
    setSearch,

    statusFilter,
    setStatusFilter,

    cursor,
    pageSize,
    goNextPage,
    goPrevPage,
    hasNextPage,
    hasPrevPage,
    resetPage,
    setStatusFilterAndReset,
  };
}
