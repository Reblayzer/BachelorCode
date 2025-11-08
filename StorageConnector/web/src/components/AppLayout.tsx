import { Outlet } from "react-router-dom";
import { TopNav } from "./TopNav";
import { SessionTimeoutMonitor } from "./SessionTimeoutMonitor";

export const AppLayout = () => (
  <div className="min-h-screen bg-slate-50 text-slate-900">
    <TopNav />
    <SessionTimeoutMonitor />
    <main className="mx-auto px-4 sm:px-6 lg:px-8 pb-16 pt-10">
      <Outlet />
    </main>
  </div>
);
