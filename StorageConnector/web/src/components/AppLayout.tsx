import { Outlet } from "react-router-dom";
import { TopNav } from "./TopNav";

export const AppLayout = () => (
  <div className="min-h-screen bg-slate-50 text-slate-900">
    <TopNav />
    <main className="mx-auto px-4 sm:px-6 lg:px-8 pb-16 pt-10">
      <Outlet />
    </main>
  </div>
);
