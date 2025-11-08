import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { useState, useMemo } from "react";
import DataTable, { type TableColumn } from "react-data-table-component";
import { getFiles, type ProviderFileItem } from "../api/files";
import { PROVIDER_META } from "../constants/providers";
import type { ProviderType } from "../api/types";
import {
  Button,
  Card,
  PageContainer,
  PageHeader,
  PageSection,
} from "../components/ui";

export const FilesPage = () => {
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedProviders, setSelectedProviders] = useState<Set<ProviderType>>(
    new Set(["Google", "Microsoft"])
  );
  const [showFilterMenu, setShowFilterMenu] = useState(false);

  const filesQuery = useQuery({
    queryKey: ["files"],
    queryFn: () => getFiles(),
    retry: 1,
  });

  // Get files data (even if loading/error, to avoid hooks order issues)
  const files = filesQuery.data || [];

  // Filter and search logic - must be called before any returns
  const filteredFiles = useMemo(() => {
    return files.filter((file) => {
      // Filter by selected providers
      if (!selectedProviders.has(file.provider as ProviderType)) {
        return false;
      }
      // Filter by search term
      if (
        searchTerm &&
        !file.name.toLowerCase().includes(searchTerm.toLowerCase())
      ) {
        return false;
      }
      return true;
    });
  }, [files, selectedProviders, searchTerm]);

  const toggleProvider = (provider: ProviderType) => {
    setSelectedProviders((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(provider)) {
        newSet.delete(provider);
      } else {
        newSet.add(provider);
      }
      return newSet;
    });
  };

  const formatFileSize = (bytes: number | null) => {
    if (bytes === null) return "Unknown";
    if (bytes === 0) return "0 Bytes";
    const k = 1024;
    const sizes = ["Bytes", "KB", "MB", "GB", "TB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + " " + sizes[i];
  };

  // Define columns for the data table
  const columns: TableColumn<ProviderFileItem>[] = [
    {
      name: "Name",
      selector: (row) => row.name,
      sortable: true,
      minWidth: "400px",
      cell: (row) => {
        const isFolder = row.mimeType?.includes("folder");
        return (
          <div className="flex items-center">
            <div className="flex-shrink-0 h-6 w-6 mr-2">
              {isFolder ? (
                <svg
                  className="h-6 w-6 text-blue-500"
                  fill="currentColor"
                  viewBox="0 0 20 20"
                >
                  <path d="M2 6a2 2 0 012-2h5l2 2h5a2 2 0 012 2v6a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" />
                </svg>
              ) : (
                <svg
                  className="h-6 w-6 text-gray-400"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={1.5}
                    d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z"
                  />
                </svg>
              )}
            </div>
            <span className="text-sm font-medium text-gray-900">
              {row.name}
            </span>
          </div>
        );
      },
    },
    {
      name: "Provider",
      selector: (row) => row.provider,
      sortable: true,
      minWidth: "200px",
      cell: (row) => {
        const provider = PROVIDER_META[row.provider as ProviderType];
        return (
          <span
            className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium whitespace-nowrap"
            style={{
              backgroundColor: provider.color + "20",
              color: provider.color,
            }}
          >
            {provider.name}
          </span>
        );
      },
    },
    {
      name: "Type",
      selector: (row) => row.mimeType || "File",
      sortable: true,
      minWidth: "200px",
      cell: (row) => {
        const isFolder = row.mimeType?.includes("folder");
        let displayType = "File";

        if (isFolder) {
          displayType = "Folder";
        } else if (row.mimeType) {
          // Extract the last part after the last slash or dot
          const parts = row.mimeType.split(/[\/\.]/);
          displayType = parts[parts.length - 1] || row.mimeType;
        }

        return (
          <span className="text-sm text-gray-900 capitalize">
            {displayType}
          </span>
        );
      },
    },
    {
      name: "Size",
      selector: (row) => row.sizeBytes || 0,
      sortable: true,
      minWidth: "200px",
      cell: (row) => (
        <span className="text-sm text-gray-900">
          {formatFileSize(row.sizeBytes)}
        </span>
      ),
    },
    {
      name: "Modified",
      selector: (row) => row.modifiedUtc,
      sortable: true,
      minWidth: "200px",
      cell: (row) => (
        <div className="whitespace-nowrap">
          <div className="text-sm text-gray-900">
            {new Date(row.modifiedUtc).toLocaleDateString()}
          </div>
          <div className="text-xs text-gray-500">
            {new Date(row.modifiedUtc).toLocaleTimeString()}
          </div>
        </div>
      ),
    },
    {
      name: "Actions",
      button: true,
      minWidth: "200px",
      cell: (row) => (
        <button
          onClick={() =>
            window.open(`/api/files/${row.provider}/${row.id}/view`, "_blank")
          }
          className="text-blue-600 hover:text-blue-900 inline-flex items-center text-sm font-medium whitespace-nowrap"
          title="Open in Provider"
        >
          <svg
            className="w-4 h-4 mr-1"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"
            />
          </svg>
          Open
        </button>
      ),
    },
  ];

  // Now handle loading/error states after all hooks are called
  if (filesQuery.isLoading) {
    return (
      <PageContainer>
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
            <p className="text-gray-600">Loading files...</p>
          </div>
        </div>
      </PageContainer>
    );
  }

  if (filesQuery.isError) {
    return (
      <PageContainer>
        <PageSection>
          <PageHeader
            title="My Files"
            description="Browse files from all your connected cloud storage providers"
          />
          <Card className="p-8 text-center">
            <svg
              className="w-16 h-16 text-red-500 mx-auto mb-4"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">
              Error Loading Files
            </h2>
            <p className="text-gray-600 mb-6">
              {filesQuery.error instanceof Error
                ? filesQuery.error.message
                : "An unexpected error occurred"}
            </p>
            <div className="flex gap-3 justify-center">
              <Button variant="primary" onClick={() => filesQuery.refetch()}>
                Try Again
              </Button>
              <Link to="/connections">
                <Button variant="secondary">Back to Connections</Button>
              </Link>
            </div>
          </Card>
        </PageSection>
      </PageContainer>
    );
  }

  // Empty state check
  if (files.length === 0) {
    return (
      <PageContainer>
        <PageSection>
          <PageHeader
            title="My Files"
            description="Browse files from all your connected cloud storage providers"
          />
          <Card className="p-12 text-center">
            <svg
              className="w-24 h-24 text-gray-400 mx-auto mb-6"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={1.5}
                d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z"
              />
            </svg>
            <h2 className="text-2xl font-semibold text-gray-900 mb-3">
              No Files Found
            </h2>
            <p className="text-gray-600 mb-6">
              You don't have any files in your connected cloud storage, or you
              haven't linked any accounts yet.
            </p>
            <Link to="/connections">
              <Button variant="primary" size="lg">
                Manage Connections
              </Button>
            </Link>
          </Card>
        </PageSection>
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <PageSection>
        <PageHeader
          title="My Files"
          description={`Showing ${filteredFiles.length} of ${files.length} ${files.length === 1 ? "file" : "files"}`}
          action={
            <Button
              variant="secondary"
              onClick={() => filesQuery.refetch()}
              isLoading={filesQuery.isFetching}
            >
              <svg
                className={`w-5 h-5 mr-2 ${filesQuery.isFetching ? "animate-spin" : ""}`}
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
                />
              </svg>
              Refresh
            </Button>
          }
        />
        {/* Search and Filter Bar */}
        <div className="flex gap-4">
          {/* Search Bar */}
          <div className="flex-1 relative">
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <svg
                className="h-5 w-5 text-gray-400"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                />
              </svg>
            </div>
            <input
              type="text"
              placeholder="Search files by name..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="block w-full pl-10 pr-3 py-2 border border-gray-300 rounded-lg focus:ring-blue-500 focus:border-blue-500"
            />
            {searchTerm && (
              <button
                onClick={() => setSearchTerm("")}
                className="absolute inset-y-0 right-0 pr-3 flex items-center"
              >
                <svg
                  className="h-5 w-5 text-gray-400 hover:text-gray-600"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            )}
          </div>
          {/* Provider Filter */}
          <div className="relative">
            <button
              onClick={() => setShowFilterMenu(!showFilterMenu)}
              className="px-4 py-2 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition flex items-center gap-2"
            >
              <svg
                className="w-5 h-5"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z"
                />
              </svg>
              Filter Providers
              {selectedProviders.size < 2 && (
                <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                  {selectedProviders.size}
                </span>
              )}
            </button>
            {showFilterMenu && (
              <div className="absolute right-0 mt-2 w-64 bg-white rounded-lg shadow-lg border border-gray-200 z-10">
                <div className="p-4">
                  <h3 className="text-sm font-semibold text-gray-900 mb-3">
                    Filter by Provider
                  </h3>
                  <div className="space-y-2">
                    {(["Google", "Microsoft"] as ProviderType[]).map(
                      (provider) => {
                        const meta = PROVIDER_META[provider];
                        return (
                          <label
                            key={provider}
                            className="flex items-center cursor-pointer hover:bg-gray-50 p-2 rounded"
                          >
                            <input
                              type="checkbox"
                              checked={selectedProviders.has(provider)}
                              onChange={() => toggleProvider(provider)}
                              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                            />
                            <span className="ml-3 text-sm text-gray-900 flex items-center gap-2">
                              {meta.name}
                            </span>
                          </label>
                        );
                      }
                    )}
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
        <Card className="overflow-hidden">
          <DataTable
            columns={columns}
            data={filteredFiles}
            pagination
            paginationPerPage={50}
            paginationRowsPerPageOptions={[20, 50, 100, 200]}
            highlightOnHover
            striped
            defaultSortFieldId={4}
            defaultSortAsc={false}
            fixedHeader
            fixedHeaderScrollHeight="60vh"
            noDataComponent={
              <div className="text-center py-8 text-gray-500">
                No files found matching your filters
              </div>
            }
            customStyles={{
              table: {
                style: {
                  fontSize: "14px",
                },
              },
              headCells: {
                style: {
                  fontSize: "12px",
                  fontWeight: "600",
                  textTransform: "uppercase",
                  paddingLeft: "16px",
                  paddingRight: "16px",
                },
              },
              cells: {
                style: {
                  paddingLeft: "16px",
                  paddingRight: "16px",
                },
              },
            }}
          />
        </Card>
      </PageSection>
    </PageContainer>
  );
};
