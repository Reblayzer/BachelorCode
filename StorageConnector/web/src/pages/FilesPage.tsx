import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import DataTable, { type TableColumn } from "react-data-table-component";
import { getFiles, type ProviderFileItem } from "../api/files";
import { PROVIDER_META } from "../constants/providers";
import type { ProviderType } from "../api/types";

export const FilesPage = () => {
  
  const filesQuery = useQuery({
    queryKey: ["files"],
    queryFn: () => getFiles(),
    retry: 1,
  });

  if (filesQuery.isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-600">Loading files...</p>
        </div>
      </div>
    );
  }

  if (filesQuery.isError) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="bg-white p-8 rounded-lg shadow-md max-w-md w-full">
          <div className="text-center">
            <svg className="w-16 h-16 text-red-500 mx-auto mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Error Loading Files</h2>
            <p className="text-gray-600 mb-6">
              {filesQuery.error instanceof Error
                ? filesQuery.error.message
                : "An unexpected error occurred"}
            </p>
            <div className="flex gap-3">
              <button
                onClick={() => filesQuery.refetch()}
                className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition"
              >
                Try Again
              </button>
              <Link
                to="/connections"
                className="flex-1 px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 transition text-center"
              >
                Back to Connections
              </Link>
            </div>
          </div>
        </div>
      </div>
    );
  }

  const files = filesQuery.data || [];

  // Define columns for the data table
  const columns: TableColumn<ProviderFileItem>[] = [
    {
      name: "Name",
      selector: (row) => row.name,
      sortable: true,
      minWidth: "350px",
      cell: (row) => {
        const isFolder = row.mimeType?.includes("folder");
        return (
          <div className="flex items-center">
            <div className="flex-shrink-0 h-6 w-6 mr-2">
              {isFolder ? (
                <svg className="h-6 w-6 text-blue-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M2 6a2 2 0 012-2h5l2 2h5a2 2 0 012 2v6a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" />
                </svg>
              ) : (
                <svg className="h-6 w-6 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                </svg>
              )}
            </div>
            <span className="text-sm font-medium text-gray-900">{row.name}</span>
          </div>
        );
      },
    },
    {
      name: "Provider",
      selector: (row) => row.provider,
      sortable: true,
      width: "200px",
      cell: (row) => {
        const provider = PROVIDER_META[row.provider as ProviderType];
        return (
          <span
            className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium whitespace-nowrap"
            style={{ backgroundColor: provider.color + "20", color: provider.color }}
          >
            {provider.icon} {provider.name}
          </span>
        );
      },
    },
    {
      name: "Type",
      selector: (row) => row.mimeType || "File",
      sortable: true,
      minWidth: "600px",
      cell: (row) => {
        const isFolder = row.mimeType?.includes("folder");
        return (
          <span className="text-sm text-gray-900">
            {isFolder ? "Folder" : row.mimeType || "File"}
          </span>
        );
      },
    },
    {
      name: "Modified",
      selector: (row) => row.modifiedUtc,
      sortable: true,
      width: "180px",
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
      width: "120px",
      cell: (row) => (
        <button
          onClick={() => window.open(`/api/files/${row.provider}/${row.id}/view`, '_blank')}
          className="text-blue-600 hover:text-blue-900 inline-flex items-center text-sm font-medium whitespace-nowrap"
        >
          <svg className="w-4 h-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
          </svg>
          Open
        </button>
      ),
    },
  ];

  if (files.length === 0) {
    return (
      <div className="mx-auto" style={{ maxWidth: '1600px' }}>
        <div className="mb-8">
            <Link
              to="/connections"
              className="inline-flex items-center text-blue-600 hover:text-blue-700 mb-4"
            >
              <svg className="w-5 h-5 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
              </svg>
              Back to Connections
            </Link>
            <h1 className="text-4xl font-bold text-gray-900">My Files</h1>
            <p className="text-gray-600 mt-2">Browse files from all your connected cloud storage providers</p>
          </div>

          <div className="bg-white rounded-lg shadow-md p-12 text-center">
            <svg className="w-24 h-24 text-gray-400 mx-auto mb-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" />
            </svg>
            <h2 className="text-2xl font-semibold text-gray-900 mb-3">No Files Found</h2>
            <p className="text-gray-600 mb-6">
              You don't have any files in your connected cloud storage, or you haven't linked any accounts yet.
            </p>
            <Link
              to="/connections"
              className="inline-flex items-center px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition"
            >
              Manage Connections
            </Link>
          </div>
      </div>
    );
  }

  return (
    <div className="mx-auto" style={{ maxWidth: '1600px' }}>
      <div className="mb-8">
          <Link
            to="/connections"
            className="inline-flex items-center text-blue-600 hover:text-blue-700 mb-4"
          >
            <svg className="w-5 h-5 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            Back to Connections
          </Link>
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-4xl font-bold text-gray-900">My Files</h1>
              <p className="text-gray-600 mt-2">
                Showing {files.length} {files.length === 1 ? "file" : "files"} from all connected providers
              </p>
            </div>
            <button
              onClick={() => filesQuery.refetch()}
              className="px-4 py-2 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition flex items-center gap-2"
              title="Refresh files"
            >
              <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
              Refresh
            </button>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow overflow-hidden">
          <DataTable
            columns={columns}
            data={files}
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
                No files found
              </div>
            }
            customStyles={{
              table: {
                style: {
                  width: '100%',
                },
              },
              rows: {
                style: {
                  minHeight: "60px",
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
      </div>
    </div>
  );
};
