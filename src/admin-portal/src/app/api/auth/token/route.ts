import { NextRequest, NextResponse } from "next/server";

const API_BASE_URL = process.env.BACKEND_API_URL || "https://localhost:44305";
const CLIENT_ID = process.env.OIDC_CLIENT_ID || "KLC_Api";
function getClientSecret(): string {
  if (process.env.NODE_ENV === "production") {
    const secret = process.env.OIDC_CLIENT_SECRET;
    if (!secret) {
      throw new Error("OIDC_CLIENT_SECRET is required in production");
    }
    return secret;
  }
  return process.env.OIDC_CLIENT_SECRET || "1q2w3e*";
}

export async function POST(request: NextRequest) {
  const clientSecret = getClientSecret();
  const body = await request.json();
  const { username, password } = body;

  if (!username || !password) {
    return NextResponse.json(
      { error: "Username and password are required" },
      { status: 400 }
    );
  }

  const response = await fetch(`${API_BASE_URL}/connect/token`, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({
      grant_type: "password",
      username,
      password,
      client_id: CLIENT_ID,
      client_secret: clientSecret,
      scope: "KLC",
    }),
  });

  const data = await response.json();

  if (!response.ok) {
    return NextResponse.json(data, { status: response.status });
  }

  return NextResponse.json(data);
}
