import { Outlet } from "react-router-dom";
import { TopNav } from "./TopNav";

export const AppLayout = () => (
  <div className="min-h-screen bg-slate-50 text-slate-900">
    <TopNav />
    <main className="mx-auto w-full max-w-5xl px-4 pb-16 pt-10">
      <Outlet />
    </main>
  </div>
);
