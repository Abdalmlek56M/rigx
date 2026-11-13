import { NextResponse, type NextRequest } from "next/server";

export function proxy(request: NextRequest) {
  const developmentSession = request.cookies.get("HardwareStore.Dev.Session");
  const productionSession = request.cookies.get("__Host-HardwareStore.Session");

  if (!developmentSession && !productionSession) {
    const loginUrl = request.nextUrl.clone();
    loginUrl.pathname = "/login";
    loginUrl.search = "";
    loginUrl.searchParams.set("returnTo", request.nextUrl.pathname);
    return NextResponse.redirect(loginUrl);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/account/:path*", "/admin", "/admin/:path*"],
};
